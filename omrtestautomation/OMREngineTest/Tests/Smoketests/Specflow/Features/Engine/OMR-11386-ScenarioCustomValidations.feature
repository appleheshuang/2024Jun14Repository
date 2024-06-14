Feature: CustomValidations
	CustomValidations
	@engine

@CORE @Engine
Scenario Outline: OMR-11386-CustomValidations
	Given the tenant configuration defined by 'smoketest-org.json'
	And the scenario test '<SetName>' defined by '<JsonFile>'
	And custom validation defined by '<CustomValidationPath>'
	When I '<JobType>' the scenario
	And the scenario is '<ExpStatus>'
	Then check all results per '<TestFile>'


	Examples:
	| SetName                   | JsonFile                               | JobType   | ExpStatus | CustomValidationPath                                             | TestFile                                                        |
	| PostCustomValidationsCalc | Smoketest/Scenario/BasicSmokeTest.json | calculate | ERROR     | Scenario/OMR-11386-CustomValidations/PostExampleRequestBody.json | Scenario/OMR-11386-CustomValidations/CustomValidationTests.json |
	| PreCustomValidationsCalc  | Smoketest/Scenario/BasicSmokeTest.json | calculate | ERROR     | Scenario/OMR-11386-CustomValidations/PreExampleRequestBody.json  | Scenario/OMR-11386-CustomValidations/CustomValidationTests.json |
