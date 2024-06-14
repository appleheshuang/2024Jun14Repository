Feature: Scenario tests OMR-15271

@CORE @Engine
Scenario Outline: OMR-15271 OMProductAccTerrExplicitExclusion Merge
		Execute two Scenarios consecutively, with the second one testing for different positive outcomes + SOMCreatedById, LastScenario populated on insert & update
		C11253083,C11253090 : ADD OMProductAccTerrExplicit/Exclusion
		C11253086, C11253093: RESTART OMProductAccTerrExplicit/Exclusion
		C11253087, C11253094 : END OMProductAccTerrExplicit/Exclusion
		C11253088, C11253095: DELETE OMProductAccTerrExplicit/Exclusion
		C11253085, C11253092: PEXP OMProductAccTerrExplicit/Exclusion
		C11339463, C11339464: RESTART+ END OMProductAccTerrExplicit/Exclusion
		C11253106: LastScenario and SOMCreatedById are populated for Account Product Territory Explicits and Exlucions
		C11253119: Updated LastScenarioId for AccountProductTerritory Explicits & Exclusions records

	Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings '<OptimizerSettings>'
	And I commit initial data '<InitialOMProductAccTerrExplicitsExclusionsSetupResults>'
	Then check All results per '<InitialOMProductAccTerrExplicitsExclusionsSetupResults>'
	Given the scenario test '<SetName>' defined by '<MergeActionsSetup>'
	When I 'calculate' the scenario and check results

	Examples:
	| InitialOMProductAccTerrExplicitsExclusionsSetupResults                                                                                | SetName                            | MergeActionsSetup                                                           | OptimizerSettings                          |
	| Scenario/MergeTests/OMR-15217-Scenario Merge-OMProductAccTerrExplicit-Exclusion/OMR-15271-ADD-OMProductAccTerrExplicit-Exclusion.json | Test-ProductAccTerrExpl/Excl-Merge | Scenario/MergeTests/OMR-15271-Merge-OMProductAccTerrExplicit-Exclusion.json | RuleEngineEnabled=true;CalculateMaps=false |

@CORE @Engine
Scenario Outline: OMR-15271 OMProductAccTerrExplicitExclusion Validation
	Execute two Scenarios consecutively, with the second one testing for different FK validation errors:
	Scope only to those that fail on scenario processing (in difference to those failing on posting to SF)
	C11253125: Validation: Creating an AccountProductTerritory record using an Account/Territory/Product that do not exist (only for non-existent Account as dummy Product & Territory are failing earlier on posting to SF)
	C11253124: Validation: adding an ATP Explicit & Exclusion where Account, Product & Territory date ranges do not overlap results in an error
	C11253126: Validation: no overlapping records exist with the same Account, Territory and Product UIDs

	Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings '<OptimizerSettings>'
	And I commit initial data '<InitialOMProductAccTerrExplicitsExclusionsSetupResults>'
	Then check All results per '<InitialOMProductAccTerrExplicitsExclusionsSetupResults>'
	Given the scenario test '<SetName>' defined by '<ValidationScenarios>'
	When I '<JobType>' the scenario
	And the scenario is '<ExpStatus>'
	Then check all results per '<ValidationScenarios>'

	Examples:
	| InitialOMProductAccTerrExplicitsExclusionsSetupResults                                                                                                  | SetName                                                                                      | OptimizerSettings                          | ValidationScenarios                                                                                                                                  | JobType   | ExpStatus |
	| Scenario/MergeTests/OMR-15217-Scenario Merge-OMProductAccTerrExplicit-Exclusion/OMR-15271-ADD-OMProductAccTerrExplicit-Exclusion-ForValidationTest.json | Test-ProductAccTerrExplExcl/Validation_Non-existentAccount                                   | RuleEngineEnabled=true;CalculateMaps=false | Scenario/MergeTests/OMR-15217-Scenario Merge-OMProductAccTerrExplicit-Exclusion/OMR-15271-Non-existentAccount.json                                   | calculate | ERROR     |
	| Scenario/MergeTests/OMR-15217-Scenario Merge-OMProductAccTerrExplicit-Exclusion/OMR-15271-ADD-OMProductAccTerrExplicit-Exclusion-ForValidationTest.json | Test-ProductAccTerrExplExcl/Validation_ProductTerritoryDoNotOverlap                          | RuleEngineEnabled=true;CalculateMaps=false | Scenario/MergeTests/OMR-15217-Scenario Merge-OMProductAccTerrExplicit-Exclusion/OMR-15271-ProductTerritoryDoNotOverlap.json                          | calculate | ERROR     |
	| Scenario/MergeTests/OMR-15217-Scenario Merge-OMProductAccTerrExplicit-Exclusion/OMR-15271-ADD-OMProductAccTerrExplicit-Exclusion-ForValidationTest.json | Test-ProductAccTerrExplExcl/Validation_CreatingOverlappingOMProductAccTerrExplicit_Exclusion | RuleEngineEnabled=true;CalculateMaps=false | Scenario/MergeTests/OMR-15217-Scenario Merge-OMProductAccTerrExplicit-Exclusion/OMR-15271-CreatingOverlappingOMProductAccTerrExplicit_Exclusion.json | calculate | ERROR     |