using System;
using System.Collections.Generic;
using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.LayoutRenderers;

namespace Elmah.Io.NLog
{
    [Target("elmah.io")]
    public class ElmahIoTarget : TargetWithLayout
    {
        private IElmahioAPI _client;
        private readonly string DefaultLayout;
        private bool _usingDefaultLayout;
        private Guid _logId;

        [RequiredParameter]
        public string ApiKey { get; set; }

        // Needs to be a string and not a guid, in order for .NET core to work
        [RequiredParameter]
        public string LogId
        {
            get
            {
                return _logId != Guid.Empty ? _logId.ToString() : null;
            }
            set
            {
                _logId = Guid.Parse(value);
            }
        }

        public string Application { get; set; }

        private MachineNameLayoutRenderer _machineNameLayoutRenderer;

#if NET45
        private IdentityLayoutRenderer _identityLayoutRenderer;
#endif

        public ElmahIoTarget()
        {
            OptimizeBufferReuse = true;
            DefaultLayout = Layout?.ToString();
        }

        public ElmahIoTarget(IElmahioAPI client) : this()
        {
            _client = client;
        }

        protected override void InitializeTarget()
        {
            _usingDefaultLayout = Layout?.ToString() == DefaultLayout;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            if (_client == null)
            {
                _client = ElmahioAPI.Create(ApiKey);
            }

            var title = _usingDefaultLayout ? logEvent.FormattedMessage : Layout.Render(logEvent);

            var message = new CreateMessage
            {
                Title = title,
                Severity = LevelToSeverity(logEvent.Level),
                DateTime = logEvent.TimeStamp.ToUniversalTime(),
                Detail = logEvent.Exception?.ToString(),
                Data = PropertiesToData(logEvent),
                Source = Source(logEvent),
                Hostname = Hostname(logEvent),
                Application = ApplicationName(logEvent),
                User = User(logEvent),
                // Resolve the rest from structured logging
                Method = logEvent.String("method"),
                Version = logEvent.String("version"),
                Url = logEvent.String("url"),
                Type = logEvent.String("type"),
                StatusCode = logEvent.Integer("statuscode"),
            };

            _client.Messages.CreateAndNotify(_logId, message);
        }

        private string ApplicationName(LogEventInfo logEvent)
        {
            var application = logEvent.String("application");
            if (!string.IsNullOrWhiteSpace(application)) return application;
            return Application;
        }

        private string Source(LogEventInfo logEvent)
        {
            var source = logEvent.String("source");
            if (!string.IsNullOrWhiteSpace(source)) return source;
            return logEvent.LoggerName;
        }

        private string Hostname(LogEventInfo logEvent)
        {
            var hostname = logEvent.String("hostname");
            if (!string.IsNullOrWhiteSpace(hostname)) return hostname;
            if (_machineNameLayoutRenderer == null)
            {
                _machineNameLayoutRenderer = new MachineNameLayoutRenderer();
            }
            return _machineNameLayoutRenderer.Render(logEvent);
        }

        private string User(LogEventInfo logEvent)
        {
            var user = logEvent.String("user");
            if (!string.IsNullOrWhiteSpace(user)) return user;
#if NET45
            if (_identityLayoutRenderer == null)
            {
                _identityLayoutRenderer = new IdentityLayoutRenderer
                {
                    Name = true,
                    AuthType = false,
                    IsAuthenticated = false
                };
            }
            user = _identityLayoutRenderer.Render(logEvent);
            return string.IsNullOrWhiteSpace(user) ? null : user;
#else
            return null;
#endif
        }

        private List<Item> PropertiesToData(LogEventInfo logEvent)
        {
            if (!logEvent.HasProperties) return null;

            var items = new List<Item>();
            foreach (var obj in logEvent.Properties)
            {
                if (obj.Value != null)
                {
                    items.Add(new Item { Key = obj.Key.ToString(), Value = obj.Value.ToString() });
                }
            }
            return items;
        }

        private string LevelToSeverity(LogLevel level)
        {
            if (level == LogLevel.Debug) return nameof(Severity.Debug);
            if (level == LogLevel.Error) return nameof(Severity.Error);
            if (level == LogLevel.Fatal) return nameof(Severity.Fatal);
            if (level == LogLevel.Trace) return nameof(Severity.Verbose);
            if (level == LogLevel.Warn) return nameof(Severity.Warning);
            return nameof(Severity.Information);
        }
    }
}
