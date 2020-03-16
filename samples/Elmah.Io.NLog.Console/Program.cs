using NLog;
using System;
using System.Collections.Generic;

namespace Elmah.Io.NLog.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Decorate all log messages with common properties like application name and version
            GlobalDiagnosticsContext.Set("Application", "My cool app");

            var log = LogManager.GetCurrentClassLogger();
            log.Debug("This is a debug message");
            log.Error(new Exception(), "This is an error message");
            log.Fatal("This is a fatal message");
            log.Warn("This is a warning message");

            // Add custom properties to a log message
            var infoMessage = new LogEventInfo(LogLevel.Info, "", "This is an information message");
            infoMessage.Properties.Add("Some Property Key", "Some Property Value");
            infoMessage.Properties.Add("Some list", new[] { 1, 2, 3 });
            log.Info(infoMessage);

            // Decorate all log messages in this thread with a custom property
            MappedDiagnosticsContext.Set("application", "Thread app");
            log.Trace("This is a trace message");

            // Example of using structured logging. {user} will go into the User field on elmah.io
            log.Info("This is info with some structured logging: {quote} from {user}", "Hasta la vista, baby", "Arnold Schwarzenegger");

            // More advanced structured logging with examples of all of the reserved names used to get info into the different fields on the elmah.io UI
            log.Info("A message with {type} {hostname} {application} {user} {source} {method} {version} {url}, {statusCode}, {serverVariables}, {cookies}, {form} and {queryString}",
                "custom type",
                "custom hostname",
                "custom application",
                "custom user",
                "custom source",
                "custom method",
                "custom version",
                "custom url",
                500,
                new Dictionary<string, string> { { "REMOTE_ADDR", "1.1.1.1" } },
                new Dictionary<string, string> { { "_ga", "GA1.3.1162527071.1564749318" } },
                new Dictionary<string, string> { { "username", "Arnold" } },
                new Dictionary<string, string> { { "id", "42" } });

            // Flushes and closes all targets (recommended to call this just before application exit)
            LogManager.Shutdown();
        }
    }
}
