Feature: Scenario tests OMR-523, OMR-950, OMR-951
    Merge Relational Entities from Input to Output schemas
    OMR-18899, OMR-18900 Validation of DEL action for past User and Geo assignments

@MAT @Engine
Scenario Outline: Merge OMUserAssignment
    Given the tenant configuration defined by 'smoketest-org.json'
    And I post the salesforce users in file '<UserFile>'
	And I commit initial data '<SetUpFile>'
    Given the scenario test '<SetName2>' defined by '<JsonFile2>'
    When I 'calculate' the scenario and check results

    Examples:
    | Testname         | UserFile            | SetUpFile                                       | JsonFile2                                             | SetName2                 | 
    | OMUserAssignment | Users/Userdata.json | Scenario/MergeTests/Setup-OMUserAssignment.json | Scenario/MergeTests/Run-Actions-OMUserAssignment.json | Simulate-Actions-UserAss |

@MAT @Engine
Scenario Outline: Merge OMProductExplicit/AccountProductRule
    Given the tenant configuration defined by 'smoketest-org.json'
	And I commit initial data '<SetUpFile>'    
    Then check NestedExpand results per '<SetUpFile>'
    Given the scenario test '<SetName2>' defined by '<JsonFile2>'
    When I 'calculate' the scenario and check results

    Examples:
    | Testname                                                | SetUpFile                                                              | JsonFile2                                                                    | SetName2                                 |
    | OMProductExplicit/Exclusion OMAccountExplicit/Exclusion | Scenario/MergeTests/Setup-OMProductExplicit-Exclusion.json             | Scenario/MergeTests/Run-Actions-OMProductExplicit-Exclusion.json             | Simulate-Actions-ProdExpl-Excl           | 
    | AccountProductRule                                      | Scenario/MergeTests/Setup-OMAccountExplicit-Exclusion-AccProdRule.json | Scenario/MergeTests/Run-Actions-OMAccountExplicit-Exclusion-AccProdRule.json | Simulate-Actions-AccExpl-Excl-AccPrdRule |

@MAT @Engine
Scenario Outline: Merge relational entities
    Given the tenant configuration defined by 'smoketest-org.json'
    And a unique id for the test 
    And I post unique salesforce 'Product' from file 'Products/Product.json'
    And I wait for 'Product' identified by 'UniqueIntegrationId' to static sync
	And I commit initial data '<SetUpFile>'
    Given the scenario test '<SetName2>' defined by '<JsonFile2>'
    When I 'calculate' the scenario and check results

    Examples:
    | Testname                                                                                         | SetUpFile                                                                                                                 | JsonFile2                                                                                                                     | SetName2       | 
    | AccOwner, GeoTerr, ProdSF, AccProdExpl, AccProdTerrExpl, TerrHierarhy, SFHierarhy, AccSFExlusion | Scenario/MergeTests/Setup-AccOwner-GeoTerr-ProdSF-AccProdExpl-AccProdTerrExpl-TerrHierarhy-SFHierarhy-AccSFExlusion.json | Scenario/MergeTests/Run-Actions-AccOwner-GeoTerr-ProdSF-AccProdExpl-AccProdTerrExpl-TerrHierarhy-SFHierarhy-AccSFExlusion.json | Simulate-Merge | 

@CORE @Engine
Scenario Outline: OMR-18900 Delete handling for Geo and User assignments
    C16188948 Scenario fails with InvalidAction when DEL a GeoTerr or UserAssignment that spans today (current)
    C16188949 Scenario fails with InvalidAction when DEL a GeoTerr or UserAssignment that ends in the past (past)
    C16188950,C17319267 Scenario succeeds when DEL a GeoTerr or UserAssignment that starts today or in the future
    C17319268,C17319266 Scenario fails with InvalidAction when cascade DEL a Territory with past/current GeoTerr or UserAssignment
    C17319270,C17319269 Scenario succeeds when cascade DEL a Territory with today/future GeoTerr or UserAssignment
    C17715854 Scenario failure - On commit rollback post-validation failures
    
    Given the tenant configuration defined by 'smoketest-org.json'
    And the test data path <Path>
    And the optimizer settings 'default'
    And I post the salesforce users in file '<UserFile>'
    
    # Setup past, current and future geo and user assignments
	And I commit initial data '<SetupFile>' and check results
    
    # Check failed and successful delete scenarios
    When I simulate the scenario and check results from table

    | TestName                       | ScenarioFile                                | ExpStatus |
    | ERROR Del past                 | DeletePast-UserAssign-GeoTerr.json          | ERROR     |
    | ERROR Del current              | DeleteCurrent-UserAssign-GeoTerr.json       | ERROR     |
    | INAC Del today+future          | DeleteFuture-UserAssign-GeoTerr.json        | CALTD     |
    | ERROR Cascade-Del terr (past)  | CascadeDeletePast-UserAssign-GeoTerr.json   | ERROR     |
    | INAC Cascade-Del terr (future) | CascadeDeleteFuture-UserAssign-GeoTerr.json | CALTD     |

    Given the existing scenario named 'ERROR Cascade-Del terr (past)'
    When I 'commit' the scenario
    And the scenario fails
    Then check all results per 'CascadeDeletePast-UserAssign-GeoTerr.json'
    Then check all results per 'CascadeDeletePast-CommitRollbackChecks.json'

    Examples:
    | Testname               | UserFile            | Path                                         | SetupFile                     |
    | DEL-UserAssign-GeoTerr | Users/Userdata.json | Scenario/OMR-18900-Handling-of-Delete-action | Setup-UserAssign-GeoTerr.json |

@CORE @Engine
Scenario Outline: OMR-18900 Cascade delete territory handling with past INAC Geo and User assignments
    OMR-19796 Delete validation for geography alignment fires for historical records during territory cascade
    C17773191 Scenario succeeds when cascade DEL a Territory with inactive GeoTerr or UserAssignment regardless of timespan
    
    Given the tenant configuration defined by 'smoketest-org.json'
    And the test data path <Path>
    And I post the salesforce users in file '<UserFile>'
    
    # Setup past, current and future geo and user assignments for the territory
    # Inactivate past, current geo and user assignments
    # Cascade delete territory
    When I run the ordered scenarios and check results from table
    | Order | TestName                          | ScenarioFile                               | JobType   | ExpStatus |
    | 0     | 19796-Create initial assignments  | CascadeDeleteInactive-InitialSetup.json    | commit    | COMTD     |
    | 1     | 19796-Inactivate past assignments | CascadeDeleteInactive-InactivatePast.json  | commit    | COMTD     |
    | 2     | 19796-Cascade delete territory    | CascadeDeleteInactive-DeleteTerritory.json | calculate | CALTD     |
	
    Examples:
    | Testname                  | UserFile            | Path                               | ScenarioFile                   |
    | CascadeDeletePastInactive | Users/Userdata.json | Scenario/OMR-18900-Handling-of-Delete-action | CascadeDeletePastInactive.json |
