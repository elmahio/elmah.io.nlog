using NLog;
using System;

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
            System.Console.ReadLine();
        }
    }
}
