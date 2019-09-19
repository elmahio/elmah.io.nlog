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
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Linq;

namespace Elmah.Io.NLog
{
    [Target("elmah.io")]
    public class ElmahIoTarget : AsyncTaskTarget
    {
#if DOTNETCORE
        internal static string _assemblyVersion = typeof(ElmahIoTarget).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
#else
        internal static string _assemblyVersion = typeof(ElmahIoTarget).Assembly.GetName().Version.ToString();
#endif

        private IElmahioAPI _client;
        private readonly string DefaultLayout;
        private bool _usingDefaultLayout;
        private Guid _logId;
        private string _apiKey;

        [RequiredParameter]
        public string ApiKey
        {
            get
            {
                return _apiKey;
            }
            set
            {
                var apiKey = RenderLogEvent(value, LogEventInfo.CreateNullEvent());
                _apiKey = apiKey;
            }
        }

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
                var logId = RenderLogEvent(value, LogEventInfo.CreateNullEvent());
                _logId = Guid.Parse(logId);
            }
        }

        public Action<CreateMessage> OnMessage { get; set; }

        public Action<CreateMessage, Exception> OnError { get; set; }

        public Func<CreateMessage, bool> OnFilter { get; set; }

        public string Application { get => (ApplicationLayout as SimpleLayout)?.Text; set => ApplicationLayout = value; }

        public IWebProxy WebProxy { get; set; }

        public Layout HostnameLayout { get; set; } = "${event-properties:hostname:whenEmpty=${event-properties:Hostname:whenEmpty=${event-properties:HostName:whenEmpty=${aspnet-request-host:whenEmpty=${machinename}}}}}";

        public Layout CookieLayout { get; set; } = "${event-properties:cookies:whenEmpty=${event-properties:Cookies:whenEmpty=${aspnet-request-cookie:outputFormat=Json}}}";

        public Layout FormLayout { get; set; } = "${event-properties:form:whenEmpty=${event-properties:Form:whenEmpty=${aspnet-request-form:outputFormat=Json}}}";

        public Layout QueryStringLayout { get; set; } = "${event-properties:querystring:whenEmpty=${event-properties:queryString:whenEmpty=${event-properties:QueryString:whenEmpty=${aspnet-request-querystring:outputFormat=Json}}}}";

        public Layout HeadersLayout { get; set; } = "${event-properties:servervariables:whenEmpty=${event-properties:serverVariables:whenEmpty=${event-properties:ServerVariables:whenEmpty=${aspnet-request-headers:outputFormat=Json}}}}";

        public Layout SourceLayout { get; set; } = "${event-properties:source:whenEmpty=${event-properties:Source:whenEmpty=${logger}}}";

        public Layout ApplicationLayout { get; set; } = "${event-properties:application:whenEmpty=${event-properties:Application}}";

#if NET45
        public Layout UserLayout { get; set; } = "${event-properties:user:whenEmpty=${event-properties:User:whenEmpty=${aspnet-user-identity:whenEmpty=${identity:authType=false:isAuthenticated=false}}}}";
#else
        public Layout UserLayout { get; set; } = "${event-properties:user:whenEmpty=${event-properties:User:whenEmpty=${aspnet-user-identity}}}";
#endif

        public Layout MethodLayout { get; set; } = "${event-properties:method:whenEmpty=${event-properties:Method:whenEmpty=${aspnet-request-method}}}";

        public Layout VersionLayout { get; set; } = "${event-properties:version:whenEmpty=${event-properties:Version}}";

        public Layout UrlLayout { get; set; } = "${event-properties:url:whenEmpty=${event-properties:Url:whenEmpty=${event-properties:URL:whenEmpty=${aspnet-request-url}}}}";

        public Layout TypeLayout { get; set; } = "${event-properties:type:whenEmpty=${event-properties:Type}}";

        public Layout StatusCodeLayout { get; set; } = "${event-properties:statuscode:whenEmpty=${event-properties:Statuscode:whenEmpty=${event-properties:statusCode:whenEmpty=${event-properties:StatusCode}}}}";

        public ElmahIoTarget()
        {
            DefaultLayout = Layout?.ToString();
            IncludeEventProperties = true;
            TaskDelayMilliseconds = 250;// Delay to optimize for bulk send (reduce http requests)
            TaskTimeoutSeconds = 150;   // Long timeout to allow http request to complete before starting next task
            RetryCount = 0;             // Skip retry on error / timeout
            BatchSize = 50;             // Avoid too many messages in a single batch (reduce request size)
        }

        public ElmahIoTarget(IElmahioAPI client) : this()
        {
            _client = client;
        }

        protected override void InitializeTarget()
        {
            _usingDefaultLayout = Layout == null || Layout.ToString() == DefaultLayout;
            base.InitializeTarget();
        }

        protected override Task WriteAsyncTask(LogEventInfo logEvent, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();    // Never reached, because of override of IList-handler
        }

        protected override Task WriteAsyncTask(IList<LogEventInfo> logEvents, CancellationToken cancellationToken)
        {
            if (_client == null)
            {
                ElmahioAPI api = new ElmahioAPI(new ApiKeyCredentials(ApiKey), HttpClientHandlerFactory.GetHttpClientHandler(new ElmahIoOptions
                {
                    WebProxy = WebProxy
                }));
                api.HttpClient.Timeout = new TimeSpan(0, 0, 5);
                api.HttpClient.DefaultRequestHeaders.UserAgent.Clear();
                api.HttpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(new ProductHeaderValue("Elmah.Io.NLog", _assemblyVersion)));
                api.Messages.OnMessage += (sender, args) =>
                {
                    OnMessage?.Invoke(args.Message);
                };
                api.Messages.OnMessageFail += (sender, args) =>
                {
                    OnError?.Invoke(args.Message, args.Error);
                };
                _client = api;
            }

            IList<CreateMessage> messages = null;
            for (int i = 0; i < logEvents.Count; ++i)
            {
                var logEvent = logEvents[i];
                var title = _usingDefaultLayout ? logEvent.FormattedMessage : Layout.Render(logEvent);

                var message = new CreateMessage
                {
                    Title = title,
                    TitleTemplate = logEvent.Message ?? title,
                    Severity = LevelToSeverity(logEvent.Level),
                    DateTime = logEvent.TimeStamp.ToUniversalTime(),
                    Detail = logEvent.Exception?.ToString(),
                    Data = PropertiesToData(logEvent),
                    Source = Source(logEvent),
                    Hostname = RenderLogEvent(HostnameLayout, logEvent),
                    Application = RenderLogEvent(ApplicationLayout, logEvent),
                    User = RenderLogEvent(UserLayout, logEvent),
                    Method = RenderLogEvent(MethodLayout, logEvent),
                    Version = RenderLogEvent(VersionLayout, logEvent),
                    Url = Url(logEvent),
                    Type = Type(logEvent),
                    StatusCode = StatusCode(logEvent),
                    ServerVariables = RenderItems(logEvent, HeadersLayout),
                    Cookies = RenderItems(logEvent, CookieLayout),
                    Form = RenderItems(logEvent, FormLayout),
                    QueryString = RenderItems(logEvent, QueryStringLayout),
                };

                if (OnFilter != null && OnFilter(message))
                {
                    continue;
                }

                if (logEvents.Count == 1)
                {
                    return _client.Messages.CreateAndNotifyAsync(_logId, message);
                }

                messages = messages ?? new List<CreateMessage>(logEvents.Count);
                messages.Add(message);
            }

            if (messages?.Count > 0)
            {
                return _client.Messages.CreateBulkAndNotifyAsync(_logId, messages);
            }

            return Task.FromResult<Message>(null);
        }

        private string Url(LogEventInfo logEvent)
        {
            var url = RenderLogEvent(UrlLayout, logEvent);
            if (string.IsNullOrWhiteSpace(url)) return null;
            if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri result)) return null;
            if (result.IsAbsoluteUri) return result.AbsolutePath;
            return result.OriginalString;
        }

        private string Type(LogEventInfo logEvent)
        {
            var type = RenderLogEvent(TypeLayout, logEvent);
            if (!string.IsNullOrWhiteSpace(type)) return type;
            if (logEvent.Exception != null) return logEvent.Exception.GetBaseException().GetType().FullName;
            return null;
        }

        private string Source(LogEventInfo logEvent)
        {
            var source = RenderLogEvent(SourceLayout, logEvent);
            if (!string.IsNullOrWhiteSpace(source)) return source;
            if (logEvent.Exception == null) return logEvent.LoggerName;
            return logEvent.Exception.GetBaseException().Source;
        }

        private IList<Item> RenderItems(LogEventInfo logEvent, Layout layout)
        {
            var rendered = RenderLogEvent(layout, logEvent);
            if (string.IsNullOrWhiteSpace(rendered)) return null;
            var items = new List<Item>();
            if (rendered.StartsWith("[{") && rendered.EndsWith("}]")) // JSON rendered using a NLog ASP.NET layout renderer
            {
                var renderedJson = JsonConvert.DeserializeObject<JArray>(rendered);
                foreach (JObject item in renderedJson)
                {
                    foreach (var property in item)
                    {
                        items.Add(new Item(property.Key, property.Value?.ToString()));
                    }
                }
            }
            else // User sended something with the right name as part of structured logging or custom properties
            {
                foreach (var keyAndValue in rendered.Split(new[] { "\", \"" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var keyAndValueSplit = keyAndValue.Split(new[] { "\"=\"" }, StringSplitOptions.None);
                    if (keyAndValueSplit.Length <= 0) continue;
                    var key = keyAndValueSplit[0]?.TrimStart(new[] { '\"' }).TrimEnd(new[] { '\"' });
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    string value = null;
                    if (keyAndValueSplit.Length > 1) value = keyAndValueSplit[1].TrimStart(new[] { '\"' }).TrimEnd(new[] { '\"' });
                    items.Add(new Item(key, value));
                }
            }

            return items;
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
            if (!ShouldIncludeProperties(logEvent) && ContextProperties.Count == 0) return null;

            var properties = GetAllProperties(logEvent);

            var items = new List<Item>();
            StringBuilder sb = new StringBuilder();
            var valueFormatter = ConfigurationItemFactory.Default.ValueFormatter;
            foreach (var obj in properties)
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
                    items.Add(new Item { Key = obj.Key, Value = text });
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
