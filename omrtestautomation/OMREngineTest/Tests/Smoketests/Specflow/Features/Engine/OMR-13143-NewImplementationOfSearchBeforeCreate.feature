Feature: OMR-13143
	New implementation of Search-Before-Create. Pending = Approved

@MAT @OCESync 
Scenario: SBC with ApprovalStatus approved, pending -> rejected
	# C10190744 Existing territory alignments added today are removed (INAC) when Approval Status is Pending -> Rejected
	Given the tenant configuration defined by 'ocesync-org.json'
	And the optimizer settings '<OptimizerConfigs>'
	And a unique id for the test
	And I post native salesforce 'Account' from file 'OCEData/OCEAccounts.json'
	When I PULL_ACCOUNT to-from OCE-P
	Then check account-sync results per '<Path>/OMR-13143SyncTestResults.json'
	Given I commit initial data '<ScenarioFile>' and publish other
	Then check Territory2 results per '<Path>/OMR-13143SyncTestResults.json'
	Given I post oce salesforce 'CustomerAlignmentRule' from file '<Path>/OMR-13143CustomerAlignmentRulesApprovedPending.json'
	When I PULL_ACCOUNT to-from OCE-P
	Then check ApprovedPendingResults results per '<Path>/OMR-13143CheckAccountExplicits.json'
	Given I post oce salesforce 'CustomerAlignmentRule' from file '<Path>/OMR-13143CustomerAlignmentRulesRejected.json'
	When I PULL_ACCOUNT to-from OCE-P
	Then check RejectedResults results per '<Path>/OMR-13143CheckAccountExplicits.json'

	Examples:
	| TestName                                    | Path                                                  | ScenarioFile                       | OptimizerConfigs                           |
	| EndDating existing SBC territory alignments | OCEData/OMR-13143-NewSearchBeforeCreateImplementation | OMR-13143ScenarioTerritoryAdd.json | RuleEngineEnabled=true;CalculateMaps=false |

@MAT @OCESync
Scenario: SBC with existing accounts explicits and exclusions
	# C10190747 Account-territory alignment not created with an active (EffectiveDate<TODAY<EndDate) territory/salesforce exclusion
	# C10190748 Account-territory alignment created with EndDate=AccountExclusion.EffectiveDate-1)
	# C10204429 Existing OMR generated territory alignments are not end dated by SBC process
	# C10348344 AccountExplicits not created when existing OMR/SBC generated AccountExplicit is active
	# C10348345 Account-territory alignment created with EndDate=AccountExplicit.EffectiveDate-1

	# DataProtection testcases
	# C446918461 Account pull does not create records for OMAccounts in OMAccountPurge where OMAccountPurge.PurgeCompleted != null 

	Given the tenant configuration defined by 'ocesync-org.json'
	And the optimizer settings '<OptimizerConfigs>'
	And a unique id for the test
	And I post native salesforce 'Account' from file '<Path>/UniqueOCEAccounts.json'
	When I PULL_ACCOUNT to-from OCE-P
	Then check account-sync results per '<Path>/OMR-13143SyncTestResults.json'
	Given I commit initial data '<ScenarioFile>' and publish other
	Then check Territory2 results per '<Path>/OMR-13143SyncTestResults.json'
	Given I post oce salesforce 'CustomerAlignmentRule' from file '<CustomerAlignmentRuleFile>'
	When I PULL_ACCOUNT to-from OCE-P
	Then check all results per '<Path>/OMR-13143CheckExplicitMergeWithExisting.json'
	When I dataprotect the account 'PendingFutureExclusion-{{TestUniqueIntegrationId}}' and run a recalc
	Then check all results per 'Smoketest/LoadData/omr-18946/OMR-13143CheckAfterDataProtection.json'
	Given I post oce salesforce 'CustomerAlignmentRule' from file '<CustomerAlignmentRuleFile>'
	When I PULL_ACCOUNT to-from OCE-P
	Then check all results per 'Smoketest/LoadData/omr-18946/OMR-13143CheckAfterDataProtection.json'


	Examples:
	| Path                                                  | ScenarioFile                                  | CustomerAlignmentRuleFile               | OptimizerConfigs                                                                 |
	| OCEData/OMR-13143-NewSearchBeforeCreateImplementation | OMR-13143ScenarioWithExplicitsExclusions.json | OMR-13143CustomerAlignmentRulesAll.json | RuleEngineEnabled=true;CalculateMaps=false;GlobalAccountDataProtectionAction=DEL |
	