using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Amazon.DynamoDBv2;
using Newtonsoft.Json.Linq;


namespace MainFramework
{
    public class TenantFixture : IDisposable
    {
        public TenantConfig Config { get; private set; }
        public IAmazonDynamoDB Client { get; private set; }
        public bool OperationFailed { get; private set; }
        private const string repo_name = "omrtestautomation";
        private const string default_testenv = "master";
        private string configPath; private string altPath;

        public TenantFixture(string filename="config.json")
        {
            string test_env = TestEnv();
            string target_version = TargetVersion(); 
            //if the full file path is provided
            try
            {
                configPath = filename;
                File.ReadAllText(configPath);
            }
            catch
            {
                var currDir = Environment.CurrentDirectory;
                
                // look for file in the local project TenantConfigs
                int srchPath = currDir.IndexOf("bin");
                string rootPath = currDir.Substring(0, currDir.Length - (currDir.Length - srchPath));
                configPath = Path.Combine(rootPath, "TenantConfigs", test_env, filename.Replace("org",test_env));

                if (!File.Exists(configPath))
                    if (File.Exists(Path.Combine(rootPath, "TenantConfigs", filename.Replace("org", test_env))))
                        // fall back to env-named local file
                        configPath = Path.Combine(rootPath, "TenantConfigs", filename.Replace("org", test_env));
                    else if (File.Exists(Path.Combine(rootPath, "TenantConfigs", filename)))
                        // fall back to default local file
                        configPath = Path.Combine(rootPath, "TenantConfigs", filename);
                    else
                    {
                        // look for file in MainFramework TenantConfigs
                        srchPath = currDir.IndexOf(repo_name) + repo_name.Length;
                        rootPath = currDir.Substring(0, currDir.Length - (currDir.Length - srchPath));
                        altPath = Path.Combine(rootPath, "MainFramework", "TenantConfigs", test_env, filename.Replace("org", test_env));
                        if (File.Exists(altPath))
                            configPath = altPath;
                        else if (File.Exists(Path.Combine(rootPath, "MainFramework", "TenantConfigs", filename.Replace("org", test_env))))
                            // fall back to env-named MainFramework file
                            configPath = Path.Combine(rootPath, "MainFramework", "TenantConfigs", filename.Replace("org", test_env));
                        else if (File.Exists(Path.Combine(rootPath, "MainFramework", "TenantConfigs", filename)))
                            // fall back to default MainFramework file
                            configPath = Path.Combine(rootPath, "MainFramework", "TenantConfigs", filename);
                    }
            }
            Config = JsonConvert.DeserializeObject<TenantConfig>(File.ReadAllText(configPath));
            Config.targetEnv = Config.targetEnv ?? test_env.Replace("-","");
            if (target_version != "none") // use the env variable if passed in, otherwise default to the tenant config
                Config.targetVersion = target_version;
        }
        public TenantFixture()
        {
            string test_env = TestEnv();
            var currDir = Environment.CurrentDirectory;
            int srchPath = currDir.IndexOf("bin");
            string rootPath = currDir.Substring(0, currDir.Length - (currDir.Length - srchPath));
            configPath = Path.Combine(rootPath, "TenantConfigs", test_env);

            srchPath = currDir.IndexOf(repo_name) + repo_name.Length;
            rootPath = currDir.Substring(0, currDir.Length - (currDir.Length - srchPath));
            altPath = Path.Combine(rootPath, "MainFramework", "TenantConfigs", test_env);
        }
        public string[] GetConfigs(string subfolder, string pattern = "*.json")
        {
            string dir_path = Path.Combine(configPath, subfolder);
            if ( ! Directory.Exists(dir_path) )
                dir_path = Path.Combine(altPath, subfolder);
            try { return Directory.GetFiles(dir_path, pattern); }
            catch { return new string[0]; }
        }

        public void Save()
        {
            String jsonContent = JsonConvert.SerializeObject(Config, Formatting.Indented);
            System.IO.File.WriteAllText(configPath, jsonContent);
        }
        public void Dispose()
        {

        }
        private string TestEnv()
        {
            List<string> valid_envs = new List<string> { "master", "demo", "staging", "prod", "preprod" };
            valid_envs.Add("staging-preprod");
            valid_envs.Add("staging-prod");
            valid_envs.Add("preprod-eu");
            valid_envs.Add("preprod-apac");
            valid_envs.Add("preprod-us");
            valid_envs.Add("prod-eu");
            valid_envs.Add("prod-apac");
            valid_envs.Add("prod-us");

            var TestEnv = Environment.GetEnvironmentVariable("TARGET_ENV") ?? default_testenv;
            if (valid_envs.Contains(TestEnv)) return TestEnv;
            else
            {
                char[] split_chars = new char[] { '_', '/' };
                string[] branch_name_parts = TestEnv.ToString().Split(split_chars, StringSplitOptions.RemoveEmptyEntries);
                foreach (string partial in branch_name_parts)
                    if (valid_envs.Contains(partial))
                        return partial;
            }
            return default_testenv;
        }
        private string TargetVersion()
        {
            return Environment.GetEnvironmentVariable("TARGET_VERSION") ?? "none";
        }
    }
}
