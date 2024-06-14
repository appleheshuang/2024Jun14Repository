Feature: Account Territory Precedence
	Related Stories:
		- OMR-846 Precedence of Explicits vs. Exclusions in Account Territory Alignment,
		- OMR-131 Merge account explicits and excludes - Explicit Wins,
		- OMR-132 Merge account explicits and excludes - Exclusion Wins,
		- OMR-2505 Merge account explicits and excludes - Second One Wins.

@CORE @Engine
Scenario Outline: Account Territory Precedence
	Overlaps between conflicting Account Explicits and Exclusions are controlled by 2 settings:
		- 'AccountTerritoryPrecedence': ExplicitWins, ExclusionWins (default), SecondOneWins.
		- 'SalesForceExclusionOverTerrExplicit': true (default), false.

	Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings '<Settings>'
	And I commit initial data 'Scenario/AccountTerritoryPrecedence/<ScenarioFileName>_Setup.json' and check results
	Given the scenario test '<SetName> Test' defined by 'Scenario/AccountTerritoryPrecedence/<ScenarioFileName>_Test.json'
	When I 'calculate' the scenario and check snow results

	Examples:
	| ScenarioFileName                      | SetName                              | Settings                                                                           |
	| ExplicitWins_SFExclOverTerrExplTrue   | ExplicitWins,  ✓ SFExclOverTerrExpl | AccountTerritoryPrecedence=ExplicitWins;SalesForceExclusionOverTerrExplicit=true   |
	| ExclusionWins_SFExclOverTerrExplFalse | ExclusionWins, ✗ SFExclOverTerrExpl | AccountTerritoryPrecedence=ExclusionWins;SalesForceExclusionOverTerrExplicit=false |
	| ExclusionWins_SFExclOverTerrExplTrue  | ExclusionWins, ✓ SFExclOverTerrExpl | AccountTerritoryPrecedence=ExclusionWins;SalesForceExclusionOverTerrExplicit=true  |
	| SecondOneWins_SFExclOverTerrExplFalse | SecondOneWins, ✗ SFExclOverTerrExpl | AccountTerritoryPrecedence=SecondOneWins;SalesForceExclusionOverTerrExplicit=false |
