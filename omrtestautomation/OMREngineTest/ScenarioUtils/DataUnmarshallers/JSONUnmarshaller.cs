using MainFramework;
using MainFramework.GlobalUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OMREngineTest.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TechTalk.SpecFlow;

namespace OMREngineTest.ScenarioUtils
{
    public class JSONUnmarshaller
    {
        protected CodeHelper chelp = new CodeHelper();
        protected FileHelper fileHelper = new FileHelper();
        protected string tenantconfigfile;
        protected s3TenantFixture tenant;
        protected ScenarioContext scenarioContext;
        //protected SalesforceApi sf = new SalesforceApi(tenant.Config);

        protected Dictionary<string, dynamic> baseDataMap;
        protected Dictionary<string, dynamic> dataMap;
        protected Dictionary<string, int> dataMapCounts;

        public string testId { get; set; }

        private const string testDataRoot = "TestData";
        public string testDataPath = testDataRoot;

        public JSONUnmarshaller(ScenarioContext scenarioContext, string tenantconfigfile = "smoketest-org.json", string testid = null, string datapath = null)
        {
            this.scenarioContext = scenarioContext;
            this.tenantconfigfile = tenantconfigfile;
            testId = testid ?? SetTestId();
            testDataPath = datapath ?? testDataRoot;

            if (!this.scenarioContext.TryGetValue("baseDataMap", out baseDataMap)) {
                baseDataMap = new Dictionary<string, dynamic>();
            }
            if (!this.scenarioContext.TryGetValue("dynamicDataMap", out dataMap))
            {
                dataMap = new Dictionary<string, dynamic>();
            }
            if (!this.scenarioContext.TryGetValue("dynamicDataCounts", out dataMapCounts))
            {
                dataMapCounts = new Dictionary<string, int>();
            }
            tenant = new s3TenantFixture(tenantconfigfile);
            chelp.GetDBTime(tenant.Config.snowConnection, tenant.Config.targetEnv);

            SetInitialDataMapValues();
        }
        private void SetInitialDataMapValues()
        {
            dataMap["TestUniqueIntegrationId"] = testId;
        }
        public string ResetTestId(string uid = null)
        {
            testId = uid ?? chelp.GenerateTestId();
            return testId;
        }
        public string SetTestId(string uid = null)
        {
            testId = uid ?? testId ?? chelp.GenerateTestId();
            return testId;
        }

        public void SetTestDataPath(string path)
        {
            testDataPath = fileHelper.GetFilePath(path,testDataRoot);
        }

        public string[] GetFilesFromFilePath(string filePath)
        {
            string configPath = fileHelper.GetFilePath(filePath, testDataPath);
            string[] files;

            try
            {
                files = Directory.GetFiles(configPath);
            }
            catch (System.IO.IOException)
            {
                files = new string[1] { configPath };
            }
            catch (System.ArgumentNullException)
            {
                files = new string[1] { filePath };
            }

            return files;
        }
        public string[] GetFilesFromDirPath(string dirPath)
        {
            string configPath = fileHelper.GetDirectoryPath(dirPath, testDataPath);
            string[] files;

            try
            {
                files = Directory.GetFiles(configPath);
            }
            catch (System.IO.IOException)
            {
                files = new string[1] { configPath };
            }

            return files;
        }
        public List<Tuple<string, JObject>> GetObjectsFromFile(string[] files, string setname = "")
        {
            List<Tuple<string, JObject>> templates = new List<Tuple<string, JObject>>();
            foreach (string file in files)
            {
                string filename = Path.GetFileName(file);
                var sr = new StringReader(File.ReadAllText(file));
                using var reader = new JsonTextReader(sr);
                reader.DateParseHandling = DateParseHandling.None;
                Tuple<string, JObject> testfile_data = new Tuple<string, JObject>(setname == "" ? filename.Replace(".json", "") : setname, JObject.Load(reader));
                //Tuple<string, JObject> testfile_data = new Tuple<string, JObject>(setname == "" ? filename.Replace(".json", "") : setname, JObject.Parse(File.ReadAllText(file)));

                templates.Add(testfile_data);
            }

            return templates;
        }

        public string SetStringValueWithBaseData(string stringToReplace, Dictionary<string, dynamic> baseDataMap, Dictionary<string, dynamic> dataMap)
        {
            List<string> regexKeys = FileHelper.ExtractRegex(stringToReplace);
            int index = FileHelper.GetIndex(stringToReplace) ?? 0;
            foreach (string regexKey in regexKeys)
            {
                string regexKeyLowercase = regexKey.ToLower();
                if (baseDataMap.Keys.Contains(regexKey))
                {
                    stringToReplace.Replace("{{" + regexKey + "}}", baseDataMap[regexKey]);
                    return baseDataMap[regexKey];
                }
                if (dataMap.Keys.Contains(regexKeyLowercase + "[" + index + "]"))
                {
                    return dataMap[regexKeyLowercase+"["+index+"]"];
                }
                if (dataMap.Keys.Contains(regexKeyLowercase))
                {
                    stringToReplace.Replace("{{" + regexKey + "}}", dataMap[regexKeyLowercase]);
                    return dataMap[regexKeyLowercase];
                }
            }
            return stringToReplace;
        }

        protected string SetFieldProperties(string value, Dictionary<string, dynamic> baseDataMap, Dictionary<string, dynamic> dataMap)
        {
            string fieldVal = value;
            if (fieldVal != null)
            {
                List<string> regexKeys = FileHelper.ExtractRegex(fieldVal);
                foreach (string regexKey in regexKeys)
                {
                    int index = FileHelper.GetIndex(regexKey) ?? 0;
                    string regexKeyLowercase = regexKey.ToLower();
                    if (baseDataMap.Keys.Contains(regexKey))
                    {
                        fieldVal = Regex.Replace(fieldVal, "{{" + regexKey + "}}", baseDataMap[regexKey]);
                        continue;
                    }
                    if (dataMap.Keys.Contains(regexKeyLowercase + "[" + index + "]"))
                    {
                        fieldVal = Regex.Replace(fieldVal, Regex.Escape("{{" + regexKeyLowercase + "}}"), dataMap[regexKeyLowercase + "[" + index + "]"], options: RegexOptions.IgnoreCase);
                        continue;
                    }
                    if (dataMap.Keys.Contains(regexKeyLowercase))
                    {
                        fieldVal = Regex.Replace(fieldVal, Regex.Escape("{{" + regexKeyLowercase + "}}"), dataMap[regexKeyLowercase], RegexOptions.IgnoreCase);
                        continue;
                    }
                    if (regexKey.Equals("today"))
                    {
                        string newString = Regex.Replace(fieldVal, "{{" + regexKey + "}}", chelp.Today, RegexOptions.IgnoreCase);
                        fieldVal = newString;
                    }
                    if (regexKey.Equals("dbt_today"))
                    {
                        string newString = Regex.Replace(fieldVal, "{{" + regexKey + "}}", chelp.Dbt_Today, RegexOptions.IgnoreCase);
                        fieldVal = newString;
                    }
                    if (regexKey.Equals("dbt_yesterday"))
                    {
                        string newString = Regex.Replace(fieldVal, "{{" + regexKey + "}}", chelp.Dbt_Yesterday, RegexOptions.IgnoreCase);
                        fieldVal = newString;
                    }
                    if (regexKey.Equals("dbt_current"))
                    {
                        string newString = Regex.Replace(fieldVal, "{{" + regexKey + "}}", chelp.Dbt_Current, RegexOptions.IgnoreCase);
                        fieldVal = newString;
                    }
                    if (regexKey.Equals("dbt_today"))
                    {
                        string newString = Regex.Replace(fieldVal, "{{" + regexKey + "}}", chelp.Dbt_Today, RegexOptions.IgnoreCase);
                        fieldVal = newString;
                    }
                    if (regexKeyLowercase.StartsWith("today-"))
                    {
                        int day_offset = 0;
                        try { day_offset = int.Parse(regexKeyLowercase["today-".Length..]); }
                        catch { Console.WriteLine("Exception getting day offset from " + regexKeyLowercase + ". Using 0 days offset."); }
                        fieldVal = chelp.DaysAgo(day_offset);
                        continue;
                    }
                    if (regexKeyLowercase.StartsWith("today+"))
                    {
                        int day_offset = 0;
                        try { day_offset = int.Parse(regexKeyLowercase["today+".Length..]); }
                        catch { Console.WriteLine("Exception getting day offset from " + regexKeyLowercase + ". Using 0 days offset."); }
                        fieldVal = chelp.DaysFuture(day_offset);
                        continue;
                    }
                    if (regexKeyLowercase.StartsWith("dbt_today-"))
                    {
                        int day_offset = 0;
                        try { day_offset = int.Parse(regexKeyLowercase["dbt_today-".Length..]); }
                        catch { Console.WriteLine("Exception getting day offset from " + regexKeyLowercase + ". Using 0 days offset."); }
                        fieldVal = chelp.Dbt_DaysAgo(day_offset);
                        continue;
                    }
                    if (regexKeyLowercase.StartsWith("dbt_today+"))
                    {
                        int day_offset = 0;
                        try { day_offset = int.Parse(regexKeyLowercase["dbt_today+".Length..]); }
                        catch { Console.WriteLine("Exception getting day offset from " + regexKeyLowercase + ". Using 0 days offset."); }
                        fieldVal = chelp.Dbt_DaysFuture(day_offset);
                        continue;
                    }
                }
            }
            return fieldVal;
        }

        public void SetObjectPropertiesWithBaseData(object obj, Dictionary<string, dynamic> baseDataMap, Dictionary<string, dynamic> dataMap)
        {
            foreach (var fieldInfo in obj.GetType().GetFields())
            {
                if (fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    continue;
                }

                var fieldVal = SetFieldProperties(fieldInfo?.GetValue(obj)?.ToString(), baseDataMap, dataMap);
                //fieldInfo.SetValue(obj, Convert.ChangeType(fieldVal, fieldInfo.FieldType));

                Type t = Nullable.GetUnderlyingType(fieldInfo.FieldType) ?? fieldInfo.FieldType;
                object safeConversion = (fieldVal == null) ? null : Convert.ChangeType(fieldVal, t);
                fieldInfo.SetValue(obj, safeConversion);
                //fieldInfo.SetValue(obj, fieldVal);

            }
            scenarioContext["baseDataMap"] = baseDataMap;
            scenarioContext["dynamicDataMap"] = dataMap;
        }

        /// <summary>
        /// SetBaseDataMap - Resolve references in the baseDataMap itself
        /// BaseData can refer to: 
        ///  - today, today+days, today-days
        ///  - previously declared base data elements
        ///  - previously loaded data elements
        ///  New base data is combined with existing base data
        /// </summary>
        /// <param name="jsonObj">Base data from the test file</param>
        public void SetBaseDataMap(JObject jsonObj, bool replace = false)
        {
            Dictionary<string, dynamic> baseDataNew = jsonObj.ContainsKey("BaseData") ? JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonObj["BaseData"]?.ToString()) : new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> baseDataRaw = baseDataNew.ToDictionary(entry => entry.Key, entry => entry.Value);
            foreach (KeyValuePair<string, dynamic> ref_item in baseDataRaw)
            {
                string value = ref_item.Value.ToString();
                string new_value = SetFieldProperties(value, baseDataNew, dataMap);
                baseDataNew[ref_item.Key] = new_value;
            }
            if (replace)
                baseDataMap = baseDataNew;
            else
                foreach (KeyValuePair<string, dynamic> entry in baseDataNew)
                    if (!baseDataMap.TryAdd(entry.Key, entry.Value))
                        try
                        {
                            baseDataMap[entry.Key] = entry.Value;
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Failed to add item to baseDataMap - Key:" + entry.Key + ", Value:" + entry.Value, e);
                        }
            scenarioContext["baseDataMap"] = baseDataMap;
        }


        public JObject SetPropertiesWithBaseDataDynamic(JObject obj, Dictionary<string, dynamic> baseDataMap, Dictionary<string, dynamic> dataMap)
        {
            JObject withbasepropset = new JObject();
            foreach (KeyValuePair<string,JToken> fieldInfo in obj)
            {
                var fieldVal = SetFieldProperties(fieldInfo.Value?.ToString(), baseDataMap, dataMap);
                string boolSubstituted = fieldVal;
                boolSubstituted = boolSubstituted.Replace("False", "false");
                boolSubstituted = boolSubstituted.Replace("True", "true");
                //withbasepropset.Add(fieldInfo.Key, boolSubstituted);
                try { withbasepropset.Add(fieldInfo.Key, JObject.Parse(boolSubstituted)); }
                catch (JsonReaderException jre) { withbasepropset.Add(fieldInfo.Key, boolSubstituted); }
            }
            return withbasepropset;
        }

        private void UpdateDataMap(object obj, Dictionary<string, dynamic> dataMap, int index = 0)
        {
            foreach (var fieldValue in obj.GetType().GetFields())
            {
                dataMap[obj.GetType().Name.ToLower() + "." + fieldValue.Name.ToLower() + "[" + index + "]"] = fieldValue.GetValue(obj)?.ToString();
                scenarioContext["dynamicDataMap"] = dataMap;
            }
        }
        private void IncrementCount(string keyName, int incr=1)
        {
            if (dataMapCounts.ContainsKey(keyName))
                dataMapCounts[keyName] += incr;
            else
                dataMapCounts.Add(keyName, incr);
            scenarioContext["dynamicDataCounts"] = dataMapCounts;
        }
        private int GetCount(string keyName)
        {
            if (dataMapCounts.ContainsKey(keyName))
                return dataMapCounts[keyName];
            else
            {
                dataMapCounts.Add(keyName, 0);
                return 0;
            }
        }

        /// <summary>
        /// DataMapPop populates the provided object from the data map
        /// </summary>
        /// <param name="obj">The object to pop from the DataMap</param>
        /// <param name="obj_name">The object name</param>
        /// <returns>The most last object (of this type) added to the data map</returns>
        public Object DataMapPop(Object obj, string obj_name)
        {
            int index = GetCount(obj_name + "Count") - 1;
            // update scenario info
            foreach (var fieldValue in obj.GetType().GetFields())
                try
                {
                    string dataMapItem = obj_name + "." + fieldValue.Name.ToLower() + "[" + index + "]";
                    var value = dataMap[obj_name + "." + fieldValue.Name.ToLower() + "[" + index + "]"];
                    if (value != null)
                        fieldValue.SetValue(obj, value);
                }
                catch (Exception e)
                {
                    throw new Exception(obj_name + " not found in DataMap", e);
                }
            return obj;
        }

        /// <summary>
        /// Resets the dataMap to initial data (for use in scenarios) in the object and Specflow's ScenarioContext.
        /// This method is used when running multiple scenarios in a single ScenarioContext
        /// </summary>
        public void ResetDataMap(StaticDataMap static_data)
        {
            dataMap = new Dictionary<string, dynamic>(static_data.savedDataMap);
            scenarioContext["dynamicDataMap"] = dataMap;
            dataMapCounts = new Dictionary<string, int>(static_data.savedDataCounts);
            scenarioContext["dynamicDataCounts"] = dataMapCounts;
        }

        /// <summary>
        /// Adds to a dictionary called dataMap into Specflow's ScenarioContext.
        /// Fetches the current count of the object and starts indexing from there, else start from 0
        /// This method is used when we expect a list of objects to be parsed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objs"></param>
        /// <param name="keyName"></param>
        public void AddToDataMapListOfObjects<T>(List<T> objs, string keyName)
        {
            int currentCount = GetCount(keyName);
            for (int i = 0; i < objs?.Count; i++)
            {
                UpdateDataMap(objs[i], dataMap, currentCount+i);
                IncrementCount(keyName);
            }
        }
        /// <summary>
        /// Adds to a dictionary called dataMap into Specflow's ScenarioContext.
        /// Fetches the current count of the object and starts indexing from there, else start from 0
        /// This method is used when we expect a single object to be parsed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="keyName"></param>
        public void AddToDataMapSingleObject<T>(T obj, string keyName)
        {
            //if (!scenarioContext.ContainsKey(keyName)) scenarioContext.Set<int>(0, keyName);
            //int currentCount = scenarioContext.Get<int>(keyName);
            UpdateDataMap(obj, dataMap, GetCount(keyName));
            IncrementCount(keyName);
            //currentCount++;
            //scenarioContext.Set<int>(currentCount, keyName);
        }

        public void SetTestUniqueIntegrationId(object obj, string uid=null)
        {
            foreach (var fieldInfo in obj.GetType().GetFields())
            {
                if (fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    continue;
                }
                var fieldVal = fieldInfo.GetValue(obj);
                if (fieldVal != null)
                {
                    string stringVal = fieldVal.ToString();
                    stringVal = Regex.Replace(stringVal, "{{TestUniqueIntegrationId}}", uid ?? testId, RegexOptions.IgnoreCase);
                    Type t = Nullable.GetUnderlyingType(fieldInfo.FieldType) ?? fieldInfo.FieldType;
                    object safeConversion = (stringVal == null) ? null : Convert.ChangeType(stringVal, t);
                    fieldInfo.SetValue(obj, safeConversion);
                    //fieldInfo.SetValue(obj, Convert.ChangeType(stringVal, fieldInfo.FieldType));
                }
            }
        }

        public TestDefinition GetTests(string filePath, Scenario scenario = null)
        {
            //Scenario scenario = scenarioContext.ContainsKey("Scenario") ? scenarioContext.Get<Scenario>("Scenario") : null;
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
            foreach (Tuple<string, JObject> testdata in templates)
            {
                string scenarioname = testdata.Item1;
                JObject jsonObj = testdata.Item2;
                SetBaseDataMap(jsonObj);
                
                if (jsonObj.ContainsKey("Tests")) {

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

        public string AppendUIDToFieldIfDoesntExist(string fieldValue, string scenarioUid)
        {
            string finalFieldVal = fieldValue is null ? 
                null : fieldValue.Contains(scenarioUid) ? 
                fieldValue : fieldValue + scenarioUid;
            return finalFieldVal;
        }

    }
    public class UnmarshallerException : Exception
    {
        public UnmarshallerException(string message)
        : base(message)
        {
        }

        public UnmarshallerException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }


}
