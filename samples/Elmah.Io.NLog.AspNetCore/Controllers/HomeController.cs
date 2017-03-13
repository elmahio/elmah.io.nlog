using System;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace Elmah.Io.NLog.AspNetCore.Controllers
{
    public class HomeController : Controller
    {
        Logger _log = LogManager.GetCurrentClassLogger();

        public IActionResult Index()
        {
            _log.Info("Calling Index");
            return View();
        }

        public IActionResult About()
        {
            _log.Warn("About warning");
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            try
            {
                var i = 0;
                var result = 10 / i;
            }
            catch (Exception e)
            {
                _log.Error(e, "Error during contact");
            }

            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
