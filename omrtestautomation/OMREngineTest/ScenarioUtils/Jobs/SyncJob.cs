using System;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections;
using MainFramework;
using MainFramework.GlobalUtils;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using OMREngineTest.Utils;

namespace OMREngineTest.ScenarioUtils.Jobs
{
    public struct sync_params
    {
        public string StartFrom { get; set; }
        public paramlist Parameters { get; set; }
    }
    public struct paramlist
    {
        public bool SyncAll;
        public paramlist(bool v)
        {
            SyncAll = v;
        }
    }
    public class SyncJob : OptimizerJob
    {
        private SalesForceHelper sfHelp;
        private string sfNamespace;
        private List<string> check_tables;


        public SyncJob(TenantConfig t, eToken token, string id, List<string> snowtables = null)
            : base(t, token, id)
        {
            sfHelp = new SalesForceHelper(t.clientID , t.clientSecret, t.orgUserName, t.orgPassword, t.orgURL, t.sfnamespace);
            sfNamespace = t.sfnamespace.EndsWith("__") || t.sfnamespace.Length==0 ? t.sfnamespace : t.sfnamespace + "__";
            api.done_statuses.Add("OK");
            check_tables = snowtables;
        }
/*      Use default EngineJob Status
 *      public string JobStatus()
        {
            return checkSFLoadedData();
        }*/

        public HttpStatusCode syncTestData(bool syncall=false, string startfrom=null )
        {
            //Run SyncJob
            sync_params syncBody = new sync_params();
            syncBody.StartFrom = startfrom;
            syncBody.Parameters = new paramlist(syncall);
            return SubmitJob("sync", syncBody);
        }

        public string Sync(bool syncall = false, bool ok_to_fail = false)
        {
            string sync_error;
            //sync

            HttpStatusCode syncjob_reqstatus = syncTestData(syncall);
            bool sync_requested = HttpStatusCode.OK == syncjob_reqstatus;
            bool sync_success = false;
            if (sync_requested)
            {
                sync_success = IsSuccessful();
                sync_error = FailMessage();
                if (!sync_success && ok_to_fail)
                {
                    Console.WriteLine(sync_error);
                    return "OK";
                }
            }
            else
                sync_error = "Sync submit error: " + syncjob_reqstatus;
            return sync_success ? "OK" : sync_error;
        }

        public string FailMessage()
        {
            return "Sync status = " + latest_status + ",\nErrors:" + GetJobError();
        }

        public string checkSFLoadedData()
        {

            var sfTable = "";
            var strReturn = "Sf data => ";
            var isLoaded = "OK";

            //Don't check tables that aren't sync'd to SF
            check_tables.Remove("OMAccount");
            check_tables.Remove("OMAccountAddress");
            check_tables.Remove("OMAffiliation");

            foreach (var strTable in check_tables)
            {
                    sfTable = sfNamespace + strTable + "__c";
                    int recCount = int.Parse(sfHelp.getTableCount(sfTable));
                        if (recCount > 0)
                        {
                            strReturn = strReturn + strTable + " : " + recCount.ToString() + ";";
                        }
                        else
                        {
                            strReturn = strReturn + strTable + " : " + "0;";
                            isLoaded = "NOT OK";
                        }
                    strReturn = strReturn + "\r\n";
            }

            //in case we need to check per table strReturn is ready
            if (isLoaded == "NOT OK")
            {
                return strReturn;
            }
            else
            {
                return isLoaded;
            }
        }

    }
}