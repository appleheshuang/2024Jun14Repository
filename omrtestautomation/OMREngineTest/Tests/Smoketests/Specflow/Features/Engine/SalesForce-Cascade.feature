Feature: SalesForce Cascade
    OMR-11923 SalesForce action cascade into child records in hierarchy and related entities

@CORE @Engine
Scenario Outline: Multiple Scenarios With Positive Result
        C9323503: EFFE, EXPR SalesForce with cascade
        C9324507: Restart -> child has later effective date
        C9324510: Restart -> child has earlier effective date
        C9324508: End -> child has earlier end date
        C9325482: Cascade Salesforce Delete 
        C9324511: End -> child has later end date
        C9323508: Effective + End -> child has same dates
        C9325482: Delete SalesForce cascade to SalesForce child records
        C9387627: Delete SalesForce cascade to territory child records

    Given the tenant configuration defined by 'smoketest-org.json'
    And I post the salesforce users in file 'Users/Userdata.json'
    And the scenario test '<Test> Setup' defined by 'Scenario/SalesForce-Cascade/<Setup>.json'
    When I 'commit' the scenario 
    And the scenario is successful
    Given the scenario test '<Test> Test' defined by 'Scenario/SalesForce-Cascade/<Scenario>_Test.json'
    When I 'calculate' the scenario and check results

    Examples:
    | Test                         | Setup                    | Scenario                            | 
    | Cascade move SF EFFE forward | SalesForce-Cascade_Setup | SalesForce-Cascade_EFFE_MoveForward |        
    | Cascade move SF EFFE back    | SalesForce-Cascade_Setup | SalesForce-Cascade_EFFE_MoveBack    |         
    | Cascade move SF END forward  | SalesForce-Cascade_Setup | SalesForce-Cascade_EXPR_MoveForward |         
    | Cascade move SF END back     | SalesForce-Cascade_Setup | SalesForce-Cascade_EXPR_MoveBack    |        
    | Cascade delete               | SalesForce-Cascade_Setup | SalesForce-Cascade_DEL              |       
