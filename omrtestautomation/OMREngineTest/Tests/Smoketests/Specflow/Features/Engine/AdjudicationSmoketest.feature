Feature: Adjudication tests
	Adjudication based scenarios

@CORE @Engine
Scenario Outline: Future scenario based adjudication
	C16123900 AccountExplicit and AccountExclusion created by CRs have correct CreatedBy user
	C16123902 AccountExplicit and AccountExclusion created by CRs have correct LastModifiedBy user
	C15539890 GeographyTerritory created by CRs have correct CreatedBy user
	C16123903 GeographyTerritory created by CRs have correct LastModifiedBy user
	C8143798 Adjudication API use case 3: Approver simulation
	C8231496 Future Adj: Affiliation Rule + Explicit & Exclusion - Add Account CR - Merge
	C8231498 Future Adj: Affiliation Rule + Explicit & Exclusion - Delete Account CR- Merge
	C8231499 Future Adj: Affiliation Rule + Explicit & Exclusion - Delete Account CR within Explicit- Merge
	C8231479 Current/Future Adj: Affiliation Rule + Explicit & Exclusion - Initial Adjudication simulate
	C8231484 Future Adj: Affiliation Rule + Explicit & Exclusion - Add Account CR (Adj simulated by REP)
	C8231487 Future Adj: Affiliation Rule + Explicit & Exclusion - Delete Account CR (Adj simulated by REP)
	C8231489 Future Adj: Affiliation Rule + Explicit & Exclusion - Delete Account CR within Explicit (Adj simulated by REP)
	C8231502 Future Ajd + Scenario: Affiliation Rule + Explicit & Exclusion (Adj simulated by REP)
	
	Also covers OMR-16903 Adjudication simulation should store gains/losses at the time the simulation runs
	C15602170 Future adjudication (scenario based) GainLoss - Account Add/Delete CRs
	C17730370 Future adjudication (scenario based) GainLoss - Geography Add/Delete CRs
	C17765825 Future adjudication (scenario based) GainLoss + Affliliation Rule

	Given the tenant configuration defined by 'smoketest-org.json'
	And the org is setup for adjudication
	And the optimizer settings '<ClientSettings>'
	
	# Set up data for scratch-org test
	And I post the salesforce users in file 'Adjudication/AdjudicationSmoketests/AdjUsersIE.json' 
	And I bulkload from the bulkload settings in 'Adjudication/AdjudicationSmoketests/BulkloadAccountsForAdj.json'
	
	# HO creates the scenario, releases the adjudication
	Given I assume the ho identity
	And I commit initial data '<DataPath>/InitialScenario.json' and check results
	And the scenario test '<Scenarioname>' defined by '<DataPath>/<ScenarioSetup>'
	When I 'simulate' the scenario and check results
	Given the scenario based adjudication defined in '<DataPath>/<AdjudicationSetup>'
	When I release the adjudication
	Then check all results per '<DataPath>/<AdjudicationSetup>'

	# Rep CRs, simulate, submit
	Given I assume the rep identity
	And the change requests '<DataPath>/<RepCRs>'
	When as the requester I simulate the adjudication with PullChangeRequests
	Then check all results per '<DataPath>/<RepCRs>'
	When I submit the change requests to be approved

	# DM CRs, simulate, submit, approve
	Given I assume the dm identity
	And the change requests '<DataPath>/<DmCRs>'
	When as the approver I simulate the adjudication
	Then check all results per '<DataPath>/<DmCRs>'
	When I submit the change requests to be approved
	And I approve the account change requests
	And I approve the geography change requests

	# HO closes the adjudication, commits the linked scenario
	Given I assume the ho identity
	When I close the adjudication
	And I 'commit' the adjudication linked scenario
	Then check all results per '<DataPath>/FinalAdjudicationResults.json'

	Examples:
	| Scenarioname                  | DataPath                                   | AdjudicationSetup | ScenarioSetup       | ClientSettings | RepCRs                | DmCRs                |
	| ScenarioAdjudicationSmoketest | Adjudication/AdjudicationSmoketests/Future | Adjudication.json | FutureScenario.json | default        | ChangeRequestRep.json | ChangeRequestDm.json |

@CORE @Engine 
Scenario Outline: Current adjudication user assignment
	C16123900 AccountExplicit and AccountExclusion created by CRs have correct CreatedBy user
	C16123902 AccountExplicit and AccountExclusion created by CRs have correct LastModifiedBy user
	C15539890 GeographyTerritory created by CRs have correct CreatedBy user
	C16123903 GeographyTerritory created by CRs have correct LastModifiedBy user
	C8231479 Current/Future Adj: Affiliation Rule + Explicit & Exclusion - Initial Adjudication simulate

	Also covers OMR-16903 Adjudication simulation should store gains/losses at the time the simulation runs
	C15602159 Current adjudication (non scenario based) GainLoss - Account Add/Delete CRs
	C17730369 Current adjudication (non scenario based) GainLoss - Geography Add/Delete CRs
	C17765826 Current adjudication (non scenario based) GainLoss + Affiliation Rule

	Given the tenant configuration defined by 'smoketest-org.json'
	And the org is setup for adjudication
	And the optimizer settings '<ClientSettings>'

	# Set up data for scratch-org test
	And I post the salesforce users in file 'Adjudication/AdjudicationSmoketests/AdjUsersIE.json' 
	And I bulkload from the bulkload settings in 'Adjudication/AdjudicationSmoketests/BulkloadAccountsForAdj.json'
	
	# HO creates the scenario, releases the adjudication
	Given I assume the ho identity
	And I commit initial data '<DataPath>/<SetupFile>' and check results
	Given the current adjudication defined in '<DataPath>/<AdjudicationFile>'
	When I release the adjudication
	Then check all results per '<DataPath>/<AdjudicationFile>'
	
	# Rep CRs, simulate, submit
	Given I assume the rep identity
	And the change requests '<DataPath>/<RepCRs>'
	When as the requester I simulate the adjudication
	Then check all results per '<DataPath>/<RepCRs>'
	When I submit the change requests to be approved

	# DM CRs, simulate, submit, approve
	Given I assume the dm identity
	And the change requests '<DataPath>/<DmCRs>'
	When as the approver I simulate the adjudication
	Then check all results per '<DataPath>/<DmCRs>'
	When I submit the change requests to be approved
	And I approve the account change requests
	And I approve the geography change requests

	# HO closes the adjudication, commits the linked (dummy) scenario
	Given I assume the ho identity
	When I close the adjudication
	And process the current adjudication
	Then check all results per '<DataPath>/FinalAdjudicationResults.json'

	Examples:
	| Testname                     | DataPath                                    | AdjudicationFile         | SetupFile            | ClientSettings | RepCRs                | DmCRs                |
	| CurrentAdjudicationSmoketest | Adjudication/AdjudicationSmoketests/Current | CurrentAdjudication.json | InitialScenario.json | default        | ChangeRequestRep.json | ChangeRequestDm.json |

@CORE @Engine 
Scenario Outline: Automatic count metrics
	C17715898 OMScenarioSummary2 has CurrencyIsoCode per the Region
	C17715896 OMTerritoryAdjudicationResult has CurrencyIsoCode per the Region 
	C17734947 Automatic count Metrics & Rollup - Default settings
	C17734948 Automatic count Metrics & Rollup - Regional settings
	C17734949 Adjudication Results for Automatic metric counts
	C17735656 Apply Automatic count metrics on approval of current adjudication

	Given the tenant configuration defined by 'smoketest-org.json'
	And the test data path Adjudication/AdjudicationSmoketests
	And the org is setup for adjudication

	# Set up data
	And I post the salesforce users in file 'AdjUsersIE.json' 
	And I bulkload from the bulkload settings in 'BulkloadAccountsForAdj.json'

	# Create the scenario, check ScenarioSummary, Metrics
	And the scenario test '<TestName>' defined by '<ScenarioSetup>'
	When I '<ExecutionType>' the scenario and check results

	# Create the adjudication, check initial results
	Given the adjudication defined as '<AdjudicationSetup>' is scenario based '<IsScenarioBased>'
	When I release the adjudication
	Then check all results per '<AdjudicationSetup>'

	# Add/remove accounts and geos, check before and after results
	Given the change requests '<AdjCRs>'
	When as the requester I simulate the adjudication
	Then check all results per '<AdjCRs>'

	# Approve all, process and check results
	When I approve the account change requests
	And I approve the geography change requests
	And I close the adjudication
	And I <ExecutionType> the adjudication results
	Then check all results per 'AutoCounts/FinalResults.json'

	Examples:
	| TestName                    | ExecutionType | IsScenarioBased | AdjCRs                            | AdjudicationSetup            | ScenarioSetup                           |
	| Scenario-based-adjudication | calculate     | true            | AutoCounts/AdjChangeRequests.json | AutoCounts/Adjudication.json | AutoCounts/InitialScenario-Future.json  |
	| Current-adjudication        | commit        | false           | AutoCounts/AdjChangeRequests.json | AutoCounts/Adjudication.json | AutoCounts/InitialScenario-Current.json |
