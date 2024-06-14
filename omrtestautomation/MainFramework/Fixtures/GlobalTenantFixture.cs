using System;
using MainFramework;
using System.IO;
using Newtonsoft.Json;
using Amazon.DynamoDBv2;
using MainFramework.GlobalUtils;
using Xunit.Abstractions;

namespace MainFramework
{
    public class GlobalTenantFixture : IDisposable
    {
        public TenantConfig Config { get; private set; }
        public IAmazonDynamoDB Client { get; private set; }
        public bool OperationFailed { get; private set; }
        //private readonly ITestOutputHelper _output;
        public GlobalTenantFixture(string _filename)
        {
            string rootPath = new FileHelper().globalRootDir();
            var configDir = new DirectoryInfo(rootPath).FullName;
            var files = Directory.GetFiles(configDir, _filename, SearchOption.AllDirectories);
            Config = JsonConvert.DeserializeObject<TenantConfig>(File.ReadAllText(files[0]));
        }
        public void Dispose()
        {

        }
    }
}
