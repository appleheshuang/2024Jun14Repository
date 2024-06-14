using System.Threading.Tasks;

namespace Sailfish.RealignmentEngine.Scenario
{
    /// <summary>
    /// Defines the functionality required to push and pull data from Snowflake to external storage.
    /// </summary>
    public interface ISnowflakeDataSource
    {

        /// <summary>
        /// Push scenario data from external storage to a schema in Snowflake
        /// </summary>
        /// <param name="key">location of scenario data in the external storage</param>
        /// <param name="scenarioSchema">Schema name where scenario data is pushed to</param>
        void PushScenarioData(string key, string scenarioSchema);

        /// <summary>
        /// Pull output data from Snowflake to external storage
        /// </summary>
        /// <param name="scenarioId">Scenario ID</param>
        /// <param name="workspaceSchema">Output workspace schema name</param>
        /// <returns>location of output data in the external storage</returns>
        /// 
        string PullOutputData(string scenarioId, string workspaceSchema);
    }
}