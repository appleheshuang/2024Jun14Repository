using MainFramework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TechTalk.SpecFlow;
using OMREngineTest.Constructors.SalesforceObjects.OCEObjects;
using Optimizer.Constructors.SalesforceObjects.OCEObjects;

namespace OMREngineTest.ScenarioUtils.DataUnmarshallers
{
    public enum sf_obj_type { native, unique, oce, omr };

    public class SalesforceUnmarshaller : OptimizerUnmarshaller
    {
        public SalesforceUnmarshaller(ScenarioContext scenarioContext, string tconfig, string path=null) : base(scenarioContext, tconfig, datapath: path)
        {
        }

        public List<User> PostUsersFromFilePath(string filePath)
        {
            string[] files = GetFilesFromFilePath(filePath);

            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);

            List<User> usersToReturn = new List<User>();
            foreach (Tuple<string, JObject> testdata in templates)
            {
                string scenarioname = testdata.Item1;
                JObject jsonObj = testdata.Item2;

                SetBaseDataMap(jsonObj);

                var users = jsonObj["User"]?.ToObject<List<User>>();
                foreach (User user in users ?? Enumerable.Empty<User>())
                {
                    string scenarioUniqueIntegrationID = chelp.RandomString(5) + Convert.ToString(new Random().Next(10, 1000)) + DateTime.Now.ToString("yyyyMMdd");

                    SetObjectPropertiesWithBaseData(user, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(user, scenarioUniqueIntegrationID);
                    user.OMRegionId__c = user.OMRegionId__c == "" ? user.OMRegionId__c : sf.LookupValue("OMRegion__c", "Id", "SOMRegionId__c", user.OMRegionId__c);
                    if (!user.UserName__c.Contains(domain)) user.UserName__c = domain + "_" + user.UserName__c;
                    if (!user.UniqueIntegrationId__c.Contains(domain)) user.UniqueIntegrationId__c = domain + "_" + user.UniqueIntegrationId__c;
                    try
                    {
                        user.Id = sf.Post_Object("OMUser__c", user);
                        user.SOMUserId__c = sf.LookupValue("OMUser__c", "SOMUserId__c", "UniqueIntegrationId__c", user.UniqueIntegrationId__c);
                        //user.SOMUserId__c = sf.CheckRecordExists("User", user.UniqueIntegrationId__c, "SOMUserId__c");
                    }
                    catch
                    {
                        user.Id = sf.LookupValue("OMUser__c", "Id", "UniqueIntegrationId__c", user.UniqueIntegrationId__c);
                        user.SOMUserId__c = sf.LookupValue("OMUser__c", "SOMUserId__c", "UniqueIntegrationId__c", user.UniqueIntegrationId__c);

                        //user.SOMUserId__c = sf.CheckRecordExists("User", user.UniqueIntegrationId__c, "SOMUserId__c");
                    }
                    usersToReturn.Add(user);
                }
                AddToDataMapListOfObjects(users, "usersCount");
            }
            return usersToReturn;
        }
        /// <summary>
        /// POSTs SalesForce objects into the target org
        /// If the object is unique (has a unique UniqueIntegrationId) it will POST the object and adds it to the list of objects to return
        /// If the object already exists, an exception is thrown and caught and looks up the Id and adds the object to the list of objects to return
        /// </summary>
        /// <param name="type">native, unique, oce, or omr</param>
        /// <param name="objtype">Name of object - with or without namespace prefic and suffix</param>
        /// <param name="filePath"></param>
        /// <param name="methodType">POST, PATCH</param>
        /// <param name="StrId"></param>
        /// <returns></returns>
        public List<Object> PostSFObjectsFromFilePath(sf_obj_type type, string objtype, string filePath, string methodType, string StrId=null, string UniqueIdCol=null)
        {
            string id_column = UniqueIdCol ?? "UniqueIntegrationId__c";
            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);

            List<Object> objectsToReturn = new List<Object>();
            foreach (Tuple<string, JObject> testdata in templates)
            {
                JObject jsonObj = testdata.Item2;
                SetBaseDataMap(jsonObj);
                var sf_objects = jsonObj[objtype]?.ToObject<List<JObject>>();
                foreach (JObject objx in sf_objects ?? Enumerable.Empty<JObject>())
                {
                    JObject obj = PrepareObjectData(objx, objtype);
                    string objname = objtype;
                    switch (type)
                    {
                        case sf_obj_type.native:
                            try
                            {
                                obj.Add("Id", sf.Post_NativeObject(objtype, obj));
                            }
                            catch
                            {
                                JToken uid;
                                    if (obj.TryGetValue("OCE__UniqueIntegrationID__c", out uid))
                                        obj.Add("Id", sf.LookupValueNoSubs(objtype, "Id", "OCE__UniqueIntegrationID__c", uid.ToString()));
                            }                     
                            break;
                        case sf_obj_type.oce:
                            objname =
                                (objtype.StartsWith("OCE__") ? objtype.Substring("OCE__".Length) : objtype)
                                + (objtype.EndsWith("__c") ? "" : "__c");
                            switch (methodType)
                            {
                                case "post":
                                    try
                                    {
                                        obj.Add("Id", sf.Post_Object(objname, obj, "OCE__"));
                                    }
                                    catch
                                    {
                                        JToken uid;
                                        if (obj.TryGetValue(id_column, out uid))
                                            obj.Add("Id", sf.LookupValue(objtype, "Id", id_column, uid.ToString(), "OCE__"));
                                    }
                                    break;
                                case "patch":
                                    try
                                    {
                                        sf.Patch_OCE_Objects(objtype, obj, sf.LookupValueNoSubs(objtype, "Id", "OCE__UniqueIntegrationID__c", (obj.GetValue("OCE__UniqueIntegrationID__c").ToString())));
                                    }
                                    catch
                                    {
                                        throw new Exception("Record not Found For " + "OCE__UniqueIntegrationID__c :" + (obj.GetValue("OCE__UniqueIntegrationID__c").ToString()));
                                    }
                                    break;
                            }
                            break;
                        default:
                            objname =
                                (objtype.StartsWith("OM") ? "" : "OM")
                                + objtype
                                + (objtype.EndsWith("__c") ? "" : "__c");
                            obj = PostOMRObject(methodType.ToLower(), objname,obj, id_column);
                            break;
                    }
                    switch (objtype)
                    {
                        case "Region": objectsToReturn.Add(obj.ToObject<Region>()); break; ;
                        case "Account": objectsToReturn.Add(obj.ToObject<Account>()); break;
                        case "Product": objectsToReturn.Add(obj.ToObject<Product>()); break;
                        case "ProductHierarchy": objectsToReturn.Add(obj.ToObject<ProductHierarchy>()); break;
                        case "Geography": objectsToReturn.Add(obj.ToObject<Geography>()); break;
                        case "Topic": objectsToReturn.Add(obj.ToObject<TopicOCED>()); break;
                        case "Journey": objectsToReturn.Add(obj.ToObject<Journey>()); break;
                        case "JourneyTopic": objectsToReturn.Add(obj.ToObject<JourneyTopic>()); break;
                        case "JourneyProduct": objectsToReturn.Add(obj.ToObject<JourneyProduct>()); break;
                        case "JourneyActivity": objectsToReturn.Add(obj.ToObject<JourneyActivity>()); break;
                        case "OCE__Topic__c": objectsToReturn.Add(obj.ToObject<TopicOCEP>()); break;
                        case "CustomerAlignmentRule": objectsToReturn.Add(obj.ToObject<CustomerAlignmentRule>()); break;
                        case "Call": objectsToReturn.Add(obj.ToObject<Call>()); break;
                        case "Territory2": objectsToReturn.Add(obj.ToObject<Territory2>()); break;
                        case "TopicProduct": objectsToReturn.Add(obj.ToObject<TopicProduct>()); break;
                        case "LineOfBusiness": objectsToReturn.Add(obj.ToObject<LOB>()); break;
                        case "Opt": objectsToReturn.Add(obj.ToObject<Opt>()); break;
                        case "OptDetail": objectsToReturn.Add(obj.ToObject<OptDetail>()); break;
                        case "Address": objectsToReturn.Add(obj.ToObject<OCEAddress>()); break;
                        case "AccountAddress": objectsToReturn.Add(obj.ToObject<AccountAddress>()); break;
                        case "AccountTerritoryProduct": objectsToReturn.Add(obj.ToObject<AccountTerritoryProduct>()); break;
                        case "OCE__Product__c": objectsToReturn.Add(obj.ToObject<ProductOCEP>()); break;
                        case "SalesForce": objectsToReturn.Add(obj.ToObject<SalesForce>()); break;
                        case "MetricDefinition": objectsToReturn.Add(obj.ToObject<MetricDefinition>()); break;           
                        case "User": User this_usr = obj.ToObject<User>(); objectsToReturn.Add(this_usr); break;
                        case "UserAddress": objectsToReturn.Add(obj.ToObject<UserAddress>()); break;
                        case "OCE__PlanCycle__c":objectsToReturn.Add(obj.ToObject<PlanCycleOCEP>());break;
                        default: throw new UnmarshallerException("Salesforce object not supported: " + objtype);
                    }
                }
                AddToDataMapListOfObjects(objectsToReturn, objtype + "sCount");
            }
            return objectsToReturn;
        }

        /// <summary>
        /// Post or Patch an OMR object
        /// Called by PostSFObjectsFromFilePath
        /// If caled with post and the object already exists then lookup the id and patch instead
        /// Returns object with id and som id populated
        /// </summary>
        /// <param name="methodType">post,patch</param>
        /// <param name="objname">Salesforce object name, excl namespace</param>
        /// <param name="obj">Object representing the request body</param>
        /// <param name="id_column">The column that uniquely identifies that record</param>
        /// <returns>The original object with Id and SOM Ids populated</returns>
        JObject PostOMRObject(string methodType, string objname, JObject obj, string id_column) 
        {
            string som_id_clm = "S" + objname.Replace("__c", "Id__c");
            try
            {
                switch (methodType)
                {
                    case "post": 
                        obj.Add("Id", sf.Post_Object(objname, obj));
                        obj.TryAdd(som_id_clm, sf.LookupValue(objname, som_id_clm, "Id", obj.GetValue("Id").ToString()));
                        break;
                    case "patch": 
                        sf.Patch_Object(objname, obj, sf.LookupValue(objname, "Id", id_column, (obj.GetValue(id_column).ToString()))); 
                        break;
                }
            }
            catch (Exception e)
            {
                JToken uid;
                if (obj.TryGetValue(id_column, out uid))
                    switch (methodType)
                    {
                        case "post":
                            string obj_id = sf.LookupValue(objname, "Id", id_column, uid.ToString());
                            if (e.Message.ToLower().Contains("duplicate value found"))
                                sf.Patch_Object(objname, obj, obj_id);
                            obj.Add("Id", obj_id);
                            obj.TryAdd(som_id_clm, sf.LookupValue(objname, som_id_clm, "Id", obj_id));
                            break;
                        case "patch":
                            sf.Patch_Object(objname, obj, sf.LookupValue(objname, "Id", id_column, uid.ToString()));
                            break;
                    }
            }
            return obj;
        }

        JObject PrepareObjectData(JObject sfobj, string sfobjtype)
        {
            // Replace Test Uid
            string new_data = Regex.Replace(sfobj.ToString(), "{{TestUniqueIntegrationId}}", testId, RegexOptions.IgnoreCase);
            sfobj = JObject.Parse(new_data); 
            
            // Set Object properties
            sfobj = SetPropertiesWithBaseDataDynamic(sfobj, baseDataMap, dataMap);

            // Lookup OMRegionId
            if (sfobjtype != "Region") DecodeRegionId(sfobj);
            
            // Object type specific things
            switch (sfobjtype)
            {
                case "Account": sfobj.Add("RecordTypeId", sf.LookupValue("RecordType", "Id", "DeveloperName", "BC")); break;
            }
            return sfobj;
        }
        public string DecodeRegionId(JObject sfobj)
        {
            string region_key = "OMRegionId__c";
            if (sfobj.ContainsKey(region_key))
            {
                string som_region_id = sfobj.GetValue(region_key).ToString();
                if (som_region_id != "")
                {
                    string decoded_region_id = sf.LookupValue("OMRegion__c", "Id", "SOMRegionId__c", som_region_id);
                    if (decoded_region_id.StartsWith("Not found")) throw new UnmarshallerException("Region not found with SOMRegionId=" + som_region_id);
                    else sfobj[region_key] = decoded_region_id;
                }
                return sfobj.GetValue(region_key).ToString();
            }
            return region_key + " not found in object";
        }

    }
}
