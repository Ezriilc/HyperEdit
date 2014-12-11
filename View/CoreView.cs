using UnityEngine;

namespace HyperEdit.View
{
    public class CoreView : View
    {
        public static void Create()
        {
            var view = new CoreView();
            Window.Create("HyperEdit", 100, -1, view.Draw);
        }

        private CoreView() { }

        public override void Draw(Window window)
        {
            base.Draw(window);
            if (GUILayout.Button(new GUIContent("Close all", "Closes all windows")))
                Window.CloseAll();
            if (GUILayout.Button(new GUIContent("Orbit Editor", "Opens the Orbit Editor window")))
                CreateView(new Model.OrbitEditor());
            if (GUILayout.Button(new GUIContent("Planet Editor", "Opens the Planet Editor window")))
                CreateView(new Model.PlanetEditor());
            if (GUILayout.Button(new GUIContent("Ship Lander", "Opens the Ship Lander window")))
                CreateView(new Model.Lander());
            if (GUILayout.Button(new GUIContent("Misc Tools", "Opens the Misc Tools window")))
                CreateView(new Model.MiscEditor());
            if (GUILayout.Button(new GUIContent("About", "Opens the About window")))
                AboutWindow.Create();
        }
    }
}
