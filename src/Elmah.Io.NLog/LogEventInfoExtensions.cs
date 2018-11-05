using NLog;
using System.Linq;

namespace Elmah.Io.NLog
{
    internal static class LogEventInfoExtensions
    {
        internal static int? Integer(this LogEventInfo logEvent, string name)
        {
            if (logEvent.Properties == null || !logEvent.Properties.Any()) return null;
            if (!logEvent.Properties.Keys.Any(key => key.ToString().ToLower().Equals(name.ToLower()))) return null;

            var property = logEvent.Properties.First(prop => prop.Key.ToString().ToLower().Equals(name));
            var value = property.Value;
            if (value == null) return null;

            if (!int.TryParse(value.ToString(), out int result)) return null;
            return result;
        }

        internal static string String(this LogEventInfo logEvent, string name)
        {
            if (logEvent.Properties == null || !logEvent.Properties.Any()) return null;
            if (!logEvent.Properties.Keys.Any(key => key.ToString().ToLower().Equals(name.ToLower()))) return null;

            var property = logEvent.Properties.First(prop => prop.Key.ToString().ToLower().Equals(name));
            var value = property.Value;
            if (value == null) return null;
            return value.ToString();
        }
    }
}
