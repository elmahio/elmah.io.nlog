using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Extensions.Logging;
using System;

namespace Elmah.Io.NLog.ConsoleCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .Build();

            LogManager.Configuration = new NLogLoggingConfiguration(config.GetSection("NLog"));

            Logger logger = LogManager.GetCurrentClassLogger();

            logger.Debug("This is a debug message");
            logger.Error(new Exception(), "This is an error message");
            logger.Fatal("This is a fatal message");

            LogManager.Shutdown();
        }
    }
}
