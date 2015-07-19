using System.Linq;
using Elmah.Io.Client;
using Moq;
using NLog;
using NLog.Config;
using NUnit.Framework;
using ILogger = Elmah.Io.Client.ILogger;

namespace Elmah.Io.NLog.Tests
{
    public class ElmahIoTargetTest
    {
        [Test]
        public void CanWrite()
        {
            // Arrange
            var loggerMock = new Mock<ILogger>();
            Message loggedMessage = null;
            loggerMock.Setup(x => x.Log(It.IsAny<Message>())).Callback<Message>(msg => loggedMessage = msg);
            var target = new ElmahIoTarget(loggerMock.Object);

            var config = new LoggingConfiguration();
            config.AddTarget("elmah.io", target);

            var rule = new LoggingRule("*", LogLevel.Info, target);
            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;

            var logger = LogManager.GetLogger("Test");

            // Act
            var logEventInfo = new LogEventInfo(LogLevel.Warn, "", "Warning");
            logEventInfo.Properties.Add("Key", "Value");
            logger.Log(logEventInfo);

            // Assert
            Assert.That(loggedMessage, Is.Not.Null);
            Assert.That(loggedMessage.Severity, Is.EqualTo(Severity.Warning));
            Assert.That(loggedMessage.Title, Is.StringContaining("Warning"));
            Assert.That(loggedMessage.Data.Count, Is.EqualTo(1));
            Assert.That(loggedMessage.Data.First().Key, Is.EqualTo("Key"));
            Assert.That(loggedMessage.Data.First().Value, Is.EqualTo("Value"));
        }
    }
}