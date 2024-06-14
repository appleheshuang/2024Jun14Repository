Feature: PickListValidation
	PickListValidation
@schemasync

@CORE @SchemaSync
Scenario Outline: OMR-10020-PickListValidation
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I submit a unique bulkload request from file '<BulkLoadFile>'
	Then The bulkload has errors per '<BulkLoadFile>'
	And check all results per '<BulkLoadFile>'

Examples:
	| TestName                       | BulkLoadFile                                                  |
	| ValidAndInvalid_RollBack_False | Smoketest/LoadData/omr-10020/BulkOMR10020_Rollback_False.json |
	| ValidAndInvalid_RollBack_True  | Smoketest/LoadData/omr-10020/BulkOMR10020_Rollback_True.json  |
	
@CORE @Schema
Scenario: OMR-19132 Bulk load does not validate existing column data
	C16169231 OMR-19132 Skip picklist validation for columns excluded from the load file
	C9727883 PicklistValidate parameter set to false
	
	Given the tenant configuration defined by 'smoketest-org.json'
	And the test data path BulkLoad/omr-19132
	# Load the account with invalid AccountType
	And I bulkload from the bulkload settings in 'InvalidDataAllowed.json'
	Then check all results per 'InvalidDataAllowed.json'
	# Successfully update the status, ignoring existing invalid data
	Given I bulkload from the bulkload settings in 'UpdateExcludesInvalidCol.json'
	Then check inac_account_invalid_type results per 'UpdateExcludesInvalidCol.json'
	# Successfully update the AccountType to a valid picklist value
	Given I bulkload from the bulkload settings in 'UpdateWithValidData.json'
	Then check account_has_valid_type results per 'UpdateWithValidData.json'	
	# Error updating the AccountType to an invalid picklist value, data not updated
	Given I submit the bulkload 'UpdateWithInvalidData.json' 
	Then The bulkload has errors per 'UpdateWithInvalidData.json' 
	And check account_has_valid_type results per 'UpdateWithValidData.json'
