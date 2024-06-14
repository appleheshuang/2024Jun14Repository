using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Sailfish.RealignmentEngine.Tests
{
    /// <summary>
    /// Config settings for my dev environment loaded from a json file
    /// default config file is: ../../../../integration-test/config/scenario-intg-test.json
    /// 
    /// Usage:
    ///    var setting_value = DevConfig.Get("setting-name");
    ///    var connectionString = DevConfig.Snowflake;
    ///    var connectionString = DevConfig.Get("snowflake");
    ///    var myConfig = new DevConfig("my-config-file");
    ///    var x = myConfig["x"];
    /// </summary>
    public class DevConfig
    {
        /// <summary>
        /// Dev config settings read from json file in configDir/configFile relative to engine folder (?)
        /// </summary>
        /// <param name="configFile"></param>
        public DevConfig(string configFile = null, string configDir = null, string ConfigFileName = null)
        {
            if (ConfigFileName == null)
            {
                ConfigFileName = "smoketest-org.json";
            }
            string configDirLocal = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", configDir ?? "Configs")).FullName;
            string configDirGlobal = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "MainFramework", configDir ?? "TenantConfigs")).FullName;
            string filename = configFile ?? ConfigFileName;
            configFile = File.Exists(Path.Combine(configDirLocal, filename)) 
                ? Path.Combine(configDirLocal, filename) 
                : Path.Combine(configDirGlobal, filename);
            _config = new ConfigurationBuilder().AddJsonFile(configFile).Build();
            //if (_config["snowflake"].Contains("USER=your-user;")) throw new ArgumentException($"Snowflake connection not set in config file: {configFile}");
        }

        public IConfigurationRoot Config => _config;
        public string this[string setting] => _config[setting];

        /// <summary>
        /// Access to underlying IConfigurationRoot object
        /// </summary>
        public static DevConfig Default
        {
            get
            {
                if (_default == null)
                    _default = new DevConfig();
                return _default;
            }
        }

        /// <summary>
        /// Get setting from dev.config
        /// </summary>
        /// <param name="setting">Name of setting</param>
        /// <returns></returns>
        public static string Get(string setting) => Default[setting];

        /// <summary>
        /// Gets the snowflake connection string from the default config file
        /// </summary>
        public static string Snowflake => Default["snowflake"];

        public static string ObscurePassword(string s) => Regex.Replace(s, @"\b(?<=password\s*=\s*)([^;]*)", "********", RegexOptions.IgnoreCase);

        IConfigurationRoot _config;

        static DevConfig _default;
    }
}
