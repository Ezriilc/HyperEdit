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

    public class ConditionalView : IView
    {
        private readonly Func<bool> doDisplay;
        private readonly IView toDisplay;

        public ConditionalView(Func<bool> doDisplay, IView toDisplay)
        {
            this.doDisplay = doDisplay;
            this.toDisplay = toDisplay;
        }

        public void Draw()
        {
            if (doDisplay())
                toDisplay.Draw();
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
        private readonly Action onChange;

        public ButtonView(string label, string help, Action onChange)
        {
            this.label = new GUIContent(label, help);
            this.onChange = onChange;
        }

        public void Draw()
        {
            if (GUILayout.Button(label))
            {
                onChange();
                Extensions.ClearGuiFocus();
            }
        }
    }

    public class ToggleView : IView
    {
        private readonly GUIContent label;
        private readonly Action<bool> onChange;

        public bool Value { get; set; }

        public ToggleView(string label, string help, bool initialValue, Action<bool> onChange = null)
        {
            this.label = new GUIContent(label, help);
            this.Value = initialValue;
            this.onChange = onChange;
        }

        public void Draw()
        {
            var oldValue = Value;
            Value = GUILayout.Toggle(oldValue, label);
            if (oldValue != Value && onChange != null)
            {
                onChange(Value);
                Extensions.ClearGuiFocus();
            }
        }
    }

    public class DynamicToggleView : IView
    {
        private readonly GUIContent label;
        private readonly Func<bool> getValue;
        private readonly Func<bool> isValid;
        private readonly Action<bool> onChange;

        public DynamicToggleView(string label, string help, Func<bool> getValue, Func<bool> isValid, Action<bool> onChange)
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
            {
                onChange(newValue);
                Extensions.ClearGuiFocus();
            }
        }
    }

    public class DynamicSliderView : IView
    {
        private readonly Action<double> onChange;
        private readonly GUIContent label;
        private readonly Func<double> get;

        public DynamicSliderView(string label, string help, Func<double> get, Action<double> onChange)
        {
            this.onChange = onChange;
            this.label = new GUIContent(label, help);
            this.get = get;
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            var oldValue = get();
            var newValue = (double)GUILayout.HorizontalSlider((float)oldValue, 0, 1);
            if (Math.Abs(newValue - oldValue) > 0.001)
            {
                if (onChange != null)
                {
                    onChange(newValue);
                }
                Extensions.ClearGuiFocus();
            }
            GUILayout.EndHorizontal();
        }
    }

    public class SliderView : IView
    {
        private readonly Action<double> onChange;
        private readonly GUIContent label;

        public double Value { get; set; }

        public SliderView(string label, string help, Action<double> onChange = null)
        {
            this.onChange = onChange;
            this.label = new GUIContent(label, help);
            Value = 0;
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label);
            var newValue = (double)GUILayout.HorizontalSlider((float)Value, 0, 1);
            if (Math.Abs(newValue - Value) > 0.001)
            {
                Value = newValue;
                if (onChange != null)
                {
                    onChange(Value);
                }
                Extensions.ClearGuiFocus();
            }
            GUILayout.EndHorizontal();
        }
    }

    public class ListSelectView<T> : IView
    {
        private readonly string prefix;
        private readonly Func<IEnumerable<T>> list;
        private readonly Func<T, string> toString;
        private readonly Action<T> onSelect;
        private T currentlySelected;

        public T CurrentlySelected
        {
            get { return currentlySelected; }
            set
            {
                currentlySelected = value;
                if (onSelect != null)
                    onSelect(value);
            }
        }

        public void ReInvokeOnSelect()
        {
            if (onSelect != null)
                onSelect(currentlySelected);
        }

        public ListSelectView(string prefix, Func<IEnumerable<T>> list, Action<T> onSelect = null, Func<T, string> toString = null)
        {
            this.prefix = prefix + ": ";
            this.list = list;
            this.toString = toString ?? (x => x.ToString());
            this.onSelect = onSelect;
            this.currentlySelected = default(T);
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(prefix + (currentlySelected == null ? "<none>" : toString(currentlySelected)));
            if (GUILayout.Button("Select"))
            {
                Extensions.ClearGuiFocus();
                var realList = list();
                if (realList != null)
                    WindowHelper.Selector("Select", realList, toString, t => CurrentlySelected = t);
            }
            GUILayout.EndHorizontal();
        }
    }

    public class TextBoxView<T> : IView
    {
        private readonly GUIContent label;
        private readonly TryParse<T> parser;
        private readonly Func<T, string> toString;
        private readonly Action<T> onSet;
        private string value;
        private T obj;

        public bool Valid { get; private set; }

        public T Object
        {
            get { return obj; }
            set
            {
                this.value = toString(value);
                obj = value;
            }
        }

        public TextBoxView(string label, string help, T start, TryParse<T> parser, Func<T, string> toString = null, Action<T> onSet = null)
        {
            this.label = label == null ? null : new GUIContent(label, help);
            this.toString = toString ?? (x => x.ToString());
            value = this.toString(start);
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
                obj = tempValue;
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
                {
                    onSet(Object);
                    Extensions.ClearGuiFocus();
                }
                GUILayout.EndHorizontal();
            }
        }
    }

    public class TabView : IView
    {
        private readonly List<KeyValuePair<string, IView>> views;
        private KeyValuePair<string, IView> current;

        public TabView(List<KeyValuePair<string, IView>> views)
        {
            this.views = views;
            this.current = views[0];
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            foreach (var view in views)
            {
                if (view.Key == current.Key)
                {
                    GUILayout.Button(view.Key, Extensions.PressedButton);
                }
                else
                {
                    if (GUILayout.Button(view.Key))
                    {
                        current = view;
                        Extensions.ClearGuiFocus();
                    }
                }
            }
            GUILayout.EndHorizontal();
            current.Value.Draw();
        }
    }
}
