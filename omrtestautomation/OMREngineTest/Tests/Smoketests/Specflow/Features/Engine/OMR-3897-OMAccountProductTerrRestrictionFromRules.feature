Feature: OMR-3897 OMAccountProductTerrRestrictionFromOMAccountProductRules
    Calculate AccountProductTerritoryRestriction from AccountProductRule

@MAT @Engine
Scenario Outline: Add Delete and Edit AccountProductTerritoryRestriction Rule
	#Covers C9323476, C9323477 and C9323481
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I post unique salesforce 'Product' from file 'Products/OMR-3897/APTR_Products.json'
	And I commit initial data '<SetUpFile>'
	Then check all results per '<SetUpFile>'
	Given the scenario test '<SetName2>' defined by '<File>'
	When I 'calculate' the scenario and check all results

Examples:
	| Testname                               | SetUpFile                 | File                           | SetName2                             | 
	| Add Delete Edit AccountProductTerrRule | APTR/Setup-APTR-Data.json | APTR/Add-Edit-Delete-APTR.json | Add Edit Delete Account Product Rule | 

	
@MAT @Engine
Scenario Outline: Effective and End Date APT Rule
	#Covers C8028496 and C8028497
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I post unique salesforce 'Product' from file 'Products/OMR-3897/APTR_Products.json'
	And I commit initial data '<SetUpFile>'
	Then check all results per '<SetUpFile>'
	Given the scenario test '<SetName2>' defined by '<File>'
	When I 'calculate' the scenario and check all results

Examples:
	| Testname           | SetUpFile                           | File                          | SetName2                       |
	| EFFE and EXPR APTR | APTR/Setup-EFFE-EXPR-APTR-Data.json | APTR/EFFE-EXPR-APTR-Data.json | EFFE EXPR Account Product Rule |