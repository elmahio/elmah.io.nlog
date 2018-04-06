using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Elmah.Io.NLog.AspNetCore2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;

        public HomeController(ILogger<HomeController> logger)
        {
            this.logger = logger;
        }

        public IActionResult Index()
        {
            logger.LogInformation("Calling Index");
            return View();
        }

        public IActionResult About()
        {
            // Example of structured logging
            logger.LogWarning("About with {method}", Request.Method);

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
                logger.LogError(e, "Error during contact");
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
