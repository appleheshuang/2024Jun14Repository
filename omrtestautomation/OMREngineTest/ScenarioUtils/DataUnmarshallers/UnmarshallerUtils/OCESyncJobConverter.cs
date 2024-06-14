using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OMREngineTest.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace OMREngineTest.ScenarioUtils.DataUnmarshallers.UnmarshallerUtils
{
    public class OCESyncJobConverter : JsonConverter
    {
        public override bool CanWrite { get { return false; } }

        public override bool CanConvert(Type objectType)
        {
            //return objectType == typeof(IOCESyncJobParameters);
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            /*var jsonObject = JObject.Load(reader);
            OCESyncJobRequestBody reqBody = new OCESyncJobRequestBody();
            switch (jsonObject["Subtype"].ToString())
            {
                case "PUBLISH_OTHER_OMR_DATA":
                    reqBody.Parameters = new PUBLISH_OTHER_OMR_DATA();
                    break;
                case "PUBLISH_CUSTOMER_ALIGNMENT":
                    reqBody.Parameters = new PUBLISH_CUSTOMER_ALIGNMENT();
                    break;
                case "PULL_ACCOUNT":
                    reqBody.Parameters = new PULL_ACCOUNT();
                    break;
                case "INITIAL_LOAD":
                    reqBody.Parameters = new INITIAL_LOAD();
                    break;
            }
            serializer.Populate(jsonObject.CreateReader(), reqBody);
            return reqBody;*/
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("Use default serialization.");
        }
    }
}
