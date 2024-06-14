using System;
using System.IO;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Sailfish.RealignmentEngine.Salesforce;
using Sailfish.RealignmentEngine;
using System.Collections.Generic;
using System.Data;
using MainFramework.GlobalUtils;

namespace MainFramework.GlobalUtils
{
    public class SalesforceFunctions
    {
        private ISalesforceRestAPI _salesforceRestAPI;
        private string ResourcesPath = Path.Combine("temp-resources");
        private List<string> salesforceStandards = new List<string>() { "Name", "Id", "CreatedById", "LastModifiedById", "OwnerId", "CreatedDate", "LastModifiedDate" };

        public SalesforceFunctions(SfdcCredentials sfdcCredentials)
        {
            _salesforceRestAPI = new SalesforceRestAPI(sfdcCredentials);
        }

        public void extractData(String query, string fileName, string strDirPath)
        {
            FileHelper jfh = new FileHelper();
            DataTable table = new DataTable();

            string tableFolder = jfh.GetDirectoryPath(strDirPath);
            Directory.CreateDirectory(tableFolder);
            //jfh.deletefile(tableFolder, fileName+".csv");

            using (BulkQueryJob job = new BulkQueryJob(fileName, _salesforceRestAPI))
            {
                Batch batch = job.CreateBatch(query);
                batch.Refresh();
                while (batch.State != "Completed")
                {
                    if ((batch.State == "Failed") || (batch.State == "Not Processed"))
                        throw new ScenarioSyncException($"Salesforce bulk query failed for: {query}");
                    Task.Delay(500);
                    batch.Refresh();
                }
                if (batch.NumberRecordsProcessed > 0)
                {
                    List<string> results = batch.GetResultList();
                    foreach (string result in results)
                    {
                        batch.DownloadResult(result, string.Format("{0}/{1}.csv", tableFolder, fileName+"_"+jfh.getTimeStamp()));
                    }
                }
            }            
        }
    }
    //test
}
