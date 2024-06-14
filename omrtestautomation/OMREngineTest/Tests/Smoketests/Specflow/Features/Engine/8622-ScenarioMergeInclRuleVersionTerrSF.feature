Feature: Scenario tests OMR-8622
	Scenario merge - incl Rule-Version, Territory-SalesForce

@MAT @Engine
Scenario Outline: Merge Territory-SalesForce all actions
	Given the tenant configuration defined by 'smoketest-org.json'
	And the scenario test '<SetName>' defined by '<JsonFile>'
	When I 'commit' the scenario  
	And the scenario is successful
	Given the scenario test '<SetName2>' defined by '<JsonFile2>'
	When I 'calculate' the scenario and check results

	Examples:
	| Testname                   | JsonFile                                                                     | JsonFile2                                                                                 | SetName               | SetName2           | 
	| Merge territory-salesforce | Smoketest/Scenario/MergeTests/MergeRootObjects-Territory-SalesForce-Add.json | Smoketest/Scenario/MergeTests/Run-Merge-SalesForce-Territory-ADD-EDIT-EFFE-EXPR-PEXP.json | AutoCommit-Merge-Root | AutoSimulate-Merge | 

@Core @Engine
Scenario Outline: Double Scenario Merge Rule
	Given the tenant configuration defined by 'smoketest-org.json'
	And I commit initial data '<SetupFile>' and check results
	Given the scenario test '<SetName2>' defined by '<JsonFile2>'
	When I 'calculate' the scenario and check results

	Examples:
	| Testname   | SetupFile                                         | JsonFile2                                                        | SetName2           | 
	| Merge rule | Smoketest/Scenario/MergeTests/Rule-Setup-ADD.json | Smoketest/Scenario/MergeTests/Rule-Test-EDIT-EFFE-EXPR-PEXP.json | AutoSimulate-Merge |
