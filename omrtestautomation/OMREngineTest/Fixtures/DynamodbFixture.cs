using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace OMREngineTest
{
  public sealed class DynamodbFixture : IDisposable
  {
    public AmazonDynamoDBClient Client { get; private set; }
    public bool OperationSucceeded { get; private set; }
    public bool OperationFailed { get; private set; }
    public DynamodbFixture()
    {
      OperationFailed = false;
      OperationSucceeded = false;
      try { Client = new AmazonDynamoDBClient(); }
      catch (Exception ex)
      {
        Console.WriteLine("     FAILED to create a DynamoDB client; " + ex.Message);
        OperationFailed = true;
      }
      OperationSucceeded = true;
    }
    public async Task<GetItemResponse> GetTestData(string testName)
    {
      var key = new Dictionary<string, AttributeValue>
      {
        {"Id", new AttributeValue { S = testName }}
      };
      var request = new GetItemRequest
      {
        TableName = "OMREngineTest",
        Key = key
      };
      var ret = await Client.GetItemAsync(request);
      return ret;
    }

    public void Dispose()
    {
      Client.Dispose();
    }
  }
}
