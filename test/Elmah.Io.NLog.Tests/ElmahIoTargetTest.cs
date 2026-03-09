#pragma warning disable S2925 // "Thread.Sleep" should not be used in tests
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Elmah.Io.Client;
using NLog;
using NLog.Config;
using NLog.Targets;
using NSubstitute;
using NUnit.Framework;

namespace Elmah.Io.NLog.Tests
{
    public class ElmahIoTargetTest
    {
        IElmahioAPI clientMock;
        IMessagesClient messagesClientMock;
        Logger logger;

        [SetUp]
        public void SetUp()
        {
            clientMock = Substitute.For<IElmahioAPI>();
            messagesClientMock = Substitute.For<IMessagesClient>();
            clientMock.Messages.Returns(messagesClientMock);
            var target = new ElmahIoTarget(clientMock)
            {
                ApiKey = "ApiKey",
                LogId = Guid.NewGuid().ToString(),
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message:withexception=true}",
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
            messagesClientMock.CreateAndNotifyAsync(Arg.Any<Guid>(), Arg.Do<CreateMessage>(msg => loggedMessage = msg), Arg.Any<CancellationToken>());

            // Act
            logger.Info("Info message {method} {version} {url} {user} {type} {statusCode} {source} {hostname} {application} {correlationId} {category} {serverVariables} {cookies} {form} {queryString}",
                HttpMethod.Get,
                "1.0.0",
                new Uri("http://a.b/"),
                "Mal",
                "System.NullReferenceException",
                404,
                "The source",
                "The hostname",
                "The application",
                "The correlationId",
                "The category",
                new Dictionary<string, string> { { "serverVariableKey", "serverVariableValue" } },
                new Dictionary<string, string> { { "cookieKey", "cookieValue" } },
                new Dictionary<string, string> { { "formKey", "formValue" } },
                new Dictionary<string, string> { { "queryStringKey", "queryStringValue" } });
            for (int i = 0; i < 10; ++i)
            {
                if (loggedMessage != null)
                    break;
                Thread.Sleep(1000);
            }

            // Assert
            Assert.That(loggedMessage, Is.Not.Null);
            Assert.That(loggedMessage.Title, Is.EqualTo("Info message GET 1.0.0 http://a.b/ Mal System.NullReferenceException 404 The source The hostname The application The correlationId The category serverVariableKey=serverVariableValue cookieKey=cookieValue formKey=formValue queryStringKey=queryStringValue"));
            Assert.That(loggedMessage.TitleTemplate, Is.EqualTo("Info message {method} {version} {url} {user} {type} {statusCode} {source} {hostname} {application} {correlationId} {category} {serverVariables} {cookies} {form} {queryString}"));
            Assert.That(loggedMessage.Method, Is.EqualTo("GET"));
            Assert.That(loggedMessage.Version, Is.EqualTo("1.0.0"));
            Assert.That(loggedMessage.Url, Is.EqualTo("/"));
            Assert.That(loggedMessage.User, Is.EqualTo("Mal"));
            Assert.That(loggedMessage.Type, Is.EqualTo("System.NullReferenceException"));
            Assert.That(loggedMessage.StatusCode, Is.EqualTo(404));
            Assert.That(loggedMessage.Source, Is.EqualTo("The source"));
            Assert.That(loggedMessage.Hostname, Is.EqualTo("The hostname"));
            Assert.That(loggedMessage.Application, Is.EqualTo("The application"));
            Assert.That(loggedMessage.CorrelationId, Is.EqualTo("The correlationId"));
            Assert.That(loggedMessage.Category, Is.EqualTo("The category"));
            Assert.That(loggedMessage.ServerVariables.Any(sv => sv.Key == "serverVariableKey" && sv.Value == "serverVariableValue"));
            Assert.That(loggedMessage.Cookies.Any(sv => sv.Key == "cookieKey" && sv.Value == "cookieValue"));
            Assert.That(loggedMessage.Form.Any(sv => sv.Key == "formKey" && sv.Value == "formValue"));
            Assert.That(loggedMessage.QueryString.Any(sv => sv.Key == "queryStringKey" && sv.Value == "queryStringValue"));
        }

        [Test]
        public void CanSetCategoryName()
        {
            // Arrange
            CreateMessage loggedMessage = null;
            messagesClientMock.CreateAndNotifyAsync(Arg.Any<Guid>(), Arg.Do<CreateMessage>(msg => loggedMessage = msg), Arg.Any<CancellationToken>());

            // Act
            logger.Info("Info message");
            for (int i = 0; i < 10; ++i)
            {
                if (loggedMessage != null)
                    break;
                Thread.Sleep(1000);
            }

            // Assert
            Assert.That(loggedMessage, Is.Not.Null);
            Assert.That(loggedMessage.Category, Is.EqualTo("Test"));
        }

        [Test]
        public void CanWrite()
        {
            // Arrange
            CreateMessage loggedMessage = null;
            messagesClientMock.CreateAndNotifyAsync(Arg.Any<Guid>(), Arg.Do<CreateMessage>(msg => loggedMessage = msg), Arg.Any<CancellationToken>());

            // Act
            var logEventInfo = new LogEventInfo(LogLevel.Warn, "", "Warning");
            logEventInfo.Properties.Add("Key", "Value");
            logEventInfo.Properties.Add("List", new[] { 1, 2, 3 });
            logger.Log(logEventInfo);
            for (int i = 0; i < 10; ++i)
            {
                if (loggedMessage != null)
                    break;
                Thread.Sleep(1000);
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
            messagesClientMock.CreateBulkAndNotifyAsync(Arg.Any<Guid>(), Arg.Do<IList<CreateMessage>>(msgs => loggedMessages = msgs), Arg.Any<CancellationToken>());
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
                Thread.Sleep(1000);
            }

            // Assert
            Assert.That(loggedMessages != null);
            Assert.That(loggedMessages.Count, Is.EqualTo(5));
            var loggedMessage = loggedMessages[0];
            Assert.That(loggedMessage, Is.Not.Null);
            Assert.That(loggedMessage.Severity, Is.EqualTo(Severity.Warning.ToString()));
            Assert.That(loggedMessage.Title, Does.Contain("Hello World"));
            Assert.That(loggedMessage.Data.Count, Is.EqualTo(1));
            Assert.That(loggedMessage.Data.First().Key, Is.EqualTo("ThreadId"));
            Assert.That(loggedMessage.Data.First().Value, Is.EqualTo(Environment.CurrentManagedThreadId.ToString()));
        }
    }
}
#pragma warning restore S2925 // "Thread.Sleep" should not be used in tests