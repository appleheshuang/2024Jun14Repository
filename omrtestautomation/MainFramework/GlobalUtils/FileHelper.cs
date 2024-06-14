using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Xunit.Abstractions;

namespace MainFramework.GlobalUtils
{
    public struct FileAndPath {
        public string file { get; }
        public string path { get; }
        public FileAndPath(string combinedpath)
        {
            char[] splitter = new char[] { '\\', '/' };
            string[] path_array = combinedpath.Split(splitter);
            int last = path_array.Length - 1;
            file = path_array[last];
            path = "";
            for (int i = 0; i < last; i++) path = Path.Combine(path, path_array[i]);
        }
        public FileAndPath(string sep_file, string sep_path)
        {
            string combinedpath = sep_path + Path.DirectorySeparatorChar + sep_file;
            char[] splitter = new char[] { '\\', '/' };
            string[] path_array = combinedpath.Split(splitter);
            int last = path_array.Length - 1;
            file = path_array[last];
            path = "";
            for (int i = 0; i < last; i++) path = Path.Combine(path, path_array[i]);
        }
        public string NormalizedPath
        {
            get
            {
                return Path.Combine(path, file);
            }
        }
    }
    
    public class FileHelper
    {
        //private readonly ITestOutputHelper _output;
        public static string CompareResult = "true";
        public const string globalRoot = "MainFramework";

        public FileHelper()
        {
            //_output = output;
        }

        /**
         * Input SQL and FileName, generate JSON file by search results from Snowflake
         */

        public string generateJsonFile(String sql, String fileName, string relativeFilePath, string Uniqkey, string snowdb)
        {
            //Fetch OriginalData from Snowflake
            QuerySnowflake qsfc = new QuerySnowflake(snowdb);
            String jsonContent = qsfc.ConvertWholeTabletoJsonFormatWithUniqkey(sql, Uniqkey);

            //WriteJSON file
            System.IO.File.WriteAllText(@GetFilePath(fileName, relativeFilePath), jsonContent);

            return jsonContent;
        }

        public JObject loadJsonFile(String FilePath)
        {
            JObject o1 = JObject.Parse(File.ReadAllText(@FilePath));
            return o1;
        }
        public List<string> loadCsvToList(String FilePath)
        {
            List<string> listA = new List<string>();
            using (var reader = new StreamReader(@FilePath))
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine().ToString();
                    List<string> values = line.Split(',').ToList();
                    listA.AddRange(values);
                }
            return listA;
        }
        public List<string> loadCsvToList(string fileName, string RelativefilePath)
        {
            return loadCsvToList(GetFilePath(fileName, RelativefilePath));
        }

        public string writeFilePath(string filename, string data)
        {
            FileAndPath norm_filepath = new FileAndPath(filename);
            DirectoryInfo new_dir = Directory.CreateDirectory(norm_filepath.path);
            string new_file = Path.Combine(new_dir.FullName, norm_filepath.file);
            File.WriteAllText(Path.Combine(norm_filepath.path, norm_filepath.file), data);
            return new_file;
        }
        public string GetFilePath(string filePath, string RelativefilePath, bool get_local_path = true)
        {
            FileAndPath thefile = new FileAndPath(RelativefilePath + Path.DirectorySeparatorChar + filePath);
            string fileName = thefile.file;
            string normalisedPath = thefile.path;
            string rootPath = new FileHelper().globalRootDir();
            var configDir = new DirectoryInfo(rootPath).FullName;
            var files = Directory.GetFiles(configDir, fileName, SearchOption.AllDirectories);
            var file = files.Where(file => file.Contains(normalisedPath)).FirstOrDefault();
            return file; 
        }

        public string GetDirectoryPath(string RelativeDirectoryPath)
        {
            String dataFileDir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..")).FullName;
            return Path.Combine(dataFileDir, RelativeDirectoryPath);
        }
        public string GetDirectoryPath(string dir_name, string RelativeDirectoryPath)
        {
            String dataFileDir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..")).FullName;
            return Path.Combine(dataFileDir, RelativeDirectoryPath, dir_name);
        }
        public string GlobalDirectoryPath(string RelativeDirectoryPath)
        {
            String dataFileDir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..","..",globalRoot)).FullName;
            return Path.Combine(dataFileDir, RelativeDirectoryPath);
        }

        public StringBuilder CompareObjects(JObject source, JObject target)
        {
            StringBuilder returnString = new StringBuilder();
            foreach (KeyValuePair<string, JToken> sourcePair in source)
            {
                if (sourcePair.Value.Type == JTokenType.Object)
                {
                    returnString.Append(string.Format(Environment.NewLine + "Compare {0}" + Environment.NewLine, sourcePair.Key));
                    if (target.GetValue(sourcePair.Key) == null)
                    {
                        returnString.Append("Key " + sourcePair.Key
                                            + " not found" + Environment.NewLine);
                        CompareResult = "false";
                    }
                    else if (target.GetValue(sourcePair.Key).Type != JTokenType.Object)
                    {
                        returnString.Append("Key " + sourcePair.Key
                                            + " is not an object in target" + Environment.NewLine);
                    }
                    else
                    {
                        returnString.Append(CompareObjects(sourcePair.Value.ToObject<JObject>(),
                            target.GetValue(sourcePair.Key).ToObject<JObject>()));
                    }
                }
                else if (sourcePair.Value.Type == JTokenType.Array)
                {
                    if (target.GetValue(sourcePair.Key) == null)
                    {
                        returnString.Append("Key " + sourcePair.Key
                                            + " not found" + Environment.NewLine);
                        CompareResult = "false";
                    }
                    else
                    {
                        returnString.Append(CompareArrays(sourcePair.Value.ToObject<JArray>(),
                            target.GetValue(sourcePair.Key).ToObject<JArray>(), sourcePair.Key));
                    }
                }
                else
                {
                    JToken expected = sourcePair.Value;
                    var actual = target.SelectToken(sourcePair.Key);
                    if (actual == null)
                    {
                        returnString.Append("Key " + sourcePair.Key
                                            + " not found" + Environment.NewLine);
                        CompareResult = "false";
                    }
                    else
                    {
                        if (!JToken.DeepEquals(expected, actual))
                        {
                            returnString.Append("Key " + sourcePair.Key + ": "
                                                + sourcePair.Value + " !=  "
                                                + target.Property(sourcePair.Key).Value
                                                + Environment.NewLine);
                            CompareResult = "false";
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, JToken> targetPair in target)
            {
                if (targetPair.Value.Type == JTokenType.Object)
                {
                    if (source.GetValue(targetPair.Key) == null)
                    {
                        returnString.Append(Environment.NewLine + string.Format("Compare {0}" + Environment.NewLine, targetPair.Key));
                        returnString.Append("Key " + targetPair.Key
                                            + " newly added" + Environment.NewLine);
                        CompareResult = "false";
                    }
                }
                else if (targetPair.Value.Type == JTokenType.Array)
                {
                    if (source.GetValue(targetPair.Key) == null)
                    {
                        returnString.Append("Key " + targetPair.Key
                                            + " newly added" + Environment.NewLine);
                        CompareResult = "false";
                    }
                }
                else
                {
                    JToken expected = targetPair.Value;
                    var actual = source.SelectToken(targetPair.Key);
                    if (actual == null)
                    {
                        returnString.Append("Key " + targetPair.Key
                                            + " newly added" + Environment.NewLine);
                        CompareResult = "false";
                    }
                }
            }
            //_output.WriteLine(returnString.ToString());
            //System.IO.File.WriteAllText(@fp.GetFilePath(diffFile), returnString.ToString());
            return returnString;
        }

        public StringBuilder CompareArrays(JArray source, JArray target, string arrayName = "")
        {
            var returnString = new StringBuilder();
            for (var index = 0; index < source.Count; index++)
            {

                var expected = source[index];
                if (expected.Type == JTokenType.Object)
                {
                    var actual = (index >= target.Count) ? new JObject() : target[index];
                    returnString.Append(CompareObjects(expected.ToObject<JObject>(),
                        actual.ToObject<JObject>()));
                }
                else
                {

                    var actual = (index >= target.Count) ? "" : target[index];
                    if (!JToken.DeepEquals(expected, actual))
                    {
                        if (String.IsNullOrEmpty(arrayName))
                        {
                            returnString.Append("Index " + index + ": " + expected
                                                + " != " + actual + Environment.NewLine);
                        }
                        else
                        {
                            returnString.Append("Key " + arrayName
                                                + "[" + index + "]: " + expected
                                                + " != " + actual + Environment.NewLine);
                        }
                    }
                }
            }
            return returnString;
        }

        public string returnRootDir(){
            var currDir = Environment.CurrentDirectory;
            int srchPath = currDir.IndexOf("bin");
            string rootPath = currDir.Substring(0, currDir.Length - (currDir.Length - srchPath));
            return rootPath;
        }
		public string globalRootDir()
		{
            var currDir = Environment.CurrentDirectory;
            string globalRootPath = Path.GetFullPath(Path.Combine(currDir, @"../../../../"));
            return globalRootPath;
        }
		public bool COMPARE_fileVfile(string path1, string path2){
            byte[] file1 = File.ReadAllBytes(path1);
            byte[] file2 = File.ReadAllBytes(path2);

            if (file1.Length == file2.Length){
                for(int i=0; i<file1.Length; i++){
                    if(file1[i] != file2[i]){
                        return false;
                    }
                }
                return true;
            }else{
                return false;
            }
        }

        public void LOAD_toXML(string fileLoc, string strXML){
            XmlDocument xdoc = new XmlDocument();
            xdoc.LoadXml(strXML);
            xdoc.Save(fileLoc);
        }

        public void generateTextFile(String fileName, String Content, string relativeFilePath)
        {
            System.IO.File.WriteAllText(@GetFilePath(fileName, relativeFilePath), Content);
        }

        public void deletefile(string relativeFilePath, string fileName)
        {
            deletefile(string.Format("{0}/{1}", relativeFilePath, fileName));
        }
        public void deletefile(string filePath)
        {
            //delelte existing file
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public Boolean checkfileExists(string relativeFilePath, string fileName)
        {
            Boolean fileExists = false;
            //delelte existing file
            if (File.Exists(string.Format("{0}/{1}", relativeFilePath, fileName)))
            {
                fileExists = true;
            }
            return fileExists;
        }

        public string getTimeStamp()
        {
            String timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            return timeStamp;
        }

        public static List<string> ExtractRegex(string propName)
        {
            string regexPattern = "(?<={{).*?(?=}})";
            //"{{.*?}}|(?=\\[).[0-9]+?(?>\\])";
            Regex regex = new Regex(regexPattern);
            //Match match = regex.Match(propName);
            List<string> matches = regex.Matches(propName)
                .Cast<Match>()
                .Select(m => m.Value)
                .ToList();


            //If there is a string between {{ and }}, use that string to see whether that is a key in BaseData
            return matches ?? new List<string>();
        }

        public static string ReplaceRegex(string fieldValue, string uid)
        {
            return Regex.Replace(fieldValue, @"\b{{TestUniqueIntegrationId}}\b", uid);
        }

        public static int? GetIndex(string fieldValue)
        {
            string regexPattern = "(?<=\\[).[0-9]?(?=\\])";
            Regex regex = new Regex(regexPattern);
            Match match = regex.Match(fieldValue);

            if (match.Success)
                return int.Parse(match.Value);

            return null;
        }
        public string SetFieldProperties(string value, Dictionary<string, dynamic> baseDataMap, Dictionary<string, dynamic> dataMap)
        {
            CodeHelper chelp = new CodeHelper();
            string fieldVal = value;
            if (fieldVal != null)
            {
                List<string> regexKeys = FileHelper.ExtractRegex(fieldVal);
                foreach (string regexKey in regexKeys)
                {
                    int index = FileHelper.GetIndex(regexKey) ?? 0;
                    string regexKeyLowercase = regexKey.ToLower();
                    if (baseDataMap.Keys.Contains(regexKey))
                    {
                        fieldVal = Regex.Replace(fieldVal, "{{" + regexKey + "}}", baseDataMap[regexKey]);
                        continue;
                    }
                    if (dataMap.Keys.Contains(regexKeyLowercase + "[" + index + "]"))
                    {
                        fieldVal = Regex.Replace(fieldVal, Regex.Escape("{{" + regexKeyLowercase + "}}"), dataMap[regexKeyLowercase + "[" + index + "]"], options: RegexOptions.IgnoreCase);
                        continue;
                    }
                    if (dataMap.Keys.Contains(regexKeyLowercase))
                    {
                        fieldVal = Regex.Replace(fieldVal, Regex.Escape("{{" + regexKeyLowercase + "}}"), dataMap[regexKeyLowercase], RegexOptions.IgnoreCase);
                        continue;
                    }
                    if (regexKeyLowercase.StartsWith("today-"))
                    {
                        int day_offset = 0;
                        try { day_offset = int.Parse(regexKeyLowercase["today-".Length..]); }
                        catch { Console.WriteLine("Exception getting day offset from " + regexKeyLowercase + ". Using 0 days offset."); }
                        fieldVal = Regex.Replace(fieldVal, Regex.Escape("{{" + regexKeyLowercase + "}}"), chelp.DaysAgo(day_offset), RegexOptions.IgnoreCase);
                        continue;
                    }
                    if (regexKeyLowercase.StartsWith("today+"))
                    {
                        int day_offset = 0;
                        try { day_offset = int.Parse(regexKeyLowercase["today+".Length..]); }
                        catch { Console.WriteLine("Exception getting day offset from " + regexKeyLowercase + ". Using 0 days offset."); }
                        fieldVal = Regex.Replace(fieldVal, Regex.Escape("{{" + regexKeyLowercase + "}}"), chelp.DaysFuture(day_offset), RegexOptions.IgnoreCase);
                        continue;
                    }
                    if (regexKeyLowercase.StartsWith("dbt_today-"))
                    {
                        int day_offset = 0;
                        try { day_offset = int.Parse(regexKeyLowercase["dbt_today-".Length..]); }
                        catch { Console.WriteLine("Exception getting day offset from " + regexKeyLowercase + ". Using 0 days offset."); }
                        fieldVal = chelp.Dbt_DaysAgo(day_offset);
                        continue;
                    }
                    if (regexKeyLowercase.StartsWith("dbt_today+"))
                    {
                        int day_offset = 0;
                        try { day_offset = int.Parse(regexKeyLowercase["dbt_today+".Length..]); }
                        catch { Console.WriteLine("Exception getting day offset from " + regexKeyLowercase + ". Using 0 days offset."); }
                        fieldVal = chelp.Dbt_DaysFuture(day_offset);
                        continue;
                    }
                }
            }
            return fieldVal;
        }
    }
}
