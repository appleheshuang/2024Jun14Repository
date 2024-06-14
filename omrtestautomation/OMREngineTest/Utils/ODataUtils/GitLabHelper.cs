using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Collections;
using System.Xml;
using System.Linq;
using MainFramework.GlobalUtils;
using System.Threading.Tasks;

namespace ODataTest.ODataUtils
{
    public class GitLabHelper
    {
        public string getCICDVariable(string projectID, string privateToken, string baseURL, string gitVariable)
        {
            HTTPClient_AsyncRequest _gitlabClient = new HTTPClient_AsyncRequest();
            //var projectID = gitlabSettings["projectID"].ToString();
            //var privateToken = gitlabSettings["privateToken"].ToString();
            _gitlabClient.SET_BaseAddress(baseURL);
            _gitlabClient.ADD_Headers("PRIVATE-TOKEN", privateToken);

            var strRequest = "variables/" + gitVariable;
            Task<string> gitTask = _gitlabClient.GET_AsyncRequest_ResponseOnly(strRequest);
            gitTask.Wait();
            JObject gitLabEnvConfig = JObject.Parse(gitTask.Result.ToString());
            string gitFile = gitLabEnvConfig["value"].ToString();

            return gitFile;
        }
    }
}
