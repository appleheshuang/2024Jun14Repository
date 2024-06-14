Feature: Product
	Bulk load, sync, align

@CORE @OCESync
Scenario: Product bulkload and sync to OCE-P
# C9650718 OMR-13250 Populate OCE StartDate with EffectiveDate
# C9382852 OMR-11313 - OMProduct with null RegionId are not synced to OCE
# C8764593 BulkAPI - CreatedDate  is populated with current timestamp when missing from load file
# C8787200 BulkAPI - LastModifiedDate  is populated with current timestamp when missing from load file 
# C8787284 Regression - upload file with CreatedDate & LastModifiedDate columns into OMR table that has those columns

	Given the tenant configuration defined by 'ocesync-org.json'
	And a unique id for the test
	#Bulkload unique Products
	And I bulkload unique '<ProductBulkLoad>'
	Then check all results per '<ProductBulkLoad>'
	When I PUBLISH_OTHER_OMR_DATA to-from OCE-P with 'default' mode
	Then check all results per '<ProductOCESyncTests>'

	Examples:
	| ProductBulkLoad                     | ProductOCESyncTests                          |
	| Products/OMR-13250/ProductLoad.json | Products/OMR-13250/ProductOCESyncChecks.json |
