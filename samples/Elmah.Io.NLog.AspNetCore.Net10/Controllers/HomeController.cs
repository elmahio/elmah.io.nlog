using Elmah.Io.NLog.AspNetCore.Net10.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Elmah.Io.NLog.AspNetCore.Net10.Controllers
{
    public class HomeController(ILogger<HomeController> logger) : Controller
    {
        public IActionResult Index()
        {
            // Simple information logging
            logger.LogInformation("Calling Index");

            // Example of structured logging
            logger.LogWarning("Index with {Method}", Request.Method);

            try
            {
                var i = 0;
                var result = 10 / i;
                ViewBag.Result = result;
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
