using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using MainFramework.GlobalUtils;
using MainFramework.Constructors;

namespace MainFramework
{
    public class s3TenantFixture : IDisposable
    {
        public TenantConfig Config { get; private set; }
        private const string default_testenv = "master";

        public s3TenantFixture(string filename)
        {
            string test_env = TestEnv();
            string target_org = TargetOrg();
            string org_suffix = (target_org == "default") ? test_env : target_org;
            string target_version = TargetVersion();
            S3Client s3reader = new S3Client();
            string tenantconfig_str;

            //Look for filename in each location - from local to global
            string[] target_file = new string[] {
                test_env + "/" + filename.Replace("org", org_suffix), // env path with org suffix
                test_env + "/" + filename.Replace("org", test_env), // env path with env suffix
                test_env + "/" + filename, // env path with default suffix
                filename.Replace("org", org_suffix), // global with org suffix
                filename.Replace("org", test_env), // global with env suffix
                filename // global with default filename
            };
            Tuple<bool, string> target_config = new Tuple<bool, string>(false,"");
            for (int i = 0; i < target_file.Length && !target_config.Item1; i++)
                target_config = s3reader.getTenantConfig(target_file[i]);
            if (target_config.Item1) tenantconfig_str = target_config.Item2;
            else throw s3reader.ConfigNotFoundException("Config file not found in env or global configs: " + filename);

            //Config = JsonConvert.DeserializeObject<TenantConfig>(tenantconfig_str);
            if (target_config.Item2.Contains("CONFIG_API_TOKEN"))
            {
                Config = JsonConvert.DeserializeObject<DigitalTenantConfig>(tenantconfig_str);
            }
            else
            {
                Config = JsonConvert.DeserializeObject<TenantConfig>(tenantconfig_str);
            }
            Config.targetEnv = Config.targetEnv ?? test_env;
            if (target_version != "none") // use the env variable if passed in, otherwise default to the tenant config
                Config.targetVersion = target_version;
        }
        public s3TenantFixture()
        {
        }
        public string[] GetConfigs(string subfolder, string prefix = "")
        {
            S3Client s3reader = new S3Client();
            try { return s3reader.getTenantConfigs(TestEnv()+"/"+subfolder, prefix);}
            catch { return new string[0]; }
        }

        public void Dispose()
        {

        }
        private string TestEnv()
        {
            FileHelper fh = new FileHelper();
            List<string> known_envs = fh.loadCsvToList("NamedEnvs.csv", "GlobalConfigs");
            
            var TestEnv = Environment.GetEnvironmentVariable("TARGET_ENV") ?? "TargetEnvNotSet";
            if (known_envs.Contains(TestEnv)) return TestEnv;
            else
            {
                char[] split_chars = new char[] { '_', '/' };
                string[] branch_name_parts = TestEnv.ToString().Split(split_chars, StringSplitOptions.RemoveEmptyEntries);
                foreach (string partial in branch_name_parts)
                    if (known_envs.Contains(partial))
                        return partial;
            }
            return TestEnv;
        }
        private string TargetVersion()
        {
            return Environment.GetEnvironmentVariable("TARGET_VERSION") ?? "none";
        }
        private string TargetOrg()
        {
            return Environment.GetEnvironmentVariable("TARGET_ORG") ?? "default";
        }
    }
}
