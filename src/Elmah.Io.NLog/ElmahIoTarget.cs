using System;
using System.Collections.Generic;
using Elmah.Io.Client;
using Elmah.Io.Client.Models;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Layouts;
using System.Text;
using NLog.MessageTemplates;

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

        public string Application { get => (ApplicationLayout as SimpleLayout)?.Text; set => ApplicationLayout = value; }

        public Layout HostnameLayout { get; set; } = "${event-properties:hostname:whenEmpty=${event-properties:Hostname:whenEmpty=${event-properties:HostName:whenEmpty=${machinename}}}}";

        public Layout SourceLayout { get; set; } = "${event-properties:source:whenEmpty=${event-properties:Source:whenEmpty=${logger}}}";

        public Layout ApplicationLayout { get; set; } = "${event-properties:application:whenEmpty=${event-properties:Application}}";

#if NET45
        public Layout UserLayout { get; set; } = "${event-properties:user:whenEmpty=${event-properties:User:whenEmpty=${identity:authType=false:isAuthenticated=false}}}";
#else
        public Layout UserLayout { get; set; } = "${event-properties:user:whenEmpty=${event-properties:User}}";
#endif

        public Layout MethodLayout { get; set; } = "${event-properties:method:whenEmpty=${event-properties:Method}}";

        public Layout VersionLayout { get; set; } = "${event-properties:version:whenEmpty=${event-properties:Version}}";

        public Layout UrlLayout { get; set; } = "${event-properties:url:whenEmpty=${event-properties:Url:whenEmpty=${event-properties:URL}}}";

        public Layout TypeLayout { get; set; } = "${event-properties:type:whenEmpty=${event-properties:Type}}";

        public Layout StatusCodeLayout { get; set; } = "${event-properties:statuscode:whenEmpty=${event-properties:Statuscode:whenEmpty=${event-properties:statusCode:whenEmpty=${event-properties:StatusCode}}}}";

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
            _usingDefaultLayout = Layout == null || Layout.ToString() == DefaultLayout;
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
                Source = RenderLogEvent(SourceLayout, logEvent),
                Hostname = RenderLogEvent(HostnameLayout, logEvent),
                Application = RenderLogEvent(ApplicationLayout, logEvent),
                User = RenderLogEvent(UserLayout, logEvent),
                // Resolve the rest from structured logging
                Method = RenderLogEvent(MethodLayout, logEvent),
                Version = RenderLogEvent(VersionLayout, logEvent),
                Url = RenderLogEvent(UrlLayout, logEvent),
                Type = RenderLogEvent(TypeLayout, logEvent),
                StatusCode = StatusCode(logEvent),
            };

            _client.Messages.CreateAndNotify(_logId, message);
        }

        private int? StatusCode(LogEventInfo logEvent)
        {
            var statusCode = RenderLogEvent(StatusCodeLayout, logEvent);
            if (string.IsNullOrWhiteSpace(statusCode)) return null;
            if (!int.TryParse(statusCode, out int result)) return null;
            return result;
        }

        private List<Item> PropertiesToData(LogEventInfo logEvent)
        {
            if (!logEvent.HasProperties) return null;

            var items = new List<Item>();
            StringBuilder sb = new StringBuilder();
            var valueFormatter = ConfigurationItemFactory.Default.ValueFormatter;
            foreach (var obj in logEvent.Properties)
            {
                if (obj.Value != null)
                {
                    string text;
                    if (obj.Value is string)
                    {
                        text = (string)obj.Value;
                    }
                    else
                    {
                        sb.Length = 0;  // Reuse StringBuilder
                        valueFormatter.FormatValue(obj.Value, null, CaptureType.Normal, null, sb);
                        text = sb.ToString();
                    }
                    items.Add(new Item { Key = obj.Key.ToString(), Value = text });
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
