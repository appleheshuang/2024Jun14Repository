using System;
using System.Collections;
using System.Data;
using System.Text;
using Newtonsoft.Json.Linq;
using static log4net.Appender.RollingFileAppender;

namespace MainFramework.GlobalUtils
{
    public class CodeHelper
    {
        public int COUNT_reader(IDataReader reader)
        {
            int count = 0;
            while (reader.Read())
            {
                count = count + 1;
            }
            return count;
        }

        // Dbt_Yesterday is Work-around for OMR-11746
        public string Dbt_Yesterday { get { return TodayDBT.AddDays(-1).ToString("yyyy-MM-dd"); } }
        public string Dbt_Today { get { return TodayDBT.ToString("yyyy-MM-dd"); } }
        public string Dbt_DaysAgo(int x) { return TodayDBT.AddDays(-x).ToString("yyyy-MM-dd"); }
        public string Dbt_DaysFuture(int x) { return TodayDBT.AddDays(x).ToString("yyyy-MM-dd"); }
        public string Yesterday { get { return DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"); } }
        public string Today { get { return DateTime.UtcNow.ToString("yyyy-MM-dd"); } }
        public string Open { get { return "3999-12-31"; } }
        public string DaysAgo(int x) { return DateTime.UtcNow.AddDays(-x).ToString("yyyy-MM-dd"); }
        public string DaysFuture(int x) { return DateTime.UtcNow.AddDays(x).ToString("yyyy-MM-dd"); }
        public string Dbt_Current { get { return DBT_Current.ToString("yyyy-MM-dd HH:mm:ss zzz"); } }
        public string Dbt_1DayAgo { get { return DBT_Current.AddDays(-1).ToString("yyyy-MM-dd HH:mm:ss zzz"); } }
        public int COUNT_json(string strJSON)
        {
            JArray jo = JArray.Parse(strJSON);
            int count = jo.Count;
            return count;
        }

        private DateTime TodayDBT;
        private DateTimeOffset DBT_Current;

        public DateTime GetDBTime(string snow, string env)
        {
            if (snow != null && snow.Contains("user="))
            {
                QuerySnowflake qs = new QuerySnowflake(snow);
                TodayDBT = qs.ReturnSingleDateTimeValue("select to_timestamp_ntz(current_timestamp) as \"CurrTime\" from dual", "CurrTime");
                DBT_Current = qs.ReturnSingleDateTimeColumnValueFromSnowFlake("select to_timestamp_tz(current_timestamp::string) as \"CurrTime\" from dual", "CurrTime");
            }
            else
                TodayDBT = DateTime.UtcNow.AddHours(TimezoneOffset(env).Item1); // Needed for unknown snowflake, eg. tenant create  
            return TodayDBT;
        }

        private Tuple<int, string> TimezoneOffset(string env)
        {
            if (env.EndsWith("-us")) return Tuple.Create(-7, "-0700");
            else if (env.EndsWith("-eu")) return Tuple.Create(2, "0200");
            else if (env.EndsWith("-ap")) return Tuple.Create(8, "0800");
            else if (env.StartsWith("staging")) return Tuple.Create(2, "0200"); // default to eu
            else return Tuple.Create(-7, "-0700"); // default to us
        }

        public string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }
            return builder.ToString();
        }
        public string RandomNumeric(int size)
        {
            Random random = new Random();
            int rand = 0;
            for (int i = 0; i < size; i++)
            {
                rand += random.Next(1, 9) * (int)Math.Pow(10, i);
                //rand += random.Next(0, 9) * (int)Math.Pow(10, i);
            }
            return rand.ToString();
        }
        /// <summary>
        /// Generate a unique id for the test
        /// Last four digits are the day of year
        /// </summary>
        /// <param name="length">Default to 9 digits = 5 random + MMdd</param>
        /// <returns></returns>
        public string GenerateTestId(int length=9)
        {
            return RandomNumeric(length - 4) + DateTime.Now.ToString("MMdd");
        }

        public Hashtable setUpAllHeaders(string contentType = "application/json", string xapikey = "NONE", string authorization = "NONE", string refreshtoken = "NONE")
        {
            Hashtable headers = new Hashtable();
            headers.Add("Content-Type", contentType);
            headers.Add("x-api-key", xapikey);
            headers.Add("Authorization", authorization);
            if (refreshtoken != "NONE")
            {
                headers.Add("refreshtoken", refreshtoken);
            }

            return headers;
        }

    }
}