using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace HyperEdit.Model
{
    public static class SiSuffix
    {
        private static readonly Dictionary<string, double> Suffixes = new Dictionary<string, double>
        {
            { "Y", 1e24 },
            { "Z", 1e21 },
            { "E", 1e18 },
            { "P", 1e15 },
            { "T", 1e12 },
            { "G", 1e9 },
            { "M", 1e6 },
            { "k", 1e3 },
            { "h", 1e2 },
            { "da", 1e1 },

            { "d", 1e-1 },
            { "c", 1e-2 },
            { "m", 1e-3 },
            { "u", 1e-6 },
            { "n", 1e-9 },
            { "p", 1e-12 },
            { "f", 1e-15 },
            { "a", 1e-18 },
            { "z", 1e-21 },
            { "y", 1e-24 }
        };

        public static bool TryParse(string s, out float value)
        {
            double dval;
            var success = TryParse(s, out dval);
            value = (float)dval;
            return success;
        }

        public static bool TryParse(string s, out double value)
        {
            s = s.Trim();
            double multiplier;
            var suffix = Suffixes.FirstOrDefault(suf => s.EndsWith(suf.Key, StringComparison.Ordinal));
            if (suffix.Key != null)
            {
                s = s.Substring(0, s.Length - suffix.Key.Length);
                multiplier = suffix.Value;
            }
            else
                multiplier = 1.0;
            if (double.TryParse(s, out value) == false)
                return false;
            value *= multiplier;
            return true;
        }

        public static bool TryParse(string s, out FloatCurve value)
        {
            value = new FloatCurve();
            try {
                JsonUtility.FromJsonOverwrite(s, value);
                return true;
            } catch (Exception e) {
                return false;
            }
        }

        public static bool TryParse(string s, out String value) {
            value = s;
            return true;
        }

        public static bool TryParseFloatCurve(string s, out string value) {
            value = s;
            FloatCurve floatCurve;
            return TryParse(s, out floatCurve);
        }

        /*
        // Not currently used.  Si suffixes are unnecessary and confusing.  Possibly useful with modification for clarity and practicality.
        public static string ToString(this double value)
        {
            var log = Math.Log10(Math.Abs(value));
            var minDiff = double.MaxValue;
            var minSuffix = new KeyValuePair<string, double>("", 1);
            foreach (var suffix in Suffixes.Concat(new[] { new KeyValuePair<string, double>("", 1) }))
            {
                var diff = Math.Abs(log - Math.Log10(suffix.Value));
                if (diff < minDiff)
                {
                    minDiff = diff;
                    minSuffix = suffix;
                }
            }
            value /= minSuffix.Value;
            return value.ToString("F") + minSuffix.Key;
        }
        */
    }
    
}
