Feature: Browser Snowflake API tests
	Execute queries using session token provided to post to Snowflake rest api
	OMR-19542 Schema changes on tenant creation and upgrade to support new user and role for browser access
	OMR-19135 API to provide a Snowflake session token and URL
	@Schema

@CORE @Schema
Scenario Outline: Browser requests
C17751091 Use snowflake session token to query whitelisted tables
C17751115 Query view on whitelisted table
C17751092 Don't allow query on restricted tables
C17751093 Don't allow updates on allowed tables
C17751095 Don't allow query on whitelisted table with join on restricted table

	Given the tenant configuration defined by 'smoketest-org.json'
	And the test data path BrowserUserAccess
	And the default schema '<Schema>'
	When I issue the snowflake request '<TestName>' per '<TestFile>'
	Then the request status is '<ExpStatus>'
	And the response 'code' matches '<ExpCode>'
	And the response 'message' contains '<ExpMessage>'
	And I check the response data

	Examples:
	| TestName                  | TC        | TestFile                    | Schema | ExpStatus           | ExpCode | ExpMessage                                  |
	| WhitelistedTable          | C17751091 | OMAccountQuery.json         | STATIC | OK                  | 090001  | Statement executed successfully             |
	| WhitelistedCustom         | C17751091 | OMProductQuery.json         | STATIC | OK                  | 090001  | Statement executed successfully             |
	| WhitelistedView           | C17751115 | VProductHierarchy.json      | STATIC | OK                  | 090001  | Statement executed successfully             |
	| RestrictedTable           | C17751092 | OMGeoRestricted.json        | STATIC | UnprocessableEntity | 002003  | does not exist or not authorized            |
	| WhitelistedJoinRestricted | C17751095 | OMGeoAccountRestricted.json | STATIC | UnprocessableEntity | 002003  | does not exist or not authorized            |
	| UpdateRecord              | C17751093 | OMUpdateQuery.json          | STATIC | UnprocessableEntity | 003001  | Insufficient privileges to operate on table |
