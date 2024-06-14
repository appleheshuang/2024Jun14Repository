Feature: Account Merge
OMR-942 Match-merge of account territory alignment / ALG-REQ029
OMR-582 Merge API

@CORE @Schema
Scenario Outline: Account Merge Optimizer
	C4332356 Successful OMAccount Merge
	C4332357 OMAccount Merge with no initial Explicit/Exclusion overlaps
	C4333369 OMAccount Merge having the winner with no initial Explicts/Exclusions
	C4333370 OMAccount Merge having Explicit/Exclusion overlaps
	C4333371 OMAccount Merge with Loosing Account having more Explicit/Exclusion
	C4333372 OMAccount Merge with no child tables
	C4333373 OMAccount Merge with different Territories

	Given the tenant configuration defined by 'smoketest-org.json'
	And the test data path <Path>
	And a unique id for the test
	# setup test data
	And I bukload '<OMAccounts>' using temp credentials
	Given the scenario test 'AccountMerge-Setup' defined by 'SetupTerritoryRule.json'
	And I add to the scenario '<AdditionalActions>'
	When I 'commit' the scenario and check results
	Then check all results in '<AdditionalActions>'
	# Merge, recalc and check results
	When I merge accounts as '<WinnerLoserRequest>'
	Then I run a recalc
	And check all results in '<FinalResultsAll>'

	Examples:
	| TestName | Path         | OMAccounts    | AdditionalActions        | WinnerLoserRequest | FinalResultsAll       |
	| OMR      | AccountMerge | OMAccount.csv | ScenarioActions-AllCases | Merge.json         | MergeResults-AllCases |
