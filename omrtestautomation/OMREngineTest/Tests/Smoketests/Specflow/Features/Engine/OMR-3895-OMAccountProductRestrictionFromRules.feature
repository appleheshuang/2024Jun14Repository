Feature: OMAccountProductRestriction From OMAccountProductExplicit And OMAccountProductRules
	OMR-3893 Calculate AccountProductRestriction from AccountProductExplicit data
    OMR-3895 Calculate AccountProductRestriction from AccountProductRule

@MAT @Engine
Scenario Outline: Calculate APR
	Calculate APR from AccountProduct Explicits + Rules
	#Covers C8028495, C8028498 and C8028499
	#Covers C8027851, C8027853 and C8029400
	#Covers C8028496 and C8028497
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I post unique salesforce 'Product' from file 'APR/<Products>'
	And I check that updates have static synced per 'APR/<Products>'
	And I commit initial data '<Path>/<SetUpFile>'
	Then check all results per '<Path>/<SetUpFile>'
	Given the scenario test '<SetName2>' defined by '<Path>/<File>'
	When I 'calculate' the scenario and check all results

Examples:
	| Testname                   | Path                  | Products              | SetUpFile                                      | File                                           | SetName2                             | 
	| APExplicit Add Delete Move | APR/Expl-Add-Edit-Del | APRProducts.json      | Setup-Account-Product-Explicit_Data.json       | Delete-Move-Account-Product-Explicit_Data.json | Delete-Move Account Product Explicit | 
	| APExplicit EFFE EXPR       | APR/Expl-Effe-Expr    | APRProducts_EFFE.json | Setup-Account-Product-Explicit_Data_EFFE.json  | EFFE-Account-Product-Explicit_Data.json        | EFFE Account Product Explicit        | 
	| APRule Add Delete Edit     | APR/Rule-Add-Edit-Del | APRProducts.json      | Setup-Account-Product-Rule_Data.json           | Add-Edit-Delete-Account-Product-Rule.json      | Add Edit Delete Account Product Rule |
	| APRule EFFE EXPR           | APR/Rule-Effe-Expr    | APRProducts_EFFE.json | Setup-Account-Product-Rule_Data_EFFE_EXPR.json | EFFE_EXPR-Account-Product-Rule.json            | EFFE EXPR Account Product Rule       |

@MAT @Engine
Scenario Outline: Recalc APR for Explict with Preservation Logic
	# Covers C8028500 and C8029401
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	
	#Account and product bulk load for Account Product Explicit
	And I post unique salesforce 'Product' from file '<Path>/Products.json'
	And I submit a unique bulkload request from file '<Path>/BulkOMR3893AccountsExplicit.json'
	Then The bulkload is successful
	Then check all results per '<Path>/BulkOMR3893AccountsExplicit.json'
	Given I check that updates have static synced per '<Path>/Products.json'
	
	# Commit the explicits and APR rule
	And I commit initial data '<SetUpFile>'
	Then check all results per '<SetUpFile>'
	
	# INAC account and product for explicits, Add a new account that matches the rule
	Given I patch unique salesforce 'Product' from file '<Path>/InactivateProduct.json'
	And I submit a unique bulkload request from file '<Path>/BulkOMR3895AccountDynamic.json'
	Then I 'INAC' the 'OMAccount' with UniqueId 'Add{{TestUniqueIntegrationId}}'
	And The bulkload is successful
	Then check all results per '<Path>/BulkOMR3895AccountDynamic.json'
	Given I check that updates have static synced per '<Path>/InactivateProduct.json'
	
	# Recalc and check APR as a result of rule + inactive account, product
	Then I run a recalc
	Then check all results per '<File>'
	
Examples:
	| Testname                     | Path                | SetUpFile                                  | File                                 |
	| RECALC AccountProductExplict | APR/PreserveHistory | Setup-RECALC-Account-Product-Explicit.json | RECALC-Account-Product-Explicit.json | 