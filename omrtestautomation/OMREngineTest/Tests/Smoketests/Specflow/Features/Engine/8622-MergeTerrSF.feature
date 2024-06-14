Feature: Scenario tests OMR-525
	Edit Salesforce several times
	
Covers OMR-525 and OMR-7561
@CORE @Engine
Scenario Outline: Edit Territory (SF1=>SF2) with SalesForce cascade
	C9323509 - Multi-SF Territory - Delete Starting SF 
	Given the tenant configuration defined by 'smoketest-org.json'
	And the optimizer settings '<OptimizerSettings>'
	And I commit initial data '<SetUpFile>' and check results
	Given the scenario test '<SetName2>' defined by '<JsonFile2>'
	When I 'commit' the scenario and check snow results
	Given the scenario test '<SetName3>' defined by '<JsonFile3>'
	When I 'calculate' the scenario and check snow results
	Given the scenario test '<SetName5>' defined by '<JsonFile5>'
	When I 'commit' the scenario 
	Given the scenario test '<SetName6>' defined by '<JsonFile6>'
	When I 'calculate' the scenario and check results
	Examples:
	| Testname                 | SetUpFile                                     | JsonFile2                                | JsonFile3                                | JsonFile4                                | SetName2          | SetName3            | SetName5            | JsonFile5                      | SetName6            | JsonFile6                    | OptimizerSettings      |
	| Multiple territory edits | Scenario/Edit-Territory/Precondition-Add.json | Scenario/Edit-Territory/Run-EditSF1.json | Scenario/Edit-Territory/Run-EditSF2.json | Scenario/Edit-Territory/Run-EditSF3.json | AutoCommit-Edit-1 | AutoSimulate-Edit-2 | AutoSimulate-Edit-3 | Run-EditSF4SFCascadeSetup.json | AutoSimulate-Edit-4 | Run-EditSF4SFCascadeDel.json | RuleEngineEnabled=true |
