using NLog;
using System;
using System.Linq;

namespace Elmah.Io.NLog
{
    internal static class LogEventInfoExtensions
    {
        internal static int? Integer(this LogEventInfo logEvent, string name)
        {
            if (logEvent.Properties == null || !logEvent.Properties.Any()) return null;
            if (!logEvent.Properties.Keys.Any(key => name.Equals(key.ToString(), StringComparison.OrdinalIgnoreCase))) return null;

            var property = logEvent.Properties.First(prop => name.Equals(prop.Key.ToString(), StringComparison.OrdinalIgnoreCase));
            var value = property.Value;
            if (value == null) return null;

            if (!int.TryParse(value.ToString(), out int result)) return null;
            return result;
        }

        internal static string String(this LogEventInfo logEvent, string name)
        {
            if (logEvent.Properties == null || !logEvent.Properties.Any()) return null;
            if (!logEvent.Properties.Keys.Any(key => name.Equals(key.ToString(), StringComparison.OrdinalIgnoreCase))) return null;

            var property = logEvent.Properties.First(prop => name.Equals(prop.Key.ToString(), StringComparison.OrdinalIgnoreCase));
            var value = property.Value;
            if (value == null) return null;
            return value.ToString();
        }
    }
}
