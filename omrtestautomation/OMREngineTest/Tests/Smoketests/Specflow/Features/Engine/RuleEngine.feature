Feature: RuleEngine
    Related Stories:
    - OMR-635 Open-ended alignments,
    - OMR-8623 Account alignment per SalesForce,
    - OMR-10070 Geography-based Rules,
    - OMR-8624 Affiliation-based Rules,
    - OMR-8687 Universe-based Rules,
    - OMR-8625 Limit Rules to Region.
    - OMR-15569 Geography and Affiliation-based Rules

@CORE @Engine
Scenario Outline: Alignment by Rule type
    Related Test Cases:
    - C7650605: Rules with no End date result in open-ended account alignments,
    - C8625303, C8625306: Geography-based Rules with explicits and excusions,
    - C8625304, C8625307: Affiliation-based Rules with explicits and excusions,
    - C8625305, C8625308: Universe-based Rules with account filtering, explicits and excusions,
    - C7651287, C7651298, C7651299, C7651301: Limit Account alignment to Region,

    Given the tenant configuration defined by 'smoketest-org.json'
    And the scenario test '<Test>' defined by 'Scenario/RuleEngine/<Scenario>.json'
    When I 'calculate' the scenario and check snow results

    Examples:
    | Test                                  | Scenario                          | 
    | Open-ended alignments                 | OpenEndedAlignments               |
    | Affiliation-based Rules               | AffiliationBasedRules             | 
    | Geography-based Rules                 | GeographyBasedRules               | 
    | Geography and Affiliation-based Rules | AffiliationAndGeographyBasedRules |
#   | Universe-based Rules    | UniverseBasedRules     |
#   | Limit Rules to Region   | LimitAlignmentToRegion | 


@CORE @Engine
Scenario Outline: Alignment per SalesForce
    Related Test Cases:
    - C7688660, C7688661: Account alignment is added and removed per Territory being added/removed from a SalesForce on which Geo Rule is based.
    - C12526344 - AccountProductTerritory + Territory Cascade: If Parent_New_EndDate is before Child_Current_EndDate
    #removed Hierarchy from all the relevant titles within this case to avoid misunderstanding as the automated cases are not requiring and are not setting any hierarchy within SalesForce or Territory.

    Given the tenant configuration defined by 'smoketest-org.json'
	And I commit initial data 'Scenario/RuleEngine/<Scenario>_Setup.json' and check results
    Given the scenario test '<Test> Test' defined by 'Scenario/RuleEngine/<Scenario>_Test.json'
    When I 'calculate' the scenario and check all results

    Examples:
    | Test                     | Scenario   |
    | Account alignment per SF | AlignPerSF |


@CORE @Engine
Scenario Outline: Alignment by Geography + Explicits RuleScope
    Related Test Cases:
    - C310418391, C310418385, C310418384, C310418382: OMR-15569 Geography and Affiliation-based Rules

    Given the tenant configuration defined by 'smoketest-org.json'
    And the scenario test '<Test>' defined by 'Scenario/RuleEngine/OMR-15569/<Scenario>.json'
    When I 'calculate' the scenario and check snow results

    Examples:
    | Test                                                | Scenario                                               | 
    | GeographyAndAffiliationCompleteOverlap              | AffiliationAndGeographyCompleteOverlap                 | 
    | GeographyAndAffiliationNoOverlap                    | AffiliationAndGeographyNoOverlap                       |          
    | GeographyAndAffiliationWithGeographyOnlyAffiliation | GeographyBasedAffiliationAndGeographyExplicitRuleScope |          
    | GeographyAndAffiliationContinuous                   | AffiliationAndGeographyContinuous                      |          
