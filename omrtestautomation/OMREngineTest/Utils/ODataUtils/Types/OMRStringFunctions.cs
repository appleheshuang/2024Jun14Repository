using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using MainFramework.GlobalUtils;
using Newtonsoft.Json.Linq;
using ODataTest.ODataUtils;
using System.Data;
using System.Linq;
using Newtonsoft.Json;

namespace ODataTest.Types
{
    public class OMRStringFunctions : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            
            ODataHelper _config = new ODataHelper();
            string rootPath = _config.returnRootDir();
            var configPath = rootPath + "TestData\\OData\\" + "ODATATEST_STRINGFUNC.csv";
            configPath = configPath.Replace("\\", "/");
            var testDataAll = System.IO.File.ReadAllText(configPath).Split("\r\n").ToList();

            JObject postTemplate = _config.readTestDataJSON(rootPath + "TestData/OData/postRequest_schema.json");


            for (int i = 1; i < testDataAll.Count; i++)
            {
                var item = testDataAll[i].ToString();
                var byComma = item.Split("|");
                
                List<string> xList = new List<string>();
                xList.Add(byComma[1].ToString());
                xList.Add(byComma[2].ToString());
                xList.Add(byComma[3].ToString());
                xList.Add(byComma[4].ToString());
                xList.Add(byComma[5].ToString());                
                
                var extParameter = postTemplate[byComma[1].ToString()];
                //xList.Add(extParameter.ToString());
                xList.Add(JsonConvert.SerializeObject(extParameter, Formatting.Indented));

                object[] objX = new object[xList.Count];
                int ctr = 0;
                foreach (var j in xList)
                {
                    objX[ctr] = j;
                    ctr = ctr + 1;
                }
                yield return objX;
            }

        }
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
