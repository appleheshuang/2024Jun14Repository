Feature: OMR-935 InactiveUncalledAccounts
	Related Task:
	  - OMR-11388 Automate - Inactivate uncalled

@CORE @OCESync
Scenario: InactiveUncalledAccounts_PositiveCase
# C7951145 OMR-3224 Sync Call History Data from OCE Sales
# C8213304 "ExplicitAccountTerritoryAlignment": true, x days configured to > 0, no call in history, Don't remove if x days have not passed since effective date
# C9660822 "ExplicitAccountTerritoryAlignment": true, x days configured to > 0 , have no call in history, remove
# C7955769 "ExplicitAccountTerritoryAlignment": true, x days configured to > 0 , have 1 call in past X days, no remove
# C7955769 "ExplicitAccountTerritoryAlignment": true, x days configured to > 0, no call, Account Explicit not from OCE-P, no remove.
# C7955774 "ExplicitAccountTerritoryAlignment": true, x days configured to > 0, have call on exact X day, no remove
	Given the tenant configuration defined by 'ocesync-org.json'
	And the optimizer settings '<OptimizerSettings>'
	And a unique id for the test
	# Create the Account in OCE-P and pull to OMR
    And I post native salesforce 'Account' from file 'OCEData\OMR-11388-InactiveUncalled\InactiveUncalledOCEAccounts.json'
	When I PULL_ACCOUNT to-from OCE-P
	# Create the Territory and AccountExplicit in OMR
	Given I commit initial data 'OCEData\OMR-11388-InactiveUncalled\TerritoryAdd.json'
	# Create calls and SBC explicits in OCE-P and pull to OMR
	Given I post oce salesforce 'Call' from file 'OCEData\OMR-11388-InactiveUncalled\Call.json'
	And I post oce salesforce 'CustomerAlignmentRule' from file 'OCEData\OMR-11388-InactiveUncalled\CustomerAlignmentRule.json'
	When I PULL_ACCOUNT to-from OCE-P
	#When I trigger a successful DataSync 'ACCOUNT_PULL' with parameter body defined by 'DataSync/DataSyncAccountPull.json'
	Then check CheckAccountExplicitAfterPullAccount results per 'OCEData\OMR-11388-InactiveUncalled\InactiveUncalled_queryResults.json'
	Then I run the recalc '<TestName>'
	Then check all results per 'OCEData\OMR-11388-InactiveUncalled\InactiveUncalled_queryResults.json'

	Examples:
	| TestName                                       | OptimizerSettings                                                                                      |  
	| InactivateWhenNoCallsWithinXDaysPresentRecords | InactivateUncalledUserCreatedAlignment=1;ExplicitAccountTerritoryAlignment=true;RuleEngineEnabled=true |

