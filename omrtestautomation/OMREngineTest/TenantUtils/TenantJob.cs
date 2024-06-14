using System;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using MainFramework;
using MainFramework.GlobalUtils;
using System.Collections.Generic;

namespace OMREngineTest.Utils
{
    public struct tenant_body
    {
        public string TenantName { get; set; }
        //public sf_node OCE { get; set; }
        public sf_node OMR { get; set; }
        public string OrgId { get; set; }
        public int NoOfTerritories;
        public int ComputeType;
        public string TargetEnvironment;
        public bool EnableDisasterRecovery;
        public package_node[] Packages;
        public bool OptimizerEnabled;
        public tenant_body(string _name, string orgid, string env, sf_node omr, List<package_node> package_node, bool optimizerEnabled, int terr=40)
        {
            TenantName = _name;
            TargetEnvironment = env;
            OMR = omr;
            OrgId = orgid;
            NoOfTerritories = terr;
            ComputeType = 1;
            EnableDisasterRecovery = false;
            Packages = package_node.ToArray();
            OptimizerEnabled = optimizerEnabled;
        }
    }
    public struct response_status_message {
        public HttpStatusCode statuscode {get;set;}
        public string responsemessage {get;set;}
        public response_status_message(HttpStatusCode statusCode,string msg) {
            statuscode = statusCode;
            responsemessage = msg;
        }
    }
    public struct sf_node
    {
        public string ClientSecret;
        public string ClientId;
        public string SalesforceInstanceUrl;
        public string Password;
        public string User;
        public string Namespace;
        public sf_node(string client, string secret, string url, string user, string pswd, string ns=null)
        {
            ClientId = client;
            ClientSecret = secret;
            SalesforceInstanceUrl = url;
            User = user;
            Password = pswd;
            Namespace = ns.EndsWith("__") ? ns : ns+"__" ;
        }
    }
    public struct package_node
    {
        public string Name;
        public string Version;
        public package_node(string name="OCEO",string ver = "latest")
        {
            Name = name;
            Version = ver;
        }
    }

    public struct tenant_disable
    {
        public string TenantId;
        public tenant_disable(string t)
        {
            TenantId = t;
        }
    }

    public class TenantJob : OptimizerApi
    {
        public string tenant_id;
        public string org_id;
        private sf_node omr_sf;
        private List<package_node> packages = new List<package_node>();
        private string target;
        private bool optimizerEnabled;

        public TenantJob(TenantConfig t, bool token_only=false)
            : base(t)
        {
            // Get the tenantcreator credentials for the env data and get a token
            EnvFixture _env = new EnvFixture("envConfig-" + t.targetEnv + ".json");
            EnvConfig envConfig = _env.Config;
            if (envConfig.creator_pswd == null || envConfig.creator_pswd == "")
                throw new MissingCredentialsException("Tenant creator password is unknown");
            GetToken(envConfig.tenantcreator, envConfig.creator_pswd, envConfig.master_apikey);
            t.sfApiKey = envConfig.master_apikey;
            t.generic_sfApiKey = envConfig.generic_sfApiKey;
            if (t.targetVersion == "" || t.targetVersion == null) t.targetVersion = "latest";
            SetVersion();
            // Define tenant create done statuses
            if (!token_only)
            {
                done_statuses = new List<string> { };
                done_statuses.Add(_status_code["tenant_create_fail"]);
                done_statuses.Add(_status_code["tenant_create_success"]);
                // Create the sf node for new tenant create
                omr_sf = new sf_node(t.clientID, t.clientSecret, t.orgURL, t.orgUserName, t.orgPassword, t.sfnamespace);
                //packages = new List<package_node>() { new package_node("OCEO", t.targetVersion) };
                //if (t.OCEDVersion != "" && t.OCEDVersion != null)
                //    packages.Add(new package_node("OCED", t.OCEDVersion));
                //if (t.OCESegVersion != "" && t.OCESegVersion != null)
                //    packages.Add(new package_node("OCESeg", t.OCESegVersion));
            }
            target = t.targetEnv;
            org_id = t.orgid;
        }

        public void AddPackageNode(string packageName, string targetVersion)
        {
            this.packages.Add(new package_node(packageName, targetVersion));
        }

        public void SetOptimizerEnabled(bool optimizerEnabled)
        {
            this.optimizerEnabled = optimizerEnabled;
        }

        public string JobStatus()
        {
            return Request_RestSharpValue("message", "tenantStatus", "GET", api_param: tenant_id);
        }

        public string JobError()
        {
            return Request_RestSharpValue("error", "tenantStatus", "GET", api_param: tenant_id);
        }

        public Tuple<response_status_message, TenantConfig> tenantCreate(string tenantName, string targetEnv=null)
        {
            //Request create tenant async 
            tenant_body tenantBody = new tenant_body(tenantName, org_id, targetEnv ?? target, omr_sf, packages, optimizerEnabled);
            Object req_body = tenantBody;
            Tuple<IRestResponse, String> response = Request_RestSharp("tenantCreate", "POST", request_body: req_body);
            TenantConfig new_tenant = new TenantConfig();

            if (HttpStatusCode.OK == response.Item1.StatusCode)
            {
                //Parse the response body
                var chkbody = response.Item2;
                JObject joChk = JObject.Parse(chkbody);
                tenant_id = joChk["tenantId"].ToString();

                new_tenant.TenantId = tenant_id;
                new_tenant.TenantUser = joChk["cognitoAccount"]["userName"].ToString();
                new_tenant.TenantPassword = joChk["cognitoAccount"]["password"].ToString();
                new_tenant.sfApiKey = joChk["salesforceAPIKey"].ToString();
                new_tenant.lexiApiKey = joChk["lexiAPIKey"].ToString();
                new_tenant.targetEnv = targetEnv ?? target;
                new_tenant.generic_sfApiKey = t.generic_sfApiKey;
            }

            // return the status code and new tenant info
            response_status_message response_message = new response_status_message(response.Item1.StatusCode, response.Item2);
            return new Tuple<response_status_message, TenantConfig>(response_message, new_tenant);
        }

        public string tenantDisable(string t)
        {
            object request_body = new tenant_disable(t);
            return Request_RestSharpValue("message", "tenantDisable", "POST", request_body);
        }
        public string tenantEnable(string t)
        {
            object request_body = new tenant_disable(t);
            return Request_RestSharpValue("message", "tenantEnable", "POST", request_body);
        }

        public Tuple<HttpStatusCode, List<tenantListElement>> listTenants()
        {
            Tuple < IRestResponse, String > response = Request_RestSharp("tenantList", "GET");
            HttpStatusCode status = response.Item1.StatusCode;
            List<tenantListElement> elements;
            try {
                JArray tenant_list = JArray.Parse(response.Item2);
                elements = tenant_list.ToObject<List<tenantListElement>>();
            }
            catch (Exception e)
            {
                throw JsonParseException("tenantList", "GET", response.Item2,e);
            }
            return new Tuple<HttpStatusCode, List<tenantListElement>>(status, elements);
        }
        public Tuple<HttpStatusCode, List<tenantToBePurgedElement>> listTenantsToPurge()
        {
            Tuple<IRestResponse, String> response = Request_RestSharp("tenantPurgeDisabled", "GET");
            HttpStatusCode status = response.Item1.StatusCode;
            List<tenantToBePurgedElement> elements;
            try
            {
                JObject resp_obj = JObject.Parse(response.Item2);
                //JArray obj_array = resp_obj["tenantsToBePurged"].ToOb;
                elements = resp_obj["tenantsToBePurged"].ToObject<List<tenantToBePurgedElement>>();
            }
            catch (Exception e)
            {
                throw JsonParseException("tenantsToBePurged", "GET", response.Item2, e);
            }
            return new Tuple<HttpStatusCode, List<tenantToBePurgedElement>>(status, elements);
        }

        private JsonReaderException JsonParseException(string api_name, string api_method, string response_body, Exception e)
        {
            string display_error = "Failure to parse response for " + api_name + " " + api_method + "\n Response=" + response_body;
            return new JsonReaderException("!!!" + display_error + "\n", e);
        }

        public string checkSnowLoadedData(List<string> tables, QuerySnowflake sfHelp, int intWait=poll_interval)
        {
            string snow_schema = "STATIC";
            var strReturn = "Snow => ";
            var isLoaded = "OK";

            foreach (var sfTable in tables)
            {
                strReturn += sfTable + " : ";
                int recCount = int.Parse(sfHelp.ReturnSingleColumnValueFromSnowFlake("select count(*) \"recs\" from \""+snow_schema+"\".\""+sfTable+"\";","recs"));
                if (recCount > 0)
                        {
                            strReturn += recCount.ToString() + ";";
                        }
                        else
                        {
                            strReturn += "0;";
                            isLoaded = "NOT OK";
                        }
                 strReturn += "\r\n";
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