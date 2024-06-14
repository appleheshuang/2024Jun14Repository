using Sailfish.RealignmentEngine.Salesforce;
using Sailfish.RealignmentEngine.Tests;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest.DataUtils.Salesforce
{
    public class ConnectSalesForce
    {
        public static SfdcCredentials sfdcCredentials = null;
        public ConnectSalesForce()
        {
            var config = new DevConfig();
            string _instanceUrl = config["InstanceUrl"];
            string _clientId = config["ClientId"];
            string _clientSecret = config["ClientSecret"];
            string _username = config["Username"];
            string _password = config["Password"];

            sfdcCredentials = new SfdcCredentials(_instanceUrl, _clientId, _clientSecret, _username, _password);
        }
    }
}
