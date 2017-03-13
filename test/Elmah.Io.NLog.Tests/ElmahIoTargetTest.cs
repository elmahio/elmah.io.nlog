using System;
using System.Linq;
using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using Moq;
using NLog;
using NLog.Config;
using NUnit.Framework;

namespace Elmah.Io.NLog.Tests
{
    public class ElmahIoTargetTest
    {
        [Test]
        public void CanWrite()
        {
            // Arrange
            var clientMock = new Mock<IElmahioAPI>();
            var messagesMock = new Mock<IMessages>();
            clientMock.Setup(x => x.Messages).Returns(messagesMock.Object);
            CreateMessage loggedMessage = null;
            messagesMock
                .Setup(x => x.CreateAndNotify(It.IsAny<Guid>(), It.IsAny<CreateMessage>()))
                .Callback<Guid, CreateMessage>((logId, msg) =>
                {
                    loggedMessage = msg;
                });
            var target = new ElmahIoTarget(clientMock.Object)
            {
                ApiKey = "ApiKey",
                LogId = Guid.NewGuid()
            };

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
            Assert.That(loggedMessage.Severity, Is.EqualTo(Severity.Warning.ToString()));
            Assert.That(loggedMessage.Title, Does.Contain("Warning"));
            Assert.That(loggedMessage.Data.Count, Is.EqualTo(1));
            Assert.That(loggedMessage.Data.First().Key, Is.EqualTo("Key"));
            Assert.That(loggedMessage.Data.First().Value, Is.EqualTo("Value"));
        }
    }
}