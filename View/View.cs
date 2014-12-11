using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyperEdit.View
{
    public abstract class View
    {
        private Dictionary<string, string> _textboxInputs = new Dictionary<string, string>();

        public delegate bool TryParse<T>(string str, out T value);

        private bool _allValid = true;

        protected bool AllValid { get { return _allValid; } }

        protected T GuiTextField<T>(string key, GUIContent display, TryParse<T> parser, T value, Func<T, string> toString = null)
        {
            if (display != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(display);
            }
            if (_textboxInputs.ContainsKey(key) == false)
                _textboxInputs[key] = toString == null ? value.ToString() : toString(value);

            T tempValue;
            var isValid = parser(_textboxInputs[key], out tempValue);
            if (!isValid)
                _allValid = false;

            if (isValid)
            {
                _textboxInputs[key] = GUILayout.TextField(_textboxInputs[key]);
            }
            else
            {
                var color = GUI.color;
                GUI.color = Color.red;
                _textboxInputs[key] = GUILayout.TextField(_textboxInputs[key]);
                GUI.color = color;
            }
            if (display != null)
            {
                GUILayout.EndHorizontal();
            }
            return isValid ? tempValue : value;
        }

        protected T? GuiTextFieldSettable<T>(string key, GUIContent display, TryParse<T> parser, T value, Func<T, string> toString = null) where T : struct
        {
            GUILayout.BeginHorizontal();
            if (display != null)
                GUILayout.Label(display);
            value = GuiTextField(key, null, parser, value, toString);
            var set = GUILayout.Button("Set", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            return set ? value : (T?)null;
        }

        protected void ClearTextFields()
        {
            _textboxInputs.Clear();
        }

        public virtual void Draw(Window window)
        {
            _allValid = true;
        }

        public static void CreateView(object model)
        {
            var lander = model as Model.Lander;
            var misc = model as Model.MiscEditor;
            var orbit = model as Model.OrbitEditor;
            var planet = model as Model.PlanetEditor;
            var sma = model as Model.SmaAligner;
            if (lander != null) LanderView.Create(lander);
            if (misc != null) MiscEditorView.Create(misc);
            if (orbit != null) OrbitEditorView.Create(orbit);
            if (planet != null) PlanetEditorView.Create(planet);
            if (sma != null) SmaAlignerView.Create(sma);
        }
    }
}
