Feature: Territory Metrics calculation
	V19 Custom Territory Metrics (AUX)
	Automation assisted, Run manually

@Manual
Scenario Outline: Territory Metrics - Count
	C17751988 Count ProductTerritory + FilterBySalesForceProducts
	C17751990 Territory metric in Scenario and Adjudication results
	C15539907 Simulates for a future-dated scenario

	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And the test data path Metrics/TerrMetrics
	Given the scenario test '<SetName>' defined by '<TestScenario>'
	And I bulkload unique '<TerritoryMetricsDef>'
	When I 'calculate' the scenario
	And the scenario is successful
	Then check <CalculationResults> results per '<TestScenario>'
	# Adjudication initial results populated
	Given the scenario based adjudication defined in 'Adjudication.json'
	When I release the adjudication
	Then check <AdjInitialResults> results per 'Adjudication.json'
	When I close the adjudication 
	
	Examples:
	| SetName                    | TerritoryMetricsDef          | CalculationResults                                                                                          | TestScenario              | AdjInitialResults             |
	| ProductAlign Unfiltered    | TerrMetricData-Basic.json    | Metric-TerritoryProductCount-Total,SS2-TerritoryProductCount-Total,SS2-NotPopulatedForTerrWithoutAlignments | FutureSFProductAlign.json | InitialTerritoryResults_Total |
	| FilterBySalesForceProducts | TerrMetricData-Filtered.json | Metric-TerritoryProductCount-InSF,SS2-TerritoryProductCount-InSF                                            | FutureSFProductAlign.json | InitialTerritoryResults_InSF  |

	@Manual
	Scenario Outline: Territory Metrics - Sum
	C17751989 Sum OMAccountTerritoryFields.AnnualCallGoal + CustomFilterExpression
	C15579020 Metric recalculation on Recalc job

	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And the test data path Metrics/TerrMetrics
	Given I commit initial data '<InitialScenario>'
	Then check alignment,excl-alignment results per '<InitialScenario>'
	Given I bulkload unique '<DataForMetricsCalc>'
	And I bulkload unique '<TerritoryMetricsDef>' 
	Then I run a recalc 
	And check <CalculationResults> results per '<InitialScenario>'
	
	Examples:
	| SetName             | TerritoryMetricsDef          | CalculationResults                               | DataForMetricsCalc         | InitialScenario          |
	| Sum Unfiltered      | TerrMetricData-Basic.json    | Metric-TerritoryProductSum-All,Metric-ExcludedSF | LoadAccountTerrFields.json | CurrentAccountAlign.json |
	| WithCustomFiltering | TerrMetricData-Filtered.json | Metric-TerritoryProductSum-Filter                | LoadAccountTerrFields.json | CurrentAccountAlign.json |