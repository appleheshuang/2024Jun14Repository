using Newtonsoft.Json;

namespace OMREngineTest
{
    public class Recalc: Scenario
    {
        public Recalc(string id, string name, string desc = null)
        {
            Id = id;
            Name = name;
            Description__c = desc ?? "Auto-recalc";
            Type__c = "RECALC";
        }

        public override string ToString()
        {
            return string.Format("Recalc {0} Name {1}", UniqueIntegrationId__c, Name);
        }
    };
}
