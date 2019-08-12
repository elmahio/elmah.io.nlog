using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using Moq;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace Elmah.Io.NLog.Tests
{
    public class ElmahIoTargetTest
    {
        Mock<IElmahioAPI> clientMock;
        Mock<IMessages> messagesMock;
        Logger logger;

        [SetUp]
        public void SetUp()
        {
            clientMock = new Mock<IElmahioAPI>();
            messagesMock = new Mock<IMessages>();
            clientMock.Setup(x => x.Messages).Returns(messagesMock.Object);
            var target = new ElmahIoTarget(clientMock.Object)
            {
                ApiKey = "ApiKey",
                LogId = Guid.NewGuid().ToString(),
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}",
            };

            var config = new LoggingConfiguration();
            config.AddTarget("elmah.io", target);

            var rule = new LoggingRule("*", LogLevel.Info, target);
            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;

            logger = LogManager.GetLogger("Test");
        }

        [Test]
        public void CanFillFieldsFromStructuredLogging()
        {
            // Arrange
            CreateMessage loggedMessage = null;
            messagesMock
                .Setup(x => x.CreateAndNotifyAsync(It.IsAny<Guid>(), It.IsAny<CreateMessage>()))
                .Callback<Guid, CreateMessage>((logId, msg) =>
                {
                    loggedMessage = msg;
                });

            // Act
            logger.Info("Info message {method} {version} {url} {user} {type} {statusCode} {source} {hostname} {application}",
                HttpMethod.Get, "1.0.0", new Uri("http://a.b/"), "Mal", "System.NullReferenceException", 404, "The source", "The hostname", "The application");
            for (int i = 0; i < 10; ++i)
            {
                if (loggedMessage != null)
                    break;
                System.Threading.Thread.Sleep(1000);
            }

            // Assert
            Assert.That(loggedMessage, Is.Not.Null);
            Assert.That(loggedMessage.Title, Is.EqualTo("Info message GET \"1.0.0\" http://a.b/ \"Mal\" \"System.NullReferenceException\" 404 \"The source\" \"The hostname\" \"The application\""));
            Assert.That(loggedMessage.Method, Is.EqualTo("GET"));
            Assert.That(loggedMessage.Version, Is.EqualTo("1.0.0"));
            Assert.That(loggedMessage.Url, Is.EqualTo("http://a.b/"));
            Assert.That(loggedMessage.User, Is.EqualTo("Mal"));
            Assert.That(loggedMessage.Type, Is.EqualTo("System.NullReferenceException"));
            Assert.That(loggedMessage.StatusCode, Is.EqualTo(404));
            Assert.That(loggedMessage.Source, Is.EqualTo("The source"));
            Assert.That(loggedMessage.Hostname, Is.EqualTo("The hostname"));
            Assert.That(loggedMessage.Application, Is.EqualTo("The application"));
        }

        [Test]
        public void CanWrite()
        {
            // Arrange
            CreateMessage loggedMessage = null;
            messagesMock
                .Setup(x => x.CreateAndNotifyAsync(It.IsAny<Guid>(), It.IsAny<CreateMessage>()))
                .Callback<Guid, CreateMessage>((logId, msg) =>
                {
                    loggedMessage = msg;
                });

            // Act
            var logEventInfo = new LogEventInfo(LogLevel.Warn, "", "Warning");
            logEventInfo.Properties.Add("Key", "Value");
            logEventInfo.Properties.Add("List", new[] { 1, 2, 3 });
            logger.Log(logEventInfo);
            for (int i = 0; i < 10; ++i)
            {
                if (loggedMessage != null)
                    break;
                System.Threading.Thread.Sleep(1000);
            }

            // Assert
            Assert.That(loggedMessage, Is.Not.Null);
            Assert.That(loggedMessage.Severity, Is.EqualTo(Severity.Warning.ToString()));
            Assert.That(loggedMessage.Title, Does.Contain("Warning"));
            Assert.That(loggedMessage.Data.Count, Is.EqualTo(2));
            Assert.That(loggedMessage.Data.First().Key, Is.EqualTo("Key"));
            Assert.That(loggedMessage.Data.First().Value, Is.EqualTo("Value"));
            Assert.That(loggedMessage.Data.Last().Key, Is.EqualTo("List"));
            Assert.That(loggedMessage.Data.Last().Value, Is.EqualTo("1, 2, 3"));
        }

        [Test]
        public void BulkWrite()
        {
            // Arrange
            IList<CreateMessage> loggedMessages = null;
            messagesMock
                .Setup(x => x.CreateBulkAndNotifyAsync(It.IsAny<Guid>(), It.IsAny<IList<CreateMessage>>()))
                .Callback<Guid, IList<CreateMessage>>((logId, msgs) =>
                {
                    loggedMessages = msgs;
                });
            var elmahTarget = LogManager.Configuration.AllTargets.OfType<ElmahIoTarget>().FirstOrDefault();
            elmahTarget?.ContextProperties?.Add(new TargetPropertyWithContext("ThreadId", "${threadid}"));
            LogManager.ReconfigExistingLoggers();

            // Act
            for (int i = 0; i < 5; ++i)
            {
                logger.Warn("Hello World {0}", i);
            }
            for (int i = 0; i < 10; ++i)
            {
                if (loggedMessages != null)
                    break;
                System.Threading.Thread.Sleep(1000);
            }

            // Assert
            Assert.That(loggedMessages != null);
            Assert.That(loggedMessages.Count, Is.EqualTo(5));
            var loggedMessage = loggedMessages.First();
            Assert.That(loggedMessage, Is.Not.Null);
            Assert.That(loggedMessage.Severity, Is.EqualTo(Severity.Warning.ToString()));
            Assert.That(loggedMessage.Title, Does.Contain("Hello World"));
            Assert.That(loggedMessage.Data.Count, Is.EqualTo(1));
            Assert.That(loggedMessage.Data.First().Key, Is.EqualTo("ThreadId"));
            Assert.That(loggedMessage.Data.First().Value, Is.EqualTo(System.Threading.Thread.CurrentThread.ManagedThreadId.ToString()));
        }
    }
}