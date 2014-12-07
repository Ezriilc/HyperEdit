//
// This file is part of the HyperEdit plugin for Kerbal Space Program, Copyright Erickson Swift, 2013.
// HyperEdit is licensed under the GPL, found in COPYING.txt.
// Currently supported by Team HyperEdit, and Ezriilc.
// Original HyperEdit concept and code by khyperia (no longer involved).
//
// Thanks to Payo for inventing, writing and contributing the PlanetEditor component.
//

using System;
using System.ComponentModel;

namespace HyperEdit
{
    public static class FieldProxy
    {
        public static IWindowContent Create<T>(string fieldName, Func<T> get, Action<T> set)
        {
            if (typeof(T) == typeof(bool))
                return new Toggle(fieldName, (bool)(object)get(), b => set((T)(object)b));
            return new TextBox(fieldName, get().ToString(), s => TrySet(s, set));
        }

        private delegate bool TryParse<T>(string str, out T value);

        private static void TrySet<T>(string value, Action<T> set)
        {
            if (typeof(T) == typeof(UnityEngine.Color))
            {
                SetColor(value, c => set((T)(object)c));
                return;
            }
            if (typeof(T) == typeof(string))
            {
                set((T)(object)value);
                return;
            }
            if (typeof(T) == typeof(double) || typeof(T) == typeof(float))
            {
                double number;
                if (SiSuffix.TryParse(value, out number) == false)
                {
                    ErrorPopup.Error("\"" + value + "\" was not in the correct format");
                    return;
                }
                if (typeof(T) == typeof(float))
                    set((T)(object)(float)number);
                else
                    set((T)(object)number);
            }

            ErrorPopup.Error("Internal error: Type " + typeof(T).Name + " cannot be parsed");
        }

        private static string TrimUnityColor(string value)
        {
            value = value.Trim();
            if (value.StartsWith("RGBA"))
                value = value.Substring(4).Trim();
            value = value.Trim('(', ')');
            return value;
        }

        private static void SetColor(string value, Action<UnityEngine.Color> set)
        {
            string parseValue = TrimUnityColor(value);
            if (parseValue == null)
            {
                ErrorPopup.Error("\"" + value + "\" was not in the correct format");
                return;
            }
            UnityEngine.Color color = new UnityEngine.Color();
            string[] values = parseValue.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length == 3 || values.Length == 4)
            {
                if (!float.TryParse(values[0], out color.r) ||
                    !float.TryParse(values[1], out color.g) ||
                    !float.TryParse(values[2], out color.b))
                {
                    ErrorPopup.Error("\"" + value + "\" was not in the correct format");
                    return;
                }
                if (values.Length == 3 && !float.TryParse(values[3], out color.a))
                {
                    ErrorPopup.Error("\"" + value + "\" was not in the correct format");
                    return;
                }
                set(color);
                return;
            }
            ErrorPopup.Error("\"" + value + "\" was not in the correct format");
        }
    }
}