Feature: Bulkload

@CORE @Schema
Scenario: Bulkload data with duplicate UniqueIntegrationIds
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I submit a unique bulkload request from file '<BulkloadSettingsFile>'
	Then The bulkload is successful
	Then check all results per '<BulkloadSettingsFile>'

	Examples:
	| BulkloadSettingsFile                                                              | 
	| Smoketest/LoadData/omr-9930/omr9930-BulkloadWithDuplicateUniqueIntegrationId.json | 

@CORE @Schema 
Scenario: Post processing OMAffiliation bulkload 
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I submit a unique bulkload request from file '<AccountLoadFile>'
	Then The bulkload is successful
	Then check all results per '<BulkloadResultsFile>'
	Examples:
	| AccountLoadFile                                        | BulkloadResultsFile                                                               |
	| Smoketest/LoadData/omr-10805/BulkOMR10805Accounts.json | Smoketest/LoadData/omr-10805/omr10805-PostProcessingForBulkloadOMAffiliation.json |

@CORE @Schema
Scenario: OMR-13032 Audit on bulk load 
	Given the tenant configuration defined by 'smoketest-org.json'
	And I bulkload from the bulkload settings in '<SettingOMAuditConfig>'
	And a unique id for the test
	And I submit a unique bulkload request from file '<BulkLoadAdd>'
	Then The bulkload is successful
	Then check all results per '<BulkloadAddResultsFile>'
	Given I submit a unique bulkload request from file '<BulkLoadUpdateInactivate>'
	Then The bulkload is successful
	Then check all results per '<BulkLoadUpdateInactivateResults>'

	Examples:
	| SettingOMAuditConfig                                            | BulkLoadAdd                                        | BulkLoadUpdateInactivate                                        | BulkloadAddResultsFile                                              | BulkLoadUpdateInactivateResults                                            |
	| Smoketest/LoadData/omr-13032/OMR13032BulkLoadOMAuditConfig.json | Smoketest/LoadData/omr-13032/OMR13032Bulk_Add.json | Smoketest/LoadData/omr-13032/OMR13032Bulk_UpdateInactivate.json | Smoketest/LoadData/omr-13032/OMR3032_AuditOnBulkLoadAddResults.json | Smoketest/LoadData/omr-13032/OMR3032_AuditOnBulkLoadUpdateINACResults.json |
