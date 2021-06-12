using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autofac.Core;
using Microsoft.Extensions.Caching.Memory;
using Hangfire;

namespace Covid19TW
{
    public interface IDataManager
    {
        RawInfectedData GetRawData();
        void SetCountryDataNoCache();
        Country GetCountry(out string updatedTime);
        CountryCacheData GetCountryDataNoCache();

    }
    public class DataManager : IDataManager
    {
        const string _COUNTRY_CACHE_KEY = "_COUNTRY_CACHE_KEY";

        public Country GetCountry(out string updatedTime)
        {
            updatedTime = string.Empty;

            CountryCacheData countryCacheData = IoC.GetCache().Get<CountryCacheData>(_COUNTRY_CACHE_KEY);
            if (countryCacheData == null)
            {
                countryCacheData = GetCountryDataNoCache();
                IoC.GetCache().Set(_COUNTRY_CACHE_KEY, countryCacheData);
            }
            updatedTime = countryCacheData.UpdatedTime.ToString("yyyy/MM/dd");
            return countryCacheData.Country;
        }

        [AutomaticRetry(Attempts = 5, DelaysInSeconds = new int[] { 600 })]
        public void SetCountryDataNoCache()
        {
            CountryCacheData oldData = IoC.GetCache().Get<CountryCacheData>(_COUNTRY_CACHE_KEY);
            CountryCacheData countryCacheData = GetCountryDataNoCache();
            if (oldData == null || oldData.UpdatedTime != countryCacheData.UpdatedTime)
            {
                Console.WriteLine("Reset countryDataNoCache to {0} at {1}",
                    countryCacheData.UpdatedTime.ToString("yyyy/MM/dd"),
                    DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
                IoC.GetCache().Set(_COUNTRY_CACHE_KEY, countryCacheData);
            }
        }

        public CountryCacheData GetCountryDataNoCache()
        {
            Country country = new Country { Name = "台灣" };
            List<City> cities = country.Cities;
            cities.Add(new City { Name = "台北市" });
            cities.Add(new City { Name = "新北市" });
            cities.Add(new City { Name = "基隆市" });
            cities.Add(new City { Name = "宜蘭縣" });
            cities.Add(new City { Name = "桃園市" });
            cities.Add(new City { Name = "新竹市" });
            cities.Add(new City { Name = "新竹縣" });
            cities.Add(new City { Name = "苗栗縣" });
            cities.Add(new City { Name = "台中市" });
            cities.Add(new City { Name = "彰化縣" });
            cities.Add(new City { Name = "南投縣" });
            cities.Add(new City { Name = "雲林縣" });
            cities.Add(new City { Name = "台南市" });
            cities.Add(new City { Name = "嘉義縣" });
            cities.Add(new City { Name = "嘉義市" });
            cities.Add(new City { Name = "高雄市" });
            cities.Add(new City { Name = "屏東縣" });
            cities.Add(new City { Name = "花蓮縣" });
            cities.Add(new City { Name = "台東縣" });
            cities.Add(new City { Name = "澎湖縣" });
            cities.Add(new City { Name = "連江縣" });
            cities.Add(new City { Name = "空值" });

            HashSet<string> handledData = new HashSet<string>();
            var data = GetRawData();

            foreach (var infected in data.Infecteds)
            {
                string cityName = infected.縣市;
                string townName = infected.鄉鎮;

                City city = cities.First(t => t.Name == cityName);
                try
                {
                    Town town = city.Towns.FirstOrDefault(t => t.Name == townName);
                    if (town == null)
                    {
                        town = new Town { Name = infected.鄉鎮 };
                        city.Towns.Add(town);
                    }
                    int infectedNumber = Convert.ToInt32(infected.確定病例數);
                    country.Total += infectedNumber;
                    city.Total += infectedNumber;
                    town.Total += infectedNumber;
                    DateTime infectedTime = DateTime.ParseExact(infected.個案研判日, "yyyyMMdd", null);
                    string date = infectedTime.ToString("yyyy-MM-dd");
                    if (infectedTime > data.UpdatedTime)
                    {
                        data.UpdatedTime = infectedTime;
                    }

                    // country 每日
                    InfectedDailyNumber countryDailyNumber = country.Infecteds.FirstOrDefault(t => t.Date == date);
                    if (countryDailyNumber == null)
                    {
                        countryDailyNumber = new InfectedDailyNumber
                        {
                            Date = date,
                            Number = 0
                        };
                        country.Infecteds.Add(countryDailyNumber);
                    }
                    countryDailyNumber.Number += infectedNumber;
                    // city 每日
                    InfectedDailyNumber cityDailyNumber = city.Infecteds.FirstOrDefault(t => t.Date == date);
                    if (cityDailyNumber == null)
                    {
                        cityDailyNumber = new InfectedDailyNumber
                        {
                            Date = date,
                            Number = 0
                        };
                        city.Infecteds.Add(cityDailyNumber);
                    }
                    cityDailyNumber.Number += infectedNumber;
                    // town 每日
                    InfectedDailyNumber townDailyNumber = town.Infecteds.FirstOrDefault(t => t.Date == date);
                    if (townDailyNumber == null)
                    {
                        townDailyNumber = new InfectedDailyNumber
                        {
                            Date = date,
                            Number = 0
                        };
                        town.Infecteds.Add(townDailyNumber);
                    }
                    townDailyNumber.Number += infectedNumber;
                }
                catch
                {
                    throw new Exception("發生錯誤. city=" + infected.縣市 + ", town=" + infected.鄉鎮 + ", date=" + infected.個案研判日);
                }
            }

            int queryLimit = IoC.GetConfig().ItemsQueryLimit;
            if (queryLimit == 0)
            {
                queryLimit = 15;
            }
            //fix country dailyInfecteds
            country.Infecteds = ImproveInfecteds(country.Infecteds, queryLimit, data.UpdatedTime.Date);
            //fix city dailyInfecteds
            foreach (var city in country.Cities)
            {
                city.Infecteds = ImproveInfecteds(city.Infecteds, queryLimit, data.UpdatedTime.Date);
                foreach (var town in city.Towns)
                {
                    town.Infecteds = ImproveInfecteds(town.Infecteds, queryLimit, data.UpdatedTime.Date);
                }
            }

            CountryCacheData countryCacheData = new CountryCacheData
            {
                UpdatedTime = data.UpdatedTime,
                Country = country
            };

            return countryCacheData;
        }

        private List<InfectedDailyNumber> ImproveInfecteds(List<InfectedDailyNumber> dailyInfecteds, int top, DateTime recordDate)
        {
            int avgCalcDays = 5;
            List<InfectedDailyNumber> result = new List<InfectedDailyNumber>();
            DateTime startDate = recordDate.AddDays(-1 * (top + avgCalcDays));
            DateTime endDate = recordDate;
            var dict = dailyInfecteds.ToDictionary(t => t.Date);

            for (DateTime dt = endDate; dt >= startDate; dt = dt.AddDays(-1))
            {
                string dateKey = dt.ToString("yyyy-MM-dd");
                if (dict.ContainsKey(dateKey))
                {
                    result.Add(dict[dateKey]);
                }
                else
                {
                    result.Add(new InfectedDailyNumber
                    {
                        Date = dateKey
                    });
                }
            }
            for (int i = 0; i < top; i++)
            {
                int avgSum = 0;
                for (int j = i; j < i + avgCalcDays; j++)
                {
                    avgSum += result[j].Number;
                }
                result[i].Avg7 = avgSum / avgCalcDays;
            }
            result = result.Take(top).ToList();
            return result;
        }

        public RawInfectedData GetRawData()
        {
            Regex regex = new Regex(@"最後更新[</a-zA-Z>\r\n =""-/!_]*([0-9-T:+]*)"">",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);
            string rawDataUrl = "https://od.cdc.gov.tw/eic/Day_Confirmation_Age_County_Gender_19CoV.json";
            RawInfectedData data = null;

            data = new RawInfectedData();
            HttpClient cli = new HttpClient();
            var resp = cli.GetAsync(rawDataUrl).Result;
            if (resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string json = resp.Content.ReadAsStringAsync().Result;
                try
                {
                    data.Infecteds = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Infected>>(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: " + ex);
                    throw;
                }
            }

            resp = cli.GetAsync("https://data.cdc.gov.tw/dataset/agsdctable-day-19cov").Result;
            if (resp.StatusCode == System.Net.HttpStatusCode.OK)
            {
                string text = resp.Content.ReadAsStringAsync().Result;
                try
                {
                    Match match = regex.Match(text);
                    if (match.Success)
                    {
                        string updatedTime = match.Groups[1].Value;
                        data.UpdatedTime = DateTime.Parse(updatedTime);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: " + ex);
                    throw;
                }
            }

            return data;
        }

    }

    public class RawInfectedData
    {
        public DateTime UpdatedTime { get; set; }
        public List<Infected> Infecteds { get; set; }
    }

    public class CountryCacheData
    {
        public DateTime UpdatedTime { get; set; }
        public Country Country { get; set; }
    }


}