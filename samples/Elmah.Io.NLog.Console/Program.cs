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
            log.Info(infoMessage);

            log.Warn("This is a warning message");
            log.Trace("This is a trace message");
            System.Console.ReadLine();
        }
    }
}
