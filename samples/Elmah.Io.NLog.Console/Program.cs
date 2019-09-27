using NLog;
using System;
using System.Collections.Generic;

namespace Elmah.Io.NLog.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var log = LogManager.GetCurrentClassLogger();
            log.Debug("This is a debug message");
            log.Error(new Exception(), "This is an error message");
            log.Fatal("This is a fatal message");

            var infoMessage = new LogEventInfo(LogLevel.Info, "", "This is an information message");
            infoMessage.Properties.Add("Some Property Key", "Some Property Value");
            infoMessage.Properties.Add("Some list", new[] { 1, 2, 3 });
            log.Info(infoMessage);

            log.Warn("This is a warning message");
            log.Trace("This is a trace message");
            log.Info("This is info with some structured logging: {quote} from {user}", "Hasta la vista, baby", "Arnold Schwarzenegger");

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

            System.Console.ReadLine();
            LogManager.Shutdown();  // Flushes and closes all targets (recommended to call this just before application exit)
        }
    }
}
