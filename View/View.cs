using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyperEdit.View
{
    public interface IView
    {
        void Draw();
    }

    public class CustomView : IView
    {
        private readonly Action draw;

        public CustomView(Action draw)
        {
            this.draw = draw;
        }

        public void Draw()
        {
            draw();
        }
    }

    public class LabelView : IView
    {
        private readonly GUIContent label;

        public LabelView(string label, string help)
        {
            this.label = new GUIContent(label, help);
        }

        public void Draw()
        {
            GUILayout.Label(label);
        }
    }

    public class VerticalView : IView
    {
        private readonly ICollection<IView> views;

        public VerticalView(ICollection<IView> views)
        {
            this.views = views;
        }

        public void Draw()
        {
            GUILayout.BeginVertical();
            foreach (var view in views)
            {
                view.Draw();
            }
            GUILayout.EndVertical();
        }
    }

    public class ButtonView : IView
    {
        private readonly GUIContent label;
        private readonly Func<bool> isValid;
        private readonly Action onChange;

        public ButtonView(string label, string help, Func<bool> isValid, Action onChange)
        {
            this.label = new GUIContent(label, help);
            this.isValid = isValid;
            this.onChange = onChange;
        }

        public void Draw()
        {
            var valid = isValid();
            if (valid)
            {
                if (GUILayout.Button(label))
                {
                    onChange();
                }
            }
            else
            {
                GUILayout.Button(label);
            }
        }
    }

    public class ToggleView : IView
    {
        private readonly GUIContent label;
        private readonly Func<bool> getValue;
        private readonly Func<bool> isValid;
        private readonly Action<bool> onChange;

        public ToggleView(string label, string help, Func<bool> getValue, Func<bool> isValid, Action<bool> onChange)
        {
            this.label = new GUIContent(label, help);
            this.getValue = getValue;
            this.isValid = isValid;
            this.onChange = onChange;
        }

        public void Draw()
        {
            var oldValue = getValue();
            var newValue = GUILayout.Toggle(oldValue, label);
            if (oldValue != newValue && isValid())
                onChange(newValue);
        }
    }

    public class TextBoxView<T> : IView
    {
        private readonly GUIContent label;
        private readonly View.TryParse<T> parser;
        private readonly Action<T> onSet;
        private string value;
        private T obj;

        public bool Valid { get; private set; }

        public T Object
        {
            get { return obj; }
            set
            {
                this.value = value.ToString();
                obj = value;
            }
        }

        public TextBoxView(string label, string help, string start, View.TryParse<T> parser, Action<T> onSet = null)
        {
            this.label = label == null ? null : new GUIContent(label, help);
            value = start;
            this.parser = parser;
            this.onSet = onSet;
        }

        public void Draw()
        {
            if (label != null || onSet != null)
            {
                GUILayout.BeginHorizontal();
                if (label != null)
                    GUILayout.Label(label);
            }

            T tempValue;
            Valid = parser(value, out tempValue);

            if (Valid)
            {
                value = GUILayout.TextField(value);
                Object = tempValue;
            }
            else
            {
                var color = GUI.color;
                GUI.color = Color.red;
                value = GUILayout.TextField(value);
                GUI.color = color;
            }
            if (label != null || onSet != null)
            {
                if (onSet != null && Valid && GUILayout.Button("Set"))
                    onSet(Object);
                GUILayout.EndHorizontal();
            }
        }
    }

    public abstract class View
    {
        private Dictionary<string, string> _textboxInputs = new Dictionary<string, string>();

        public delegate bool TryParse<T>(string str,out T value);

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

        protected float Slider(GUIContent display, float oldval, Model.SliderRange range, ref bool changed)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(display);
            var newval = GUILayout.HorizontalSlider(oldval, range.Min, range.Max);
            GUILayout.EndHorizontal();
            if (changed == false)
                changed = newval != oldval;
            return newval;
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
            var orbit = model as Model.OrbitEditor;
            var planet = model as Model.PlanetEditor;
            var sma = model as Model.SmaAligner;
            if (orbit != null)
                OrbitEditorView.Create(orbit);
            if (planet != null)
                PlanetEditorView.Create(planet);
            if (sma != null)
                SmaAlignerView.Create(sma);
        }
    }
}
