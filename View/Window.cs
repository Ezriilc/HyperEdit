using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace HyperEdit.View
{
    static class WindowHelper
    {
        public static void Prompt(string prompt, Action<string> complete)
        {
            var str = "";
            Window.Create(prompt, 200, 100, w =>
            {
                str = GUILayout.TextField(str);
                if (GUILayout.Button("OK"))
                {
                    complete(str);
                    w.Close();
                }
            });
        }

        public static void Selector<T>(string title, IEnumerable<T> elements, Func<T, string> nameSelector, Action<T> onSelect)
        {
            var collection = elements.Select(t => new { value = t, name = nameSelector(t) }).ToList();
            Window.Create(title, 200, 100, w =>
            {
                foreach (var item in collection)
                {
                    if (GUILayout.Button(item.name))
                    {
                        onSelect(item.value);
                        w.Close();
                        return;
                    }
                }
            });
        }

        public static void Error(string v)
        {
            Window.Create("Error", 200, 100, w => GUILayout.Label(v));
        }
    }

    public class Window : MonoBehaviour
    {
        private static GameObject _gameObject;
        private static GameObject GameObject
        {
            get
            {
                if (_gameObject == null)
                {
                    _gameObject = new GameObject("HyperEditWindowManager");
                    DontDestroyOnLoad(_gameObject);
                }
                return _gameObject;
            }
        }

        private string _title;
        private bool _shrinkHeight;
        private Rect _windowRect;
        private Action<Window> _drawFunc;

        public static void Create(string title, int width, int height, Action<Window> drawFunc)
        {
            var window = GameObject.AddComponent<Window>();
            window._shrinkHeight = height == -1;
            if (window._shrinkHeight)
                height = 5;
            window._title = title;
            window._windowRect = new Rect(100, 100, width, height);
            window._drawFunc = drawFunc;
        }

        private Window() { }

        public void Update()
        {
            if (_shrinkHeight)
                _windowRect.height = 5;
        }

        public void OnGUI()
        {
            GUI.skin = HighLogic.Skin;
            _windowRect = GUILayout.Window(GetInstanceID(), _windowRect, DrawWindow, _title, GUILayout.ExpandHeight(true));
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("Close"))
                Close();
            _drawFunc(this);
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        public void Close()
        {
            Destroy(this);
        }

        internal static void CloseAll()
        {
            foreach (var window in GameObject.GetComponents<Window>())
                window.Close();
        }
    }
}
