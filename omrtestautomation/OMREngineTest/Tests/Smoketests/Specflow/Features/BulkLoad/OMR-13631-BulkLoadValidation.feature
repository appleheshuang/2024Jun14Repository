Feature: OMR-13631 - BulkLoad Date Range Validation

# Covers OMR-13631
# TestRail IDs:
# C10414323, C10414324, C10414325, C10414326, C10414327, C10414328, C10414329, C10414330, C10414331, C10414332, C10414333, C10414334, C10414335, C10414336, C10414337, C10414338, C10414339

@CORE @Schema
# TestRail IDs:
# C10414323: Child record load – active records – parent record doesn’t exist
# C10414324: Child record load – active records – parent record is inactive
# C10414325: Child record load – active records – parent record exists and is active with invalid date range
Scenario Outline: Child record loads – active records – expected failures
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I submit a unique bulkload request from file '<BulkLoadSettingsFile>'
	Then The bulkload has errors per '<ErrorMessageFile>'
	Then check all results per '<ResultsFile>'

	Examples:
	| BulkLoadSettingsFile                                       | ResultsFile                                                | ErrorMessageFile                                           |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414323.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414323.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414323.json |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414324.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414324.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414324.json |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414325.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414325.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414325.json |

@CORE @Schema
# TestRail IDs:
# C10414326: Child record load – active records – parent record exists and is active with valid date range
# C10414328: Child record load – inactive record – valid parent record
# C10414329: Child record load – inactive record – parent record does not exist
# C10414331: Child record load – inactive record – parent record is inactive
# C10414333: Child record load – inactive record – parent record exists and is active with invalid date range
Scenario Outline: Child record loads - expected successes
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I submit a unique bulkload request from file '<BulkLoadSettingsFile>'
	Then The bulkload is successful
	Then check all results per '<BulkLoadResultsFile>'

	Examples:
	| BulkLoadSettingsFile                                       | BulkLoadResultsFile                                        |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414326.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414326.json |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414328.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414328.json |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414329.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414329.json |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414331.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414331.json |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414333.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414333.json |

@CORE @Schema
# TestRail IDs:
# C10414327: Child record update – active records – valid parent record – update effective/end dates: valid -> invalid
# C10414330: Child record update – inactive to active – parent record does not exist
# C10414332: Child record update – inactive to active – parent record is inactive
# C10414334: Child record update – inactive to active – parent record exists and is active with invalid date range
# C10414335: Parent record update – active record – child records active – update effective/end dates: valid -> invalid
Scenario Outline: Child and parent record loads followed by updates to those records
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I submit a unique bulkload request from file '<BulkLoadSettingsFile>'
	Then The bulkload is successful
	Then check all results per '<BulkLoadResultsFile>'
	Given I submit a unique bulkload request from file '<BulkUpdateSettingsFile>'
	Then The bulkload has errors per '<BulkUpdateErrorMessageFile>'
	Then check all results per '<BulkUpdateResultsFile>'

	Examples:
	| BulkLoadSettingsFile                                              | BulkLoadResultsFile                                               | BulkUpdateSettingsFile                                              | BulkUpdateResultsFile                                               | BulkUpdateErrorMessageFile                                          |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414327_1-Load.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414327_1-Load.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414327_2-Update.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414327_2-Update.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414327_2-Update.json |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414330_1-Load.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414330_1-Load.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414330_2-Update.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414330_2-Update.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414330_2-Update.json |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414332_1-Load.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414332_1-Load.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414332_2-Update.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414332_2-Update.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414332_2-Update.json |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414334_1-Load.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414334_1-Load.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414334_2-Update.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414334_2-Update.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414334_2-Update.json |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414335_1-Load.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414335_1-Load.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414335_2-Update.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414335_2-Update.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414335_2-Update.json |

@CORE @Schema
# TestRail IDs:
# C10414336: Parent record update – active record – child records inactive – update effective/end dates: valid -> invalid
Scenario Outline: Parent record loads followed by updates to those records
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I submit a unique bulkload request from file '<BulkLoadSettingsFile>'
	Then The bulkload is successful
	Then check all results per '<BulkLoadResultsFile>'
	Given I submit a unique bulkload request from file '<BulkUpdateSettingsFile>'
	Then The bulkload is successful
	Then check all results per '<BulkUpdateResultsFile>'

	Examples:
	| BulkLoadSettingsFile                                              | BulkLoadResultsFile                                               | BulkUpdateSettingsFile                                              | BulkUpdateResultsFile                                               |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414336_1-Load.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414336_1-Load.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414336_2-Update.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414336_2-Update.json |

@CORE @Schema
# TestRail IDs:
# C10414337: Load both parent and child records in one bulk load
Scenario Outline: Load both parent and child records in one bulk load
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I submit a unique bulkload request from file '<BulkLoadSettingsFile>'
	Then The bulkload has errors per '<ErrorMessageFile>'
	Then check all results per '<ResultsFile>'

	Examples:
	| BulkLoadSettingsFile                                       | ResultsFile                                                | ErrorMessageFile                                           |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414337.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414337.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414337.json |

@CORE @Schema
# TestRail IDs:
# C10414338: Rollback: Load multiple child files, with one file containing some invalid-date-range records – rollback = false
# C10414339: Rollback: Load multiple child files, with one file containing some invalid-date-range records – rollback = true
Scenario Outline: Rollback tests, with both rollback=false and rollback=true
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I submit a unique bulkload request from file '<BulkLoadSettingsFile>'
	Then The bulkload has errors per '<ErrorMessageFile>'
	Then check all results per '<ResultsFile>'

	Examples:
	| BulkLoadSettingsFile                                       | ResultsFile                                                | ErrorMessageFile                                           |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414338.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414338.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414338.json |
	| Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414339.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414339.json | Smoketest/LoadData/omr-13631/Bulk_OMR-13631_C10414339.json |
