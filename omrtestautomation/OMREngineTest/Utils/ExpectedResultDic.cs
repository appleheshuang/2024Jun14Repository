using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MainFramework.GlobalUtils;

namespace OMREngineTest.Utils
{

    public class ExpectedResultDic
    {
        private Dictionary<string, dynamic> _testDic = null;
        public static List<object[]> _ScenarioFieldsData = null;
        public static List<object[]> _timeStampData = null;
        public static List<string> _QueryList = null;
        public static Dictionary<string, string> _QueryDictionary = null;
        private int _timeStampInterval = 0;
        private string _snowdb = string.Empty;
        private string _schema = string.Empty;
        private string _sfprefix;

        public ExpectedResultDic(string db, string schemaname = "OUTPUT", string sfnamespace = "iq_sj_omr_01", int interval = 60)
        {
            _testDic = new Dictionary<string, dynamic>();
            _timeStampInterval = interval * 1000;
            _snowdb = db;
            _schema = schemaname;
            _sfprefix = sfnamespace + (sfnamespace.EndsWith('_') ? "" : "__");
        }

        public string getSchema()
        {
            return _schema;
        }
        public void ResetSchema(string schemaname = "OUTPUT")
        {
            _schema = schemaname;
        }

        public QueryResult GetTest(string testname)
        {
            string q = _testDic[testname].GetQuery(_schema);
            string r = _testDic[testname].GetResult();
            string o = _testDic[testname].GetOdataReq();
            string s = _testDic[testname].GetSfQuery(_sfprefix);
            return new QueryResult(q, r, o, s);
        }
        public Tuple<string, string> GetSnowTest(string testname)
        {
            string q = _testDic[testname].GetQuery(_schema);
            string r = _testDic[testname].GetResult();
            return new Tuple<string, string>(q, r);
        }
        public Tuple<string, string> GetOdataTest(string testname)
        {
            string o = _testDic[testname].GetOdataReq();
            string r = _testDic[testname].OdataExpResult();
            return new Tuple<string, string>(o, r);
        }
        public Tuple<string, string> GetSfTest(string testname)
        {
            string s = _testDic[testname].GetSfQuery(_sfprefix);
            string r = _testDic[testname].SfExpResult();
            return new Tuple<string, string>(s, r);
        }
        public string ParseSnowRes(string testname, string res)
        {
            try
            {
                return _testDic[testname].ParseSnowdata(res);
            }
            catch (Exception e)
            {
                throw ParseResultException("Snowflake", testname, res, e);
            }
        }
        public string ParseSnowApiRes(string testname, string res)
        {
            try
            {
                return _testDic[testname].ParseSnowApidata(res);
            }
            catch (Exception e)
            {
                throw ParseResultException("Snowflake Rest API", testname, res, e);
            }
        }
        public string ParseOdataRes(string testname, string res)
        {
            try
            {
                return _testDic[testname].ParseOdata(res);
            }
            catch (Exception e)
            {
                throw ParseResultException("Odata", testname, res, e);
            }
        }

        public string ParseSFdata(string testname, JArray res, string sfNameSpace)
        {
            try {
                return _testDic[testname].ParseSFdata(res, sfNameSpace);
            }
            catch (Exception e)
            {
                string sf_res_info = "Namespace=" + sfNameSpace + " Res=" + res.ToString();
                throw ParseResultException("SFquery", testname, sf_res_info, e);
            }
        }

        private JsonReaderException ParseResultException(string test_type, string test_name, string test_result, Exception e)
        {
            string display_error = "Failure to parse " + test_type + " response for " + test_name + "\n Response=" + test_result;
            return new JsonReaderException("!!!" + display_error + "\n", e);
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

        public List<KeyValuePair<string, Tuple<string, string>>> GetTestType(string test_type)
        {
            List<KeyValuePair<string, Tuple<string, string>>> _tests = new List<KeyValuePair<string, Tuple<string, string>>>();
            foreach (KeyValuePair<string, dynamic> entry in _testDic)
            {
                string q; string r;
                switch (test_type)
                {
                    case "sf":
                        q = entry.Value.sfquery != "skip" ? entry.Value.GetSfQuery(_sfprefix) : null;
                        r = entry.Value.SfExpResult();
                        break;
                    case "odata":
                        q = entry.Value.odata_req != "skip" ? entry.Value.GetOdataReq() : null;
                        r = entry.Value.OdataExpResult();
                        break;
                    default:
                        q = entry.Value.query != "skip" ? entry.Value.GetQuery(_schema) : null;
                        r = entry.Value.GetResult();
                        break;
                }
                if (q != null)
                {
                    Tuple<string, string> qr = new Tuple<string, string>(q, r);
                    _tests.Add(new KeyValuePair<string, Tuple<string, string>>(entry.Key, qr));
                }
            }
            return _tests;
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

        public void AddQueryResult(string QueryName, QueryResult qr)
        {
            _testDic.Remove(QueryName);
            _testDic.Add(QueryName, qr);
        }
        public void AddQueryResult(string QueryName, string SQL, string exp = "[]", string odata = null, string sf = null)
        {
            QueryResult newquery = new QueryResult(SQL, exp, odata, sf);
            AddQueryResult(QueryName, newquery);
        }

    }

}