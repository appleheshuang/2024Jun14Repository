using System;
using MainFramework.GlobalUtils;

// deprecated - Use SalesforceApi to create salesforce objects; Keeping for OCESync compatibility (for now)
namespace OMREngineTest.Utils
{
    public class ScenarioFunction:SalesforceApi
    {

        public ScenarioFunction(string accesstoken, string orgURL, string sf_prefix)
            : base(accesstoken, orgURL, sf_prefix)
        {
        }

        public String Post_Scenario(string tablename, Object obj)
        {
            return Post_Object(tablename, obj);
        }

        public void Delete_Scenario(string tablename, string strId)
        {
            Delete_Object(tablename, strId);
        }
        public void Patch_Scenario(string tablename, Object obj, string strId)
        {
            Patch_Object(tablename, obj, strId);
        }

    }
}
