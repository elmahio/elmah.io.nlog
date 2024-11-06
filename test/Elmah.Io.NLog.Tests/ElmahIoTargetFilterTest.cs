using NLog.Common;
using NLog.Config;
using NLog;
using NUnit.Framework;
using System;
using System.Threading;

namespace Elmah.Io.NLog.Tests
{
    public class ElmahIoTargetFilterTest
    {
        [Test]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2925:\"Thread.Sleep\" should not be used in tests", Justification = "Needed to test target from unit test")]
        public void CanFilter()
        {
            // Arrange
            InternalLogger.LogLevel = LogLevel.Debug;
            InternalLogger.LogFile = "c:\\temp\\tester\\test.txt";

            var messages = 0;
            var target = new ElmahIoTarget
            {
                Name = "elmah.io",
                ApiKey = "ApiKey",
                LogId = Guid.NewGuid().ToString(),
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message:withexception=true}",
                OnMessage = msg => messages++,
                OnFilter = msg => true,
            };

            var config = new LoggingConfiguration();
            config.AddTarget(target);

            var rule = new LoggingRule("*", LogLevel.Info, target);
            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;

            var logger = LogManager.GetLogger("Test");

            // Act
            logger.Error("An error");
            Thread.Sleep(1000);

            // Assert
            Assert.That(messages, Is.EqualTo(0));
        }
    }
}
