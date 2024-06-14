using System;
using System.IO;
using Newtonsoft.Json;
using MainFramework.GlobalUtils;
//using Amazon;
//using Amazon.SecretsManager;
//using Amazon.SecretsManager.Model;

namespace MainFramework
{    
    public class EnvFixture : IDisposable
    {
        //private ITestOutputHelper _output;
        public EnvConfig Config { get; private set; }
        public EnvFixture(string ConfigFileName = "envConfig.json")
        {
            if (!ConfigFileName.StartsWith("envConfig")) ConfigFileName = "envConfig-" + ConfigFileName;
            if (!ConfigFileName.EndsWith(".json")) ConfigFileName = ConfigFileName + ".json";
            string rootPath = new FileHelper().globalRootDir();
            var configDir = new DirectoryInfo(rootPath).FullName;
            var files = Directory.GetFiles(configDir, ConfigFileName, SearchOption.AllDirectories);
            Config = JsonConvert.DeserializeObject<EnvConfig>(File.ReadAllText(files[0]));
            //Config.creator_pswd = GetSecret(Config.Secret)
        }
        public void Dispose() { }

        /*
         * AWSSDK.SecretsManager version="3.3.0" targetFramework="net45"
         *
        public string GetSecret(string secretName)
        {
            string region = "us-east-1";
            string secret = "";

            MemoryStream memoryStream = new MemoryStream();

            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

            GetSecretValueRequest request = new GetSecretValueRequest();
            request.SecretId = secretName;
            request.VersionStage = "AWSCURRENT"; // VersionStage defaults to AWSCURRENT if unspecified.

            GetSecretValueResponse response = null;

            // In this sample we only handle the specific exceptions for the 'GetSecretValue' API.
            // See https://docs.aws.amazon.com/secretsmanager/latest/apireference/API_GetSecretValue.html
            // We rethrow the exception by default.

            try
            {
                response = client.GetSecretValueAsync(request).Result;
            }
            catch (DecryptionFailureException e)
            {
                // Secrets Manager can't decrypt the protected secret text using the provided KMS key.
                // Deal with the exception here, and/or rethrow at your discretion.
                throw;
            }
            catch (InternalServiceErrorException e)
            {
                // An error occurred on the server side.
                // Deal with the exception here, and/or rethrow at your discretion.
                throw;
            }
            catch (InvalidParameterException e)
            {
                // You provided an invalid value for a parameter.
                // Deal with the exception here, and/or rethrow at your discretion
                throw;
            }
            catch (InvalidRequestException e)
            {
                // You provided a parameter value that is not valid for the current state of the resource.
                // Deal with the exception here, and/or rethrow at your discretion.
                throw;
            }
            catch (ResourceNotFoundException e)
            {
                // We can't find the resource that you asked for.
                // Deal with the exception here, and/or rethrow at your discretion.
                throw;
            }
            catch (System.AggregateException ae)
            {
                // More than one of the above exceptions were triggered.
                // Deal with the exception here, and/or rethrow at your discretion.
                throw;
            }

            // Decrypts secret using the associated KMS CMK.
            // Depending on whether the secret is a string or binary, one of these fields will be populated.
            if (response.SecretString != null)
            {
                secret = response.SecretString;
            }
            else
            {
                memoryStream = response.SecretBinary;
                StreamReader reader = new StreamReader(memoryStream);
                string decodedBinarySecret = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadToEnd()));
            }

            // Your code goes here.
        }*/
    }
}