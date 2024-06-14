Feature: Scenario tests OMR-13883

@CORE
Scenario Outline: OMR-13883 Recursive Child Product Cascade
	#TCs in order from top to bottom in table:
	#C10439842 Product Cascade Effective Date - Earlier effective date
	#C10439843 Product Cascade Effective Date - Later EffectiveDate
	#C10439852 Product Cascade End Date - Earlier end date  
	#C10439848 Product Cascade End Date - Later End date
	#C10439841 Product Cascade Inactivate - Inactivate Product with related entries in subtables
	#C10439855 Product Cascade Effective Date - Earlier effective date, having child entities with earlier and later effective dates
	#C10439854 Product Cascade End Date - Later end date, having child entities with earlier and later end dates
	#C10439844 Product Cascade Effective Date - New effective date is after child's end date
	#C10439853 Product Cascade End Date - End before child's start date
	#C11339465 Product Cascade Effective Date - Child product is inactive
	#C11339466 Product Cascade End Date - Child product is inactive
	Given the tenant configuration defined by 'smoketest-org.json'
	And the test data path Scenario/OMR-13883
	And a unique id for the test
	
	And I post unique salesforce 'Product' from file '<InitialProductFile>'
	And I post unique salesforce 'ProductHierarchy' from file '<InitialProductFile>'
	And I check that updates have static synced per '<InitialProductFile>'
	
	Given I commit initial data '<InitialDataFile>'
	When the scenario is successful
	Given I patch unique salesforce 'Product' from file '<ProductEditFile>'
	And I get the internal scenario with name like %'Brand-{{TestUniqueIntegrationId}}'%
	When the scenario has status 'COMTG'
	Then check all results per '<IntermittentCheckFile>'
	When the scenario has status 'COMTD'
	#Wait for OMProduct and OMProductHierarchy to staticsync changes back before checking results
	Then I wait for the table 'OMProduct' to static sync after the scenario has completed
	Then I wait for the table 'OMProductHierarchy' to static sync after the scenario has completed
	Then check all results per '<ScenarioFile>'

	Examples:
	| TestName                                                             | InitialProductFile                                                  | InitialDataFile                                                            | ScenarioFile                                                                       | ProductEditFile                                                                        | IntermittentCheckFile                                        |
	| EFFE-ToEarlier                                                       | DataSetup/Products/OMR-13883-ProductHierarchy-Setup.json            | DataSetup/Scenario/RecursiveChildProductCascadeInitialData.json            | Tests/RecursiveChildProductCascade - EFFE-ToEarlier.json                           | DataSetup/Products/OMR-13883-ProductEdit-EFFE-ToEarlier.json                           | Tests/RecursiveChildProductCascade - CheckCascadeStatus.json |
	| EFFE-ToLater                                                         | DataSetup/Products/OMR-13883-ProductHierarchy-Setup.json            | DataSetup/Scenario/RecursiveChildProductCascadeInitialData.json            | Tests/RecursiveChildProductCascade - EFFE-ToLater.json                             | DataSetup/Products/OMR-13883-ProductEdit-EFFE-ToLater.json                             | Tests/RecursiveChildProductCascade - CheckCascadeStatus.json |
	| END-ToEarlier                                                        | DataSetup/Products/OMR-13883-ProductHierarchy-Setup.json            | DataSetup/Scenario/RecursiveChildProductCascadeInitialData.json            | Tests/RecursiveChildProductCascade - END-ToEarlier.json                            | DataSetup/Products/OMR-13883-ProductEdit-END-ToEarlier.json                            | Tests/RecursiveChildProductCascade - CheckCascadeStatus.json |
	| END-ToLater                                                          | DataSetup/Products/OMR-13883-ProductHierarchy-Setup.json            | DataSetup/Scenario/RecursiveChildProductCascadeInitialData.json            | Tests/RecursiveChildProductCascade - END-ToLater.json                              | DataSetup/Products/OMR-13883/OMR-13883-ProductEdit-END-ToLater.json                    | Tests/RecursiveChildProductCascade - CheckCascadeStatus.json |
	| EFFE-ToLaterAndEND-ToLater                                           | DataSetup/Products/OMR-13883-ProductHierarchy-Setup.json            | DataSetup/Scenario/RecursiveChildProductCascadeInitialData.json            | Tests/RecursiveChildProductCascade - EFFE-ToLater-END-ToLater.json                 | DataSetup/Products/OMR-13883-ProductEdit-EFFE-ToLater-END-ToLater.json                 | Tests/RecursiveChildProductCascade - CheckCascadeStatus.json |
	| INAC-StatusCascade                                                   | DataSetup/Products/OMR-13883-ProductHierarchy-Setup.json            | DataSetup/Scenario/RecursiveChildProductCascadeInitialData.json            | Tests/RecursiveChildProductCascade - INAC-ByStatus.json                            | DataSetup/Products/OMR-13883-ProductEdit-INAC-ByStatus.json                            | Tests/RecursiveChildProductCascade - CheckCascadeStatus.json |
	| EFFE-ToEarlier-EffectiveDateToEarlierWithChildrenEffeEarlierAndLater | DataSetup/Products/OMR-13883-ProductHierarchy-OffsetChildDates.json | DataSetup/Scenario/RecursiveChildProductCascadeInitialDataOffsetDates.json | Tests/RecursiveChildProductCascade - EFFE-ToEarlier-BetweenChildEffectiveDate.json | DataSetup/Products/OMR-13883-ProductEdit-EFFE-ToEarlier-BetweenChildEffectiveDate.json | Tests/RecursiveChildProductCascade - CheckCascadeStatus.json |
	| END-ToLater-EndDateToLaterWithChildrenEndEarlierAndLater             | DataSetup/Products/OMR-13883-ProductHierarchy-OffsetChildDates.json | DataSetup/Scenario/RecursiveChildProductCascadeInitialDataOffsetDates.json | Tests/RecursiveChildProductCascade - END-ToLater-BetweenChildEndDate.json          | DataSetup/Products/OMR-13883-ProductEdit-END-ToLater-BetweenChildEndDate.json          | Tests/RecursiveChildProductCascade - CheckCascadeStatus.json |
	| EFFE-ToLaterWith-ChangeEffectiveDateToAfterChildEndDate              | DataSetup/Products/OMR-13883-ProductHierarchy-OffsetChildDates.json | DataSetup/Scenario/RecursiveChildProductCascadeInitialDataOffsetDates.json | Tests/RecursiveChildProductCascade - EFFE-ToLater-AfterChildEndDate.json           | DataSetup/Products/OMR-13883-ProductEdit-EFFE-ToLater-AfterChildEndDate.json           | Tests/RecursiveChildProductCascade - CheckCascadeStatus.json |
	| END-ToEarlier-ChangeEndDateToBeforeChildEffectiveDate                | DataSetup/Products/OMR-13883-ProductHierarchy-OffsetChildDates.json | DataSetup/Scenario/RecursiveChildProductCascadeInitialDataOffsetDates.json | Tests/RecursiveChildProductCascade - END-ToEarlier-BeforeChildEffectiveDate.json   | DataSetup/Products/OMR-13883-ProductEdit-END-ToEarlier-BeforeChildEffectiveDate.json   | Tests/RecursiveChildProductCascade - CheckCascadeStatus.json |
	| EFFE-ToEarlierWithChildINAC                                          | DataSetup/Products/OMR-13883-ProductHierarchy-ChildInactive.json    | DataSetup/Scenario/RecursiveChildProductCascadeInitialDataACTVOnly.json    | Tests/RecursiveChildProductCascade - EFFE-ToEarlier-ChildrenINAC.json              | DataSetup/Products/OMR-13883-ProductEdit-EFFE-ToEarlier.json                           | Tests/RecursiveChildProductCascade - CheckCascadeStatus.json |
	| END-ToLaterWithChildINAC                                             | DataSetup/Products/OMR-13883-ProductHierarchy-ChildInactive.json    | DataSetup/Scenario/RecursiveChildProductCascadeInitialDataACTVOnly.json    | Tests/RecursiveChildProductCascade - END-ToLater-ChildrenINAC.json                 | DataSetup/Products/OMR-13883-ProductEdit-END-ToLater.json                              | Tests/RecursiveChildProductCascade - CheckCascadeStatus.json |

@CORE
# INAC-EffeDate-After-EndDate is tracked by https://jiraims.rm.imshealth.com/browse/OMR-17913.
Scenario Outline: OMR-13883 Special case OMR-17913 - INAC-EffeDate-After-EndDate
	#C10439841 Product Cascade Inactivate - Inactivate Product with related entries in subtables
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I post unique salesforce 'Product' from file '<Path>/<InitialProductFile>'
	And I post unique salesforce 'ProductHierarchy' from file '<Path>/<InitialProductFile>'
	And I check that updates have static synced per '<Path>/<InitialProductFile>'
	Given I commit initial data '<Path>/<InitialDataFile>'
	When the scenario is successful
	Given I patch unique salesforce 'Product' from file '<Path>/<ProductEditFile>'
	And I get the internal scenario with name like %'Brand-{{TestUniqueIntegrationId}}'%
	When the scenario is 'ERROR'

	Examples:
	| TestName                    | Path                         | InitialProductFile                             | InitialDataFile                                       | ProductEditFile                                     |
	| INAC-EffeDate-After-EndDate | Scenario/OMR-13883/DataSetup | Products/OMR-13883-ProductAdd-SpecialCase.json | Scenario/RecursiveChildProductCascadeInitialData.json | Products/OMR-13883-ProductEdit-INAC-ByDateMove.json |
