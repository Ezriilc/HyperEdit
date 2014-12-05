//
// This file is part of the HyperEdit plugin for Kerbal Space Program, Copyright Erickson Swift, 2013.
// HyperEdit is licensed under the GPL, found in COPYING.txt.
// Currently supported by Team HyperEdit, and Ezriilc.
// Original HyperEdit concept and code by khyperia (no longer involved).
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HyperEdit
{
    public class Window
    {
        private static GameObject _gameObject;
        private static GameObject GameObject
        {
            get
            {
                if (_gameObject == null)
                {
                    _gameObject = new GameObject("HyperEditWindowManager");
                    UnityEngine.Object.DontDestroyOnLoad(_gameObject);
                }
                return _gameObject;
            }
        }

        private static IEnumerable<WindowDrawer> WindowDrawers
        {
            get { return GameObject.GetComponents<WindowDrawer>(); }
        }

        public static void CloseAll()
        {
            foreach (var window in WindowDrawers)
                window.Window = null;
            TrimRenderers();
        }

        public static void EnsureSingleton<T>(T window) where T : Window
        {
            foreach (var windowDrawer in WindowDrawers.Where(windowDrawer => windowDrawer.Window is T && windowDrawer.Window != window))
                windowDrawer.Window = null;
        }

        public static void TrimRenderers()
        {
            foreach (var windowDrawer in WindowDrawers.Where(windowDrawer => windowDrawer.Window == null))
                UnityEngine.Object.Destroy(windowDrawer);
        }

        public Rect WindowRect { get; set; }
        public string Title { get; set; }
        public List<IWindowContent> Contents { get; set; }

        public void OnGUI(int windowId)
        {
            GUI.skin = HighLogic.Skin;
            WindowRect = string.IsNullOrEmpty(Title)
                                        ? GUILayout.Window(windowId, WindowRect, DrawWindow, GUIContent.none, WindowOptions)
                                        : GUILayout.Window(windowId, WindowRect, DrawWindow, Title, WindowOptions);
        }

        public void OpenWindow()
        {
            if (WindowDrawers.Any(w => w.Window == this))
                return;
            var windowDrawer = WindowDrawers.FirstOrDefault(w => w.Window == null) ?? GameObject.AddComponent<WindowDrawer>();
            windowDrawer.Window = this;
        }

        public void CloseWindow()
        {
            var thisDrawer = WindowDrawers.FirstOrDefault(w => w.Window == this);
            if (thisDrawer != null)
                thisDrawer.Window = null;
        }

        private void DrawWindow(int id)
        {
            if (Contents != null)
            {
                GUILayout.BeginVertical();
                foreach (var content in Contents)
                    content.Draw();
                GUILayout.EndVertical();
            }
            GUI.DragWindow();
        }

        public T2 FindField<T1, T2>(string key) where T1 : IValueHolder<T2>, IWindowContent
        {
            if (Contents == null)
                return default(T2);
            foreach (var content in Contents.OfType<T1>().Where(content => content.Name == key))
                return content.Value;
            return default(T2);
        }

        public void SetField<T1, T2>(string key, T2 value) where T1 : class, IValueHolder<T2>, IWindowContent
        {
            if (Contents == null)
                return;
            foreach (var content in Contents.OfType<T1>().Where(content => content.Name == key))
            {
                content.Value = value;
                return;
            }
            MonoBehaviour.print("HyperEdit error: SetField key '" + key + "' not found");
        }

        protected virtual GUILayoutOption[] WindowOptions
        {
            get { return new[] { GUILayout.ExpandHeight(true) }; }
        }
    }

    class WindowDrawer : MonoBehaviour
    {
        public Window Window;
        private int _instanceId = -1;

        public void OnGUI()
        {
            if (_instanceId == -1)
                _instanceId = GetInstanceID();
            if (Window == null)
                return;
            Window.OnGUI(_instanceId);
        }
    }

    public class Selector<T> : Window
    {
        public Selector(string title, IEnumerable<T> elements, Func<T, string> nameSelector, Action<T> onSelect)
        {
            EnsureSingleton(this);
            Title = title;
            WindowRect = new Rect(Screen.width * 3 / 4 - 125, Screen.height / 2 - 200, 250, 400);
            Contents = new List<IWindowContent>
                {
                    new Scroller(elements.Select(a =>
                                                 (IWindowContent) new CustomDisplay(() =>
                                                     {
                                                         if (!GUILayout.Button(nameSelector(a))) return;
                                                         onSelect(a);
                                                         CloseWindow();
                                                     })).ToArray()),
                    new Button("Cancel", CloseWindow)
                };
        }
    }

    public class Prompt : Window
    {
        public Prompt(string question, Action<string> onAccept)
        {
            EnsureSingleton(this);
            Title = question;
            WindowRect = new Rect(Screen.width / 2 - 100, Screen.height / 2 - 50, 200, 100);
            Contents = new List<IWindowContent>
                {
                    new TextBox(null, ""),
                    new HorizontalList(new IWindowContent[] {new Button("OK", () =>
                        {
                            onAccept(FindField<TextBox, string>(null));
                            CloseWindow();
                        }), new Button("Cancel", CloseWindow)})
                };
        }
    }

    public interface IWindowContent
    {
        void Draw();
    }

    public interface IValueHolder<T>
    {
        string Name { get; }
        T Value { get; set; }
    }

    public class Label : IWindowContent
    {
        public string Text { get; set; }

        public Label(string text)
        {
            Text = text;
        }

        public void Draw()
        {
            GUILayout.Label(Text);
        }
    }

    public class TextBox : IWindowContent, IValueHolder<string>
    {
        public string Name { get; private set; }
        public string Value { get; set; }
        public Action<string> OnPress { get; set; }

        public TextBox(string fieldName, string textValue)
        {
            Name = fieldName;
            Value = textValue;
        }

        public TextBox(string fieldName, string textValue, Action<string> onPress)
        {
            Name = fieldName;
            Value = textValue;
            OnPress = onPress;
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            if (string.IsNullOrEmpty(Name) == false)
                GUILayout.Label(Name, GUILayout.ExpandWidth(false));
            Value = GUILayout.TextField(Value, GUILayout.ExpandWidth(true));
            if (OnPress != null && GUILayout.Button("Set", GUILayout.ExpandWidth(false)))
                OnPress(Value);
            GUILayout.EndHorizontal();
        }
    }

    public class Button : IWindowContent
    {
        public string Text { get; set; }
        public Action OnClick { get; set; }

        public Button(string text, Action onClick)
        {
            Text = text;
            OnClick = onClick;
        }

        public void Draw()
        {
            if (GUILayout.Button(Text))
                OnClick();
        }
    }

    // onClick returns the new text of the button
    public class DynamicButton : IWindowContent
    {
        public string Text { get; set; }
        public Func<string> OnClick { get; set; }

        public DynamicButton(string text, Func<string> onClick)
        {
            Text = text;
            OnClick = onClick;
        }

        public void Draw()
        {
            if (GUILayout.Button(Text))
            {
                string newText = OnClick();
                if (newText != null)
                    Text = newText;
            }
        }
    }

    public class Toggle : IWindowContent, IValueHolder<bool>
    {
        public string Name { get; private set; }
        public Action<bool> OnChange { get; set; }
        public bool Value { get; set; }

        public Toggle(string text, bool value, Action<bool> onChange)
        {
            Name = text;
            OnChange = onChange;
            Value = value;
        }

        public void Draw()
        {
            var prev = Value;
            Value = GUILayout.Toggle(Value, Name);
            if (prev != Value)
                OnChange(Value);
        }
    }

    public class Scroller : IWindowContent
    {
        public IWindowContent[] Contents { get; set; }
        public Vector2 Position { get; set; }

        public Scroller(IWindowContent[] contents)
        {
            Contents = contents;
        }

        public void Draw()
        {
            Position = GUILayout.BeginScrollView(Position);
            foreach (var windowContent in Contents)
                windowContent.Draw();
            GUILayout.EndScrollView();
        }
    }

    public class HorizontalList : IWindowContent
    {
        public IWindowContent[] Contents { get; set; }

        public HorizontalList(params IWindowContent[] contents)
        {
            Contents = contents;
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            foreach (var windowContent in Contents)
                windowContent.Draw();
            GUILayout.EndHorizontal();
        }
    }

    public class Slider : IWindowContent, IValueHolder<float>
    {
        public string Name { get; private set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public float Value { get; set; }
        public Action<float> OnChange { get; set; }

        public Slider(string name, float min, float max, float value, Action<float> onChange)
        {
            Name = name;
            Min = min;
            Max = max;
            Value = value;
            OnChange = onChange;
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Name);
            var prevValue = Value;
            Value = GUILayout.HorizontalSlider(Value, Min, Max);
            if (Math.Abs(Value - prevValue) > 1E-5f)
                OnChange(Mathf.Clamp(Value, Min, Max));
            GUILayout.EndHorizontal();
        }
    }

    public class CustomDisplay : IWindowContent
    {
        public Action DrawFunc { get; set; }

        public CustomDisplay(Action drawFunc)
        {
            DrawFunc = drawFunc;
        }

        public void Draw()
        {
            DrawFunc();
        }
    }

    public class ListSelect<T> : IWindowContent
    {
        private readonly IList<T> _elements;
        private readonly string[] _names;
        private readonly Action<T> _onChange;
        private int _selectedItem;

        public ListSelect(IList<T> elements, Func<T, string> nameSelector, Action<T> onChange)
        {
            _elements = elements;
            _names = elements.Select(nameSelector).ToArray();
            _onChange = onChange;
        }

        public T Selected
        {
            get { return _elements[_selectedItem]; }
            set { _selectedItem = _elements.IndexOf(value); }
        }

        public void Draw()
        {
            GUILayout.BeginHorizontal();
            for (var i = 0; i < _names.Length; i++)
            {
                var pressed = i == _selectedItem ? GUILayout.Button(_names[i], Settings.PressedButton) : GUILayout.Button(_names[i]);
                if (pressed)
                {
                    _onChange(_elements[i]);
                    _selectedItem = i;
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}
