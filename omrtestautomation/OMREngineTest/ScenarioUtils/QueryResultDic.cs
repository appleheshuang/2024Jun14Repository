using System;
using System.Collections.Generic;
using Xunit.Abstractions;
using MainFramework.GlobalUtils;

namespace OMREngineTest.Utils
{

    public class QueryResultDic
    {
        private Dictionary<string, dynamic> _testDic = null;
        public static List<object[]> _ScenarioFieldsData = null;
        public static List<object[]> _timeStampData = null;
        public static List<string> _QueryList = null;
        public static Dictionary<string, string> _QueryDictionary = null;
        private int _timeStampInterval = 0;
        private string _snowdb = string.Empty;
        private string _schema = string.Empty;

        public QueryResultDic(string db, string schemaname = "OUTPUT", int interval=60)
        {
            _testDic = new Dictionary<string, dynamic>();
            _timeStampInterval = interval*1000;
            _snowdb = db;
            _schema = schemaname;
        }

        public void ResetSchema(string schemaname = "OUTPUT")
        {
            _schema = schemaname;
        }

        public QueryResult GetTest(string testname)
        {
            string q = _testDic[testname].GetQuery( _schema);
            string r = _testDic[testname].GetResult();
            return new QueryResult(q, r);
        }

        public IEnumerable<object[]> ScenarioMergeTestData()
        {
            _ScenarioFieldsData = new List<object[]>();
            foreach (KeyValuePair<string, dynamic> entry in _testDic)
            {
                var obj1 = (new object[] { entry.Value.GetQuery(_schema), entry.Value.exp_result });
                _ScenarioFieldsData.Add(obj1);
            }
            return _ScenarioFieldsData;
        }

        public IEnumerable<object[]> TimeStampTestData()
        {
            _ScenarioFieldsData = new List<object[]>();
            foreach (string query in _QueryList)
            {
                var obj1 = (new object[] { query });
                _ScenarioFieldsData.Add(obj1);
            }
            return _ScenarioFieldsData;
        }

        //check snowflake records updated within the Acceptable Time interval
        public Boolean checkTimeStamp(List<DateTimeOffset> snowflakeTimeStamp)
        {
            Boolean checkTimeStamp = true;
            for (int i = 0; i < snowflakeTimeStamp.Count; i++)
            {
                if (snowflakeTimeStamp[i].AddMinutes(_timeStampInterval) <= DateTimeOffset.Now)
                {
                    checkTimeStamp = false;
                }
            }

            if (snowflakeTimeStamp.Count == 0)
                checkTimeStamp = false;

            return checkTimeStamp;
        }

        public void AddQueryResult(string QueryName, QueryResult qr)
        {
            _testDic.Remove(QueryName);
			_testDic.Add(QueryName, qr);
        }		
		public void AddQueryResult(string QueryName, string SQL, string exp="[]")
        {
			QueryResult newquery = new QueryResult(SQL,exp);
			AddQueryResult(QueryName, newquery);
        }  

/*  What is this method used for?      
    public List<string> BuildQueryList(List<string> input)
        {
            _QueryList = new List<string>();
            for (int i = 0; i < input.Count; i++) //count=3
            {
                string sql = input[i];
                string query = BuildQuery(sql, _DB, _schema);
                _QueryList.Add(query);
            }
            return _QueryList;
        } */
	}
    
}