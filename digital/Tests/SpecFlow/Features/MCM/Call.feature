Feature: Call

OCEMCM-19552 Import Call Data from MC to Snowflake
C15662799 All fields with value will sync to snowflake successfully.
C15662800 Only have mandatory fields and the record will sync to snowflake.
@EvaJun
Scenario: Call
	Given the tenant configuration defined by '<Tenant>'
	And a unique id for the test
	Given I post Digital record to table from Table
	| DE                                  | datafileNew                                 |  
	| DE_Call_Master_Message__IQ          | MCM/Call/CallMasterMessage/New.json         |
	| DE_Contact_Master__IQ               | MCM/Contact/ContactMaster/New.json          |
	| DE_Call_Master__IQ                  | MCM/Call/Call/New.json                      |
	| DE_Call_Master_Promotional_Item__IQ | MCM/Call/CallMasterPromotionalItem/New.json |
	| DE_Call_Master_Sample_Order__IQ     | MCM/Call/CallSampleOrder/New.json           |
	| DE_Call_Master_Sample__IQ           | MCM/Call/CallSample/New.json                |
	| DE_Call_Master_Product_Detail__IQ   | MCM/Call/CallProductDetail/New.json         |
	When I trigger a DigitalDataSync with Type 'ScheduledSFMCDataSync' and cache 'true' with ExcludedAliases 'OMAccountActivityResponse'
	Then check all DigitalDataSync jobs finished 
	Then check digital results from table
	| DE  | resultNew                                           |
	| all | MCM/Call/Call/NewSyncQueryResults.json              |
	| all | MCM/Call/CallMasterPromotionalItem/NewSyncQueryResults.json |
	| all | MCM/Call/CallSampleOrder/NewSyncQueryResults.json           |
	| all | MCM/Call/CallSample/NewSyncQueryResults.json                |
	| all | MCM/Call/CallProductDetail/NewSyncQueryResults.json         |
	| all | MCM/Call/CallMasterMessage/NewSyncQueryResults.json |  

	Given I post Digital record to table from Table
	| DE                                  | datafileUpdate                                 | 
	| DE_Call_Master_Message__IQ          | MCM/Call/CallMasterMessage/Update.json         |
	| DE_Call_Master__IQ                  | MCM/Call/Call/Update.json                      |
	| DE_Call_Master_Promotional_Item__IQ | MCM/Call/CallMasterPromotionalItem/Update.json |
	| DE_Call_Master_Sample_Order__IQ     | MCM/Call/CallSampleOrder/Update.json           |
	| DE_Call_Master_Sample__IQ           | MCM/Call/CallSample/Update.json                |
	| DE_Call_Master_Product_Detail__IQ   | MCM/Call/CallProductDetail/Update.json         |
	When I trigger a DigitalDataSync with Type 'ScheduledSFMCDataSync' and cache 'true' with ExcludedAliases 'OMAccountActivityResponse'
	Then check all DigitalDataSync jobs finished 
	Then check digital results from table
	| DE  | resultUpdate                                                   |
	| all | MCM/Call/Call/UpdateSyncQueryResults.json                      |  
	| all | MCM/Call/CallMasterPromotionalItem/UpdateSyncQueryResults.json |
	| all | MCM/Call/CallProductDetail/UpdateSyncQueryResults.json         |
	| all | MCM/Call/CallSampleOrder/UpdateSyncQueryResults.json           |
	| all | MCM/Call/CallSample/UpdateSyncQueryResults.json                |
	| all | MCM/Call/CallMasterMessage/UpdateSyncQueryResults.json         |


	Examples:
	| Tenant               |
	| digital-org0325.json |  


@EvaJun14
Scenario: AddingTwoNumbers
When I add two Numbers