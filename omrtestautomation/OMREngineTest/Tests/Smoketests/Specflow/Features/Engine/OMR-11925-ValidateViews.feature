Feature: OMR-11925 Validate Account Views

A short summary of the feature

# Covers OMR-6303/OMR-11925
# TestRail IDs:
# C8245636, C8247473, C8257476, C8257477, C8257474, C8257475, C9627434, C9627435, C9627436, C9627437, C9627438, C9627439, C9627440
@CORE @Engine
Scenario Outline: Generate Account View Data and Verify
	Given the tenant configuration defined by 'smoketest-org.json'
	And I bulkload from the bulkload settings in 'Smoketest/LoadData/omr-11925/BulkOMR11925.json'
	And the scenario test '<ScenarioName>' defined by '<JsonFile>'
	When I '<JobType>' the scenario and check results

	Examples:
	| ScenarioName                            | JsonFile                                                                        | JobType   | 
	| OMR-11925_AccountTerritoryAlignmentRule | Smoketest/Scenario/OMR-11925_AccountTerritoryAlignmentRule.json                 | commit    | 
