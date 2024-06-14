Feature: AccountTerritoryProduct Ratings OMR-13844 OMR-13845
	Account-Product and Account-Territory-Product Ratings Sync & use in rules

@CORE @OCESync
Scenario Outline: AccountTerritoryProduct for OCE/OCE-Lexi
	Given the tenant configuration defined by '<TenantConfig>'
	And the optimizer settings '<OptimizerSettings>'
	And a unique id for the test
	And I post native salesforce 'Account' from file 'OCEData/OCEAccounts.json'
	And I post unique salesforce 'Product' from file 'Products/Product.json'
	And I wait for 'Product' identified by 'UniqueIntegrationId' to static sync
	When I PULL_ACCOUNT to-from OCE-P with '<Mode>' mode
	And I PUBLISH_OTHER_OMR_DATA to-from OCE-P with '<Mode>' mode
	#Territory2 records omitted as there is a Salesforce trigger to wipe OCE__Territory__c from AccountTerritoryProduct
	#Given the scenario test '<SetName>' defined by '<JsonFile>'
	#When I '<JobType>' the scenario
	#And the scenario subjob publishOther is successful within 15 min  
	#And the scenario subjob publishAlignments is successful within 15 min
	Given I post native salesforce 'OCE__Product__c' from file 'OCEData/OCEProducts.json'
	And I post oce salesforce 'AccountTerritoryProduct' from file '<AccountTerritoryProductFile>'
	When I PULL_ACCOUNT to-from OCE-P with '<Mode>' mode
	Then check <SetName> results per '<TestFile>'

	Examples:
	| TenantConfig     | SetName                    | JsonFile                                                                       | JobType | OptimizerSettings  | Mode | AccountTerritoryProductFile                                            | TestFile                                                                   |
	| ocesync-org.json | AccountTerritoryProductOCE | OCEData/OMR-13844-AccountTerritoryProduct/AccountTerritoryProductScenario.json | commit  | CalculateMaps=true | OCE  | OCEData\OMR-13844-AccountTerritoryProduct/AccountTerritoryProduct.json | OCEData/OMR-13844-AccountTerritoryProduct/AccountTerritoryProductSync.json |
	#The following could cause inconsistent results if it is pulled via another OCESync job with mode = OCE
	#| ocelexisync-org.json | AccountTerritoryProductOCELexi | OCEData/OMR-13844-AccountTerritoryProduct/AccountTerritoryProductScenario.json | commit  | CalculateMaps=true | OCE_LEXI | OCEData\OMR-13844-AccountTerritoryProduct/AccountTerritoryProduct.json        | OCEData/OMR-13844-AccountTerritoryProduct/AccountTerritoryProductTest.json |

@CORE @OCESync
Scenario Outline: AccountTerritoryProduct for non-existing Product in Optimizer
	Given the tenant configuration defined by 'ocesync-org.json'
	And a unique id for the test
	And I post native salesforce 'Account' from file 'OCEData/OCEAccounts.json'
	Given I post native salesforce 'OCE__Product__c' from file 'OCEData/OCEProductsExisting.json'
	And I post oce salesforce 'AccountTerritoryProduct' from file '<AccountTerritoryProductFile>'
	When I PULL_ACCOUNT to-from OCE-P with '<Mode>' mode
	Then check <SetName> results per '<TestFile>'


	Examples:
	| SetName             | JsonFile                                                                       | JobType | OptimizerSettings  | Mode | AccountTerritoryProductFile                                            | TestFile                                                                   |
	| InvalidSOMProductId | OCEData/OMR-13844-AccountTerritoryProduct/AccountTerritoryProductScenario.json | commit  | CalculateMaps=true | OCE  | OCEData\OMR-13844-AccountTerritoryProduct/AccountTerritoryProduct.json | OCEData/OMR-13844-AccountTerritoryProduct/AccountTerritoryProductSync.json |

@CORE @Engine
Scenario Outline: 13845 AccountTerritoryProduct use in Rules
	Given the tenant configuration defined by 'ocesync-org.json'
	And a unique id for the test
	## Pre-conditions - create accounts, products, initial scenario data; then AccountTerritoryProduct
	And I post native salesforce 'Account' from file 'OCEData/OCEAccounts.json'
	And I post unique salesforce 'Product' from file '<Path>/OMProduct.json'
	And I wait for 'Product' identified by 'UniqueIntegrationId' to static sync
	And I commit initial data '<Path>/<InitialJsonFile>' and publish other
	## Get the OCE Product Ids, create the ATP, sync and check preconditions before proceeding
	And I post native salesforce 'OCE__Product__c' from file '<Path>/OCEProducts.json'
	And I post oce salesforce 'AccountTerritoryProduct' from file '<Path>/OCEAccountTerritoryProduct.json'
	When I PULL_ACCOUNT to-from OCE-P
	Then check <PreconditionChecks> results per '<Path>/<ScenarioRulesFile>'
	## Scenario with ALGN, ENGP and APTR rules
	Given the scenario test '<SetName>' defined by '<Path>/<ScenarioRulesFile>'
	When I 'simulate' the scenario and check results

	Examples:
	| SetName                      | Path                                            | InitialJsonFile      | ScenarioRulesFile         | PreconditionChecks                       |
	| AccountTerritoryProductRules | OCEData/OMR-13845-AccountTerritoryProduct-Rules | ATPRulesInitial.json | ScenarioWithATPRules.json | PreCondition-AccountTerritoryProductSync |
