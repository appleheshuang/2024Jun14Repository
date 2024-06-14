Feature: Recalc
	Recalc Tests
	@engine

@MAT @Engine
Scenario Outline: Deactivate account and recalc 
	Deactivate account and recalculate date and also verifies retention history of OMAccount table for the INAC account
	C7963762: OData 'as-of-date' simple query for a table with Retention_History > 1 day
	C7967768: Odata 'as-of-date' queries with 'filter'
	# C15522294 - odata call with SalesForce-API-KEY and as-of-date in the past returns actual state of the table
	# C15522295 - odata call with LEXI-API-KEY and as-of-date in the past returns current state of the table	
	# C15533370 - odata call with LEXI-API-KEY and as-of-date in the past BEFORE DB creation returns current state of the table

	Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings 'RuleEngineEnabled=true;CalculateMaps=false'
	And a unique id for the test
	And I bulkload unique '<AccountLoadFile>'
	Given the scenario test 'Recalc-prep' defined by '<Path><SetupFile>'
	#Bulkload unique Accounts
	When I 'commit' the scenario and check results
	And I 'INAC' the account '<RecalcAccountId>' and run a recalc
	Then check all results per '<Path><PostRecalcTestFile>'
	When I use lexi-api-key instead of sf-api-key
	Then check all results per '<Path><PostRecalcTestFile2>'

	Examples:
	| PostRecalcTestFile                 | PostRecalcTestFile2                 | RecalcAccountId                    | SetupFile            | AccountLoadFile                       | Path                |
	| RecalcWithInactiveAccountTest.json | RecalcWithInactiveAccountTest2.json | RECALC-{{TestUniqueIntegrationId}} | RecalcSmokeTest.json | Smoketest/LoadData/RecalcAccount.json | Smoketest/Scenario/ |

@CORE @Engine
# C318666166 - Verify rollback occurs when OMRegion.NightlyThreshold = 0, OMRegion.ThresholdAction = ERROR and OMRecalcNotification.PercentageChange is 0
# C318666167 - Verify rollback occurs when OMRegion.NightlyThreshold = 100%, OMRegion.ThresholdAction = ERROR and OMRecalcNotification.PercentageChange is 100%
# C318666168 - Verify rollback occurs when OMRegion.NightlyThreshold = 100%, OMRegion.ThresholdAction = ERROR and OMRecalcNotification.PercentageChange is 200%
# C318666169 - Verify rollback does not occur when OMRegion.ThresholdAction = ERROR and OMRegion.Threshold > OMRecalcNotification.PercentageChange
# C318666171 - All accounts under one rule to another valid rule should not generate errors
Scenario: OMR-15543 Changing OMAccount specialty and running recalc
	Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings 'RuleEngineEnabled=true;CalculateMaps=false'
	And the test data path Smoketest/LoadData/omr-15543
	And a unique id for the test
	#Reset the accounts to its original state so that we can re-run the same tests again without change in behaviour
	Given I submit the bulkload '<InitialSpecialties>'
	And I patch omr salesforce 'Region' from file 'ClearThresholds.json'
	And I check that updates have static synced per 'ClearThresholds.json'
	Then I run a recalc

	Given I patch omr salesforce 'Region' from file 'SetThresholds.json'
	And I check that updates have static synced per 'SetThresholds.json'
	And the scenario test 'Rollback-recalc-prep' defined by '<SetupFile>'
	When I 'commit' the scenario
	And the scenario is successful
	#Change some of the accounts such that when a recalc is run, some regions will have their NightlyThreshold limits exceeded
	Given I submit the bulkload '<SpecialtyChangeFiles>'
	Then I run a recalc
	Then check all results per '<PostRecalcTestFile>'

	Examples:
	| TestName                      | SetupFile                        | SpecialtyChangeFiles            | PostRecalcTestFile                       | InitialSpecialties      |
	| Realign to valid rule         | RollbackChangeRuleSmoketest.json | RollbackDataChangeSpecialty.json        | RecalcWithRollbackValidRuleTest.json     | RollbackAccountSpecialties.json |
	| Realign to non-existent rule  | RollbackChangeRuleSmoketest.json | RollbackDataChangeSpecialtyInvalid.json | RecalcWithRollbackINACorInvalidTest.json | RollbackAccountSpecialties.json |
	| INAC accounts aligned to rule | RollbackChangeRuleSmoketest.json | RollbackDataChangeINACAccounts.json     | RecalcWithRollbackINACorInvalidTest.json | RollbackAccountSpecialties.json |
