using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MainFramework.GlobalUtils;
using Xunit.Abstractions;

namespace MainFramework
{

    public class globalCodes
    {
		/// <summary>
		/// Codes read from json file in GlobalConfigs folder
		/// </summary>
		/// <param name="configFile"></param>
		public globalCodes(string ConfigFile= "StatusCodes.json")
        {
			string rootPath = new FileHelper().globalRootDir();
			var configDir = new DirectoryInfo(rootPath).FullName;
            var files = Directory.GetFiles(configDir, ConfigFile, SearchOption.AllDirectories);
            _config = new ConfigurationBuilder().AddJsonFile(files[0]).Build();
        }

        public IConfigurationRoot Config => _config;
        public string this[string setting] => _config[setting];

        /// <summary>
        /// Access to underlying IConfigurationRoot object
        /// </summary>
        public static globalCodes Default
        {
            get
            {
                if (_default == null)
                    _default = new globalCodes();
                return _default;
            }
        }

		/// <summary>
		/// Get setting from dev.config
		/// </summary>
		/// <param name="setting">Name of setting</param>
		/// <returns></returns>
		public static string Get(string setting, string value)
		{
			JObject _codes = JObject.Parse(Default[setting]);
			return _codes[value].ToString();
		}

        IConfigurationRoot _config;

        static globalCodes _default;
    }
}
