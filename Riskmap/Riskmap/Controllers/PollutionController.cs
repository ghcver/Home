using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Riskmap.Controllers
{
    public class PollutionController : Controller
    {
        // GET: Pollution
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ShowCd()
        {
            return View("Show", LoadData("Cd"));
        }

        public ActionResult ShowPd()
        {
            return View("Show", LoadData("Pd"));
        }

        public ActionResult ShowZn()
        {
            return View("Show", LoadData("Zn"));
        }

        public ActionResult ShowAs()
        {
            return View("Show", LoadData("As"));
        }

        public ActionResult ShowCu()
        {
            return View("Show", LoadData("Cu"));
        }

        public ActionResult ShowCr()
        {
            return View("Show", LoadData("Cr"));
        }

        private List<Site> LoadData(string filename)
        {
            List<Site> list = new List<Site>();

            foreach (var item in System.IO.File.ReadAllLines(Server.MapPath($"~/App_Data/{filename}.txt")))
            {
                if(item.Trim() == string.Empty)
                {
                    continue;
                }
                string[] temp = item.Split('\t');
                list.Add(new Site(decimal.Parse(temp[0]), decimal.Parse(temp[1]), decimal.Parse(temp[2])));
            }

            decimal line = decimal.Parse(ConfigurationManager.AppSettings[filename]);
            list = list.Where(i => i.Value > line).ToList();

            return list;
        }
    }

    public struct Site
    {
        public Site(decimal latitude, decimal longitude, decimal value)
        {
            Latitude = latitude;
            Longitude = longitude;
            Value = value;
        }

        public decimal Latitude;
        public decimal Longitude;
        public decimal Value;
    }
}