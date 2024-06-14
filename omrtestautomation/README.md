# OMR Test Automation

This repository contains env configuration and xunit tests to execute OMR APIs and check results. 
Tests are categorized as 
* Setup - always, ensures env is available, tenant valid, initial test data is loaded
* Smoketest - always, ensures all services are running and basing basic tests
* MAT - always on scheduled pipelines, manual otherwise
* CORE - "
* AUX - manual, can fail
* Latest - tests applicable to the in-development version. Never run in prod, preprod
* InTest - manual, can fail. Shortcut to executing a limited number of tests on the pipeline

## Getting started
### Setting up the automation project locally
The automation project runs off a framework known as Specflow. You will need to install the Specflow Visual Studio Extension in order for the feature files to be found by the project. This can be found by going into Visual Studio -> Extensions -> Search for 'Specflow' -> Install Specflow for Visual Studio 20XX -> Restart Visual Studio

The project now runs on .netcore3.1, you can find this here https://dotnet.microsoft.com/en-us/download/dotnet/3.1

The rest of the project should work out of the box.

### Add or replace a tenant config
Having created a new org and tenant manually or using ScratchOrg automation ...
 * Name the json file for the test and environment it is used for 
    * eg. smoketest-master.json is the org+tenant to use to run smoketests against the "master" env
	* eg. tenantcreate-org.json is the sf org to use for tenant create in any environment
 * You can alternatively name the file for the specific org to use
    * eg. smoketest-customization-ruby-26382.json
 * Place the file in the test s3 bucket, "environment_configs" directory, within the env-specific folder
    * eg. Global env configs can go diretly within environment_configs/

### Running tests locally
* Configure a new system Environment Variable "TARGET_ENV" to the target config folder. If not configured master is used
* Re-open Test Explorer window in VS
#### Target a specific org using
* Configure a new system Environment Variable "TARGET_ORG" to the target a specific config file. If not configured the default for the targeted env is used
   * Eg. TARGET_ORG = customization-ruby-26382
*Re-open Test Explorer window in VS
#### Add a new config w/out replacing current configs
* Create a new folder on s3 and upload your test file to environment_configs/<yourname>/
* Add <yourname> to the list of valid_envs in s3TenantFixture 
* Configure Environment Variable "TARGET_ENV" to the target config folder <yourname>
* Execute tests from the Test Explorer. Note that for a new tenant BulkLoadAndSync need to be executed first

### Running the test pipeline
Tests use env variables to select the targeted env config file. Version is normally the one targeted by the sf org.
 * TARGET_ENV is the env to test against. The target env must match a TenantConfig folder. Currently have [these named environments](../master/MainFramework/GlobalConfigs/NamedEnvs.csv)
	This setting determines the tenant config files to use for tests. If none is provided the branch name is used
 * TARGET_VERSION can be used if there is no sf org for the test. Otherwise the version targeted by the sf org is used, or "latest" if the branch is master.

## Smoketests

### Pre-requisites
 * Add the tenant config as described above
## Data 
Automated scenario-based smoketests (SubmitSmokeTest, CommitSmokeTest, ScenarioRulesDisabled, CommitWithMaps) currently retrieve all the necessary information to run a successful smoke test from a JSON file. 
Necessary information, in this context, refers to:
 * Test configuration - For example, jobType which currently supports "calculate" or "commit"
 * Salesforce objects. Salesforce objects are posted and assigned to a scenario with every run of the smoketest and constitute the test-data for the smoke execution. A well-defined JSON can contain an Scenario object with arbitrarily complex rules and various object assignments (Territories, AccountExplicits, etc) to it.
 * OData/Snowflake/Salesforce queries - Scenarios that have been posted are then submitted to the engine. It is then up to the tester to determine if the calculated result is functionally correct. This is done by making use of OData and/or Snowflake and/or Salesforce object queries, which needs to be written by the tester.

Documentation on how to craft your own Scenario for a smoketest can be found on the following page: https://jiraims.rm.imshealth.com/wiki/display/TMD/Crafting+Scenario+JSON+for+Smoketests

### Snoketest execution for a new tenant
 * Execute the ValidateOrgTenant test to ensure the tenant is configured correctly
 * Execute the SystemStatus test to ensure the system is live
 * Execute BulkLoadAndSyncAll to load the starting test data
 Can now execute any of the scenario smoketests. Note that scneario tests and recalc should not be executed concurrently.

## OCE Sync tests
### Pre-requisites
TBD
### Sync test execution
TBD

## OData tests
### Pre-requisites
TBD
### Odata test execution
TBD

## Performance tests
### Pre-requisites
Perfload tenant is created and has bulkload into output enabled. "BulkLoadIntoOutput": 1
### Collecting the performance test results
 * Execute the performance job on the pipeline (having set the target env and org as applicable)
 * Download the performance raw and summary results from the job artifacts
 * Compare to previously baselined data
 * Note: One-off Bulkload and sync all timings are written to standard output
### Adding to/changing the performance tests
 * Currently only scenarios and recalcs are supported. Place json for additional tests in the Performance folder. 
 * Configs are used to detemine the execution type (commit or simulate), scenario name, and settings
 * The preload config can be used to create any data used by the performance test (or load it into the data map)
```json  
"Configs": {
    "name": "AutoCommitWithMaps",
    "jobtype": "commit",
    "jobparams": "CalculateMaps=true;RuleEngineEnabled=true",
    "preload": 
      { "Product": "Performance/Setup/OMProduct.json" }
  }
```
 * Data tests are not executed and should not be included in the scenario json.
 * The scenarios can be submitted to run concurrently or in sequence - see the feature file for the test.

## More info
### Tenant config file
|Key|Description|Required|
|---|---|:---:|
|orgURL|URL for the salesforce org|Yes|
|clientID|App Client Id for the Optimizer Api connected app|If tenant credentials are not provided|
|clientSecret|App Client Secret for the Optimizer Api connected app|'~'|
|orgUserName|Salesforce user to access sf apis, and for use with tenantcreate (when applicable)|Yes|
|orgPassword|Password for the salesforce user|Yes|
|TenantId|Tenant|Yes|
|TenantUser|Cognito username for tenant|No|
|TenantPassword|Cognito password for tenant|No|
|snowConnection|Snowflake connection info|No|
|sfApiKey|Api key for the tenant|Yes|
|lexiApiKey|Lexi api key for the tenant|No|
|sfnamespace|Salesforce namespace prefix for object reference and sf api calls|Yes|
|targetEnv|Indicates the config file which conains env configs such as the engine url|Yes|

```json
Eg.
{
  "orgURL": "https://page-connect-27392-dev-ed.cs114.my.salesforce.com/",
  "clientID": "3MVG9iLRabl2Tf4g_8MICADjPm_LQh4w4vFrOSmnPAROIEOfJQ05kgaJxul7tRL6ZanmI4QydOkD02B718agE",
  "clientSecret": "8BB7F4256215B73BC3666ED58DA8B7D778492DF21A3E48C4FC15EF88C78EA265",
  "orgUserName": "test-xecw5ezqs5d2@example.com",
  "orgPassword": "#Pukek0123",
  "TenantId": "93dd031323004c37985f4bc18b6f1143",
  "TenantUser": "93dd031323004c37985f4bc18b6f1143",
  "TenantPassword": "hzz$$$NGY686xX@tlzpRkurucNFbwb",
  "snowConnection": "account=iqviaomrdev;user=TDEVPROD01_PROD_93DD031323004C37985F4BC18B6F1143_UR;password=jre$$@DLZ859fd0U9jReLLm8WTzjBo;db=TDEVPROD01_PROD_93DD031323004C37985F4BC18B6F1143_DB;host=iqviaomrdev.us-east-1.snowflakecomputing.com;warehouse=COMPUTE_WH;role=TDEVPROD01_PROD_93DD031323004C37985F4BC18B6F1143_RL;",
  "sfApiKey": "8b9pQnDn8aa3t9tPcpj71cwkXivBJIGalkKtZXr7",
  "lexiApiKey": "dm8NRjkXLDaRiTraIxj9G8EXI59q7Meh5x5McRsC",
  "sfnamespace": "iq_sj_omr_01__",
  "targetEnv": "stagingprod"
}
```
