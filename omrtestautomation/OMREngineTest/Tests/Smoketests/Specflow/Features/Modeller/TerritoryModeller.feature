Feature: Territory Modelling Service

#@CORE @Latest
@InTest
Scenario Outline: Model end-to-end
	OMR-20297 Scenario calculate to support Model Id
	C18054528 Simulate the model with no scenario actions 
	C18054529 Simulate model with scenario actions 

	Given the tenant configuration defined by 'smoketest-org.json'
	And the test data path <TestPath>
	And a unique id for the test
	# Setup test data
	And I commit initial data '<SetName>-InitialData.json'

	# Calculate the model
	Given the scenario test '<SetName>' defined by '<SetName>-NoActions.json'
	And I want to calculate the model
	When I 'calculate' the scenario and check results
	And the scenario has status 'CALTD'

	Given I add to the scenario '<SetName>-WithActions.json'
	When I 'calculate' the scenario 
	And the scenario is successful
	Then check all results per '<SetName>-WithActions.json'

	# Apply the model
#	Given I want to apply the model
#	When I 'calculate' the scenario and check results

	# Adjudicate
	# Commit


	Examples:
	| SetName            | TestPath            | FilePath     |
	| BalanceUSAccounts  | Modeller/End-to-end | SomeFilePath |
