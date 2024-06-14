Feature: GDPR User Deletion
	Recalc Tests
	@engine

@CORE @Engine 
Scenario Outline: GDPR User Deletion OMR org
Covers OMR-18947  GDPR User Deletion
C17756079 GDPR User Deletion om OMR org: PurgeJob scheduled with JobParameter = DEL
C17756080 GDPR User Deletion: PurgeJob scheduled with JobParameter = MASK
C17756081 GDPR User Deletion: PurgeJob scheduled with JobParameter = NONE
C17756067 OMUsers with OMUser.DataProtected = null|false do not generate OMUserPurge records
C17756068 OMUsers with OMUser.DataProtected = true generate OMUserPurge records

	Given the tenant configuration defined by 'ocesync-org.json'
	And I bulkload from the bulkload settings in '<SettingOMAuditConfig>'
	And a unique id for the test
	And I post unique salesforce 'User' from file 'Smoketest/LoadData/omr-18947/GDPRUsersToSF.json'
	And I wait for 'User' identified by 'UniqueIntegrationId' to static sync
	And I submit a unique bulkload request from file '<UsersDataLoadFile>'
	Then The bulkload is successful
	Given the optimizer settings '<OptimizerSettings>'
	Then check all results per 'Smoketest/LoadData/omr-18947/GDPRUserInitialDataChecks.json'
	Given I commit initial data 'Smoketest/LoadData/omr-18947/GDPRUserScenario.json' and check results
	Then I run the recalc 'PopulateOMUserPurge'
	When I PUBLISH_OTHER_OMR_DATA to-from OCE-P
	Then I run the recalc 'UserDataProtectionAction'
	And check all results per '<ResultsFile>'

	Examples:
	| TestName          | UsersDataLoadFile                              | ResultsFile                                             | SettingOMAuditConfig                                    | OptimizerSettings                   |
	| GDPR UserDeletion | Smoketest/LoadData/omr-18947/GDPRUserData.json | Smoketest/LoadData/omr-18947/GDPRUserDeletionTests.json | Smoketest/LoadData/omr-18947/GDPRUserOMAuditConfig.json | FieldTitleValidation=User-Territory |