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
            if (GUILayout.Button("Close all"))
                Window.CloseAll();
            if (GUILayout.Button("Orbit Editor"))
                CreateView(new Model.OrbitEditor());
            if (GUILayout.Button("Planet Editor"))
                CreateView(new Model.PlanetEditor());
            if (GUILayout.Button("Ship Lander"))
                CreateView(new Model.Lander());
            if (GUILayout.Button("Misc Tools"))
                CreateView(new Model.MiscEditor());
        }
    }
}
