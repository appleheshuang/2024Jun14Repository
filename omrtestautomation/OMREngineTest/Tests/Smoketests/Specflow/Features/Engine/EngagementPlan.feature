Feature: EngagementPlan
	Create, adjudicate, commit and sync engagement plans

@CORE @Engine
Scenario: Engagement plan in scenario
	Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings '<OptimizerConfigs>'
	And I post the salesforce users in file '<UserFile>'
	And the scenario test '<Scenarioname>' defined by '<ScenarioFile>'
	When I '<TestType>' the scenario and check results

	Examples:
	| UserFile            | TestType  | ScenarioFile                            | Scenarioname        | OptimizerConfigs                           |
	| Users/Userdata.json | calculate | EngagementPlan/EngagementSmokeTest.json | EngagementSmoketest | RuleEngineEnabled=true;CalculateMaps=false |

@Smoketest @Engine
Scenario: Engagement plan with adjudication and commit

Also covers OMR-16903 Adjudication simulation should store gains/losses at the time the simulation runs
C15602175 Future adjudication (scenario based) + Engagement Plan via scenario GainLoss - only Account Add CRs

	Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings '<OptimizerConfigs>'
	And I post the salesforce users in file '<UserFile>'
	And the scenario test '<ScenarioName>' defined by '<ScenarioFile>'
	When I 'simulate' the scenario and check results
	Given the scenario based adjudication defined in '<AdjudicationFile>'
	When I release the adjudication with request body in '<InitialUserAssignment>'
	Then check all results per 'EngagementPlan/ScenarioAdjudicationEngagementTests.json'
	Given the change requests '<AdjudicationChangeRequestFile>'
	#Re-simulate as a rep with the changes
	When I simulate the adjudication with request body in '<AdjudicationSimulationFile>' 
	Then check all results per 'EngagementPlan/AdjudicationChangeRequestedAlignmentTests.json'
	#Submit the change request to be approved
	When I submit the change requests to be approved
	#Approve the change request
	And I approve the account change requests
	And I close the adjudication
	And I 'commit' the adjudication linked scenario
	Then check all results per '<FinalAlignmentsFile>'

	Examples:
	| ScenarioName                    | UserFile            | TestType | AdjudicationFile                                      | ScenarioFile                                            | OptimizerConfigs                           | AdjudicationSimulationFile                                         | AdjudicationChangeRequestFile                            | InitialUserAssignment                                            | FinalAlignmentsFile                                |
	| EngagementAdjudicationSmoketest | Users/Userdata.json | commit   | EngagementPlan/Adjudicate/FutureAdjudicationData.json | EngagementPlan/Adjudicate/ScenarioAdjudicationData.json | RuleEngineEnabled=true;CalculateMaps=false | EngagementPlan/Adjudicate/AdjudicationChangeRequestSimulation.json | EngagementPlan/Adjudicate/AdjudicationChangeRequest.json | EngagementPlan/Adjudicate/InitialAdjudicationUserAssignment.json | EngagementPlan/FinalAdjudicatedAlignmentTests.json |

@Smoketest @OCESync 
Scenario: OCESync with Engagement plan adjudication and scenario commit
	Given the tenant configuration defined by 'ocesync-org.json'
	And the optimizer settings '<OptimizerConfigs>'
	And a unique id for the test
	And I post native salesforce 'Account' from file 'OCEData/OCEAccounts.json'
	#When I trigger a successful DataSync 'ACCOUNT_PULL' with parameter body defined by 'DataSync/DataSyncAccountPull.json'
	When I PULL_ACCOUNT to-from OCE-P with 'default' mode
	Then check account-sync results per '<SyncTestFile>'
	Given I post the salesforce users in file 'Users/Userdata.json'
	And I post unique salesforce 'Geography' from file 'EngagementPlan/OCE-P/Geography.json'
	And I wait for 'Geography' identified by 'UniqueIntegrationId' to static sync
	And the scenario test '<Scenarioname>' defined by '<ScenarioFile>'
	When I 'simulate' the scenario 
	And the scenario is successful
	Then check all results per '<ScenarioFile>'
	Given the scenario based adjudication defined in '<AdjudicationFile>'
	When I release the adjudication with request body in '<InitialUserAssignment>'
	Then check all results per '<AdjudicationFile>'
	Given the change requests '<AdjudicationChangeRequestFile>'
	#Re-simulate as a rep with the changes
	When I simulate the adjudication with request body in '<AdjudicationSimulationFile>' 
	Then check all results per 'EngagementPlan/OCE-P/AdjudicationChangeRequestedAlignmentTests.json'
	#Submit the change request to be approved
	When I submit the change requests to be approved
	#Approve the change request
	And I approve the account change requests
	And I close the adjudication
	And I 'commit' the adjudication linked scenario
	Then check all results per '<FinalAlignmentsFile>'
	When the scenario subjob publishOther is successful within 15 min  
	And the scenario subjob publishAlignments is successful within 15 min
	#Added verification for OMR-19933 to check OCE__PlanCycle__c in OCEP
	Then check all results per '<SyncTestFile>'
	#Post CustomerAlignmentRule object to do SearchBeforeCreate
	Given I post the OCE objects in file 'OCEData/OCEData.json'
	When I PULL_ACCOUNT to-from OCE-P with 'default' mode
	#Added verification for OMR-19933 to check Plan Cycle Name in OMPlanCycle
	Then check all results per 'EngagementPlan/OCE-P/OMR-9817/RetainAuditAttributeAccountExplicit.json'

	Examples:
	| Scenarioname               | AdjudicationFile                                                  | ScenarioFile                                  | OptimizerConfigs                           | AdjudicationSimulationFile                                         | AdjudicationChangeRequestFile                            | SyncTestFile                              | InitialUserAssignment                                            | FinalAlignmentsFile                                      |
	| AdjudicationEngagementPlan | EngagementPlan/OCE-P/ScenarioAdjudicationEngagementSmoketest.json | EngagementPlan/OCE-P/EngagementSmokeTest.json | RuleEngineEnabled=true;CalculateMaps=false | EngagementPlan/Adjudicate/AdjudicationChangeRequestSimulation.json | EngagementPlan/Adjudicate/AdjudicationChangeRequest.json | EngagementPlan/OCE-P/OCESyncTestData.json | EngagementPlan/Adjudicate/InitialAdjudicationUserAssignment.json | EngagementPlan/OCE-P/FinalAdjudicatedAlignmentTests.json |
	
@CORE @Engine
Scenario: OMR-15972 Load Engagement Results
 OMR-15972 Load Engagement Results, apply HO changes
 Test cases:
  C12526329 Rules not applied for loaded engagement plan
  C12527770 EngagementExplicits created
  C12526330 Explicits created when there is no dynamic or explicit alignment
  C12526331 Partial explicit created when alignment does not cover entire plan period
  C12527771 Explicit created by EngagementPlan load takes precedence over exclusion
  C12527772 Explicit not create when alignment already exists for the entire plan period
  C12527784 Generate affiliation rule based alignments when an affiliated account is added by EP load
  C12578974 Territory does not cover entire eng plan -> observe targets, create partial explicit
  C12527781 HO Changes - Remove account that is added by EP
  C12527780 HO Changes - Edit targets for an account that is added by EP
  C12614013 HO Changes - Edit targets for an account that is added by affiliation

	Given the tenant configuration defined by '<OrgType>-org.json'
	And a unique id for the test

	# Set up data for scratch-org test
	Given I post the salesforce users in file '<TestDir>/AdjUsers_15972.json'
	And I bulkload from the bulkload settings in '<TestDir>/BulkloadAccount.json'

	# Add Engagement Plan with loaded results, explicits and exclusions, rules
	Given the scenario test '<ScenarioName>' defined by '<TestDir>/<ScenarioName>.json'
	When I 'calculate' the scenario and check all results

	# Add HO Changes Requests to the scenario and resimulate (DEL,EDIT)
	Given the scenario test '<ScenarioName>' defined by '<TestDir>/<ScenarioName>-HOChanges.json'
	When I 'calculate' the scenario and check all results

	Examples:
	| ScenarioName         | OrgType   | TestDir                                       | 
	| LoadEngagementResult | smoketest | EngagementPlan/OMR-15972-LoadEngagementResult |

@CORE @Engine
Scenario: OMR-15972 Adjudicate Loaded Engagement Results
 OMR-15972 Load Engagement Results, apply HO changes, apply rep chnages
 Test cases:
  C12527778 Adjudication - Edit targets for an account that is added by EP
  C12527779 Adjudication - Remove account that is added by EP
  C12614011 Adjudication - Edit targets for an account that is added by affiliation
  C15525616 Adjudication - Retain targets for an affiliated account, already updated by HO
  https://jiraims.rm.imshealth.com/wiki/display/TMD/OMR-15972+Bulkload+Engagement+Plan

  OMR-16903 Adjudication simulation should store gains/losses at the time the simulation runs
  C15602176 Future adjudication (scenario based) + Engagement Plan via bulkload GainLoss - Account Add/Delete CRs
  C15602177 GainLoss not updated for Edit CRs (GainLoss=NULL)

	Given the tenant configuration defined by '<OrgType>-org.json'
	And a unique id for the test

	# Set up data for scratch-org test
	Given I post the salesforce users in file '<FeatureDir>/AdjUsers_15972.json'
	And I bulkload from the bulkload settings in '<TestDir>/BulkloadAccountsForAdj.json'

	# Add Engagement Plan with loaded results, explicits and exclusions, rules
	Given the scenario test '<ScenarioName>' defined by '<TestDir>/<ScenarioName>.json'
	When I 'calculate' the scenario and check all results

	# Add HO Changes Requests to the scenario and resimulate
	Given the scenario test '<ScenarioName>' defined by '<TestDir>/<ScenarioName>-HOChanges.json'
	When I 'calculate' the scenario and check all results

	# Adjudicate - Add rep CRs, approve and re-simulate
	Given the scenario based adjudication defined in '<TestDir>/<AdjudicationDefinition>'
	When I release the adjudication
	Then check initial results per '<TestDir>/<AdjudicationDefinition>'
	Given the change requests '<TestDir>/<AdjudicationCRs>'
	When as the requestor I simulate the adjudication
	Then check all results per '<TestDir>/<AdjudicationCRs>'
	When I close the adjudication
	And I 'calculate' the adjudication linked scenario
	Then check all results per '<TestDir>/FinalResults.json'

	Examples:
	| ScenarioName       | OrgType   | AdjudicationDefinition       | AdjudicationCRs    | TestDir                                                        | FeatureDir                                       |
	| LoadEPResultForAdj | smoketest | AdjudicateLoadedEngPlan.json | RepEngPlanCRs.json | EngagementPlan/OMR-15972-LoadEngagementResult/AdjudicateBulkEP | EngagementPlan/OMR-15972-LoadEngagementResult |

@CORE @Engine @OCESync
Scenario: OMR-15972 Publish Loaded Engagement Results
 OMR-15972 Load Engagement Results, commit, publish, root cause
 Test cases:
  C12577742 Publish bulk loaded engagement plan
  C12527776 RCA shows the cause of an alignment added by EngagementPlan load

	Given the tenant configuration defined by '<OrgType>-org.json'
	And a unique id for the test

	# Set up data for publish test
	And I post native salesforce 'Account' from file '<TestDir>/OceAccountsForEP.json'
	When I PULL_ACCOUNT to-from OCE-P with 'default' mode

	# Commit Loaded Engagement Plan, publish, check RCA
	Given the scenario test '<ScenarioName>' defined by '<TestDir>/<ScenarioName>.json'
	When I 'commit' the scenario
	And the scenario is successful
	And the scenario subjob publishOther is successful within 15 min  
	Then check all results per '<TestDir>/<ScenarioName>.json'
	And I check the root cause of alignments '<TestDir>/RCA/'

	Examples:
	| ReplaceEPScenario | ScenarioName  | OrgType | TestDir                                                     |
	| NewEP             | PublishBulkEP | ocesync | EngagementPlan/OMR-15972-LoadEngagementResult/PublishBulkEP |

@CORE @OCESync 
#C12579665 - Verify new OCE__PlanCycle__c data syncs to STATIC.OMPlanCycle
#C12579667 - Verify new OCE__ActivityPlan__c data syncs to STATIC.OMActivityPlan
#C18049765 - Verify updated plan cycle name in OCEP is synced to OCEO
Scenario: OMR-16741 Engagement Plan Call reporting
	Given the tenant configuration defined by 'ocesync-org.json'
	And the optimizer settings '<OptimizerConfigs>'
	And a unique id for the test
	# Create the Account in OCE-P and pull to OMR
    And I post native salesforce 'Account' from file 'OCEData/OCEAccounts.json'
	When I PULL_ACCOUNT to-from OCE-P
	Given I post the salesforce users in file 'Users/Userdata.json'
	And I post unique salesforce 'Geography' from file 'EngagementPlan/OCE-P/Geography.json'
	And I wait for 'Geography' identified by 'UniqueIntegrationId' to static sync
	Given I commit initial data '<ScenarioFile>'
	When the scenario subjob publishOther is successful within 15 min  
	And the scenario subjob publishAlignments is successful within 15 min
	#Updated plan cycle name and sync data to OCEO -  OMR19333
	Given I patch oce salesforce 'OCE__PlanCycle__c' from file 'OCEData/OCEPlanCycle.json'
	When I PULL_ACCOUNT to-from OCE-P
	Then check all results per 'OCEData\OMR-16741-EngagementPlanReporting\OMR-16741-QueryResults.json'

	Examples:
	| Scenarioname            | ScenarioFile                                  | OptimizerConfigs                           |
	| EngagementCallReporting | EngagementPlan/OCE-P/EngagementSmokeTest.json | RuleEngineEnabled=true;CalculateMaps=false |  
