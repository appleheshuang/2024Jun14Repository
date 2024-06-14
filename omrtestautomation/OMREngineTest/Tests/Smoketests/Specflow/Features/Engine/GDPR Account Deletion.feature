Feature: GDPR Account Deletion


@CORE @Engine 
Scenario Outline: GDPR Account Deletion
Covers C446918452, C446918453, C446918454, C446918455, C446918456, C446918457
	Given the tenant configuration defined by 'ocesync-org.json'
	And the optimizer settings '<Params>'
	And a unique id for the test
	And the org is setup for adjudication
	And I post the salesforce users in file 'Adjudication/AdjudicationSmoketests/AdjUsersIE.json' 
	And I submit a unique bulkload request from file '<AccountLoadFile>'
	Then The bulkload is successful
	Given I commit initial data 'Smoketest/LoadData/omr-18946/GDPRScenario.json'
	And I submit a unique bulkload request from file '<PostCommitBulkloadFiles>'
	Then The bulkload is successful
	Given I assume the ho identity
	And the current adjudication defined in 'Adjudication/AdjudicationSmoketests/Current/CurrentAdjudication.json'
	When I release the adjudication
	Then I run the recalc 'PopulateOMAccountPurge'
	When I PUBLISH_CUSTOMER_ALIGNMENT to-from OCE-P
	Then I run the recalc 'AccountDataProtectionAction'
	And check all results per '<ResultsFile>'

	Examples:
	| TestName        | AccountLoadFile                                | ResultsFile                                           | PostCommitBulkloadFiles                                  | Params                                 |
	| GDPR GlobalDel  | Smoketest/LoadData/omr-18946/GDPRAccounts.json | Smoketest/LoadData/omr-18946/OMAccountPurgeTests.json | Smoketest/LoadData/omr-18946/GDPRPostCommitBulkload.json | GlobalAccountDataProtectionAction=DEL  |
	| GDPR GlobalMask | Smoketest/LoadData/omr-18946/GDPRAccounts.json | Smoketest/LoadData/omr-18946/OMAccountMaskTests.json  | Smoketest/LoadData/omr-18946/GDPRPostCommitBulkload.json | GlobalAccountDataProtectionAction=MASK |

