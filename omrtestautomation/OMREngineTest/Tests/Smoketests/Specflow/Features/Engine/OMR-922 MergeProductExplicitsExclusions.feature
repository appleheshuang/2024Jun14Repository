Feature: Scenario tests OMR-922

@MAT @Engine
Scenario Outline: OMR-922 positive case (C7694300, C7694301)
	Given the tenant configuration defined by 'smoketest-org.json'
	And I commit initial data '<SetUpFile>' 
	Given the scenario test '<SetName>' defined by '<JsonFile>'
	When I 'calculate' the scenario and check results

	Examples:
	| SetName            | SetUpFile                                                  | JsonFile                                                 | 
	| Test-ProdExpl/Excl | Scenario/MergeTests/Setup-OMProductExplicit-Exclusion.json | Scenario/MergeTests/Test-Add-ProdExpl-Excl-Positive.json | 


@MAT @Engine
Scenario Outline: OMR-922 negative case (C7694300, C7694301, C7707133, C7707140)
	Given the tenant configuration defined by 'smoketest-org.json'
	And I commit initial data '<SetUpFile>' 
	Given the scenario test '<SetName>' defined by '<JsonFile>'
	When I 'calculate' the scenario 
	And the scenario fails
	And the scenario is 'ERROR'

	Examples:
    | SetName            | SetUpFile                                                  | JsonFile                                                 | 
    | Test-ProdExpl/Excl | Scenario/MergeTests/Setup-OMProductExplicit-Exclusion.json | Scenario/MergeTests/Test-Add-ProdExpl-Excl-Negative.json |