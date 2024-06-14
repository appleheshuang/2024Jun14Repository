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

namespace ODataTest.Types
{
    public class OMRTableTest : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            string rootPath = new ODataHelper().returnRootDir();
            var configPath = rootPath + "TestData\\OData\\" + "ODATATEST_TABLEREQ.csv";
            configPath = configPath.Replace("\\", "/");
            var testDataAll = System.IO.File.ReadAllText(configPath).Split("\r\n").ToList();

            for (int i = 1; i < testDataAll.Count; i++)
            {
                var item = testDataAll[i].ToString();
                var byComma = item.Split("|");

                List<string> xList = new List<string>();
                xList.Add(byComma[0].ToString());
                xList.Add(byComma[1].ToString());
                xList.Add(byComma[2].ToString());

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
