using System.Security.Claims;
using System.Security.Principal;
using System.Web.Mvc;
using System.Web.Security;
using NLog;

namespace Elmah.Io.NLog.Web.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            HttpContext.User = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            var log = LogManager.GetCurrentClassLogger();
            log.Info("Message from ASP.NET MVC application");

            return Content("Hello World", "text/html");
        }
    }
}