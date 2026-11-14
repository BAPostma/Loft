using System.Collections.Generic;

namespace Loft.Function.Maintenance
{
    /// <summary>
    /// Parses a control command message body: the first line names the command,
    /// each following line is a key=value parameter.
    ///
    ///   reprocess-inbox
    ///   bucket=loft-example.com
    /// </summary>
    public class ControlCommand
    {
        public string Name { get; private set; }
        public IDictionary<string, string> Parameters { get; private set; }

        private ControlCommand() { }

        public string this[string key]
        {
            get
            {
                if (string.IsNullOrWhiteSpace(key)) return null;
                return Parameters.TryGetValue(key.ToLowerInvariant(), out var value) ? value : null;
            }
        }

        public static ControlCommand ParseFrom(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return new ControlCommand { 
                Name = string.Empty,
                Parameters = new Dictionary<string, string>() 
            };

            var lines = body.Split('\n');
            var name = string.Empty;
            var parameters = new Dictionary<string, string>();
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = line.Trim();
                    continue;
                }

                var separator = line.IndexOf('=');
                if (separator < 0) continue;

                var key = line[..separator].Trim().ToLowerInvariant();
                var value = line[(separator + 1)..].Trim();
                parameters[key] = value;
            }

            return new ControlCommand { Name = name, Parameters = parameters };
        }
    }
}
