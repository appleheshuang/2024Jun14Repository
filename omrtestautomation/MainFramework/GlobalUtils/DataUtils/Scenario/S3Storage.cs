
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Sailfish.RealignmentEngine.Scenario
{
    [ExcludeFromCodeCoverage]
    public class S3Storage : IExternalStorage
    {
        private readonly string _bucketName;
        private readonly RegionEndpoint _region;
        private readonly string _accessKey;
        private readonly string _secretKey;

        public string Host => string.Format("s3://{0}", _bucketName);

        public string Credentials
        {
            get { return string.Format("AWS_KEY_ID='{0}' AWS_SECRET_KEY='{1}'", _accessKey, _secretKey); }
        }

        public S3Storage(string bucketName, string accessKey, string secretKey, string region)
        {
            _bucketName = bucketName;
            _accessKey = accessKey;
            _secretKey = secretKey;
            _region = RegionEndpoint.GetBySystemName(region);
        }

        public async Task UploadFile(string remoteKey, string localFilepath)
        {
            IAmazonS3 s3Client = new AmazonS3Client(_accessKey, _secretKey, _region);
            var fileTransferUtility = new TransferUtility(s3Client);
            await fileTransferUtility.UploadAsync(localFilepath, _bucketName, remoteKey);
        }

        public async Task DownloadFile(string remoteKey, string localFilepath)
        {
            IAmazonS3 s3Client = new AmazonS3Client(_accessKey, _secretKey, _region);
            var fileTransferUtility = new TransferUtility(s3Client);
            await fileTransferUtility.DownloadAsync(localFilepath, _bucketName, remoteKey);
        }

        public async Task DownloadDirectory(string remoteKey, string localPath)
        {
            IAmazonS3 s3Client = new AmazonS3Client(_accessKey, _secretKey, _region);
            var fileTransferUtility = new TransferUtility(s3Client);
            await fileTransferUtility.DownloadDirectoryAsync(_bucketName, remoteKey, localPath);
        }

        public async Task UploadDirectory(string remoteKey, string localPath)
        {
            IAmazonS3 s3Client = new AmazonS3Client(_accessKey, _secretKey, _region);
            var fileTransferUtility = new TransferUtility(s3Client);
            await fileTransferUtility.UploadDirectoryAsync(localPath, $"{_bucketName}/{remoteKey}");
        }
    }
}
