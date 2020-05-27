using NLog;
using System.Threading;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Elmah.Io.NLog.AspNetMvc
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // The following code show how to get callbacks when logging through the elmah.io NLog target.
            var target = (ElmahIoTarget)LogManager.Configuration.FindTargetByName("elmahio");

            // Get a callback on every message before logging to elmah.io. Here you can set common
            // properties like a version number, application, user, etc.
            target.OnMessage = msg =>
            {
                msg.Version = "1.0.0";
                msg.User = Thread.CurrentPrincipal.Identity?.Name;
            };
            target.OnError = (msg, err) =>
            {
                // Error already logged to NLog's self log. But maybe you want to do something else here?
            };
        }
    }
}
