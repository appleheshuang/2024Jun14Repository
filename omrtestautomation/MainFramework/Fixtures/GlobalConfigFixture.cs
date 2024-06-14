using System;
using System.Net.Sockets;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using MainFramework.GlobalUtils;
using Xunit.Abstractions;

namespace MainFramework
{
    public class GlobalConfigFixture : IDisposable
    {
        public globalConfig GConfig { get; private set; }
        public GlobalConfigFixture()
        {
            //            FileHelper _filehelper = new FileHelper(_output); 
            //            string rootPath = _filehelper.returnRootDir();        
            var configPath = "D:\\Git\\omrtestautomation\\MainFramework\\GlobalConfigs\\globalConfig.json";
            GConfig = JsonConvert.DeserializeObject<globalConfig>(File.ReadAllText(configPath));
        }
        public void Dispose() { }
    }
}
