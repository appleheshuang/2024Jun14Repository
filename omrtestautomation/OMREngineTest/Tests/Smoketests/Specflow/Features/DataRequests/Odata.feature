Feature: Odata tests
	Prepare data and execute odata requests
	@Schema

@CORE @Schema
Scenario Outline: Odata requests using initial load data
	Given the tenant configuration defined by 'smoketest-org.json'
	Then check <Teststorun> results per '<TestFile>'

	Examples:
	| TestName                                | TestFile                                      | Teststorun                   |
	| OMR-16491-ExplicitLeftJoin-supports-csv | OData/Requests/ExplicitLeftJoin-with-csv.json | account_region_address_odata |
