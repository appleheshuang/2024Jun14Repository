Feature: Tenant prep


@CORE @Schema
Scenario: Tenant create - bulkload, commit, disable
	C17751091	Use snowflake session token to query whitelisted tables 
	## C17751986   Fails when tenant disabled (not automated)
	##  Not working consistently in automation - These are been removed from disable steps
	##	- When I issue the snowflake request 'merge_territory' per '<JsonFile>'
	##	- Then the request status is 'Unauthorized'

	# Create the tenant and load data
	Given the tenant configuration defined by 'tenantcreate-org.json'
	And I validate the Salesforce org
	And I want 'OCEO' with 'latest' schema
	#And I want 'OCESEG' with '17.0.0' schema
	#And I want 'OCED' with 'latest' schema 
	And I want OptimizerEnabled = true
	When I create a tenant
	Then the tenant creation is successful and I can get a token and tenant SalesForceXApiKey is generic
	And the tenant 'Replication' status is 'NA'
	Given I bulkload from the bulkload settings in '<InitialLoadFile>' and trigger post-migration push

	# Whitelist tables, execute a scenario, check the results
	Then I get the snowflake credentials
	Given I whitelist snowflake tables
	| TableName            | Schema | Type |
	| OMTerritory          | OUTPUT | 0    |
	| OMGeographyTerritory | OUTPUT | 0    |
	| OMAccountTerritory   | OUTPUT | 0    |
	And I create the snowflake user for browser access
	Then I set the snowflake token
	Given the scenario test '<SetName>' defined by '<JsonFile>'
	And the test is snowflake only
	When I '<JobType>' the scenario and check results	
	Then as the browser user I check 'merge_territory,merge_geoterr,alignments' results per '<JsonFile>'
	
	# Disable the tenant and check resources are no longer accessible
	When I disable the tenant that was just created
	Then GetToken fails for the tenant

	Examples:
	| JsonFile                                             | SetName             | JobType | InitialLoadFile                         |
	| Smoketest/Scenario/TenantCreateDisableSmokeTest.json | TenantCreateDisable | commit  | Smoketest/LoadData/InitialBulkload.json |
	
@Setup
Scenario: Initial Bulkload data setup
	Given the tenant configuration defined by '<Tenant>'
	And I sync PickList values for OMAccount table
	Then I bulkload and sync initial data
	
	Examples:
	| Tenant             |
	| smoketest-org.json |

@Setup
Scenario: Configure browser snowflake user for tenant
	Given the tenant configuration defined by '<Tenant>'
	And I whitelist snowflake tables
	| TableName         | Schema | Type |
	| OMProduct         | STATIC | 0    |
	| VProductHierarchy | STATIC | 1    |
	And I create the snowflake user for browser access
	
	Examples:
	| Tenant             |
	| smoketest-org.json |

@Setup
Scenario: Check queue health
	Given the tenant configuration defined by 'smoketest-org.json'
	And I check queue health for the env

@Smoketest
Scenario: Check engine status
	Given the tenant configuration defined by 'smoketest-org.json'
	Then I check engine is running

#   As per OMR-18491 datasync jobs are being processed by engine, so this check is no longer valid in v18+.
#@Smoketest
#Scenario: Check datasync status
#	Given the tenant configuration defined by 'smoketest-org.json'
#	Then I check 'DataSyncStatus' status is 'Running'

@Smoketest
Scenario: Check connectivity status
	Given the tenant configuration defined by '<Tenant>'
	Then I check connectivity status
	
	Examples:
	| Tenant   |
	| smoketest-org.json |
	| ocesync-org.json   |

@Setup
Scenario: Check Service Availability per Package version
	Given the tenant configuration defined by 'smoketest-org.json'
	And the package version for the tenant
	Then the 'engineVersion' matches the package version
	And the 'odataVersion' matches the package version
	And the 'tenantVersion' matches the package version
	And the 'bulkVersion' matches the package version
#   As per OMR-18491 datasync jobs are being processed by engine, so this check is no longer valid in v18+.
#	And the 'datasyncVersion' matches the package version
	And the snowflake version matches the package version

@Setup
Scenario: Check sync health
	Given the tenant configuration defined by 'ocesync-org.json'
	#Bulkload is placed here to make sure it is run before the PULL_ACCOUNT
	Then I bulkload and sync initial data 
	Given I resync accounts from OCE-P
	# Limit the number of territories in OCE-P geography to avoid sync errors
	When I end geography alignments for 'GEO01' that are 6 hours old
	Then the max number of geography alignments is 15

# Manually triggered. Use to manually clean up s3 files in case they are not cleaned up on the go.
# Use with caution
Scenario: Cleanup s3
	Given the s3 bucket for 'master'
	And I cleanup unique bulkload files for '<S3FeatureFolder>'

	Examples:
	| S3FeatureFolder              |
	| autotestdata/OMR-10020/      |
	| autotestdata/OMR-10805/      |
	| autotestdata/OMR-13032/      |
	| autotestdata/OMR-13250/      |
	| autotestdata/OMR-13631/      |
	| autotestdata/OMR-14080/      |
	| autotestdata/OMR-15972/      |
	| autotestdata/OMR-16552/      |
	| autotestdata/OMR-9930/       |
	| autotestdata/RecalcAccount/  |