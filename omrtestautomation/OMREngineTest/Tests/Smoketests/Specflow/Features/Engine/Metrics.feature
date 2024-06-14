Feature: Metrics calculation
	V18 Custom Metrics

@Engine @CORE
Scenario Outline: Geography Metrics and Rollups
	Account -> Geo Rollup
	C15605072 Recalc Acct metric rollup (Geo) with new Metric Definition
	C15605071 Calculate scenario -> Aggregate Appor to Brick Residential
	C15600642 Calculate scenario -> Aggregate Primary to Brick Residential
	C15605060 Calculate scenario -> Aggregate Primary to Postcode Residential
	C15605061 Calculate scenario -> Aggregate Apportioned to State Billing 
	C16037381 Calculate scenario -> Aggregate Apportioned to Brick Residential
	C16037383 Calculate scenario -> Aggregate Apportioned to Postcode any
	C15693890 Bad data handling: Calculate scenario -> Any Geo Type
	C15605062 Commit future scenario
	C16039671 Exclude out of region geographies, accounts
	C15600643 Recalc Acct metric rollup (Geo) with account address change
	C15605066 Recalc Acct metric rollup (Geo,Terr) with new AccountSales data
	C15605064 Recalc Acct metric rollup (Geo,Terr) with account region change
	
	Geo metric calc -> Terr Rollup
	C16113828 Recalc Geo metric with new Metric Definition
	C15605034 Calculate scenario with POST-type Geo metric with filtering
	C15605035 Calculate scenario with POST-type Geo metric -> Geo in child region
		C15600641 And rollup to Territory
		C16114277 No rollup to District
	C16113833 Calculate scenario with FilterBySalesForceProducts -> add Product to SF
		C15605042 And rollup to Territory
	C15605047 Calculate scenario with State-type Geo metric: Count of GeographyAccount
	C15605048 Calculate scenario with Brick-type Geo metric with filtering
	C15605043 Calculate scenario - add geography to territory & Rollup
	C15693891 Bad data handling: Calculate scenario -> Any Geo Type
	C16113831 Commit future scenario
		C15605040 And rollup to Territory
	C16113837 Exclude out of region Geography
	C16113832 Recalc Geo metric with geography region change
	C16113834 Recalc Geo metric with new Geo sales data

	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I commit initial data '<Path>/<PreloadSalesforceRule>' and check results
	
	# Load new account, geo, address, sales and calculate initial metrics
	And I post unique salesforce 'Geography' from file '<Path>/<GeoDefinition>-Original.json'
	And I wait for 'Geography' identified by 'UniqueIntegrationId' to static sync
	And I bulkload unique '<Path>/<UniqueAccountData>-Original.json'
	Then I run a recalc 
	And check all results per '<Path>/<UniqueAccountData>-Original.json'
	
	# Load future salesforce rule and calcuate metrics - GeoT:1,2,4,5,6; ProdSF:1,2,3,4,6
	Given the scenario test '<Scenario>' defined by '<Path>/<Scenario>.json'
	When I 'calculate' the scenario and check results 

	# Add geography-territory and product-salesforce and resimilate - GeoT:3; ProdSF:5
	Given the scenario test '<Scenario>' defined by '<Path>/<Scenario>-Add.json'
	When I 'calculate' the scenario and check results 

	# Commit the future salesforce, current metrics should not be affected
	And I 'commit' the scenario
    Then check all results per '<Path>/<UniqueAccountData>-Original.json'
	And check all results per '<Path>/<Scenario>-CommitChecks.json'

	# Update account address, sales data and re-calculate metrics
	Given I patch unique salesforce 'Geography' from file '<Path>/<GeoDefinition>-Updated.json'
	And I bulkload unique '<Path>/<UniqueAccountData>-Updated.json'
	And I check that updates have static synced per '<Path>/<GeoDefinition>-Updated.json'
	Then I run a recalc 
	And check all results per '<Path>/<UniqueAccountData>-Updated.json'

	Examples:
	| Testname       | Scenario        | UniqueAccountData | GeoDefinition | Path | PreloadSalesforceRule | AdjudicationDefinition | AdjudicationCRs |
	| AcctGeoMetrics | FutureSFProduct | DataForMetrics    | Geography     | Metrics/GeoMetrics      | InitialUKSalesProduct.json |                        |                 |

@CORE @Engine
#Covers the following stories:
#OMR-17479: Generate Base Account Metrics from configured metadata for simple count/sum case
#OMR-17480: Generate Base Account Metrics from Sales Data incorporating product filters
#OMR-17482: Account to Territory Rollup
#OMR-17484: Populate OMScenarioSummary2

#Generic metric calculation test cases covered:
#C15539897 Simulates for a past-dated scenario use the scenario effective date for metric calculation 
#C15579014 Simulates for a current-date scenario use current date for metric calculation 
#C15539907 Simulates for a future-dated scenario use the scenario effective date for metric calculation 
#C15579015 Commits for a past-dated scenario use the scenario effective date for metric calculation 
#C15579016 Commits for a current-dated scenario use the current date for metric calculation 
#C15579017 Commits for a future-dated scenario use the scenario effective date for metric calculation 

#OMR-17479: Generate Base Account Metrics from configured metadata for simple count/sum case
#C15539899 Account subtable sum/counts do not include INAC records 
#C15662802 Account SUM/COUNTS do not include Salesforces that are not effective

#OMR-17480: Generate Base Account Metrics from Sales Data incorporating product filters
#C15660317 No SOMRegion check for products when FilterBySalesforceProducts = true
#C15660319 FilterBySalesforceProducts does not include product alignments that have not started as of the Scenario.EffectiveDate
#C15660321 IncludeCompetitorProducts = true, includes all Products (including Products where Product.SOMRegionId does not match the Salesforce.SOMRegionId)
#C15660322 IncludeCompetitorProducts = false/null, metrics only defined for records with a OMProduct reference with OMProduct.CompetitorProduct=FALSE
#C15660341 Product filtering with custom filters

#OMR-17482: Account to Territory Rollup
#C16027064 OMTerritoryMetrics are rolled up from OMAccountMetric for every active+effective OMAccountTerritory with an active+effective OMTerritorySalesForce
#C15725348 Rolled-up accont metrics are duplicated for each duplicate ACTV and currently effective AccountTerritory alignments to different territories

#OMR-17484: Populate OMScenarioSummary2
#C16041160: Current date commits/simulates show NULL for Metric_OX_Before in OMScenarioSummary2 if no existing metrics
Scenario Outline: Metrics (Account and Account to Territory rollup metrics) 
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I post unique salesforce 'Product' from file 'Metrics/Data/Products/MetricProducts.json'
	And I wait for 'Product' identified by 'UniqueIntegrationId' to static sync
	And the scenario test '<SetName>' defined by 'Metrics/Scenarios/<JsonFile>'
	And I submit a unique bulkload request from file 'Metrics/Data/AccountSales/MetricSalesData.json'
	Then The bulkload is successful
	When I '<JobType>' the scenario
	And the scenario is successful
	Then check all results per 'Metrics/Tests/<TestFile>'
	Examples:
	| SetName         | JsonFile                    | JobType   | TestFile                             | AccountSalesData |
	| SimulateCurrent | CurrentMetricsScenario.json | calculate | SimulateCurrentMetricsSmoketest.json |                  |
	| SimulatePast    | PastMetricsScenario.json    | calculate | SimulatePastMetricsSmoketest.json    |                  |
	| SimulateFuture  | FutureMetricsScenario.json  | calculate | SimulateFutureMetricsSmoketest.json  |                  |
	| CommitCurrent   | CurrentMetricsScenario.json | commit    | CommitCurrentMetricsSmoketest.json   |                  |
	| CommitPast      | PastMetricsScenario.json    | commit    | CommitPastMetricsSmoketest.json      |                  |
	| CommitFuture    | FutureMetricsScenario.json  | commit    | CommitFutureMetricsSmoketest.json    |                  |
