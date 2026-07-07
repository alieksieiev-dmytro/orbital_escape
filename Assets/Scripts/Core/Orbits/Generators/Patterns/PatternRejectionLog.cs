using System.Collections.Generic;
using System.Linq;

namespace OrbitalEscape.Orbits.Generators.Patterns
{
    internal sealed class PatternRejectionLog
    {
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();
        private readonly List<string> _orderedReasons = new List<string>();

        public string Summary
        {
            get
            {
                if (_orderedReasons.Count == 0)
                {
                    return "none";
                }

                return string.Join("; ", _orderedReasons.Select(FormatReason));
            }
        }

        public void Add(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return;
            }

            if (!_counts.ContainsKey(reason))
            {
                _counts.Add(reason, 0);
                _orderedReasons.Add(reason);
            }

            _counts[reason]++;
        }

        private string FormatReason(string reason)
        {
            return $"{reason} x{_counts[reason]}";
        }
    }
}
