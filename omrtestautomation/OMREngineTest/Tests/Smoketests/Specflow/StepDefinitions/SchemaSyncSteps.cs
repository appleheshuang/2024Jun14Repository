using OMREngineTest.Tests.Smoketests.Specflow.Step_Definitions;
using TechTalk.SpecFlow.UnitTestProvider;
using TechTalk.SpecFlow;
using Xunit.Abstractions;
using Optimizer.ScenarioUtils.Jobs;

namespace Optimizer.Tests.Smoketests.Specflow.StepDefinitions
{
    [Binding]
    public class SchemaSyncSteps : BaseEngineSteps
    {
        public SchemaSyncSteps(ScenarioContext scenarioContext, IUnitTestRuntimeProvider unitTestRuntimeProvider, ITestOutputHelper output) : base(scenarioContext, output) { }

        [Given(@"I sync PickList values for OMAccount table")]
        public void GivenISyncPickListValuesForAccountTable()
        {
            PickListJob pickListJob = new PickListJob(_schemasync.t, _schemasync.engine_token);
            if (VerifyPickListValueExists("OMAccount", "AccountType", "Institution") < 1)
            {
                pickListJob.VerifyPickListJobIsSuccessful();
            }
        }

        protected int VerifyPickListValueExists(string table, string column, string code)
        {
            string query = "select count(\"Id\") \"recs\" from \"STATIC\".\"OMPickList\" where \"Table\"='" + table + "' and \"Column\" = '" + column + "' and \"Code\" = '" + code + "';";
            int recCount = int.Parse(_engine.snowq.ReturnSingleColumnValueFromSnowFlake(query, "recs"));
            return recCount;
        }

    }
}


