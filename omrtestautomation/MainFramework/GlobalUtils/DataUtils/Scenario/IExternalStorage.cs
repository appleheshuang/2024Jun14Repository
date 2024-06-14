
using System.Threading.Tasks;

namespace Sailfish.RealignmentEngine.Scenario
{
    public interface IExternalStorage
    {
        Task UploadFile(string remoteKey, string localFilePath);
        Task DownloadFile(string remoteKey, string localFilePath);
        Task DownloadDirectory(string remoteKey, string localPath);
        Task UploadDirectory(string remoteKey, string localPath);
        string Credentials { get; }
        string Host { get; }
    }
}