using MainFramework.GlobalUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TechTalk.SpecFlow;
using OMREngineTest.ScenarioUtils.DataUnmarshallers;

namespace OMREngineTest.ScenarioUtils
{
    public enum ScenarioExecutionType { commit, calculate };

    public struct ScenarioRunParams
    {
        public string jobtype;
        public string jobparams;
        public string name;
        public JObject preload;
        public ScenarioRunParams(string filepath)
        {
            var sr = new StringReader(File.ReadAllText(filepath));
            using var reader = new JsonTextReader(sr);
            reader.DateParseHandling = DateParseHandling.None;
            JObject testfiledata = JObject.Load(reader);
            this = (ScenarioRunParams)testfiledata["Configs"]?.ToObject<ScenarioRunParams>();
        }
        public Dictionary<string,string> FilesToLoadToSf()
        {
            string jsonstr = preload.ToString();
            return JsonConvert.DeserializeObject<Dictionary<string,string>>(preload.ToString());
        }

    }

    public class ScenarioJSONUnmarshaller : OptimizerUnmarshaller
    {
        public Scenario scenario;
        public List<ScenarioGeographyTerritory> scenarioGeographyTerritories;
        private List<ScenarioModel> models;

        public ScenarioJSONUnmarshaller(ScenarioContext scenarioContext, string tconfig = "smoketest-org.json", string sftoken=null, string testdatapath = null) : base(scenarioContext, tconfig, sftoken:sftoken, datapath: testdatapath) 
        {
        }

        public List<ScenarioConfig> PostScenarioObjectsFromDir(string dirpath,StaticDataMap staticdata)
        {
            string[] files = GetFilesFromDirPath(dirpath);
            List<ScenarioConfig> scenarios = new List<ScenarioConfig> { };
            foreach (string file in files)
            {
                // Read config for the scenario
                string filename = Path.GetFileName(file);
                string filepath = Path.Combine(dirpath, filename);
                ScenarioRunParams scenario_config = new ScenarioRunParams(GetFilesFromFilePath(filepath)[0]);
                // Add the scenario to the list
                ResetTestId();
                ResetDataMap(staticdata);
                ScenarioConfig this_scenario = new ScenarioConfig(PostScenarioObjectsFromFile(filepath, scenario_config.name), scenario_config);
                scenarios.Add(this_scenario);
            }
            return scenarios;
        }

        /// <summary>
        /// Add scenario actions to an existing scenario from multiple files
        /// </summary>
        /// <param name="dirpath"></param>
        public void AddScenarioObjectsFromDir(string dirpath)
        {
            string[] files = GetFilesFromDirPath(dirpath);
            foreach (string file in files)
            {
                // Read config for the scenario
                string filename = Path.GetFileName(file);
                string filepath = Path.Combine(dirpath, filename);
                PostScenarioObjectsFromFile(filepath);
            }
        }

        public Scenario PostScenarioObjectsFromFile(string filepath, string setname="")
        {
            string[] files = GetFilesFromFilePath(filepath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files, setname);
            foreach (Tuple<string, JObject> testdata in templates)
            {
                string scenarioName = testdata.Item1;
                JObject jsonObj = testdata.Item2;
                string scenarioUniqueIntegrationId = testId;

                SetBaseDataMap(jsonObj);

                ScenarioRunParams? scenario_configs = jsonObj["Configs"]?.ToObject<ScenarioRunParams>();

                // Post data into SFDC before running the scenario, if provided in scenario config
                if (!(scenario_configs?.preload == null || scenario_configs?.preload.Count == 0))
                {
                    SalesforceUnmarshaller sfUnmarshaller = new SalesforceUnmarshaller(scenarioContext, tenantconfigfile);
                    sfUnmarshaller.SetTestId(testId);
                    foreach (KeyValuePair<string, string> load_this in scenario_configs?.FilesToLoadToSf())
                    {
                        string preload_path = GetFilesFromFilePath(load_this.Value)[0];
                        List<Object> objects = sfUnmarshaller.PostSFObjectsFromFilePath(sf_obj_type.unique, load_this.Key, preload_path, "post"); //Post salesforce object
                        AddToDataMapListOfObjects(objects, load_this.Key+"Count");
                    }
                }
                
                Scenario scenario;
                // Scenario node can be missing if we're adding to an existing scenario
                if (jsonObj.ContainsKey("Scenario")) 
                {
                    scenario = jsonObj["Scenario"]?.ToObject<Scenario>();
                    SetObjectPropertiesWithBaseData(scenario, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(scenario, scenarioUniqueIntegrationId);
                    scenario.SetFieldsToUniqueIntegrationId(scenarioName, scenarioUniqueIntegrationId);
                    string som_region_id = scenario.OMRegionId__c;
                    if (som_region_id != "" && som_region_id != null) {
                        scenario.OMRegionId__c = sf.LookupValue("OMRegion__c", "Id", "SOMRegionId__c", som_region_id);
                        scenario.SOMRegionId__c = som_region_id;
                    }

                    try
                    {
                        scenario.Id = sf.Post_Object("OMScenario__c", scenario);
                    }
                    catch (Exception e)
                    {
                        scenario.OMRegionId__c = sf.CheckRecordExists("Region", som_region_id, "Id", "SOMRegionId__c", scenarioid: scenario.Id);
                        if (scenario.OMRegionId__c != SalesForceDataCollector.not_found)
                            scenario.Id = sf.Post_Object("OMScenario__c", scenario);
                        else
                            throw e;
                    }

                    //Starts at index 0 and increments by one per scenario chained.
                    AddToDataMapSingleObject(scenario, "scenarioCount");
                }
                else
                {
                    try
                    {
                        scenario = (Scenario)DataMapPop(new Scenario(),"scenario");
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Scenario not added in json; not found in existing DataMap",e);
                    }
                }

                var models = jsonObj["ScenarioModel"]?.ToObject<List<ScenarioModel>>();
                foreach (ScenarioModel model in models ?? Enumerable.Empty<ScenarioModel>())
                {
                    SetObjectPropertiesWithBaseData(model, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(model, scenarioUniqueIntegrationId);
                    model.OMScenarioId__c ??= scenario.Id;
                    model.Name = AppendUIDToFieldIfDoesntExist(model.Name, scenarioUniqueIntegrationId);
                    model.DistanceContext__c ??= "TERR";
                    model.DrivingUnit__c ??= "KM";
                    model.Id = sf.Post_Object("OMScenarioModel__c", model);
                }

                AddToDataMapListOfObjects(models, "modelCount");
                this.models = models;

                var modelTerritories = jsonObj["ScenarioModelTerritory"]?.ToObject<List<ScenarioModelTerritory>>();
                foreach (ScenarioModelTerritory modelTerritory in modelTerritories ?? Enumerable.Empty<ScenarioModelTerritory>())
                {
                    SetObjectPropertiesWithBaseData(modelTerritory, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(modelTerritory, scenarioUniqueIntegrationId);
                    modelTerritory.OMScenarioId__c ??= scenario.Id;
                    modelTerritory.Name = AppendUIDToFieldIfDoesntExist(modelTerritory.Name, scenarioUniqueIntegrationId);
                    modelTerritory.Id = sf.Post_Object("OMScenarioModelTerritory__c", modelTerritory);
                }
                AddToDataMapListOfObjects(modelTerritories, "modelTerritoryCount");

                var salesForces = jsonObj["ScenarioSalesForce"]?.ToObject<List<ScenarioSalesForce>>();
                foreach (ScenarioSalesForce salesForce in salesForces ?? Enumerable.Empty<ScenarioSalesForce>())
                {
                    SetObjectPropertiesWithBaseData(salesForce, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(salesForce, scenarioUniqueIntegrationId);
                    salesForce.OMScenarioId__c ??= scenario.Id;
                    if (salesForce.Action__c == "ADD")
                    {
                        salesForce.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(salesForce.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                        salesForce.Id = sf.Post_Object("OMScenarioSalesForce__c", salesForce);
                        salesForce.SOMSalesForceId__c = sf.ScenarioSOMId("OMScenarioSalesForce__c", salesForce.Id);
                        //salesForce.SOMSalesForceId__c = sf.CheckRecordExists("SalesForce", salesForce.UniqueIntegrationId__c, "SOMSalesForceId__c");
                    }
                    else
                    {
                        try
                        {
                            salesForce.SOMSalesForceId__c = salesForce.SOMSalesForceId__c ?? sf.LookupValue("OMSalesForce__c", "SOMSalesForceId__c", "UniqueIntegrationId__c", salesForce.UniqueIntegrationId__c);
                            salesForce.Id = sf.Post_Object("OMScenarioSalesForce__c", salesForce);
                        }
                        catch (Exception e)
                        {
                            salesForce.SOMSalesForceId__c = sf.CheckRecordExists("OMSalesForce__c", salesForce.SOMSalesForceId__c, "SOMSalesForceId__c", "SOMSalesForceId__c");
                            if (salesForce.SOMSalesForceId__c != SalesForceDataCollector.not_found && salesForce.SOMSalesForceId__c != null)
                                salesForce.Id = sf.Post_Object("OMScenarioSalesForce__c", salesForce);
                            else
                                throw e;
                        }
                    }
                }
                AddToDataMapListOfObjects(salesForces, "salesforceCount");

                var salesforceHierarchies = jsonObj["ScenarioSalesForceHierarchy"]?.ToObject<List<ScenarioSalesForceHierarchy>>();
                foreach (ScenarioSalesForceHierarchy salesForceHierarchy in salesforceHierarchies ?? Enumerable.Empty<ScenarioSalesForceHierarchy>())
                {
                    SetObjectPropertiesWithBaseData(salesForceHierarchy, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(salesForceHierarchy, scenarioUniqueIntegrationId);
                    salesForceHierarchy.OMScenarioId__c ??= scenario.Id;
                    if (salesForceHierarchy.Action__c == "ADD")
                    {
                        salesForceHierarchy.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(salesForceHierarchy.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    salesForceHierarchy.Id = sf.Post_Object("OMScenarioSalesForceHierarchy__c", salesForceHierarchy);
                    salesForceHierarchy.SOMSalesForceHierarchyId__c = sf.ScenarioSOMId("OMScenarioSalesForceHierarchy__c", salesForceHierarchy.Id);
                    //salesForceHierarchy.SOMParentSalesForceId__c = sf.LookupValue("OMScenarioSalesForceHierarchy__c", "SOMParentSalesForceId__c", "Id", salesForceHierarchy.Id);
                    //salesForceHierarchy.SOMChildSalesForceId__c = sf.LookupValue("OMScenarioSalesForceHierarchy__c", "SOMChildSalesForceId__c", "Id", salesForceHierarchy.Id);
                }
                AddToDataMapListOfObjects(salesforceHierarchies, "salesForceHierarchyCount");

                var geographies = jsonObj["Geography"]?.ToObject<List<Geography>>();
                foreach (Geography geography in geographies ?? Enumerable.Empty<Geography>())
                {
                    SetObjectPropertiesWithBaseData(geography, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(geography, scenarioUniqueIntegrationId);
                    geography.UniqueIntegrationId__c = geography.UniqueIntegrationId__c.Contains(scenarioUniqueIntegrationId)
                        ? geography.UniqueIntegrationId__c : geography.UniqueIntegrationId__c + scenarioUniqueIntegrationId;
                    geography.OMRegionId__c = scenario.OMRegionId__c;
                    geography.Id = sf.Post_Object("OMGeography__c", geography);
                    //geography.SOMGeographyId__c = sf.LookupValue("OMGeography__c", "SOMGeographyId__c", "Id", geography.Id);
                    geography.SOMGeographyId__c = sf.CheckRecordExists("Geography", geography.UniqueIntegrationId__c, "SOMGeographyId__c", scenarioid: scenario.Id);

                    //AddToDataMapSingleObject(geography, "geographyCount");
                }
                AddToDataMapListOfObjects(geographies, "geographyCount");

                //ToDo POST region
                var regions = jsonObj["Region"]?.ToObject<List<Region>>();
                foreach (Region region in regions ?? Enumerable.Empty<Region>())
                {
                    SetObjectPropertiesWithBaseData(region, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(region, scenarioUniqueIntegrationId);
                    AddToDataMapSingleObject(region, "regionCount");
                }


                var territories = jsonObj["ScenarioTerritory"]?.ToObject<List<ScenarioTerritory>>();
                foreach (ScenarioTerritory territory in territories ?? Enumerable.Empty<ScenarioTerritory>())
                {
                    SetObjectPropertiesWithBaseData(territory, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(territory, scenarioUniqueIntegrationId);
                    territory.OMScenarioId__c ??= scenario.Id;
                    if (territory.Action__c == "ADD")
                    {
                        territory.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(territory.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                        //if (tenant.Config.OceMode && !territory.Name.Contains(scenarioUniqueIntegrationId)) changed for OMR-11574
                        territory.Name = AppendUIDToFieldIfDoesntExist(territory.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    try
                    {
                        territory.Id = sf.Post_Object("OMScenarioTerritory__c", territory);
                    }
                    catch (Exception e)
                    {
                        territory.SOMSalesForceId__c = sf.CheckRecordExists("OMSalesForce__c", territory.SalesForceUniqueIntegrationId__c, "SOMSalesForceId__c");
                        bool data_found = territory.SOMSalesForceId__c != SalesForceDataCollector.not_found;
                        if (territory.Action__c != "ADD")
                        {
                            territory.SOMTerritoryId__c= sf.CheckRecordExists("OMTerritory__c", territory.UniqueIntegrationId__c, "SOMTerritoryId__c");
                            //territory.SOMTerritoryId__c = sf.CheckRecordExists("Territory", territory.UniqueIntegrationId__c, "SOMTerritoryId__c");
                            data_found = data_found && territory.SOMTerritoryId__c != SalesForceDataCollector.not_found;
                        }
                        if (data_found)
                            territory.Id = sf.Post_Object("OMScenarioTerritory__c", territory);
                        else
                            throw e;
                    }
                    territory.SOMTerritoryId__c = sf.ScenarioSOMId("OMScenarioTerritory__c", territory.Id);
                    //territory.SOMSalesForceId__c = sf.LookupValue("OMScenarioTerritory__c", "SOMSalesForceId__c", "Id", territory.Id);
                    //territory.SOMRegionId__c = sf.LookupValue("OMScenarioTerritory__c", "SOMRegionId__c", "Id", territory.Id);
                }
                AddToDataMapListOfObjects(territories, "territoryCount");


                var territoryHierarchies = jsonObj["ScenarioTerritoryHierarchy"]?.ToObject<List<ScenarioTerritoryHierarchy>>();
                foreach (ScenarioTerritoryHierarchy territoryHierarchy in territoryHierarchies ?? Enumerable.Empty<ScenarioTerritoryHierarchy>())
                {
                    SetObjectPropertiesWithBaseData(territoryHierarchy, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(territoryHierarchy, scenarioUniqueIntegrationId);
                    territoryHierarchy.OMScenarioId__c ??= scenario.Id;
                    if (territoryHierarchy.Action__c == "ADD")
                    {
                        territoryHierarchy.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(territoryHierarchy.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    territoryHierarchy.Id = sf.Post_Object("OMScenarioTerritoryHierarchy__c", territoryHierarchy);
                    territoryHierarchy.SOMTerritoryHierarchyId__c = sf.ScenarioSOMId("OMScenarioTerritoryHierarchy__c", territoryHierarchy.Id);
                    //territoryHierarchy.SOMParentTerritoryId__c = sf.LookupValue("OMScenarioTerritoryHierarchy__c", "SOMParentTerritoryId__c", "Id", territoryHierarchy.Id);
                    //territoryHierarchy.SOMChildTerritoryId__c = sf.LookupValue("OMScenarioTerritoryHierarchy__c", "SOMChildTerritoryId__c", "Id", territoryHierarchy.Id);
                }
                AddToDataMapListOfObjects(territoryHierarchies, "territoryHierarchyCount");


                var geographyTerritories = jsonObj["ScenarioGeographyTerritory"]?.ToObject<List<ScenarioGeographyTerritory>>();
                foreach (ScenarioGeographyTerritory geographyTerritory in geographyTerritories ?? Enumerable.Empty<ScenarioGeographyTerritory>())
                {
                    SetObjectPropertiesWithBaseData(geographyTerritory, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(geographyTerritory, scenarioUniqueIntegrationId);
                    geographyTerritory.OMScenarioId__c ??= scenario.Id;
                    if (geographyTerritory.Action__c == "ADD")
                    {
                        geographyTerritory.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(geographyTerritory.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    try
                    {
                        geographyTerritory.Id = sf.Post_Object("OMScenarioGeographyTerritory__c", geographyTerritory);
                    }
                    catch (Exception e)
                    {
                        if (geographyTerritory.SOMTerritoryId__c == null || geographyTerritory.SOMTerritoryId__c == "")
                            geographyTerritory.SOMTerritoryId__c = sf.CheckRecordExists("Territory", geographyTerritory.TerritoryUniqueIntegrationId__c, scenarioid: scenario.Id);
                        else
                            sf.CheckRecordExists("Territory", geographyTerritory.SOMTerritoryId__c, known_column: "SOMTerritoryId__c", scenarioid: scenario.Id);
                        if (geographyTerritory.SOMTerritoryId__c != SalesForceDataCollector.not_found)
                            geographyTerritory.Id = sf.Post_Object("OMScenarioGeographyTerritory__c", geographyTerritory);
                        else
                            throw e;
                    }
                    geographyTerritory.SOMGeographyTerritoryId__c = sf.ScenarioSOMId("OMScenarioGeographyTerritory__c", geographyTerritory.Id);
                    //geographyTerritory.SOMTerritoryId__c = sf.LookupValue("OMScenarioGeographyTerritory__c", "SOMTerritoryId__c", "Id", geographyTerritory.Id);
                    //geographyTerritory.SOMGeographyId__c = sf.LookupValue("OMScenarioGeographyTerritory__c", "SOMGeographyId__c", "Id", geographyTerritory.Id);
                }
                AddToDataMapListOfObjects(geographyTerritories, "geographyTerritoryCount");



                var accountExplicits = jsonObj["ScenarioAccountExplicit"]?.ToObject<List<ScenarioAccountExplicit>>();
                foreach (ScenarioAccountExplicit accountExplicit in accountExplicits ?? Enumerable.Empty<ScenarioAccountExplicit>())
                {
                    SetObjectPropertiesWithBaseData(accountExplicit, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(accountExplicit, scenarioUniqueIntegrationId);
                    accountExplicit.OMScenarioId__c ??= scenario.Id;
                    if (accountExplicit.Action__c == "ADD")
                    {
                        accountExplicit.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(accountExplicit.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    try
                    {
                        accountExplicit.Id = sf.Post_Object("OMScenarioAccountExplicit__c", accountExplicit);
                    }
                    catch (Exception e)
                    {
                        accountExplicit.SOMTerritoryId__c = sf.CheckRecordExists("OMTerritory__c", accountExplicit.TerritoryUniqueIntegrationId__c, "SOMTerritoryId__c");
                        if (accountExplicit.SOMTerritoryId__c != SalesForceDataCollector.not_found)
                            accountExplicit.Id = sf.Post_Object("OMScenarioAccountExplicit__c", accountExplicit);
                        else
                            throw e;
                    }
                    accountExplicit.SOMAccountExplicitId__c = sf.ScenarioSOMId("OMScenarioAccountExplicit__c", accountExplicit.Id);
                    accountExplicit.SOMTerritoryId__c = sf.LookupValue("OMScenarioAccountExplicit__c", "SOMTerritoryId__c", "Id", accountExplicit.Id);
                }
                AddToDataMapListOfObjects(accountExplicits, "accountExplicitCount");


                var accountExclusions = jsonObj["ScenarioAccountExclusion"]?.ToObject<List<ScenarioAccountExclusion>>();
                foreach (ScenarioAccountExclusion accountExclusion in accountExclusions ?? Enumerable.Empty<ScenarioAccountExclusion>())
                {
                    SetObjectPropertiesWithBaseData(accountExclusion, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(accountExclusion, scenarioUniqueIntegrationId);
                    accountExclusion.OMScenarioId__c ??= scenario.Id;
                    if (accountExclusion.Action__c == "ADD")
                    {
                        accountExclusion.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(accountExclusion.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    try
                    {
                        accountExclusion.Id = sf.Post_Object("OMScenarioAccountExclusion__c", accountExclusion);
                    }
                    catch (Exception e)
                    {
                        accountExclusion.SOMTerritoryId__c = sf.CheckRecordExists("OMTerritory__c", accountExclusion.TerritoryUniqueIntegrationId__c);
                        if (accountExclusion.SOMTerritoryId__c != SalesForceDataCollector.not_found)
                            accountExclusion.Id = sf.Post_Object("OMScenarioAccountExclusion__c", accountExclusion);
                        else
                            throw e;
                    }
                    if (accountExclusion.Action__c == "ADD")
                    {
                        accountExclusion.SOMAccountExclusionId__c = sf.ScenarioSOMId("OMScenarioAccountExclusion__c", accountExclusion.Id);
                        accountExclusion.SOMTerritoryId__c = sf.LookupValue("OMScenarioAccountExclusion__c", "SOMTerritoryId__c", "Id", accountExclusion.Id);
                        accountExclusion.SOMSalesForceId__c = sf.LookupValue("OMScenarioAccountExclusion__c", "SOMSalesForceId__c", "Id", accountExclusion.Id);
                    }
                }
                AddToDataMapListOfObjects(accountExclusions, "accountExclusionCount");


                var accountSalesForceExclusions = jsonObj["ScenarioAccountSalesForceExclusion"]?.ToObject<List<ScenarioAccountSalesForceExclusion>>();
                foreach (ScenarioAccountSalesForceExclusion accountSalesForceExclusion in accountSalesForceExclusions ?? Enumerable.Empty<ScenarioAccountSalesForceExclusion>())
                {
                    SetObjectPropertiesWithBaseData(accountSalesForceExclusion, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(accountSalesForceExclusion, scenarioUniqueIntegrationId);
                    accountSalesForceExclusion.OMScenarioId__c ??= scenario.Id;
                    if (accountSalesForceExclusion.Action__c == "ADD")
                    {
                        accountSalesForceExclusion.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(accountSalesForceExclusion.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    accountSalesForceExclusion.Id = sf.Post_Object("OMScenarioAccountSalesForceExclusion__c", accountSalesForceExclusion);
                    accountSalesForceExclusion.SOMAccountExclusionId__c = sf.LookupValue("OMScenarioAccountSalesForceExclusion__c", "SOMAccountExclusionId__c", "Id", accountSalesForceExclusion.Id);
                    // accountSalesForceExclusion.SOMAccountExclusionId__c = sf.ScenarioSOMId("OMScenarioAccountExclusion__c", accountSalesForceExclusion.Id);
                }
                AddToDataMapListOfObjects(accountSalesForceExclusions, "accountSalesForceExclusionCount");

                //only input table, after scenario is simulated the result (if any) is to be seen in OMEngagementResult, OMEngagementPlanExplicit, OMAccountExplicit, OMAcountExclusion tables
                var scenarioChangeRequests = jsonObj["ScenarioChangeRequest"]?.ToObject<List<ScenarioChangeRequest>>();
                foreach (ScenarioChangeRequest scenarioChangeRequest in scenarioChangeRequests ?? Enumerable.Empty<ScenarioChangeRequest>())
                {

                    SetObjectPropertiesWithBaseData(scenarioChangeRequest, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(scenarioChangeRequest, scenarioUniqueIntegrationId);

                    scenarioChangeRequest.OMScenarioId__c ??= scenario.Id;
                    scenarioChangeRequest.Id = sf.Post_Object("OMScenarioChangeRequest__c", scenarioChangeRequest);

                }
                AddToDataMapListOfObjects(scenarioChangeRequests, "scenarioChangeRequestCount");

                var productExplicits = jsonObj["ScenarioProductExplicit"]?.ToObject<List<ScenarioProductExplicit>>();
                foreach (ScenarioProductExplicit productExplicit in productExplicits ?? Enumerable.Empty<ScenarioProductExplicit>())
                {
                    SetObjectPropertiesWithBaseData(productExplicit, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(productExplicit, scenarioUniqueIntegrationId);
                    productExplicit.OMScenarioId__c ??= scenario.Id;
                    if (productExplicit.Action__c == "ADD")
                    {
                        productExplicit.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(productExplicit.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    try
                    {
                        productExplicit.Id = sf.Post_Object("OMScenarioProductExplicit__c", productExplicit);
                    }
                    catch (Exception e)
                    {
                        productExplicit.SOMTerritoryId__c = sf.CheckRecordExists("Territory", productExplicit.TerritoryUniqueIntegrationId__c, scenarioid: scenario.Id);
                        if (productExplicit.SOMTerritoryId__c != SalesForceDataCollector.not_found)
                            productExplicit.Id = sf.Post_Object("OMScenarioProductExplicit__c", productExplicit);
                        else
                            throw e;
                    }

                    if (productExplicit.Action__c == "ADD")
                    {
                        productExplicit.SOMProductExplicitId__c = sf.ScenarioSOMId("OMScenarioProductExplicit__c", productExplicit.Id);
                        productExplicit.SOMProductId__c = sf.LookupValue("OMScenarioProductExplicit__c", "SOMProductId__c", "Id", productExplicit.Id);
                        productExplicit.SOMTerritoryId__c = sf.LookupValue("OMScenarioProductExplicit__c", "SOMTerritoryId__c", "Id", productExplicit.Id);
                    }
                }
                AddToDataMapListOfObjects(productExplicits, "productExplicitCount");


                var productExclusions = jsonObj["ScenarioProductExclusion"]?.ToObject<List<ScenarioProductExclusion>>();
                foreach (ScenarioProductExclusion productExclusion in productExclusions ?? Enumerable.Empty<ScenarioProductExclusion>())
                {
                    SetObjectPropertiesWithBaseData(productExclusion, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(productExclusion, scenarioUniqueIntegrationId);
                    productExclusion.OMScenarioId__c ??= scenario.Id;
                    if (productExclusion.Action__c == "ADD")
                    {
                        productExclusion.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(productExclusion.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    try
                    {
                        productExclusion.Id = sf.Post_Object("OMScenarioProductExclusion__c", productExclusion);
                    }
                    catch (Exception e)
                    {
                        productExclusion.SOMTerritoryId__c = sf.CheckRecordExists("Territory", productExclusion.TerritoryUniqueIntegrationId__c, scenarioid: scenario.Id);
                        if (productExclusion.SOMTerritoryId__c != SalesForceDataCollector.not_found) 
                            productExclusion.Id = sf.Post_Object("OMScenarioProductExclusion__c", productExclusion);
                        else
                            throw e;
                    }
                    productExclusion.SOMProductExclusionId__c = sf.ScenarioSOMId("OMScenarioProductExclusion__c", productExclusion.Id);
                }
                AddToDataMapListOfObjects(productExclusions, "productExclusionCount");


                var productSalesForces = jsonObj["ScenarioProductSalesForce"]?.ToObject<List<ScenarioProductSalesForce>>();
                foreach (ScenarioProductSalesForce productSalesForce in productSalesForces ?? Enumerable.Empty<ScenarioProductSalesForce>())
                {
                    SetObjectPropertiesWithBaseData(productSalesForce, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(productSalesForce, scenarioUniqueIntegrationId);
                    productSalesForce.OMScenarioId__c ??= scenario.Id;
                    if (productSalesForce.Action__c == "ADD")
                    {
                        productSalesForce.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(productSalesForce.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    productSalesForce.Id = sf.Post_Object("OMScenarioProductSalesForce__c", productSalesForce);
                    productSalesForce.SOMProductSalesForceId__c = sf.ScenarioSOMId("OMScenarioProductSalesForce__c", productSalesForce.Id);
                    //productSalesForce.SOMSalesForceId__c = sf.ScenarioSOMId("OMScenarioSalesForce__c", productSalesForce.SalesForceUniqueIntegrationId__c, "UniqueIntegrationId__c");
                    //productSalesForce.SOMProductId__c = sf.ScenarioSOMId("OMScenarioProduct__c", productSalesForce.ProductUniqueIntegrationId__c, "UniqueIntegrationId__c");
                }
                AddToDataMapListOfObjects(productSalesForces, "productSalesForceCount");


                var accountProductExplicits = jsonObj["ScenarioAccountProductExplicit"]?.ToObject<List<ScenarioAccountProductExplicit>>();
                foreach (ScenarioAccountProductExplicit accountProductExplicit in accountProductExplicits ?? Enumerable.Empty<ScenarioAccountProductExplicit>())
                {
                    SetObjectPropertiesWithBaseData(accountProductExplicit, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(accountProductExplicit, scenarioUniqueIntegrationId);
                    accountProductExplicit.OMScenarioId__c ??= scenario.Id;
                    if (accountProductExplicit.Action__c == "ADD")
                    {
                        accountProductExplicit.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(accountProductExplicit.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    accountProductExplicit.Id = sf.Post_Object("OMScenarioAccountProductExplicit__c", accountProductExplicit);
                    accountProductExplicit.SOMAccountProductExplicitId__c = sf.ScenarioSOMId("OMScenarioAccountProductExplicit__c", accountProductExplicit.Id);
                }
                AddToDataMapListOfObjects(accountProductExplicits, "accountProductExplicitCount");


                var accountProductTerrExplicits = jsonObj["ScenarioAccountProductTerrExplicit"]?.ToObject<List<ScenarioAccountProductTerrExplicit>>();
                foreach (ScenarioAccountProductTerrExplicit accountProductTerrExplicit in accountProductTerrExplicits ?? Enumerable.Empty<ScenarioAccountProductTerrExplicit>())
                {
                    SetObjectPropertiesWithBaseData(accountProductTerrExplicit, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(accountProductTerrExplicit, scenarioUniqueIntegrationId);
                    accountProductTerrExplicit.OMScenarioId__c ??= scenario.Id;
                    if (accountProductTerrExplicit.Action__c == "ADD")
                    {
                        accountProductTerrExplicit.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(accountProductTerrExplicit.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    accountProductTerrExplicit.Id = sf.Post_Object("OMScenarioAccountProductTerrExplicit__c", accountProductTerrExplicit);
                    accountProductTerrExplicit.SOMAccountProductTerritoryExplicitId__c = sf.LookupValue("OMScenarioAccountProductTerrExplicit__c", "SOMAccountProductTerritoryExplicitId__c", "Id", accountProductTerrExplicit.Id);
                }
                AddToDataMapListOfObjects(accountProductTerrExplicits, "accountProductTerrExplicitCount");

                var scenarioProducts = jsonObj["ScenarioProduct"]?.ToObject<List<ScenarioProduct>>();
                foreach (ScenarioProduct scenarioProduct in scenarioProducts ?? Enumerable.Empty<ScenarioProduct>())
                {
                    SetObjectPropertiesWithBaseData(scenarioProduct, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(scenarioProduct, scenarioUniqueIntegrationId);
                    scenarioProduct.OMScenarioId__c ??= scenario.Id;
                    if (scenarioProduct.Action__c == "ADD")
                    {
                        scenarioProduct.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(scenarioProduct.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    scenarioProduct.Id = sf.Post_Object("OMScenarioProduct__c", scenarioProduct);
                }
                AddToDataMapListOfObjects(scenarioProducts, "scenarioProductCount");


                var rules = jsonObj["ScenarioRule"]?.ToObject<List<ScenarioRule>>();
                foreach (ScenarioRule rule in rules ?? Enumerable.Empty<ScenarioRule>())
                {
                    SetObjectPropertiesWithBaseData(rule, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(rule, scenarioUniqueIntegrationId);
                    rule.OMScenarioId__c ??= scenario.Id;

                    if (rule.Action__c == "ADD")
                    {
                        rule.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(rule.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                        rule.Id = sf.Post_Object("OMScenarioRule__c", rule);
                        rule.SOMRuleId__c = sf.LookupValue("OMScenarioRule__c", "SOMRuleId__c", "UniqueIntegrationId__c", rule.UniqueIntegrationId__c);
                        rule.SOMSalesForceId__c = sf.LookupValue("OMScenarioSalesForce__c", "SOMSalesForceId__c", "UniqueIntegrationId__c", rule.SalesForceUniqueIntegrationId__c);
                    }
                    else
                    {
                        try
                        {
                            rule.SOMRuleId__c = sf.LookupValue("OMRule__c", "SOMRuleId__c", "UniqueIntegrationId__c", rule.UniqueIntegrationId__c);
                            rule.SOMSalesForceId__c = sf.LookupValue("OMSalesForce__c", "SOMSalesForceId__c", "UniqueIntegrationId__c", rule.SalesForceUniqueIntegrationId__c);
                            rule.Id = sf.Post_Object("OMScenarioRule__c", rule);
                        }
                        catch (Exception e)
                        {                            
                            rule.SOMRuleId__c= sf.CheckRecordExists("OMRule__c", rule.UniqueIntegrationId__c, "SOMRuleId__c");
                            rule.SOMSalesForceId__c = sf.CheckRecordExists("OMSalesForce__c", rule.SalesForceUniqueIntegrationId__c, "SOMSalesForceId__c");

                            if (!rule.SOMRuleId__c.StartsWith(SalesForceDataCollector.not_found) && rule.SOMRuleId__c != null && !rule.SOMSalesForceId__c.StartsWith(SalesForceDataCollector.not_found) && rule.SOMSalesForceId__c != null)
                                rule.Id = sf.Post_Object("OMScenarioRule__c", rule);
                            else
                                throw e;
                        }
                    }

                }

                AddToDataMapListOfObjects(rules, "ruleCount");

                // The committed object for ProductAccTerrExplicit is not synced to SalesForce (as it is an Account level table), so no error handling is performed. 
                var productAccTerrExplicits = jsonObj["ScenarioProductAccTerrExplicit"]?.ToObject<List<ScenarioProductAccTerrExplicit>>();
                foreach (ScenarioProductAccTerrExplicit productAccTerrExplicit in productAccTerrExplicits ?? Enumerable.Empty<ScenarioProductAccTerrExplicit>())
                {
                    SetObjectPropertiesWithBaseData(productAccTerrExplicit, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(productAccTerrExplicit, scenarioUniqueIntegrationId);
                    productAccTerrExplicit.OMScenarioId__c ??= scenario.Id;

                    productAccTerrExplicit.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(productAccTerrExplicit.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    productAccTerrExplicit.Id = sf.Post_Object("OMScenarioProductAccTerrExplicit__c", productAccTerrExplicit);
                    productAccTerrExplicit.SOMProductAccTerrExplicitId__c = sf.LookupValue("OMScenarioProductAccTerrExplicit__c", "SOMProductAccTerrExplicitId__c", "UniqueIntegrationId__c", productAccTerrExplicit.UniqueIntegrationId__c);

                }

                AddToDataMapListOfObjects(productAccTerrExplicits, "productAccTerrExplicitsCount");

                // The committed object for ProductAccTerrExclusion is not synced to SalesForce (as it is an Account level table), so no error handling is performed. 
                var productAccTerrExclusions = jsonObj["ScenarioProductAccTerrExclusion"]?.ToObject<List<ScenarioProductAccTerrExclusion>>();
                foreach (ScenarioProductAccTerrExclusion productAccTerrExclusion in productAccTerrExclusions ?? Enumerable.Empty<ScenarioProductAccTerrExclusion>())
                {
                    SetObjectPropertiesWithBaseData(productAccTerrExclusion, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(productAccTerrExclusion, scenarioUniqueIntegrationId);
                    productAccTerrExclusion.OMScenarioId__c ??= scenario.Id;

                    productAccTerrExclusion.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(productAccTerrExclusion.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    productAccTerrExclusion.Id = sf.Post_Object("OMScenarioProductAccTerrExclusion__c", productAccTerrExclusion);
                    productAccTerrExclusion.SOMProductAccTerrExclusionId__c = sf.LookupValue("OMScenarioProductAccTerrExclusion__c", "SOMProductAccTerrExclusionId__c", "UniqueIntegrationId__c", productAccTerrExclusion.UniqueIntegrationId__c);


                }

                AddToDataMapListOfObjects(productAccTerrExclusions, "productAccTerrExclusionsCount");


                var accountProductRules = jsonObj["ScenarioAccountProductRule"]?.ToObject<List<ScenarioAccountProductRule>>();
                foreach (ScenarioAccountProductRule accountProductRule in accountProductRules ?? Enumerable.Empty<ScenarioAccountProductRule>())
                {
                    SetObjectPropertiesWithBaseData(accountProductRule, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(accountProductRule, scenarioUniqueIntegrationId);
                    accountProductRule.OMScenarioId__c ??= scenario.Id;
                    if (accountProductRule.Action__c == "ADD")
                    {
                        accountProductRule.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(accountProductRule.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    accountProductRule.Id = sf.Post_Object("OMScenarioAccountProductRule__c", accountProductRule);
                    accountProductRule.SOMAccountProductRuleId__c = sf.ScenarioSOMId("OMScenarioAccountProductRule__c", accountProductRule.Id);
                }
                AddToDataMapListOfObjects(accountProductRules, "accountProductRuleCount");


                var accountOwners = jsonObj["ScenarioAccountOwner"]?.ToObject<List<ScenarioAccountOwner>>();
                foreach (ScenarioAccountOwner accountOwner in accountOwners ?? Enumerable.Empty<ScenarioAccountOwner>())
                {
                    SetObjectPropertiesWithBaseData(accountOwner, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(accountOwner, scenarioUniqueIntegrationId);
                    accountOwner.OMScenarioId__c ??= scenario.Id;
                    if (accountOwner.Action__c == "ADD")
                    {
                        accountOwner.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(accountOwner.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    accountOwner.Id = sf.Post_Object("OMScenarioAccountOwner__c", accountOwner);
                    accountOwner.SOMAccountOwnerId__c = sf.ScenarioSOMId("OMScenarioAccountOwner__c", accountOwner.Id);
                }
                AddToDataMapListOfObjects(accountOwners, "accountOwnerCount");


                var userAssignments = jsonObj["ScenarioUserAssignment"]?.ToObject<List<ScenarioUserAssignment>>();
                foreach (ScenarioUserAssignment userAssignment in userAssignments ?? Enumerable.Empty<ScenarioUserAssignment>())
                {
                    SetObjectPropertiesWithBaseData(userAssignment, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(userAssignment, scenarioUniqueIntegrationId);
                    userAssignment.OMScenarioId__c ??= scenario.Id;
                    if (userAssignment.Action__c == "ADD")
                    {
                        userAssignment.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(userAssignment.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    userAssignment.Id = sf.Post_Object("OMScenarioUserAssignment__c", userAssignment);
                    userAssignment.SOMUserAssignmentId__c = sf.ScenarioSOMId("OMScenarioUserAssignment__c", userAssignment.Id);
                    userAssignment.SOMUserId__c = sf.LookupValue("OMScenarioUserAssignment__c", "SOMUserId__c", "Id", userAssignment.Id);
                    userAssignment.SOMTerritoryId__c = sf.LookupValue("OMScenarioUserAssignment__c", "SOMTerritoryId__c", "Id", userAssignment.Id);
                }
                AddToDataMapListOfObjects(userAssignments, "userAssignmentCount");


                var engagementPlans = jsonObj["ScenarioEngagementPlan"]?.ToObject<List<ScenarioEngagementPlan>>();
                foreach (ScenarioEngagementPlan engagementPlan in engagementPlans ?? Enumerable.Empty<ScenarioEngagementPlan>())
                {
                    SetObjectPropertiesWithBaseData(engagementPlan, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(engagementPlan, scenarioUniqueIntegrationId);
                    engagementPlan.OMScenarioId__c ??= scenario.Id;
                    if (engagementPlan.Action__c == "ADD")
                    {
                        engagementPlan.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(engagementPlan.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    }
                    engagementPlan.Id = sf.Post_Object("OMScenarioEngagementPlan__c", engagementPlan);
                    engagementPlan.SOMEngagementPlanId__c = sf.ScenarioSOMId("OMScenarioEngagementPlan__c", engagementPlan.Id);
                }
                AddToDataMapListOfObjects(engagementPlans, "engagementPlan");

                var engagementResults = jsonObj["ScenarioEngagementResult"]?.ToObject<List<ScenarioEngagementResult>>();
                foreach (ScenarioEngagementResult engagementResult in engagementResults ?? Enumerable.Empty<ScenarioEngagementResult>())
                {
                    SetObjectPropertiesWithBaseData(engagementResult, baseDataMap, dataMap);
                    SetTestUniqueIntegrationId(engagementResult, scenarioUniqueIntegrationId);
                    engagementResult.OMScenarioId__c ??= scenario.Id;
                    engagementResult.UniqueIntegrationId__c = AppendUIDToFieldIfDoesntExist(engagementResult.UniqueIntegrationId__c, scenarioUniqueIntegrationId);
                    engagementResult.Id = sf.Post_Object("OMScenarioEngagementResult__c", engagementResult);
                    engagementResult.SOMEngagementResultId__c = sf.ScenarioSOMId("OMScenarioEngagementResult__c", engagementResult.Id);
                }
                AddToDataMapListOfObjects(engagementResults, "engagementPlan");

                return scenario;
            }

            return null;
        }

        public List<CustomValidation> GetCustomValidationRequest(string filePath)
        {

            string[] files = GetFilesFromFilePath(filePath);
            List<Tuple<string, JObject>> templates = GetObjectsFromFile(files);
            foreach (Tuple<string, JObject> testdata in templates)
            {
                JObject jsonObj = testdata.Item2;
                List<CustomValidation> customValidations = jsonObj["CustomValidation"]?.ToObject<List<CustomValidation>>();
                foreach (CustomValidation customValidation in customValidations)
                {
                    SetObjectPropertiesWithBaseData(customValidation, baseDataMap, dataMap);
                    AddToDataMapSingleObject(customValidation, "customValidationCount");
                }
                return customValidations;
            }
            return null;
        }

        public string[] GetModelIds()
        {
            return models?.Select(x => x.Id).ToArray();
        }
    }
}
