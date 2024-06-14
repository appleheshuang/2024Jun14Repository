Feature: RCA based scenario tests
	Commits scenario and runs RCA
	@engine

@MAT @Engine
Scenario Outline: RunRCA on committed scenario
	Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings '<OptimizerConfigs>'
	And the scenario test '<Scenarioname>' defined by '<Directory>'
	When I '<TestType>' the scenario and check results
	Then I check the root cause of alignments 'RootCause/Data/'

	Examples:
	| Scenarioname | TestType | Directory                       | OptimizerConfigs                           |
	| AutoCalc-RCA | commit   | RootCause/RcaScenario.json | RuleEngineEnabled=true;CalculateMaps=false |
