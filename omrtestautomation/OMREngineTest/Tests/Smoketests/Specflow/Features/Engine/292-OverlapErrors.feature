Feature: Scenario tests OMR-292 and OMR-252

@CORE @Engine
Scenario Outline: OMR-292 - Sub entities Overlap Error
	#OMR-292 - As an OM user, I should have the following rules around date management (records with overlapping validity periods) / GEN-REQ011 
	#C7663359 - AccountTerritoryExclusion, AccountSalesForceExclusion, AccountTerritoryExplicit, GeographyTerritories
	#C7663360 - ProductExclusions, ProductExplicit, ProductSalesforce
	#C7663361 - SalesforceHierarchy, TerritoryHierarchies, User Assignment
	#C7663362 - Add some records with different timeframe for the tables above.

	Given the tenant configuration defined by 'smoketest-org.json'
	And I commit initial data '<SetupFle>' 
	Given the scenario test '<SetName>' defined by '<NegativeFile>'
	When I 'calculate' the scenario
	And the scenario is 'ERROR'
	Then check all results per '<NegativeFile>'
	Given the scenario test '<SetName>' defined by '<PositiveFile>'
	When I 'calculate' the scenario and check results

Examples:
	| SetName           | NegativeFile                                                  | PositiveFile                                                  | SetupFle                                                         | 
	| Overlapping Dates | Scenario/OMR-292-DuplicateError/Test-OMR292-OverlapError.json | Scenario/OMR-292-DuplicateError/Test-OMR292-PositiveCase.json | Scenario/OMR-292-DuplicateError/Setup-OMR292-MainEntityData.json | 

@CORE @Engine
Scenario Outline: OMR-252 - EFFE and EXPR outside date range Validation
	#OMR-252 - As an OM user, I should have the following rules around date management (end date) / GEN-REQ011
	#C7663423 - EXPR to make End Date earlier than Effective Date, will trigger validation error
	#C7663422 - Restart to make Effective Date later than End Date, will trigger validation error

	Given the tenant configuration defined by 'smoketest-org.json'
	And I post the salesforce users in file 'Users/OMR-252-Userdata.json'
	And I commit initial data '<SetupFle>'
	Given the scenario test '<SetName>' defined by '<EffectiveDateFile>'
	When I 'calculate' the scenario
	And the scenario is 'ERROR'
	Then check all results per '<EffectiveDateFile>'
	Given the scenario test '<SetName>' defined by '<EndDateFile>'
	When I 'calculate' the scenario
	And the scenario is 'ERROR'
	Then check all results per '<EndDateFile>'

Examples:
	| SetName             | EffectiveDateFile                       | EndDateFile                             | SetupFle                           | 
	| Error EFFE and EXPR | Scenario/OMR-252/OMR252-EFFE-Error.json | Scenario/OMR-252/OMR252-EXPR-Error.json | Scenario/OMR-252/Setup-OMR252.json | 

@CORE @Engine
Scenario Outline: OMR-799 - Validate foreign key relationships when merging Scenario data
	#C7644682 - Scenario failure - delete parent with children (foreign key validation)

	Given the tenant configuration defined by 'smoketest-org.json'
	And I commit initial data '<SetupFle>'
	Given the scenario test '<SetName>' defined by '<ForeignKeyFile>'
	When I 'calculate' the scenario
	And the scenario is 'ERROR'
	Then check all results per '<ForeignKeyFile>'

Examples:
	| SetName                  | ForeignKeyFile                            | SetupFle                                           |
	| Deleteing Parent data SF | Scenario/OMR-799/OMR799-DeleteParent.json | Scenario/OMR-799/Setup-OMR799-ParentChildData.json | 