
using System;

namespace Sailfish.RealignmentEngine.Salesforce
{
    public interface ISalesforceRestAPI
    {
        string Invoke(String reqUrl, string methodType, string contentType);

        string Invoke(String reqUrl, string methodType, string contentType, Byte[] body);

        void DownloadFile(String reqUrl, string filename);

        string UpdateSObject(string objectType, string objectId, string json);

        string RetrieveSObject(string objectType, string objectId);

        string DataService { get; }

        string AsyncService { get; }
    }
}