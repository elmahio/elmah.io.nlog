using NLog;
using System;
using System.Web.Mvc;

namespace Elmah.Io.NLog.AspNetMvc.Controllers
{
    public class HomeController : Controller
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public ActionResult Index()
        {
            Logger.Warn("Request to frontpage");
            return View();
        }

        public ActionResult About()
        {
            try
            {
                var i = 0;
                var result = 10 / i;
            }
            catch (Exception e)
            {
                // Example of error logging with exception details
                Logger.Error(e, "Error during About");
            }

            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}