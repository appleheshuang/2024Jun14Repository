using MainFramework;
using OMREngineTest.ScenarioUtils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TechTalk.SpecFlow;
using OMREngineTest.Constructors.SalesforceObjects.OCEObjects;
using Optimizer.Constructors.SalesforceObjects.OCEObjects;
using Newtonsoft.Json;
using MainFramework.GlobalUtils;
using MainFramework.Constructors;
using Sailfish.RealignmentEngine.Tests;
using OMREngineTest.Utils;

namespace OMREngineTest.ScenarioUtils.DataUnmarshallers
{
    public class DigitalUnmarshaller : JSONUnmarshaller
    {
        DigitalApi df;
        public DigitalTenantConfig DTConfig { get; private set; }

        public DigitalUnmarshaller(ScenarioContext scenarioContext, string tconfig) : base(scenarioContext, tconfig)
        {
            df = new DigitalApi(tenant.Config);
            DTConfig = (DigitalTenantConfig)tenant.Config;
        }

        public List<Object> PostDigitalObjectsFromFilePath(string tablename, string filePath, string methodType, string StrId = null)
        {
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);

            List<Object> objectsToReturn = new List<Object>();
            foreach (Tuple<string, JObject> testdata in templates)
            {
                JObject jsonObj = testdata.Item2;

                var sf_objects = jsonObj[tablename]?.ToObject<List<JObject>>();
                foreach (JObject objx in sf_objects ?? Enumerable.Empty<JObject>())
                {
                    JObject obj = PrepareObjectData(objx);
                    
                    JArray newArr = new JArray(); 
                    newArr.Add(obj);
                    string objname = tablename;
                            try
                            {
                                df.Post_Object(tablename, JsonConvert.SerializeObject(newArr));
                            }
                            catch
                            {
                                throw new UnmarshallerException("Digital DE not supported: " + tablename);
                            }
                }
                AddToDataMapListOfObjects(objectsToReturn, tablename + "sCount");
            }
            return objectsToReturn;
        }

        JObject PrepareObjectData(JObject sfobj)
        {
            // Replace Test Uid and BU
            string new_data = Regex.Replace(sfobj.ToString(), "{{TestUniqueIntegrationId}}", testId, RegexOptions.IgnoreCase);
            string new_data1 = Regex.Replace(new_data.ToString(), "{{BUID}}", DTConfig.BUID, RegexOptions.IgnoreCase);
            string new_data2 = Regex.Replace(new_data1.ToString(), "{{CRMID}}", DTConfig.CRMID, RegexOptions.IgnoreCase);
            string new_data3 = Regex.Replace(new_data2.ToString(), "{{BUIDOther}}", DTConfig.BUIDOther, RegexOptions.IgnoreCase);
            string new_data4 = Regex.Replace(new_data3.ToString(), "{{RootBUID}}", DTConfig.RootBUID, RegexOptions.IgnoreCase);

            sfobj = JObject.Parse(new_data4);
         

            // Set Object properties
            sfobj = SetPropertiesWithBaseDataDynamic(sfobj, baseDataMap, dataMap);

            return sfobj;
        }

        //will get value for BUID for query results
        public new TestDefinition GetTests(string filePath, Scenario scenario = null)
        {
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
            foreach (Tuple<string, JObject> testdata in templates)
            {
                string scenarioname = testdata.Item1;
                JObject jsonObj = testdata.Item2;
                SetBaseDataMap(jsonObj);

                if (jsonObj.ContainsKey("Tests"))
                {

                    string testsAsString = jsonObj["Tests"]?.ToString();
                    List<string> regexKeys = FileHelper.ExtractRegex(testsAsString);
                    if (regexKeys.Count > 0)
                    {
                        regexKeys.Add("testuniqueintegrationid"); //for the case where it's referred to in the BaseData
                        SetTestId();
                    }
                    foreach (string regexKeyCaseSensitive in regexKeys)
                    {
                        string regexKey = regexKeyCaseSensitive.ToLower();
                        int index = FileHelper.GetIndex(regexKey) ?? 0;

                        if (baseDataMap.ContainsKey(regexKeyCaseSensitive))
                        {
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", baseDataMap[regexKeyCaseSensitive], RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.Equals("testuniqueintegrationid"))
                        {
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", testId, RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.Equals("buid"))
                        {
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", DTConfig.BUID, RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.Equals("buidother"))
                        {
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", DTConfig.BUIDOther, RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.Equals("crmid"))
                        {
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", DTConfig.CRMID, RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.Equals("today"))
                        {
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", chelp.Today, RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.StartsWith("today-"))
                        {
                            int day_offset = 0;
                            try { day_offset = int.Parse(regexKey["today-".Length..]); }
                            catch { Console.WriteLine("Exception getting day offset from " + regexKey + ". Using 0 days offset."); }
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", chelp.DaysAgo(day_offset), RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.StartsWith("today+"))
                        {
                            int day_offset = 0;
                            try { day_offset = int.Parse(regexKey["today+".Length..]); }
                            catch { Console.WriteLine("Exception getting day offset from " + regexKey + ". Using 0 days offset."); }
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", chelp.DaysFuture(day_offset), RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.Equals("yesterday"))
                        {
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", chelp.Yesterday, RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.Equals("dbt_today"))
                        {
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", chelp.Dbt_Today, RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.Equals("dbt_yesterday"))
                        {
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", chelp.Dbt_Yesterday, RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.Equals("open"))
                        {
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", chelp.Open, RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.Equals("dbt_current"))
                        {
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", chelp.Dbt_Current, RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.StartsWith("dbt_today-"))
                        {
                            int day_offset = 0;
                            try { day_offset = int.Parse(regexKey["dbt_today-".Length..]); }
                            catch { Console.WriteLine("Exception getting day offset from " + regexKey + ". Using 0 days offset."); }
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", chelp.Dbt_DaysAgo(day_offset), RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.StartsWith("dbt_today+"))
                        {
                            int day_offset = 0;
                            try { day_offset = int.Parse(regexKey["dbt_today+".Length..]); }
                            catch { Console.WriteLine("Exception getting day offset from " + regexKey + ". Using 0 days offset."); }
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", chelp.Dbt_DaysFuture(day_offset), RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (dataMap.ContainsKey(regexKey + "[" + index + "]"))
                        {
                            try
                            {
                                string newString = Regex.Replace(input: testsAsString, pattern: Regex.Escape("{{" + regexKey + "}}"), replacement: dataMap[regexKey + "[" + index + "]"], options: RegexOptions.IgnoreCase);
                                testsAsString = newString;
                            }
                            catch (ArgumentNullException e)
                            {
                                throw new ArgumentNullException(regexKey + "[" + index + "]" + " value is null in dataMap", e);
                            }
                        }
                        if (dataMap.ContainsKey(regexKey))
                        {
                            try
                            {
                                string newString = Regex.Replace(input: testsAsString, pattern: Regex.Escape("{{" + regexKey + "}}"), replacement: dataMap[regexKey], options: RegexOptions.IgnoreCase);
                                testsAsString = newString;
                            }
                            catch (ArgumentNullException e)
                            {
                                throw new ArgumentNullException(regexKey + " value is null in dataMap", e);
                            }
                        }
                        if (regexKey.Equals("dbt_1dayago"))
                        {
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", chelp.Dbt_1DayAgo, RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                        if (regexKey.Equals("dbt_current"))
                        {
                            string newString = Regex.Replace(testsAsString, "{{" + regexKey + "}}", chelp.Dbt_Current, RegexOptions.IgnoreCase);
                            testsAsString = newString;
                        }
                    }

                    return new TestDefinition(scenarioname, testsAsString, tenantconfigfile, scenario?.Id);
                }
            }
            return null;
        }
    }
    
}
