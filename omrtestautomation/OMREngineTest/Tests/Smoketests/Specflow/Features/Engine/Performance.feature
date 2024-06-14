Feature: Performance and load tests

@Performance
Scenario Outline: Scenario Performance Light
	Given the tenant configuration defined by '<TenantConfig>'
	# Setup the data required for use in the scenarios
	And I post the salesforce users in file 'Users/Userdata.json'
	And I save these to the static data map
	# Run the scenarios and collect timings
	Given I setup the scenario performance tests found in '<PerformanceScenario>' to run <iterations> times
	And execution is <executionType> 
	When I execute scenario performance tests
	And I execute recalcs <iterations> times
	And the scenarios have completed
	Then I collect raw scenario timings in sec
	And I collect summary scenario timings in sec

	Examples:
	| executionType | TenantConfig       | PerformanceScenario | iterations |
	#| sequential    | smoketest-org.json | Performance     | 3          |
    | concurrent    | smoketest-org.json | Performance     | 5          |

@Performance
Scenario Outline: Performance under load
# OMR-15995 Run scenario & recalc tests after Bulk load and sync all from itg-data
# Note: Not run in prod, preprod since they do not do the tenant create step
# The PerfLoad tenant must have the hidden parameter “BulkLoadIntoOutput”: 1 - see OMR-12794
	Given the tenant configuration defined by '<TenantConfig>'
	Then I bulkload and sync initial '<PerfData>/ItgPerfData.json' data
	And the sync is completed within 30 min
	Then I collect bulkJob timing in sec
	And I collect syncJob timing in sec
	# Bulkload initial to prepare tenant for scenario tests
	Then I bulkload and sync initial '<PerfData>/InitialBulkload-PerfOrg.json' data
	# Setup the data required for use in the scenarios
	Given I post the salesforce users in file 'Users/Userdata.json'
	And I save these to the static data map
	# Run the scenarios and collect timings
	Given I setup the scenario performance tests found in '<PerformanceScenarios>' to run <iterations> times
	And execution is <executionType> 
	When I execute scenario performance tests
	And I execute recalcs <iterations> times
	And the scenarios have completed
	Then I collect raw scenario timings in sec
	And I collect summary scenario timings in sec

	Examples:
	| executionType | PerfData          | TenantConfig      | PerformanceScenarios | iterations |
	| concurrent    | Performance/Setup | perfload-org.json | Performance/LoadTest | 5          |