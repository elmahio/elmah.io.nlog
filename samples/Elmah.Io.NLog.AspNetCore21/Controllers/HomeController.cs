using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Elmah.Io.NLog.AspNetCore21.Models;
using Microsoft.Extensions.Logging;

namespace Elmah.Io.NLog.AspNetCore21.Controllers
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
            // Simple information logging
            logger.LogInformation("Calling Index");

            // Example of structured logging
            logger.LogWarning("Index with {method}", Request.Method);

            try
            {
                var i = 0;
                var result = 10 / i;
            }
            catch (Exception e)
            {
                // Example of error logging with exception details
                logger.LogError(e, "Error during index");
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
