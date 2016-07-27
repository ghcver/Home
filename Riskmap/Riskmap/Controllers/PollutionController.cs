using System;
using System.Collections.Generic;
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
            return View("Show");
        }
        public ActionResult ShowPd()
        {
            return View("Show");
        }
        public ActionResult ShowZn()
        {
            return View("Show");
        }
        public ActionResult ShowAs()
        {
            return View("Show");
        }
        public ActionResult ShowCu()
        {
            return View("Show");
        }
        public ActionResult ShowCr()
        {
            return View("Show");
        }
    }
}