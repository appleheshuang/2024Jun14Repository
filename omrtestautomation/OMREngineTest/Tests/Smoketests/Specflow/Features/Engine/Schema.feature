Feature: Schema
	Schema tests

@AUX @Engine
Scenario Outline: OMR-14080 CreatedBy and LastModifiedBy Columns for STATIC tables
	Given the tenant configuration defined by 'smoketest-org.json'
	And a unique id for the test
	And I commit initial data '<JsonFile>'
	Given I bulkload unique '<JsonFile>'
	Then I verify '<Schema>' tables for all results per '<JsonFile>'


Examples:
	| SetName                     | JsonFile                                           | Schema | NamedTest |
	| CreaetedByAndLastModifiedBy | Smoketest/LoadData/omr-14080/ScenarioOMR14080.json | STATIC | all       |