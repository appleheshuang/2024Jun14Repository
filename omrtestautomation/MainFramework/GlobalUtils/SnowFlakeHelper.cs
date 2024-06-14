using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Data;
using Snowflake.Data.Client;
using Newtonsoft.Json.Linq;
using System.Collections;
using CsvHelper;
using Newtonsoft.Json;

namespace MainFramework.GlobalUtils
{
    public class SnowFlakeHelper
    {

        public IDataReader CONN_EXEC_ToSnowflake(string connectionString, string sqlQuery){
            IDbConnection conn = new SnowflakeDbConnection();
            conn.ConnectionString = connectionString;
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sqlQuery;
            IDataReader reader = cmd.ExecuteReader();
            conn.Close();
            return reader;
        }

        public Dictionary<string, object> Serialize(IDataReader reader, string UniqKey)
        {
            //string UniqKey = "SOMRegionId";
            var results = new Dictionary<string, object>();
            var cols = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
                cols.Add(reader.GetName(i));
            while (reader.Read())
            {

                Dictionary<string, object> row = SerializeRow(cols, reader);                
                string keyofRow=row[UniqKey].ToString();
                results.Add(keyofRow, row);
                //results.Add(SerializeRow(cols, reader));
            }
            return results;
        }

        public Dictionary<string, object> SerializeRow(IEnumerable<string> cols,IDataReader reader)
        {
            var result = new Dictionary<string, object>();
            foreach (var col in cols)
            {
                if (
                    col == "EffectiveDate" || 
                    col == "EndDate" || 
                    col == "LicenseExpirationDate"
                    )
                {
                    string strDate = reader[col].ToString();
                    var dtDate = Convert.ToDateTime(strDate);
                    strDate = dtDate.Year + "-" + (dtDate.Month < 10 ? "0"+ dtDate.Month.ToString() : dtDate.Month.ToString()) + "-" + (dtDate.Day < 10 ? "0" + dtDate.Day.ToString() : dtDate.Day.ToString());
                    result.Add(col, strDate);
                }else if (
                    col == "CreatedDate" || 
                    col == "LastModifiedDate" || 
                    col == "ProcessingStartTime" || 
                    col == "ProcessingEndTime"                    
                    )
                {
                    string strDate = reader[col].ToString();
                    var dtDate = Convert.ToDateTime(strDate);
                    strDate = dtDate.ToString("MM/dd/yyyy HH:mm:ss");
                    result.Add(col, strDate);
                }else if (col == "Longitude" || col == "Latitude")
                {
                    var strCol = decimal.Round(decimal.Parse(reader[col].ToString()),1);
                    result.Add(col, strCol);
                }
                else
                {
                    result.Add(col, reader[col]);
                }                
                //_output.WriteLine(col+":" + reader[col].ToString());
            }
            return result;
        }

        public Dictionary<string, object> SerializeJSON(string jsonBody, string UniqKey){
            JArray ja = JArray.Parse(jsonBody.ToString());
            Dictionary<string, object> result = new Dictionary<string, object>();
            //_output.WriteLine(ja[0].ToString());
            foreach (var item in ja){
                //_output.WriteLine(item.ToString());
                //_output.WriteLine(item["SOMRegionId"].ToString());
                string keyofRow = item[UniqKey].ToString();
                JToken overRideItem = updateDates(item);
                overRideItem = updateLatitudesLongitudes(overRideItem);
                result.Add(keyofRow, overRideItem);
            }
            return result;
        }

        public JToken updateDates(JToken item)
        {
            try
            {
                var strEffectiveDate = convertToSimpleDate(item["EffectiveDate"].ToString());
                item["EffectiveDate"] = strEffectiveDate;
            }
            catch (NullReferenceException){ }

            try{
                var strEndDate = convertToSimpleDate(item["EndDate"].ToString());
                item["EndDate"] = strEndDate;
            }
            catch (NullReferenceException) { }

            try{
                var strCreatedDate = convertToDateTime(item["CreatedDate"].ToString());
                item["CreatedDate"] = strCreatedDate;
            }
            catch (NullReferenceException) { }

            try{
                var strLastModifiedDate = convertToDateTime(item["LastModifiedDate"].ToString());
                item["LastModifiedDate"] = strLastModifiedDate;
            }
            catch (NullReferenceException) { }

            try
            {
                var strLastModifiedDate = convertToDateTime(item["ProcessingStartTime"].ToString());
                item["LastModifiedDate"] = strLastModifiedDate;
            }
            catch (NullReferenceException) { }

            try
            {
                var strLastModifiedDate = convertToDateTime(item["ProcessingEndTime"].ToString());
                item["LastModifiedDate"] = strLastModifiedDate;
            }
            catch (NullReferenceException) { }

            return item;
        }


        public JToken updateLatitudesLongitudes(JToken item)
        {
            JObject jparsed = JObject.Parse(item.ToString());
            if (jparsed.ContainsKey("Longitude"))
            {
                var newDec = decimal.Round(decimal.Parse(item["Longitude"].ToString()), 1);
                item["Longitude"] = newDec;
            }

            if (jparsed.ContainsKey("Latitude"))
            {
                var newDec = decimal.Round(decimal.Parse(item["Latitude"].ToString()), 1);
                item["Latitude"] = newDec;
            }
            return item;
        }

        public string convertToSimpleDate(string strDate)
        {
            var dtDate = Convert.ToDateTime(strDate);
            strDate = dtDate.Year + "-" + (dtDate.Month < 10 ? "0" + dtDate.Month.ToString() : dtDate.Month.ToString()) + "-" + (dtDate.Day < 10 ? "0" + dtDate.Day.ToString() : dtDate.Day.ToString());
            return strDate;
        }

        public string convertToDateTime(string strDate)
        {
            var dtDate = Convert.ToDateTime(strDate);
            strDate = dtDate.ToString("MM/dd/yyyy HH:mm:ss");
            return strDate;
        }

        public int LOAD_ReaderToCSV(IDataReader reader, string filepath, string delimeter){

            List<object> recList = new List<object>();
            var cols = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
                cols.Add(reader.GetName(i));

            while (reader.Read()){
                List<string> innerList = new List<string>();
                foreach (var col in cols) {
                    if (col == "EffectiveDate" || col == "EndDate") {
                        string strDate = reader[col].ToString();
                        var dtDate = Convert.ToDateTime(strDate);
                        strDate = dtDate.Year + "-" + (dtDate.Month < 10 ? "0" + dtDate.Month.ToString() : dtDate.Month.ToString()) + "-" + (dtDate.Day < 10 ? "0" + dtDate.Day.ToString() : dtDate.Day.ToString());
                        innerList.Add(strDate.ToString());
                    } else if (col.EndsWith("Date") || col.EndsWith("Time")) {
                        string strDate = reader[col].ToString();
                        try
                        {
                            var dtDate = Convert.ToDateTime(strDate);
                            innerList.Add(dtDate.ToString("MM/dd/yyyy HH:mm:ss"));
                        }
                        catch
                        {
                            innerList.Add(strDate);
                        }
                    } else if (col == "Longitude" || col == "Latitude")
                    {
                        string strCol = decimal.Parse(reader[col].ToString()).ToString();
                        innerList.Add(strCol);
                    }
                    else {
                        innerList.Add(reader[col].ToString());
                    }                    
                }
                recList.Add(innerList);
            }
            var sortedList = this.SORT_list(recList, delimeter);
            sortedList.Insert(0,cols);

            return this.LOAD_toCSVFile(filepath, delimeter, sortedList);
        }

        public int LOAD_JsonToCSV(string jsonBody, string filepath, string delimeter){

            JArray ja = JArray.Parse(jsonBody.ToString());             
            JObject jObj = JObject.Parse(ja[0].ToString());
            List<object> recList = new List<object>();

            Dictionary<string, string> dictObj = jObj.ToObject<Dictionary<string, string>>();
            var cols = new List<string>(dictObj.Keys);

            foreach (var item in ja)
            {
                List<string> innerList = new List<string>();
                foreach (var col in cols){
                    if (col == "EffectiveDate" || col == "EndDate")
                    {
                        string strDate = item[col].ToString();
                        var dtDate = Convert.ToDateTime(strDate);
                        strDate = dtDate.Year + "-" + (dtDate.Month < 10 ? "0" + dtDate.Month.ToString() : dtDate.Month.ToString()) + "-" + (dtDate.Day < 10 ? "0" + dtDate.Day.ToString() : dtDate.Day.ToString());
                        innerList.Add(strDate.ToString());
                    }
                    else if (col == "CreatedDate" || col == "LastModifiedDate" || col == "ProcessingStartTime" || col == "ProcessingEndTime")
                    {
                        string strDate = item[col].ToString();
                        var dtDate = Convert.ToDateTime(strDate);
                        strDate = dtDate.ToString("MM/dd/yyyy HH:mm:ss");
                        innerList.Add(strDate.ToString());
                    }else if (col == "Longitude" || col == "Latitude")
                    {
                        string strCol = decimal.Parse(col.ToString()).ToString();
                        innerList.Add(strCol);
                    }
                    else
                    {
                        innerList.Add(item[col].ToString());
                    }                    
                }
                recList.Add(innerList);
            }  
            var sortedList = this.SORT_list(recList, "|");
            sortedList.Insert(0, cols);

            return this.LOAD_toCSVFile(filepath, delimeter, sortedList) ;
        }  

        public int LOAD_toCSVFile(string filepath, string delimeter, List<object> recList){ 
            int count = 0;                     
            using (var textWriter = new StreamWriter(filepath)){
                var writer = new CsvWriter(textWriter);
                writer.Configuration.Delimiter = delimeter;
                foreach (var item in recList){
                    IList colleciton = (IList)item;
                    //var itermRes = ((IEnumerable)item).GetEnumerator();
                    foreach (var iterField in colleciton)
                    {
                        writer.WriteField(iterField);
                    }
                    //writer.WriteField(item);
                    count = count + 1;
                    writer.NextRecord();
                } 
                writer.Flush();  
                textWriter.Flush();               
            }   
            return count;                  
        }  

        public List<object> SORT_list(List<object> strList, string delimeter){

            List<object> returnList = new List<object>();
            List<string> sortList = new List<string>();
            foreach (var item in strList)
            {
                IList colleciton = (IList)item;
                StringBuilder sb = new StringBuilder();
                foreach (var i in colleciton)
                {
                    sb.Append(i.ToString() + delimeter);
                }
                sortList.Add(sb.ToString());
            }
            //SORTING
            sortList.Sort();

            foreach (var item in sortList)
            {
                List<string> innerList = new List<string>(item.Split(delimeter));
                returnList.Add(innerList);
            }

            return returnList;
        }

        public string CONVERT_DataTableToJSON(DataTable table)
        {
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(table);
            return JSONString;
        }

        public void RUN_SnowCommands(string connectionString, List<string> queries)
        {
            IDbConnection conn = new SnowflakeDbConnection();
            conn.ConnectionString = connectionString;
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();

            foreach (var item in queries)
            {
                cmd.CommandText = item.ToString();               
                cmd.ExecuteNonQuery();
                cmd.CommandText = "COMMIT;";
                cmd.ExecuteNonQuery();
            }            
            conn.Close();
        }

        public int queryReturnCount(string connectionString, string strQuery)
        {
            IDbConnection conn = new SnowflakeDbConnection();
            conn.ConnectionString = connectionString;
            conn.Open();
            IDbCommand cmd = conn.CreateCommand();
            cmd.CommandText = strQuery;
            IDataReader reader = cmd.ExecuteReader();            

            DataTable dt = new DataTable();
            dt.Load(reader);

            conn.Close();

            return dt.Rows.Count;

        }
    }
}