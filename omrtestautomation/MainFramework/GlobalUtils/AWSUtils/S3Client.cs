using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon;
using Amazon.S3.Model;
using System.IO;

namespace MainFramework.GlobalUtils
{
    public class S3Client
    {
        private AmazonS3Client _client;
        private string default_bucket;
        private string tenant_config_dir;

        public S3Client(string accessKey=null, string secretKey=null, RegionEndpoint region=null, string sessionToken = null)
        {
            globalCodes default_s3config = new globalCodes("s3TestBucket.json");
            string default_accesskey = default_s3config["awsAccessKey"];
            string default_secretKey = default_s3config["awsSecretKey"];
            RegionEndpoint default_region = RegionEndpoint.USEast1;
            default_bucket = default_s3config["s3Bucket"];
            tenant_config_dir = default_s3config["tenantConfig"];
            _client = (sessionToken == null)
                ? new AmazonS3Client(accessKey ?? default_accesskey, secretKey ?? default_secretKey, region ?? default_region)
                : new AmazonS3Client(accessKey ?? default_accesskey, secretKey ?? default_secretKey, sessionToken, region ?? default_region);
        }

        public Tuple<bool, string> getTenantConfig(string filename, string subdir=null)
        {
            string filepath = subdir == null ? "" : (subdir + "/");
            if (filename.StartsWith(tenant_config_dir+"/"+filepath))
                return getFileContents(filename);
            else if (filename.StartsWith(tenant_config_dir) || filepath.StartsWith(tenant_config_dir))
                return getFileContents(filepath + filename);
            else
                return getFileContents(filepath + filename, tenant_config_dir);
        }

        public Tuple<bool,string> getFileContents(string filename, string path = null, string bucketName = null)
        {
            GetObjectRequest request = new GetObjectRequest();
            request.BucketName = bucketName ?? default_bucket;
            request.Key = normalized_path(path) + filename;
            try
            {
                Task<GetObjectResponse> s3Response = _client.GetObjectAsync(request);
                s3Response.Wait();
                StreamReader s3Reader = new StreamReader(s3Response.Result.ResponseStream);
                string strContents = s3Reader.ReadToEnd();
                return new Tuple<bool, string>(true, strContents);
            }
            catch 
            {
                return new Tuple<bool, string>(false, "");
            }
        }
        public string normalized_path(string path)
        {
            return path == null ? "" : (path + (path.EndsWith("/") ? "" : "/"));
        }
        public string trimmed_path(string path)
        {
            return path == null ? "" : path.TrimEnd('/');
        }
        public void putFileContents(string filename, string filedata, string s3path = null, string bucketName = null)
        {
            PutObjectRequest request = new PutObjectRequest();
            request.BucketName = bucketName ?? default_bucket;
            request.Key = normalized_path(s3path) + filename;
            request.ContentType = "text/plain";
            request.ContentBody = filedata;
            try
            {
                Task<PutObjectResponse> s3Response = _client.PutObjectAsync(request);
                s3Response.Wait();
                if (s3Response.Status != TaskStatus.RanToCompletion)
                    throw PutException("Failed to write file " + filename + " to bucket " + request.BucketName + "\n\t Data:\n\t" + request.ContentBody);
            }
            catch (AmazonS3Exception e)
            {
                throw PutException("Failed to write file "+filename, e);
            }
        }
        public bool purgeDirectory(string path, string bucketName = null)
        {
            ListObjectsV2Request obj_request = new ListObjectsV2Request
            {
                BucketName = bucketName ?? default_bucket,
                Prefix = path
            };
            try
            {
                // Get the files in the dir
                Task<ListObjectsV2Response> objs = _client.ListObjectsV2Async(obj_request);
                objs.Wait();
                if (objs.Status != TaskStatus.RanToCompletion) return false;
                DeleteObjectRequest del_request = new DeleteObjectRequest();
                Task<DeleteObjectResponse> del_response;
                // delete each
                foreach (S3Object f in objs.Result.S3Objects)
                {
                    del_request.Key = f.Key;
                    del_request.BucketName = f.BucketName;
                    del_response = _client.DeleteObjectAsync(del_request);
                    del_response.Wait();
                    if (del_response.Status != TaskStatus.RanToCompletion) return false;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public bool purgeItem(string item, string bucketName = null)
        {
            DeleteObjectRequest del_request = new DeleteObjectRequest
            {
                BucketName = bucketName ?? default_bucket,
                Key = item
            };
            try
            {
                Task<DeleteObjectResponse> del_response = _client.DeleteObjectAsync(del_request);
                del_response.Wait();
                if (del_response.Status != TaskStatus.RanToCompletion) return false;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public string[] getTenantConfigs(string dirname, string fileprefix = "")
        {
            string startswith = tenant_config_dir + "/" + dirname + "/" + fileprefix;
            return getDirContents(startswith);
        }

        public string[] getDirContents(string prefix, string bucketName = null)
        {
            ListObjectsRequest request = new ListObjectsRequest();
            request.BucketName = bucketName ?? default_bucket;
            request.Prefix = prefix;
            try
            {
                Task<ListObjectsResponse> s3bucketcontents = _client.ListObjectsAsync(request);
                s3bucketcontents.Wait();
                List<S3Object> s3objects = s3bucketcontents.Result.S3Objects;
                List<string> testfiles = new List<string> { };
                foreach (var s3obj in s3objects)
                    if (s3obj.Key != prefix) testfiles.Add(s3obj.Key);
                return testfiles.ToArray();
            }
            catch (AmazonS3Exception e)
            {
                throw ConfigNotFoundException("Failed to list s3 objects with " + request.Prefix, e);
            }
        }

        AmazonS3Exception ConfigNotFoundException(string message, Exception orig)
        {
            return new AmazonS3Exception("!!! " + message + "\n", orig);
        }
        public AmazonS3Exception ConfigNotFoundException(string message)
        {
            return new AmazonS3Exception("!!! " + message + "\n");
        }
        AmazonS3Exception PutException(string message, Exception orig)
        {
            return new AmazonS3Exception("!!! " + message + "\n", orig);
        }
        public AmazonS3Exception PutException(string message)
        {
            return new AmazonS3Exception("!!! " + message + "\n");
        }
    }
}