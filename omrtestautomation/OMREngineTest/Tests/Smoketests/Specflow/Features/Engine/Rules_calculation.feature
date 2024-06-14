Feature: Restate the past OMR-931
	
@MAT @Engine
Scenario Outline: Restating the past
	Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings '<OptimizerSettings>'
	And I commit initial data '<ScenarioPath>Setup-<SetName>.json' and check results
	Given the scenario test '<SetName>-Test' defined by '<ScenarioPath>Run-<SetName>.json'
	When I 'calculate' the scenario and check snow results

	Examples:
	| SetName                  | OptimizerSettings      |ScenarioPath               | 
	| RestateThePast           | RuleEngineEnabled=true |Scenario/Restate-the-past/ | 
	| SplitMoveForwardInFuture | RuleEngineEnabled=true |Scenario/Restate-the-past/ | 