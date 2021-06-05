using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Covid19TW
{
    public class Country {
        public string Name { get; set; }
        public int Total {get;set;}
        public List<City> Cities { get; set; }
        public List<InfectedDailyNumber> Infecteds { get; set; }
        
        public Country()
        {
            Cities = new List<City>();
            Infecteds = new List<InfectedDailyNumber>();
        }
    }
    public class City
    {
        public string Name { get; set; }
        public int Total {get;set;}
        public List<Town> Towns { get; set; }
        public List<InfectedDailyNumber> Infecteds { get; set; }
        
        public City()
        {
            Towns = new List<Town>();
            Infecteds = new List<InfectedDailyNumber>();
        }
    }
    public class Town
    {
        public string Name { get; set; }
        public List<InfectedDailyNumber> Infecteds { get; set; }
        public int Total {get;set;}
        public Town()
        {
            Infecteds = new List<InfectedDailyNumber>();
        }
    }

    public class InfectedDailyNumber
    {
        public string Name {get;set;}
        public string Date { get; set; }
        public int Number { get; set; }
        public int Avg7 {get;set;}
    }

    public class Infected
    {
        public string 確定病名 { get; set; }
        public string 個案研判日 { get; set; }
        public string 縣市 { get; set; }
        public string 鄉鎮 { get; set; }
        public string 是否為境外移入 { get; set; }
        public string 年齡層 { get; set; }
        public string 確定病例數 { get; set; }
    }
}