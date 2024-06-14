Feature: Scenario processing
	Scenario processing tests

@MAT @Engine
Scenario Outline: Scenario Calculate
	Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings '<OptimizerSettings>'
	And the scenario test '<SetName>' defined by '<JsonFile>'
	When I '<JobType>' the scenario with '<Mode>' mode
	And the scenario is successful
	Then check <NamedTest> results per '<TestFile>'

	Examples:
	| SetName           | JsonFile                                               | JobType   | OptimizerSettings                           | Mode | NamedTest  | TestFile                                               |
	| AutoRulesDisabled | Smoketest/Scenario/BasicSmokeTest.json                 | calculate | RuleEngineEnabled=false;CalculateMaps=false |      | alignments | Smoketest/Scenario/BasicScenarioRulesDisabledTest.json |
	| AutoSimulate      | Smoketest/Scenario/BasicSmokeTestWithProductRules.json | calculate |                                             |      | all        |                                                        |
	| StateCode2InRules | Smoketest/Scenario/OMR-17372_StateCode2InRules.json    | calculate | CalculateMaps=false                         |      | all        | Smoketest/Scenario/OMR-17372_StateCode2InRules.json    |
	| LexiFetchSettings | Smoketest/Scenario/BasicSmokeTestWithProductRules.json | calculate |                                             | lexi | all        |                                                        |

@CORE @Engine
Scenario Outline: Scenario Commit
	Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings '<OptimizerSettings>'
	And the scenario test '<SetName>' defined by '<JsonFile>'
	When I '<JobType>' the scenario and check results
	# Added for OMR-15226, test case #C10935870
	Then the committed scenario schemas are removed

	Examples:
	| SetName                         | JsonFile                                                    | JobType | OptimizerSettings                           |
	| AutoCommitWithMaps              | Smoketest/Scenario/BasicSmokeTest.json                      | commit  | CalculateMaps=true                          |
	| AutoCommitWithSpecialCharacters | Smoketest/Scenario/BasicSmokeTestWithSpecialCharacters.json | commit  | RuleEngineEnabled=false;CalculateMaps=false |               


@CORE @Engine
Scenario Outline: Scenario Failure
	Given the tenant configuration defined by 'smoketest-org.json'
	And the scenario test '<SetName>' defined by '<JsonFile>'
	When I '<JobType>' the scenario
	And the scenario fails
	And the scenario is '<ExpStatus>'

	Examples:
	| SetName | JsonFile                                   | JobType   | ExpStatus |
	| Errored | Smoketest/Scenario/BasicScenarioError.json | calculate | ERROR     |

@MAT @Engine
Scenario Outline: Purge Calculated Scenarios
	Given the tenant configuration defined by '<TenantConfig>'
	When I delete scenarios that are 3 days old 
	And the scenario is deleted within 20 minutes
	Then purged scenario schemas are removed
	
	Examples:
	| TenantConfig       |
	| smoketest-org.json |
	| ocesync-org.json   |
