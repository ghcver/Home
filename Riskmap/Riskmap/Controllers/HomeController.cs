using Riskmap.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Linq;

namespace Riskmap.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (var context = new DBModel())
            {
                context.Configuration.LazyLoadingEnabled = false;
                return View(context.Risk.ToArray());
            }
        }

        public ActionResult GetRange(long riskID)
        {
            using (var context = new DBModel())
            {
                context.Configuration.LazyLoadingEnabled = false;
                return Json(context.Range.Where(i => i.RiskID == riskID).ToArray(), JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Refresh()
        {
            using (var context = new DBModel())
            {
                context.Database.ExecuteSqlCommand("Delete from Range_Backup;");
                context.Database.ExecuteSqlCommand("Delete from Risk_Backup;");

                foreach (var risk in context.Risk.ToList())
                {
                    Risk_Backup riskBackup = new Risk_Backup();
                    riskBackup.Name = risk.Name;
                    riskBackup.Status = risk.Status;
                    riskBackup.Area = risk.Area;
                    riskBackup.CloseDate = risk.CloseDate;
                    riskBackup.ConfirmDate = risk.ConfirmDate;
                    riskBackup.Pollutant = risk.Pollutant;
                    riskBackup.FinishDate = risk.FinishDate;
                    riskBackup.Latitude = risk.Latitude;
                    riskBackup.Longitude = risk.Longitude;

                    foreach (var range in risk.Range)
                    {
                        Range_Backup rangeBackup = new Range_Backup();
                        rangeBackup.PointLatitude = range.PointLatitude;
                        rangeBackup.PointLongitude = range.PointLongitude;
                        riskBackup.Range_Backup.Add(rangeBackup);
                    }

                    context.Risk_Backup.Add(riskBackup);
                }

                context.Database.ExecuteSqlCommand("Delete from Range;");
                context.Database.ExecuteSqlCommand("Delete from Risk;");

                ///////////////////////////////////////////////////////////

                List<Risk> riskList = new List<Risk>();
                List<Range> rangeList = new List<Range>();

                string urlReturn = GetURLReturn("http://www.hjxf.net/map/NplDel.xml");

                XDocument doc = XDocument.Parse(urlReturn);
                foreach (XElement node in doc.Root.Elements("S"))
                {
                    Risk risk = new Risk();
                    risk.Name = node.Attribute("E").Value;
                    risk.Status = node.Attribute("P") != null ? node.Attribute("P").Value : string.Empty;
                    risk.Area = node.Attribute("O") != null ? node.Attribute("O").Value : string.Empty;
                    risk.CloseDate = node.Attribute("F") != null ? node.Attribute("F").Value : string.Empty;
                    risk.ConfirmDate = node.Attribute("C") != null ? node.Attribute("C").Value : string.Empty;
                    risk.Pollutant = node.Attribute("I") != null ? node.Attribute("I").Value : string.Empty;
                    risk.FinishDate = node.Attribute("D") != null ? node.Attribute("D").Value : string.Empty;
                    risk.Latitude = double.Parse(node.Attribute("J").Value);
                    risk.Longitude = double.Parse(node.Attribute("K").Value);

                    if (node.Attribute("B") != null && Uri.IsWellFormedUriString(node.Attribute("B").Value, UriKind.Absolute) == true)
                    {
                        urlReturn = GetURLReturn(node.Attribute("B").Value);
                        XDocument pointDoc = XDocument.Parse(urlReturn);
                        XElement pointNode = pointDoc.Descendants().Where(i => i.Name.LocalName == "coordinates").First();
                        string[] points = pointNode.Value.Trim().Split(' ');
                        foreach (var item in points)
                        {
                            string[] parts = item.Split(',');
                            Range range = new Range();
                            range.PointLatitude = double.Parse(parts[1]);
                            range.PointLongitude = double.Parse(parts[0]);
                            risk.Range.Add(range);

                            rangeList.Add(range);
                        }
                    }

                    riskList.Add(risk);
                }

                ////////////////////////////////////////////////////////
                string convertURL = "http://api.map.baidu.com/geoconv/v1/?coords={0}&from=1&to=5&ak=08tkyWtKvBXwum5HW5rQUCWQ";

                int index = 0;
                while (index < riskList.Count())
                {
                    List<Risk> list = riskList.Skip(index).Take(100).ToList();
                    var queryString = "";
                    for (int i = 0; i < list.Count; i++)
                    {
                        queryString = queryString + list[i].Longitude + "," + list[i].Latitude;
                        if (i != list.Count - 1)
                        {
                            queryString = queryString + ";";
                        }
                    }

                    string locationJson = GetURLReturn(string.Format(convertURL, queryString));

                    dynamic location = System.Web.Helpers.Json.Decode(locationJson);
                    for (int i = 0; i < location.result.Length; i++)
                    {
                        list[i].Latitude = (double)location.result[i].y;
                        list[i].Longitude = (double)location.result[i].x;
                    }

                    index = index + 100;
                }

                index = 0;
                while (index < rangeList.Count())
                {
                    List<Range> list = rangeList.Skip(index).Take(100).ToList();
                    var queryString = "";
                    for (int i = 0; i < list.Count; i++)
                    {
                        queryString = queryString + list[i].PointLongitude + "," + list[i].PointLatitude;
                        if (i != list.Count - 1)
                        {
                            queryString = queryString + ";";
                        }
                    }

                    string locationJson = GetURLReturn(string.Format(convertURL, queryString));

                    dynamic location = System.Web.Helpers.Json.Decode(locationJson);
                    for (int i = 0; i < location.result.Length; i++)
                    {
                        list[i].PointLatitude = (double)location.result[i].y;
                        list[i].PointLongitude = (double)location.result[i].x;
                    }

                    index = index + 100;
                }

                context.Risk.AddRange(riskList);
                context.SaveChanges();
            }

            return View();
        }

        private string GetURLReturn(string url)
        {
            using (WebClient client = new WebClient())
            {
                string result = client.DownloadString(url);
                return result;
            }
        }
    }
}