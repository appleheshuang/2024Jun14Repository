using MainFramework.GlobalUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit.Abstractions;

namespace MainFramework.TestUtils
{
    public class CommonFunction
    {

        public string GenerateRndNumber(int length, Random random)
        {
            string characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            StringBuilder result = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(characters[random.Next(characters.Length)]);
            }
            return result.ToString();
        }

        public void CopyValues<T>(T target, T source)
        {
            Type t = typeof(T);

            var properties = t.GetProperties().Where(prop => prop.CanRead && prop.CanWrite);

            foreach (var prop in properties)
            {
                var value = prop.GetValue(source, null);
                if (value != null)
                    prop.SetValue(target, value, null);
            }
        }

        public bool GetTableComparison(Dictionary<string, object> objDictSnowDb, Dictionary<string, object> objDictSalesForceDb, ITestOutputHelper _output)
        {
            bool flag = false;
            bool isValid;
            HashSet<bool> objFlag = new HashSet<bool>();

            for (int i = 0; i < objDictSnowDb.Count; i++)
            {
                string strSnowFlake = objDictSnowDb.Keys.ElementAt(i).ToString();
                //for () {
                /// string item = string.Empty;
                //objDictSalesForceDb.TryGetValue(strSnowFlake, item)
                if (!objDictSalesForceDb.ContainsKey(strSnowFlake) && !string.IsNullOrEmpty(objDictSnowDb[strSnowFlake].ToString()))
                {
                    //goto SkipToEnd;
                    switch (objDictSnowDb.Keys.ElementAt(i).ToString())
                    {
                        case "OceSalesId":

                            if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["Id"].ToString()))
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force Id are Matching");
                                flag = true;
                            }
                            else
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force Id are not Matching");
                                flag = false;
                            }
                            break;

                        case "AccountType":
                            if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__RecordTypeDeveloperName__c"].ToString()))
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force Type are Matching");
                                flag = true;
                            }
                            else
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force Type are not Matching");
                                flag = false;
                            }
                            break;

                        case "Name":
                            if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["Id"].ToString()))
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force Type are Matching");
                                flag = true;
                            }
                            else
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force Type are not Matching");
                                flag = false;
                            }
                            break;

                        case "UniqueIntegrationId":
                            if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__UniqueIntegrationID__c"].ToString()))
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__IntegrationID__c are Matching");
                                flag = true;
                            }
                            else
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__IntegrationID__c are not Matching");
                                flag = false;
                            }
                            break;


                        case "SOMProductTerritoryId":

                            if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__UniqueIntegrationID__c"].ToString()))
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__UniqueIntegrationID__c are Matching");
                                flag = true;
                            }
                            else
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__UniqueIntegrationID__c are not Matching");
                                flag = false;
                            }
                            break;

                        case "SOMTerritoryId":

                            if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__Territory__c"].ToString()))
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__Territory__c are Matching");
                                flag = true;
                            }

                            else
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__Territory__c are not Matching");
                                flag = false;
                            }
                            break;

                        case "OMAccountId":

                            if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__Account__c"].ToString()))
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__Account__c are Matching");
                                flag = true;
                            }

                            else
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__Account__c are not Matching");
                                flag = false;
                            }
                            break;
                        case "Country":

                            if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__CountryCode__c"].ToString()))
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__CountryCode__c are Matching");
                                flag = true;
                            }

                            else
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__CountryCode__c are not Matching");
                                flag = false;
                            }
                            break;


                        case "Status":
                            if (objDictSalesForceDb.ContainsKey("OCE__IsActive__c"))
                            {
                                isValid = objDictSalesForceDb.ContainsKey("OCE__IsActive__c") ? objDictSalesForceDb["OCE__IsActive__c"].ToString() == "True" : true;
                                if (isValid && objDictSnowDb[strSnowFlake].Equals("ACTV"))
                                // if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__Status__c"].ToString()))
                                {
                                    _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__Status__c are Matching");
                                    flag = true;
                                }
                                else
                                {
                                    _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__Status__c are not Matching");
                                    flag = false;
                                }
                            }
                            else if (objDictSalesForceDb.ContainsKey("OCE__Inactive__c"))
                            {
                                isValid = objDictSalesForceDb.ContainsKey("OCE__Inactive__c") ? objDictSalesForceDb["OCE__Inactive__c"].ToString() == "False" : false;
                                if (isValid && objDictSnowDb[strSnowFlake].Equals("ACTV"))
                                // if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__Status__c"].ToString()))
                                {
                                    _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__Status__c are Matching");
                                    flag = true;
                                }
                                else
                                {
                                    _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__Status__c are not Matching");
                                    flag = false;
                                }
                            }
                            break;

                        case "AddressLine1":

                            if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["Name"].ToString()))
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force Name are Matching");
                                flag = true;
                            }
                            else
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force Name are not Matching");
                                flag = false;
                            }
                            break;
                        case "Longitude":
                            //  if(Convert.ToInt32 (objDictSnowDb[strSnowFlake])== Convert.ToInt32(objDictSalesForceDb["OCE__GeoLocation__Longitude__s"]))
                            string strLongitude = objDictSalesForceDb["OCE__GeoLocation__Longitude__s"].ToString();
                            //  if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__GeoLocation__Longitude__s"].ToString()))
                            if (objDictSnowDb[strSnowFlake].ToString().Contains(strLongitude + "."))
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__GeoLocation__Longitude__s are Matching");
                                flag = true;
                            }
                            else
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__GeoLocation__Longitude__s are not Matching");
                                flag = false;
                            }
                            break;
                        case "Latitude":
                            string strLatitude = objDictSalesForceDb["OCE__GeoLocation__Latitude__s"].ToString();
                            //  if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__GeoLocation__Latitude__s"].ToString()))
                            if (objDictSnowDb[strSnowFlake].ToString().Contains(strLatitude + "."))
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__GeoLocation__Latitude__s are Matching");
                                flag = true;
                            }
                            else
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__GeoLocation__Latitude__s are not Matching");
                                flag = false;
                            }
                            break;

                        case "AddressId":
                            if (objDictSalesForceDb["OCE__WorkplaceAccountAddress__r.OCE__Address__c"] == null || string.IsNullOrEmpty(objDictSalesForceDb["OCE__WorkplaceAccountAddress__r.OCE__Address__c"].ToString()))
                            {

                                if (objDictSnowDb[strSnowFlake].ToString().Length == 0)
                                {

                                    _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__WorkplaceAccountAddress__r.OCE__Address__c are doesnt have Data  ");
                                    flag = true;
                                }
                            }
                            else
                            {
                                if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__WorkplaceAccountAddress__r.OCE__Address__c"].ToString()))
                                {
                                    _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__WorkplaceAccountAddress__r.OCE__Address__c are Matching");
                                    flag = true;
                                }
                                else
                                {
                                    _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__WorkplaceAccountAddress__r.OCE__Address__c are not Matching");
                                    flag = false;
                                }


                            }
                            break;

                        case "AddressType":
                            if (objDictSalesForceDb["OCE__WorkplaceAccountAddress__r.OCE__AddressType__c"] == null || string.IsNullOrEmpty(objDictSalesForceDb["OCE__WorkplaceAccountAddress__r.OCE__AddressType__c"].ToString()))
                            {

                                if (objDictSnowDb[strSnowFlake].ToString().Length == 0)
                                {

                                    _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__WorkplaceAccountAddress__r.OCE__AddressType__c are doesnt have Data  ");
                                    flag = true;
                                }
                            }
                            else
                            {
                                if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__WorkplaceAccountAddress__r.OCE__Address__c"].ToString()))
                                {
                                    _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__WorkplaceAccountAddress__r.OCE__AddressType__c are Matching");
                                    flag = true;
                                }
                                else
                                {
                                    _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__WorkplaceAccountAddress__r.OCE__AddressType__c are not Matching");
                                    flag = false;
                                }


                            }

                            break;
                        default:
                            if (objDictSnowDb[strSnowFlake].ToString().Equals(objDictSalesForceDb["OCE__" + strSnowFlake + "__c"].ToString()))
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__" + strSnowFlake + "__c are Matching");
                                flag = true;
                            }
                            else
                            {
                                _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force OCE__" + strSnowFlake + "__c are not Matching");
                                flag = false;
                            }

                            break;
                    }
                }

                else if (objDictSnowDb[strSnowFlake].ToString().Length == 0)
                {

                    if (objDictSalesForceDb.ContainsKey("OCE__" + strSnowFlake + "__c"))
                    {
                        //if (string.IsNullOrEmpty(objDictSalesForceDb["OCE__" + strSnowFlake + "__c"].ToString()))
                        if (objDictSalesForceDb["OCE__" + strSnowFlake + "__c"] == null)
                        {
                            _output.WriteLine("SnowFlake  " + strSnowFlake + " value :" + objDictSnowDb[strSnowFlake] + " and Sales Force " + strSnowFlake + " value is " + objDictSalesForceDb["OCE__" + strSnowFlake + "__c"] + " .");
                            flag = true;
                        }
                        else
                        {
                            flag = false;
                        }
                    }
                    else
                    {
                        //if (string.IsNullOrEmpty(objDictSalesForceDb[strSnowFlake].ToString()))
                        if (objDictSalesForceDb[strSnowFlake] == null)
                        {
                            _output.WriteLine("SnowFlake  " + strSnowFlake + " value :" + objDictSnowDb[strSnowFlake] + " and Sales Force " + strSnowFlake + " value is " + objDictSalesForceDb[strSnowFlake] + " .");
                            flag = true;
                        }
                        else
                        {
                            flag = false;
                        }

                    }
                }

                else
                {
                    if (objDictSalesForceDb[strSnowFlake].Equals(objDictSnowDb[strSnowFlake]))
                    {
                        _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force " + strSnowFlake + " are Matching");
                        flag = true;
                    }
                    else
                    {
                        _output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force " + strSnowFlake + " are not Matching");
                        flag = false;
                    }

                }
                // SkipToEnd:
                //_output.WriteLine("SnowFlake  " + strSnowFlake + " and Sales Force " + strSnowFlake + " are Having Null or Empty");
                objFlag.Add(flag);
            }

            if (objFlag.Contains(false))
            {
                return false;
            }
            else
            {
                return true;
            }
        }




    }
}
