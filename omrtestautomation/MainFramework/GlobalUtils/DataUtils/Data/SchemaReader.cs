using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace Sailfish.RealignmentEngine.Data
{
	public class SchemaReader
    {

        private enum ReaderMode { Table, TableDefinition };

        const string Identifier = @"(""[^""]*""|\w+)";
        readonly Regex regexTable = new Regex($@"^create (|or replace )table {Identifier}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public DataSet GetSnowflakeDataSet(IDbAccess db, string[] schemas)
		{
            Dictionary<string, DataTable> tables = new Dictionary<string, DataTable>();

            Dictionary<string,string> ddl_statements = new Dictionary<string, string>();

            //Resolve tables for all schemas
            foreach (var schema in schemas)
            {
                var ddl = db.ExecuteScalar<string>($"select get_ddl('schema', '{schema}')");
                ddl_statements[schema] = ddl;
                foreach (var item in ResolveMainSchema(ddl))
                    tables.Add(item.Key, item.Value);
            }

            //Fill in foreign key constraints
            foreach (var schema in schemas)
            {
                var ddl = ddl_statements[schema];
                tables = ResolveForeignKeys(ddl, tables); //Fills in constraint values
            }

            //Add collection to data set
            var fullDataSet = new DataSet();
            foreach(var table in tables.Values)
                fullDataSet.Tables.Add(table);

            return fullDataSet;
        }

		public Dictionary<string, DataTable> ResolveMainSchema(string ddl)
		{
			string schema = null;
            var mode = ReaderMode.Table;

            //List<DataTable> tables = new List<DataTable>();
            Dictionary<string, DataTable> tables = new Dictionary<string, DataTable>();

            var regexSchema = new Regex($@"^create (|or replace )schema {Identifier}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			var regexColumn = new Regex(@"([""\w\-""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var regexPk = new Regex($@"(constraint {Identifier} |)primary key [(]([^)]*)[)]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		
            string currentTableName = "";
            ddl = ddl.Replace("\r", ""); //Remove carriage returns 
            foreach (var line in ddl.Split("\n"))
			{
				Match m;

				switch (mode)
				{
					case ReaderMode.Table: // Global
						m = regexSchema.Match(line);
						if (m.Success)
						{
							schema = m.Groups[2].Value.Trim('"');
							continue;
						}

						m = regexTable.Match(line);
						if (m.Success)
                        { 
                            currentTableName = m.Groups[2].Value.Trim('"');
                            var table = new DataTable(currentTableName) {Namespace = schema};
                            tables.Add(currentTableName, table);

                            mode = ReaderMode.TableDefinition;
                        }
						break;

					case ReaderMode.TableDefinition: // Table definition
						if (line.EndsWith(';'))
						{
							mode = ReaderMode.Table;
							continue;
						}

                        //All columns
                        m = regexColumn.Match(line);
                        if (m.Success && !line.Contains("constraint") && !line.Contains("primary key"))
                        {
                            var columnName = m.Groups[1].Value.Trim('"');
                            tables[currentTableName].Columns.Add(columnName);
                            continue;
                        }

                        //Primary key
                        m = regexPk.Match(line);
						if (m.Success)
						{
							//var constraintName = m.Groups[2].Value;
                            var primaryKeyName = m.Groups[3].Value.Trim('"');

                            tables[currentTableName].PrimaryKey = new [] { tables[currentTableName].Columns[primaryKeyName] };
						}

                        break;
                }
			}

            return tables;
        }

        private Dictionary<string, DataTable> ResolveForeignKeys(string ddl, Dictionary<string,DataTable> tables)
        {
            var regexFk = new Regex($@"(constraint {Identifier} |)foreign key [(]([^)]*)[)] references ({Identifier}[.]({Identifier}[.]|)|){Identifier}\s*([(]([^)]*)[)]|)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            //Now that our columns and primary keys are enabled, lets run through the tables again to resolve the foreign keys 
            var mode = ReaderMode.Table;
            string currentTableName = "";
            foreach (var line in ddl.Split('\n'))
            {
                switch (mode)
                {
                    case ReaderMode.Table: // Global
                        var m = regexTable.Match(line);
                        if (m.Success)
                        {
                            currentTableName = m.Groups[2].Value.Trim('"');
                            mode = ReaderMode.TableDefinition;
                        }

                        break;

                    case ReaderMode.TableDefinition:

                        if (line.EndsWith(';'))
                        {
                            mode = ReaderMode.Table;
                            continue;
                        }

                        m = regexFk.Match(line);
                        if (!m.Success) continue;
                        var constraintName = m.Groups[2].Value;

                        //Get parent table foreign key
                        var parentTableName = m.Groups[8].Value.Trim('"');
                        var parentColumnName = m.Groups[10].Value.Trim('"');
                        var parentColumn = tables[parentTableName].Columns[parentColumnName];

                        //Get child table foreign key 
                        var childColumnName = m.Groups[3].Value.Trim('"');
                        var childColumn = tables[currentTableName].Columns[childColumnName];

                        var foreignKeyConstraint = new ForeignKeyConstraint(constraintName, parentColumn, childColumn);
                        tables[currentTableName].Constraints.Add(foreignKeyConstraint);
                        break;
                }
            }

            return tables;
        }
    }
}