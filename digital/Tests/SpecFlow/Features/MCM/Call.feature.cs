﻿// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (https://www.specflow.org/).
//      SpecFlow Version:3.9.0.0
//      SpecFlow Generator Version:3.9.0.0
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace Digital.Tests.SpecFlow.Features.MCM
{
    using TechTalk.SpecFlow;
    using System;
    using System.Linq;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.9.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public partial class CallFeature : object, Xunit.IClassFixture<CallFeature.FixtureData>, System.IDisposable
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
        private string[] _featureTags = ((string[])(null));
        
        private Xunit.Abstractions.ITestOutputHelper _testOutputHelper;
        
#line 1 "Call.feature"
#line hidden
        
        public CallFeature(CallFeature.FixtureData fixtureData, Digital_XUnitAssemblyFixture assemblyFixture, Xunit.Abstractions.ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
            this.TestInitialize();
        }
        
        public static void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Tests/SpecFlow/Features/MCM", "Call", "OCEMCM-19552 Import Call Data from MC to Snowflake\nC15662799 All fields with valu" +
                    "e will sync to snowflake successfully.\nC15662800 Only have mandatory fields and " +
                    "the record will sync to snowflake.", ProgrammingLanguage.CSharp, ((string[])(null)));
            testRunner.OnFeatureStart(featureInfo);
        }
        
        public static void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        public virtual void TestInitialize()
        {
        }
        
        public virtual void TestTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public virtual void ScenarioInitialize(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioInitialize(scenarioInfo);
            testRunner.ScenarioContext.ScenarioContainer.RegisterInstanceAs<Xunit.Abstractions.ITestOutputHelper>(_testOutputHelper);
        }
        
        public virtual void ScenarioStart()
        {
            testRunner.OnScenarioStart();
        }
        
        public virtual void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        void System.IDisposable.Dispose()
        {
            this.TestTearDown();
        }
        
        public virtual void Call(string tenant, string[] exampleTags)
        {
            string[] @__tags = new string[] {
                    "EvaJun"};
            if ((exampleTags != null))
            {
                @__tags = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(@__tags, exampleTags));
            }
            string[] tagsOfScenario = @__tags;
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            argumentsOfScenario.Add("Tenant", tenant);
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Call", null, tagsOfScenario, argumentsOfScenario, this._featureTags);
#line 7
this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 8
 testRunner.Given(string.Format("the tenant configuration defined by \'{0}\'", tenant), ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Given ");
#line hidden
#line 9
 testRunner.And("a unique id for the test", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "And ");
#line hidden
                TechTalk.SpecFlow.Table table1 = new TechTalk.SpecFlow.Table(new string[] {
                            "DE",
                            "datafileNew"});
                table1.AddRow(new string[] {
                            "DE_Call_Master_Message__IQ",
                            "MCM/Call/CallMasterMessage/New.json"});
                table1.AddRow(new string[] {
                            "DE_Contact_Master__IQ",
                            "MCM/Contact/ContactMaster/New.json"});
                table1.AddRow(new string[] {
                            "DE_Call_Master__IQ",
                            "MCM/Call/Call/New.json"});
                table1.AddRow(new string[] {
                            "DE_Call_Master_Promotional_Item__IQ",
                            "MCM/Call/CallMasterPromotionalItem/New.json"});
                table1.AddRow(new string[] {
                            "DE_Call_Master_Sample_Order__IQ",
                            "MCM/Call/CallSampleOrder/New.json"});
                table1.AddRow(new string[] {
                            "DE_Call_Master_Sample__IQ",
                            "MCM/Call/CallSample/New.json"});
                table1.AddRow(new string[] {
                            "DE_Call_Master_Product_Detail__IQ",
                            "MCM/Call/CallProductDetail/New.json"});
#line 10
 testRunner.Given("I post Digital record to table from Table", ((string)(null)), table1, "Given ");
#line hidden
#line 19
 testRunner.When("I trigger a DigitalDataSync with Type \'ScheduledSFMCDataSync\' and cache \'true\' wi" +
                        "th ExcludedAliases \'OMAccountActivityResponse\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 20
 testRunner.Then("check all DigitalDataSync jobs finished", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
                TechTalk.SpecFlow.Table table2 = new TechTalk.SpecFlow.Table(new string[] {
                            "DE",
                            "resultNew"});
                table2.AddRow(new string[] {
                            "all",
                            "MCM/Call/Call/NewSyncQueryResults.json"});
                table2.AddRow(new string[] {
                            "all",
                            "MCM/Call/CallMasterPromotionalItem/NewSyncQueryResults.json"});
                table2.AddRow(new string[] {
                            "all",
                            "MCM/Call/CallSampleOrder/NewSyncQueryResults.json"});
                table2.AddRow(new string[] {
                            "all",
                            "MCM/Call/CallSample/NewSyncQueryResults.json"});
                table2.AddRow(new string[] {
                            "all",
                            "MCM/Call/CallProductDetail/NewSyncQueryResults.json"});
                table2.AddRow(new string[] {
                            "all",
                            "MCM/Call/CallMasterMessage/NewSyncQueryResults.json"});
#line 21
 testRunner.Then("check digital results from table", ((string)(null)), table2, "Then ");
#line hidden
                TechTalk.SpecFlow.Table table3 = new TechTalk.SpecFlow.Table(new string[] {
                            "DE",
                            "datafileUpdate"});
                table3.AddRow(new string[] {
                            "DE_Call_Master_Message__IQ",
                            "MCM/Call/CallMasterMessage/Update.json"});
                table3.AddRow(new string[] {
                            "DE_Call_Master__IQ",
                            "MCM/Call/Call/Update.json"});
                table3.AddRow(new string[] {
                            "DE_Call_Master_Promotional_Item__IQ",
                            "MCM/Call/CallMasterPromotionalItem/Update.json"});
                table3.AddRow(new string[] {
                            "DE_Call_Master_Sample_Order__IQ",
                            "MCM/Call/CallSampleOrder/Update.json"});
                table3.AddRow(new string[] {
                            "DE_Call_Master_Sample__IQ",
                            "MCM/Call/CallSample/Update.json"});
                table3.AddRow(new string[] {
                            "DE_Call_Master_Product_Detail__IQ",
                            "MCM/Call/CallProductDetail/Update.json"});
#line 30
 testRunner.Given("I post Digital record to table from Table", ((string)(null)), table3, "Given ");
#line hidden
#line 38
 testRunner.When("I trigger a DigitalDataSync with Type \'ScheduledSFMCDataSync\' and cache \'true\' wi" +
                        "th ExcludedAliases \'OMAccountActivityResponse\'", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
#line 39
 testRunner.Then("check all DigitalDataSync jobs finished", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "Then ");
#line hidden
                TechTalk.SpecFlow.Table table4 = new TechTalk.SpecFlow.Table(new string[] {
                            "DE",
                            "resultUpdate"});
                table4.AddRow(new string[] {
                            "all",
                            "MCM/Call/Call/UpdateSyncQueryResults.json"});
                table4.AddRow(new string[] {
                            "all",
                            "MCM/Call/CallMasterPromotionalItem/UpdateSyncQueryResults.json"});
                table4.AddRow(new string[] {
                            "all",
                            "MCM/Call/CallProductDetail/UpdateSyncQueryResults.json"});
                table4.AddRow(new string[] {
                            "all",
                            "MCM/Call/CallSampleOrder/UpdateSyncQueryResults.json"});
                table4.AddRow(new string[] {
                            "all",
                            "MCM/Call/CallSample/UpdateSyncQueryResults.json"});
                table4.AddRow(new string[] {
                            "all",
                            "MCM/Call/CallMasterMessage/UpdateSyncQueryResults.json"});
#line 40
 testRunner.Then("check digital results from table", ((string)(null)), table4, "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Call: digital-org0325.json")]
        [Xunit.TraitAttribute("FeatureTitle", "Call")]
        [Xunit.TraitAttribute("Description", "Call: digital-org0325.json")]
        [Xunit.TraitAttribute("Category", "EvaJun")]
        public virtual void Call_Digital_Org0325_Json()
        {
#line 7
this.Call("digital-org0325.json", ((string[])(null)));
#line hidden
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="AddingTwoNumbers")]
        [Xunit.TraitAttribute("FeatureTitle", "Call")]
        [Xunit.TraitAttribute("Description", "AddingTwoNumbers")]
        [Xunit.TraitAttribute("Category", "EvaJun14")]
        public virtual void AddingTwoNumbers()
        {
            string[] tagsOfScenario = new string[] {
                    "EvaJun14"};
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("AddingTwoNumbers", null, tagsOfScenario, argumentsOfScenario, this._featureTags);
#line 56
this.ScenarioInitialize(scenarioInfo);
#line hidden
            bool isScenarioIgnored = default(bool);
            bool isFeatureIgnored = default(bool);
            if ((tagsOfScenario != null))
            {
                isScenarioIgnored = tagsOfScenario.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((this._featureTags != null))
            {
                isFeatureIgnored = this._featureTags.Where(__entry => __entry != null).Where(__entry => String.Equals(__entry, "ignore", StringComparison.CurrentCultureIgnoreCase)).Any();
            }
            if ((isScenarioIgnored || isFeatureIgnored))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
#line 57
testRunner.When("I add two Numbers", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.9.0.0")]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        public class FixtureData : System.IDisposable
        {
            
            public FixtureData()
            {
                CallFeature.FeatureSetup();
            }
            
            void System.IDisposable.Dispose()
            {
                CallFeature.FeatureTearDown();
            }
        }
    }
}
#pragma warning restore
#endregion
