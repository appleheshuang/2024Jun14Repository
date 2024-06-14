using System;
using System.Collections.Generic;
using System.Text;

namespace OCESyncTest.Constructors
{
    public class OCESyncParamConfig
    {
        public OCESyncParamConfig(bool rules_enabled, bool explicit_account, string ETMInitialModel, string ETMInitialTerr2Type)
                                 
        {
            RuleEngineEnabled = rules_enabled;
            ExplicitAccountTerritoryAlignment = explicit_account;
            EtmTerritoryModelName = ETMInitialModel;
            EtmTerritoryTypeName = ETMInitialTerr2Type;

        }


        public bool RuleEngineEnabled { get; set; }
        public bool ExplicitAccountTerritoryAlignment { get; set; }
        public string EtmTerritoryModelName { get; set; }
        public string EtmTerritoryTypeName { get; set; }

    }
} 

