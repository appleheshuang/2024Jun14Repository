Feature: OceSync
@OCESync

# OMR-16552 Sync UserAddress city, state, zip, country
	# C11327324 Verify City, State, Zip in User table of OCE
	# C11327325 Verify Address fields (street, city, state, zip, country, lat/lon)
	# C11327326 Verify User without HOME Address
	# C7843137,C7843138,C7843139,C7843140 Verify User.Country

@OCESync @CORE
Scenario: OMR-16552 Publish UserAddress
	Given the tenant configuration defined by 'ocesync-org.json'
	And a unique id for the test
	And I post unique salesforce 'User' from file '<SyncDataPath>'
	And I post unique salesforce 'UserAddress' from file '<SyncDataPath>'
	And I wait for 'User' identified by 'UniqueIntegrationId' to static sync
	And I wait for 'UserAddress' identified by 'UniqueIntegrationId' to static sync
	When I PUBLISH_OTHER_OMR_DATA to-from OCE-P with 'default' mode
	Then check all results per '<SyncDataPath>'

Examples:
	| SyncDataPath                       |
	| OCESync/OMR-16552-UserAddress.json |


#OMR-18279 to sync only one product territory alignment regardless of alignment type
#Covers TCs:
#C16149150 - OCE__TerritoryProductAlignment__c IsActive__c true/false status is determined by union of OMProductTerritory dates 
#C16149151 - Publish OMProductTerritory alignment with the Product+Territory alignment pre-existing, but with new product alignment type 
#C16149152 - New OMProductTerritory alignments have format ("SOMProductId" || '-' || "SOMTerritoryId") for OCE__TerritoryProductAlignmentRule__c .OCE__UniqueIntegrationID__c
#C16149153 - End date existing OMProductTerritory alignment in Optimizer but OCE__TerritoryProductAlignmentRule__c for that Product+Territory continues to be ACTV 
#C16149154 - End date existing OMProductTerritory alignment and OCE__TerritoryProductAlignment__c record for that Product+Territory becomes deleted/INAC

@OCESync @CORE
Scenario: OMR-18279 - Change Optimizer publish job to send only one record to OCE__TerritoryProductAlignmentRule__c object, if there are multiple alignments exist for the same Product and Territory combination
	Given the tenant configuration defined by 'ocesync-org.json'
	And a unique id for the test
	And I commit initial data '<PreloadData>'
	When the scenario subjob publishOther is successful within 15 min
	Then check all results per '<InitialTestPath>'
	Given the scenario test 'OMR-18279-EditTest' defined by '<DataEditFile>'
	When I 'commit' the scenario
	And the scenario is successful
	When the scenario subjob publishOther is successful within 15 min
	Then check all results per '<DataEditTestFile>'

Examples:
	| TestName                           | PreloadData                            | InitialTestPath                            | DataEditFile             | DataEditTestFile            |
	| EndDateAndDeleteOriginalAlignments | OCEData/OMR-18279/OMR-18279-Setup.json | OCEData/OMR-18279/OMR-18279-SetupTest.json | OMR-18279-DataEdits.json | OMR-18279-DataEditTest.json |


	# OMR-19934 - Support Product Territory Exclusion Granular Data in OCE-P Sync
	# C18054536 - Publish product exclusion data to OCE_TerritoryProductExclusion_c to OCEP
	# C18054537 - Expire product exclusion data and sync data OCE_TerritoryProductExclusion_c to OCEP
	# C18054538 - Update effective date for product exclusion data and sync data OCE_TerritoryProductExclusion_c to OCEP
	# C18054539 - Delete product exclusion data and sync data OCE_TerritoryProductExclusion_c to OCEP
	# C18061329 - Move product exclusion data and sync data OCE_TerritoryProductExclusion_c to OCEP
	# C18061330 - Sync product exclusion data for a product with multiple alignment type
@OCESync @CORE
Scenario: OMR-19934 - Support Product Territory Exclusion Granular Data in OCE-P Sync
	Given the tenant configuration defined by 'ocesync-org.json'
	And the test data path <Path>
	And a unique id for the test
	#And I commit initial data '<PreloadData>'
	Given the scenario test 'ProductExclusion-Setup' defined by 'ProductExclusionDataSetup.json'
	And I add to the scenario '<AdditionalActions>'
	When I 'commit' the scenario
	And the scenario is successful
	And the scenario subjob publishOther is successful within 15 min
	Then check all results in '<AdditionalActions>'
	# Update for Product Exclusion records with different actions
	Given the scenario test 'ProductExclusion-Update' defined by 'ProductExclusionDataUpdate.json'
	And I add to the scenario '<UpdateActions>'
	When I 'commit' the scenario
	And the scenario is successful
	When the scenario subjob publishOther is successful within 15 min
	Then check all results in '<UpdateActions>'

Examples:
	| TestName                            | AdditionalActions      | UpdateActions                 | Path              |
	| Sync Product Exclusion Data to OCEP | ProductExclusionAction | ProductExclusionUpdateActions | OCEData/OMR-19934 |