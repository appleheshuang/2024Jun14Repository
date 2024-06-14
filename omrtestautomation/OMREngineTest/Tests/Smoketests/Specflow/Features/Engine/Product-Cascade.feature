Feature: Scenario tests OMR-8241, OMR-8509

@MAT @Engine
Scenario Outline: Product alignment cascade
    Given the tenant configuration defined by 'smoketest-org.json'
    And the optimizer settings '<OptimizerSettings>'
    And the scenario test 'Prod cascade Setup' defined by 'Scenario/Product-Cascade/<JsonFile>'
    When I 'commit' the scenario and check results
    Given the scenario test 'Nooverlap; Add-ProdCascadeOff' defined by 'Scenario/Product-Cascade/<JsonFile2>'
    When I 'calculate' the scenario and check results
    Given the scenario test 'Overlap; EFFE-ProdSFcascadeON ' defined by 'Scenario/Product-Cascade/<JsonFile3>'
    When I 'calculate' the scenario and check results
    Given the scenario test 'External changes alignments' defined by 'Scenario/Product-Cascade/<JsonFile4>'
    When I 'calculate' the scenario and check results


    Examples:
   | JsonFile                   | JsonFile2                              | JsonFile3                             | JsonFile4                                  | OptimizerSettings |
   | Product-Cascade-Setup.json | Run-NonOverlap-Add-ProdCascadeOff.json | Run-Overlap-EFFE-ProdSFcascadeON.json | External-changes-SalesForce-Territory.json |                   |                