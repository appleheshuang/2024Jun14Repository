Feature: Bulk API buckets for Ops
OMR-5449 Bulk API buckets for Ops

@CORE @Schema
Scenario: Ops Bulkload with TempS3Credentials
	C16188946 OMR-5449: Successful bulk load with TempS3Credentials
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And the test data path <Path>
	And the bulkload settings '<BulkloadSettingsFile>'
	And unique data for each table
	| TableName        | FileName                             |
	| OMAccount        | LoadTable/OMAccount_17481.csv        |
	| OMAccountAddress | LoadTable/OMAccountAddress_17481.csv |
	| OMAffiliation    | LoadTable/OMAffiliation_17481.csv    |
	| OMGeography      | LoadTable/OMGeography_17481.csv      |
	When I get temp aws credentials 
	And I upload the data to s3
	Then I bulkload using the temp credentials
	And purge the data from s3
	And check all results per 'DataLoad-Tests.json'

	Examples:
	| TestName   | Path                            | BulkloadSettingsFile  |
	| LoadFiles  | BulkLoad/UsingTempS3Credentials | BulkloadSettings.json |

@CORE @Schema
Scenario: Ops Bulkload from dir with TempS3Credentials
	C16188946 OMR-5449: Successful bulk load with TempS3Credentials
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And the test data path <Path>
	And the bulkload settings '<BulkloadSettingsFile>'
	And unique data from the directory '<DataDir>'
	When I get temp aws credentials 
	And I upload the data to s3
	Then I bulkload using the temp credentials
	And purge the data from s3
	And check all results per 'DataLoad-Tests.json'

	Examples:
	| TestName   | DataDir | Path                            | BulkloadSettingsFile  |
	| LoadBucket | LoadAll | BulkLoad/UsingTempS3Credentials | BulkloadSettings.json |