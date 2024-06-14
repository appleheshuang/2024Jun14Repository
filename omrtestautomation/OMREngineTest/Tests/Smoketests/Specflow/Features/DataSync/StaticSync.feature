Feature: StaticSync
	@DataSync

@CORE @DataSync
Scenario: OMR-13012 Static sync with resync=true
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I post unique salesforce 'Product' from file 'Products/OMR-13012/OMR_13012_Products.json'
	And I wait for 'Product' identified by 'UniqueIntegrationId' to static sync
	And I delete row with Id 'SnowProduct-' from 'STATIC' schema 'OMProduct' table from Snowflake
	And I insert a record to snowflake OMProduct table
	And I post unique salesforce 'SalesForce' from file 'SalesForce/SalesForce.json'
	And I insert a record to snowflake OMSalesForce table
	When I run static sync with Resync true
	Then check 'ProductTest' results per 'Products/OMR-13012/OMR_13012_Products.json'
	And I verify 'OUTPUT' tables for 'SalesForceTest' results per 'Products/OMR-13012/OMR_13012_Products.json'
	