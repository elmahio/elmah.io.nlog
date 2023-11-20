using Elmah.Io.NLog.AspNetCore80.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Elmah.Io.NLog.AspNetCore80.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Simple information logging
            _logger.LogInformation("Calling Index");

            // Example of structured logging
            _logger.LogWarning("Index with {method}", Request.Method);

            try
            {
                var i = 0;
                var result = 10 / i;
            }
            catch (Exception e)
            {
                // Example of error logging with exception details
                _logger.LogError(e, "Error during index");
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
