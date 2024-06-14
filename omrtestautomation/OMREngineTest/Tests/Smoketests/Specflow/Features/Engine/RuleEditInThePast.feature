Feature: Scenario tests OMR-5337
	Edit rule effective in the past

@CORE @Engine
Scenario Outline: Edit Rule
	Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings '<OptimizerSettings>'
	And I commit initial data '<SetUpFile>' and check results
	Given the scenario test '<SetName2>' defined by '<JsonFile2>'
	When I 'commit' the scenario and check quick results
	And the scenario is successful
	Given the scenario test '<SetName3>' defined by '<JsonFile3>'
	When I 'calculate' the scenario and check results

	Examples:
	| Testname                        | SetUpFile                             | JsonFile2                            | JsonFile3                            | SetName2           | SetName3             | OptimizerSettings      |
	| Edit rule effective in the past | Scenario/Edit-Rule/Rule-Setup-V1.json | Scenario/Edit-Rule/Rule-Test-V2.json | Scenario/Edit-Rule/Rule-Test-V3.json | AutoCommit-Rule-V2 | AutoSimulate-Rule-V3 | RuleEngineEnabled=true |