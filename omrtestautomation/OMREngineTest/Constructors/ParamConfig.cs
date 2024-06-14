using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;

namespace OMREngineTest
{
    public class ParamConfig
    {
        private string defaultSettings = "RuleEngineEnabled=true";
        private Dictionary<string, string> _jobSettings, _tenantSettings, _defaultSettings;

        public ParamConfig(string jobSettings, string tenantSettings="")
		{
            _defaultSettings = ParseSettings(defaultSettings);
            _tenantSettings = ParseSettings(tenantSettings);
            _jobSettings = ParseSettings(jobSettings);
        }

        private Dictionary<string, string> ParseSettings(string values)
        {
            char[] separator_char = new char[] { ';' };
            char[] assignment_char = new char[] { '=', '|' };
            if (values == null || values == "" || values.ToLower() == "default")
                return new Dictionary<string, string>();
            else
                return values.Split(separator_char)
                    .Select(value => value.Split(assignment_char))
                    .ToDictionary(pair => pair[0], pair => pair[1]);
        }
        
        private Dictionary<string, string> AddUnique(Dictionary<string, string> orig, Dictionary<string, string> toAdd)
        {
            foreach (KeyValuePair<string, string> n in toAdd)
            {
                if (!orig.ContainsKey(n.Key))
                    orig.Add(n.Key, n.Value);
            }
            return orig;
        }

        public JObject GetSettings()
        {
            Dictionary<string, string> _allSettings = AddUnique(_jobSettings,_tenantSettings);
            _allSettings = AddUnique(_allSettings, _defaultSettings);
            return JObject.Parse(JsonConvert.SerializeObject(_allSettings));
        }
        
        public JObject GetLexiSetting()
        {
            return JObject.Parse(JsonConvert.SerializeObject(_jobSettings)); ;
        }

    }
}
