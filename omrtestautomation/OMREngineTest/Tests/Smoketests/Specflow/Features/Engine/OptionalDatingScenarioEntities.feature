Feature: Optional Effective and End Dates on Scenario Entities
    OMR-3228 Testing of optional Effective and End Dates on different Scenario entities

@CORE @Engine
Scenario Outline: Single Scenarios with positive result
    Execute a single Scenario testing for different positive outcomes
        C7854449: Territory Parent End Date not equal to Child End Date

    Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings '<Settings>'
    And I post the salesforce users in file 'Users/Userdata.json'
    And the scenario test '<Test>' defined by 'Scenario/OptionalDatingScenarioEntities/<Scenario>.json'
    When I 'calculate' the scenario and check results

    Examples:
    | Scenario                                      | Test                             | Settings               |
    | AllEntities-Rules-Hierarchies-ScenarioSummary | Optional Effective And End Dates | RuleEngineEnabled=true |


@CORE @Engine
Scenario Outline: Scenario updates
    Execute two Scenarios consecutively, with the second one testing for different positive outcomes
        C7848136: MOVE AccountExclusion
        C7848138: EDIT+EXPR Rule
        C7848139: EFFE+EXPR UserAssignment
        C7848140: EDIT+EFFE+EXPR ProductExplicit
        C7850226: EDIT+EFFE+EXPR SalesForce
        C7628777: Past dating Account Alignment to Scenario Effective Date

    Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings '<Settings>'
    And I post the salesforce users in file 'Users/Userdata.json'
    And I commit initial data 'Scenario/OptionalDatingScenarioEntities/<Scenario>_Setup.json' and check results
    Given the scenario test '<Test> Test' defined by 'Scenario/OptionalDatingScenarioEntities/<Scenario>_Test.json'
    When I 'calculate' the scenario and check results

    Examples:
    | Scenario                        | Test                                   | Settings               |
    | Multiple-Actions-Positive       | Multiple Actions Positive              | RuleEngineEnabled=true |
    | PastDate-Scenario-EffectiveDate | Past Dating to Scenario Effective Date | RuleEngineEnabled=true |

@CORE @Engine
Scenario Outline: Scenario update Resulting In Error
	Execute two Scenarios consecutively, with the second one resulting in an error due to the different start or end date on the same entity
		C7848137, C7847212: Different Dates Error

	Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings '<Settings>'
	And I commit initial data 'Scenario/OptionalDatingScenarioEntities/<Scenario>_Setup.json'
	Given the scenario test '<Test> Test' defined by 'Scenario/OptionalDatingScenarioEntities/<Scenario>_Test.json'
	When I 'calculate' the scenario
	And the scenario is 'ERROR'

	Examples:
	| Scenario                  | Test                      | Settings                |
	| Multiple-Actions-Negative | Multiple Actions Negative | RuleEngineEnabled=false |
