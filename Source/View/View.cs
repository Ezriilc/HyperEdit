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
        private readonly Action _draw;

        public CustomView(Action draw)
        {
            _draw = draw;
        }

        public void Draw()
        {
            _draw();
        }
    }

    public class ConditionalView : IView
    {
        private readonly Func<bool> _doDisplay;
        private readonly IView _toDisplay;

        public ConditionalView(Func<bool> doDisplay, IView toDisplay)
        {
            _doDisplay = doDisplay;
            _toDisplay = toDisplay;
        }

        public void Draw()
        {
            if (_doDisplay())
                _toDisplay.Draw();
        }
    }

    public class LabelView : IView
    {
        private readonly GUIContent _label;

        public LabelView(string label, string help)
        {
            _label = new GUIContent(label, help);
        }

        public void Draw()
        {
            GUILayout.Label(_label);
        }
    }

    public class VerticalView : IView
    {
        private readonly ICollection<IView> _views;

        public VerticalView(ICollection<IView> views)
        {
            _views = views;
        }

        public void Draw()
        {
            GUILayout.BeginVertical();
            foreach (var view in _views)
            {
                view.Draw();
            }
            GUILayout.EndVertical();
        }
    }

    public class ButtonView : IView
    {
        private readonly GUIContent _label;
        private readonly Action _onChange;

        public ButtonView(string label, string help, Action onChange)
        {
            _label = new GUIContent(label, help);
            _onChange = onChange;
        }

        public void Draw()
        {
            if (GUILayout.Button(_label))
            {
                _onChange();
                Extensions.ClearGuiFocus();
            }
        }
    }

    public class ToggleView : IView
    {
        private readonly GUIContent _label;
        private readonly Action<bool> _onChange;

        public bool Value { get; set; }

        public ToggleView(string label, string help, bool initialValue, Action<bool> onChange = null)
        {
            _label = new GUIContent(label, help);
            Value = initialValue;
            _onChange = onChange;
        }

        public void Draw()
        {
            var oldValue = Value;
            Value = GUILayout.Toggle(oldValue, _label);
            if (oldValue != Value && _onChange != null)
            {
                _onChange(Value);
                Extensions.ClearGuiFocus();
            }
        }
    }

    public class DynamicToggleView : IView
    {
        private readonly GUIContent _label;
        private readonly Func<bool> _getValue;
        private readonly Func<bool> _isValid;
        private readonly Action<bool> _onChange;

        public DynamicToggleView(string label, string help, Func<bool> getValue, Func<bool> isValid,
            Action<bool> onChange)
        {
            _label = new GUIContent(label, help);
            _getValue = getValue;
            _isValid = isValid;
            _onChange = onChange;
        }

        public void Draw()
        {
            var oldValue = _getValue();
            var newValue = GUILayout.Toggle(oldValue, _label);
            if (oldValue != newValue && _isValid())
            {
                _onChange(newValue);
                Extensions.ClearGuiFocus();
            }
        }
    }

    public class DynamicSliderView : IView
    {
        private readonly Action<double> _onChange;
        private readonly GUIContent _label;
        private readonly Func<double> _get;

        public DynamicSliderView(string label, string help, Func<double> get, Action<double> onChange)
        {
            _onChange = onChange;
            _label = new GUIContent(label, help);
            _get = get;
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(_label);
            var oldValue = _get();
            var newValue = (double) GUILayout.HorizontalSlider((float) oldValue, 0, 1);
            if (Math.Abs(newValue - oldValue) > 0.001)
            {
                _onChange?.Invoke(newValue);
                Extensions.ClearGuiFocus();
            }
            GUILayout.EndHorizontal();
        }
    }

    public class SliderView : IView
    {
        private readonly Action<double> _onChange;
        private readonly GUIContent _label;

        public double Value { get; set; }

        public SliderView(string label, string help, Action<double> onChange = null)
        {
            _onChange = onChange;
            _label = new GUIContent(label, help);
            Value = 0;
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(_label);
            var newValue = (double) GUILayout.HorizontalSlider((float) Value, 0, 1);
            if (Math.Abs(newValue - Value) > 0.001)
            {
                Value = newValue;
                _onChange?.Invoke(Value);
                Extensions.ClearGuiFocus();
            }
            GUILayout.EndHorizontal();
        }
    }

    public class ListSelectView<T> : IView
    {
        private readonly string _prefix;
        private readonly Func<IEnumerable<T>> _list;
        private readonly Func<T, string> _toString;
        private readonly Action<T> _onSelect;
        private T _currentlySelected;

        public T CurrentlySelected
        {
            get { return _currentlySelected; }
            set
            {
                _currentlySelected = value;
                _onSelect?.Invoke(value);
            }
        }

        public void ReInvokeOnSelect()
        {
            _onSelect?.Invoke(_currentlySelected);
        }

        public ListSelectView(string prefix, Func<IEnumerable<T>> list, Action<T> onSelect = null,
            Func<T, string> toString = null)
        {
            _prefix = prefix + ": ";
            _list = list;
            _toString = toString ?? (x => x.ToString());
            _onSelect = onSelect;
            _currentlySelected = default(T);
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(_prefix + (_currentlySelected == null ? "<none>" : _toString(_currentlySelected)));
            if (GUILayout.Button("Select"))
            {
                Extensions.ClearGuiFocus();
                var realList = _list();
                if (realList != null)
                    WindowHelper.Selector("Select", realList, _toString, t => CurrentlySelected = t);
            }
            GUILayout.EndHorizontal();
        }
    }

    public class TextBoxView<T> : IView
    {
        private readonly GUIContent _label;
        private readonly TryParse<T> _parser;
        private readonly Func<T, string> _toString;
        private readonly Action<T> _onSet;
        private string _value;
        private T _obj;

        public bool Valid { get; private set; }

        public T Object
        {
            get { return _obj; }
            set
            {
                _value = _toString(value);
                _obj = value;
            }
        }

        public TextBoxView(string label, string help, T start, TryParse<T> parser, Func<T, string> toString = null,
            Action<T> onSet = null)
        {
            _label = label == null ? null : new GUIContent(label, help);
            _toString = toString ?? (x => x.ToString());
            _value = _toString(start);
            _parser = parser;
            _onSet = onSet;
        }

        public void Draw()
        {
            if (_label != null || _onSet != null)
            {
                GUILayout.BeginHorizontal();
                if (_label != null)
                    GUILayout.Label(_label);
            }

            T tempValue;
            Valid = _parser(_value, out tempValue);

            if (Valid)
            {
                _value = GUILayout.TextField(_value);
                _obj = tempValue;
            }
            else
            {
                var color = GUI.color;
                GUI.color = Color.red;
                _value = GUILayout.TextField(_value);
                GUI.color = color;
            }
            if (_label != null || _onSet != null)
            {
                if (_onSet != null && Valid && GUILayout.Button("Set"))
                {
                    _onSet(Object);
                    Extensions.ClearGuiFocus();
                }
                GUILayout.EndHorizontal();
            }
        }
    }

    public class TextAreaView<T> : IView
    {
        private readonly GUIContent _label;
        private readonly TryParse<T> _parser;
        private readonly Func<T, string> _toString;
        private readonly Action<T> _onSet;
        private string _value;
        private T _obj;
        private Vector2 scrollPosition;

        public bool Valid { get; private set; }

        public T Object
        {
            get { return _obj; }
            set
            {
                _value = _toString(value);
                _obj = value;
            }
        }

        public TextAreaView(string label, string help, T start, TryParse<T> parser, Func<T, string> toString = null,
            Action<T> onSet = null)
        {
            _label = label == null ? null : new GUIContent(label, help);
            _toString = toString ?? (x => x.ToString());
            _value = _toString(start);
            _parser = parser;
            _onSet = onSet;
        }

        public void Draw()
        {
            if (_label != null || _onSet != null)
            {
                GUILayout.BeginVertical();
                if (_label != null)
                    GUILayout.Label(_label);
            }

            T tempValue;
            Valid = _parser(_value, out tempValue);


            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.MinWidth(100), GUILayout.MinHeight(100), GUILayout.MaxHeight(400));
            if (Valid)
            {
                _value = GUILayout.TextArea(_value);
                _obj = tempValue;
            }
            else
            {
                var color = GUI.color;
                GUI.color = Color.red;
                _value = GUILayout.TextArea(_value);
                GUI.color = color;
            }
            GUILayout.EndScrollView();
            if (_label != null || _onSet != null)
            {
                if (_onSet != null && Valid && GUILayout.Button("Set"))
                {
                    _onSet(Object);
                    Extensions.ClearGuiFocus();
                }
                GUILayout.EndVertical();
            }
        }
    }

    public class TabView : IView
    {
        private readonly List<KeyValuePair<string, IView>> _views;
        private KeyValuePair<string, IView> _current;

        public TabView(List<KeyValuePair<string, IView>> views)
        {
            _views = views;
            _current = views[0];
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            foreach (var view in _views)
            {
                if (view.Key == _current.Key)
                {
                    GUILayout.Button(view.Key, Extensions.PressedButton);
                }
                else
                {
                    if (GUILayout.Button(view.Key))
                    {
                        _current = view;
                        Extensions.ClearGuiFocus();
                    }
                }
            }
            GUILayout.EndHorizontal();
            _current.Value.Draw();
        }
    }

    public class ScrollView : IView {
        private readonly IView _view;
        private readonly GUILayoutOption[] _options;
        private Vector2 scrollPosition;

        public ScrollView(IView view, params GUILayoutOption[] options) {
            _view = view;
            _options = options;
        }

        public void Draw() {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, _options);
            _view.Draw();
            GUILayout.EndScrollView();
        }
    }
}