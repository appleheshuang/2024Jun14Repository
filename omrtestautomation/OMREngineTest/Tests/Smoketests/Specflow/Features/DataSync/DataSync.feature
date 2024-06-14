Feature: DataSync
	@DataSync

@DataSync
Scenario: OMR-10769 DataSync Custom Ordering
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I bulkload unique '<BulkLoadFile>'
	When I trigger a <ExpectedStatus> DataSync 'TestPush' with parameter body defined by '<DataSyncParamFile>'
	Then check all results per '<DataSyncParamFile>'
	Examples:
	| TestName                 | BulkLoadFile                                           | ExpectedStatus | DataSyncParamFile                                                        |
	#C10139193
	| PositiveCase             | Smoketest/LoadData/omr-10769/BulkOMR10769-Success.json | successful     | DataSync/OMR-10769CustomOrdering/C10139193-PositiveCaseStaticTables.json |
	#C10139194
	| PositiveCaseWithAliasing | Smoketest/LoadData/omr-10769/BulkOMR10769-Success.json | successful     | DataSync/OMR-10769CustomOrdering/C10139193-PositiveCaseWithAliasing.json |
	| NegativeCase             | Smoketest/LoadData/omr-10769/BulkOMR10769-Fail.json    | error          | DataSync/OMR-10769CustomOrdering/C10139196-NegativeCaseStaticTables.json |

@DataSync
Scenario: OMR-10768 DataSync Custom Mapping Product PULL
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I post unique salesforce 'Product' from file 'Products/omr-10768/DataSyncProducts.json'
	And I wait for 'Product' identified by 'UniqueIntegrationId' to static sync
	When I trigger a successful DataSync 'TestPull' with parameter body defined by 'DataSync/OMR-10768CustomMapping/CustomMappingPull.json'
	Then check all results per 'DataSync/OMR-10768CustomMapping/CustomMappingPull.json'

@DataSync
Scenario: OMR-10768 DataSync Custom Mapping Product PUSH
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	#Bulkload unique Products
	And I bulkload unique 'Smoketest/LoadData/omr-10768/BulkOMR10768-Product.json'
	When I trigger a successful DataSync 'TestPush' with parameter body defined by 'DataSync/OMR-10768CustomMapping/CustomMappingPush.json'
	Then check all results per 'DataSync/OMR-10768CustomMapping/CustomMappingPush.json'
