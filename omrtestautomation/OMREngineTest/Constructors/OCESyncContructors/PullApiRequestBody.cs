namespace OCESyncTest.Constructors
{
    public class PullApiRequestBody
    {
        public PullApiRequestBody(string type, string subType, string mode, string startFrom, bool rules_enabled = true, bool explicit_account = true, string ETMInitialModel = "ETMInitialModel", string ETMInitialTerr2Type = "ETMInitialTerr2Type")

        {
            Type = type;
            Subtype = subType;
            Mode = mode;
            StartFrom = startFrom;
            //Parameters = parameters;
            Parameters = new OCESyncParamConfig(rules_enabled, explicit_account, ETMInitialModel, ETMInitialTerr2Type);
        }

        public string Type { get; set; }
        public string Subtype { get; set; }
        public string Mode { get; set; }
        public string StartFrom { get; set; }
        //public string Parameters { get; set; }

        //public OCESyncParamConfig OCESyncParam { get; set; }
        public OCESyncParamConfig Parameters { get; set; }



    }
}