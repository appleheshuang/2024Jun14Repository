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

namespace ODataTest.ODataUtils
{
    public class ODataHelper
    {
        public static string CompareResult = "true";

        public bool COMPARE_fileVfile(string path1, string path2)
        {
            byte[] file1 = File.ReadAllBytes(path1);
            byte[] file2 = File.ReadAllBytes(path2);

            if (file1.Length == file2.Length)
            {
                for (int i = 0; i < file1.Length; i++)
                {
                    if (file1[i] != file2[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public JObject readTestDataJSON(string jsonPath)
        {            
            var retJson = System.IO.File.ReadAllText(jsonPath);
            //retJson  = JsonConvert.SerializeObject(retJson);
            JObject retJO = JObject.Parse(retJson);
            return retJO;
        }
        public string returnRootDir()
        {
            var currDir = Environment.CurrentDirectory;
            int srchPath = currDir.IndexOf("bin");
            string rootPath = currDir.Substring(0, currDir.Length - (currDir.Length - srchPath));
            return rootPath;
        }

        public StringBuilder CompareObjects(JObject source, JObject target)
        {
            StringBuilder returnString = new StringBuilder();
            foreach (KeyValuePair<string, JToken> sourcePair in source)
            {
                if (sourcePair.Value.Type == JTokenType.Object)
                {
                    returnString.Append(string.Format(Environment.NewLine + "Compare {0}" + Environment.NewLine, sourcePair.Key));
                    if (target.GetValue(sourcePair.Key) == null)
                    {
                        returnString.Append("Key " + sourcePair.Key
                                            + " not found" + Environment.NewLine);
                        CompareResult = "false";
                    }
                    else if (target.GetValue(sourcePair.Key).Type != JTokenType.Object)
                    {
                        returnString.Append("Key " + sourcePair.Key
                                            + " is not an object in target" + Environment.NewLine);
                    }
                    else
                    {
                        returnString.Append(CompareObjects(sourcePair.Value.ToObject<JObject>(),
                            target.GetValue(sourcePair.Key).ToObject<JObject>()));
                    }
                }
                else if (sourcePair.Value.Type == JTokenType.Array)
                {
                    if (target.GetValue(sourcePair.Key) == null)
                    {
                        returnString.Append("Key " + sourcePair.Key
                                            + " not found" + Environment.NewLine);
                        CompareResult = "false";
                    }
                    else
                    {
                        returnString.Append(CompareArrays(sourcePair.Value.ToObject<JArray>(),
                            target.GetValue(sourcePair.Key).ToObject<JArray>(), sourcePair.Key));
                    }
                }
                else
                {
                    JToken expected = sourcePair.Value;
                    var actual = target.SelectToken(sourcePair.Key);
                    if (actual == null)
                    {
                        returnString.Append("Key " + sourcePair.Key
                                            + " not found" + Environment.NewLine);
                        CompareResult = "false";
                    }
                    else
                    {
                        if (!JToken.DeepEquals(expected, actual))
                        {
                            returnString.Append("Key " + sourcePair.Key + ": "
                                                + sourcePair.Value + " !=  "
                                                + target.Property(sourcePair.Key).Value
                                                + Environment.NewLine);
                            CompareResult = "false";
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, JToken> targetPair in target)
            {
                if (targetPair.Value.Type == JTokenType.Object)
                {
                    if (source.GetValue(targetPair.Key) == null)
                    {
                        returnString.Append(Environment.NewLine + string.Format("Compare {0}" + Environment.NewLine, targetPair.Key));
                        returnString.Append("Key " + targetPair.Key
                                            + " newly added" + Environment.NewLine);
                        CompareResult = "false";
                    }
                }
                else if (targetPair.Value.Type == JTokenType.Array)
                {
                    if (source.GetValue(targetPair.Key) == null)
                    {
                        returnString.Append("Key " + targetPair.Key
                                            + " newly added" + Environment.NewLine);
                        CompareResult = "false";
                    }
                }
                else
                {
                    JToken expected = targetPair.Value;
                    var actual = source.SelectToken(targetPair.Key);
                    if (actual == null)
                    {
                        returnString.Append("Key " + targetPair.Key
                                            + " newly added" + Environment.NewLine);
                        CompareResult = "false";
                    }
                }
            }
            //_output.WriteLine(returnString.ToString());
            //System.IO.File.WriteAllText(@fp.GetFilePath(diffFile), returnString.ToString());

             return returnString;           
        }

        public StringBuilder CompareArrays(JArray source, JArray target, string arrayName = "")
        {
            var returnString = new StringBuilder();
            for (var index = 0; index < source.Count; index++)
            {

                var expected = source[index];
                if (expected.Type == JTokenType.Object)
                {
                    var actual = (index >= target.Count) ? new JObject() : target[index];
                    returnString.Append(CompareObjects(expected.ToObject<JObject>(),
                        actual.ToObject<JObject>()));
                }
                else
                {

                    var actual = (index >= target.Count) ? "" : target[index];
                    if (!JToken.DeepEquals(expected, actual))
                    {
                        if (String.IsNullOrEmpty(arrayName))
                        {
                            returnString.Append("Index " + index + ": " + expected
                                                + " != " + actual + Environment.NewLine);
                        }
                        else
                        {
                            returnString.Append("Key " + arrayName
                                                + "[" + index + "]: " + expected
                                                + " != " + actual + Environment.NewLine);
                        }
                    }
                }
            }
            return returnString;
        }

        public Hashtable buildDefaultHeaders(string contentType, string token, string xapikey)
        {
            Hashtable headers = new Hashtable();
            headers.Add("content-type", contentType);
            headers.Add("Authorization", token);
            headers.Add("x-api-key", xapikey);
            return headers;
        }

        public string controlPostBody( string strBody, string fieldToChange, string fieldValue)
        {
            Random rnd = new Random();
            int num = rnd.Next(1, 1000000);
            var strNum = num.ToString();
            strNum = strNum.Substring(strNum.Length - 2, 2);
            var unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            unix = unix.Substring(6, 4);
            var strSuffix = "Tst" + strNum + unix;
            strBody = strBody.Replace("strSuffix", strSuffix);
            strBody = strBody.Replace("dateNow", DateTime.Now.ToString("yyyy-MM-dd"));
            strBody = strBody.Replace("dateTimeNow", DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss") + ".0");
            //Cannot do a string replace for decimal without the quotes
            //strBody = strBody.Replace("randomDecimal", rnd.Next(1, 100).ToString());
            strBody = strBody.Replace("year", DateTime.Now.Year.ToString());

            JObject jo = JObject.Parse(strBody);
            jo[fieldToChange] = fieldValue;

            //return strBody;
            return jo.ToString();
        }

        public string removeFieldFromPostBody(string strBody, string fieldToChange)
        {
          
            JObject jo = JObject.Parse(strBody);
            jo.Remove(fieldToChange);

            //return strBody;
            return jo.ToString();
        }

        public string getResponseParsedValue(string responseBody)
        {
            JObject jo = JObject.Parse(responseBody);
            var bodyValue = jo["value"].ToString();
            return bodyValue;
        }

        public string getConnStr(string SFAccount, string SFHost, string SFUser, string SFPword, string SFDB, string SFRole, string SFWarehouse, string SFSchema)
        {
            var connStr = "" +
                "ACCOUNT=" + SFAccount +
                "; HOST=" + SFHost +
                "; USER=" + SFUser +
                "; PASSWORD=" + SFPword +
                "; DB=" + SFDB +
                "; ROLE=" + SFRole +
                "; WAREHOUSE=" + SFWarehouse +
                "; SCHEMA=" + SFSchema;

            return connStr;
        }

        public void LOAD_toXML(string fileLoc, string strXML)
        {
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(strXML);
            xdoc.Save(fileLoc);
        }

        public List<string> DELETE_testdata(SnowFlakeHelper _snowflake, string connStr, string testDataFile)
        {
            string rootPath = this.returnRootDir();
            var configPath = rootPath + "TestData\\OData\\" + testDataFile;
            configPath = configPath.Replace("\\", "/");
            var testDataAll = System.IO.File.ReadAllText(configPath).Split("\r\n").ToList();

            List<string> retRes = new List<string>();
            for (int i = 1; i < testDataAll.Count; i++)
            {
                var item = testDataAll[i].ToString();
                var splitted = item.Split("|");

                var tblName = splitted[1].ToString();
                var field = splitted[2].ToString();

                //We need to have a check here first IF THERE IS COUNT or none
                //We can get it in INFORMATION_SCHEMA
                //1 query run per table will be enough
                var cntQuery = "SELECT *FROM \"STATIC\".\"" + tblName + "\" where \"SourceId\" = 'ODATATEST';";
                var tblCount = _snowflake.queryReturnCount(connStr, cntQuery);
                if (tblCount >= 1)
                {
                    var sqlQuery = "DELETE FROM \"STATIC\".\"" + tblName + "\" where \"SourceId\" = 'ODATATEST';";
                    retRes.Add(sqlQuery);
                }
            }
            return retRes;
        }

    }
}
