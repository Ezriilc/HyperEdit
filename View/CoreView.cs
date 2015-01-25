using UnityEngine;

namespace HyperEdit.View
{
    public static class CoreView
    {
        public static void Create()
        {
            var view = View();
            Window.Create("HyperEdit", true, true, 100, -1, w => view.Draw());
        }

        public static IView View()
        {
            var closeAll = new ButtonView("Close all", "Closes all windows", Window.CloseAll);
            var orbitEditor = new ButtonView("Orbit Editor", "Opens the Orbit Editor window", OrbitEditorView.Create);
            var planetEditor = new ButtonView("Planet Editor", "Opens the Planet Editor window", PlanetEditorView.Create);
            var shipLander = new ButtonView("Ship Lander", "Opens the Ship Lander window", LanderView.Create);
            var miscTools = new ButtonView("Misc Tools", "Opens the Misc Tools window", MiscEditorView.Create);
            var about = new ButtonView("About", "Opens the About window", AboutWindow.Create);

            return new VerticalView(new IView[]
                {
                    closeAll,
                    orbitEditor,
                    planetEditor,
                    shipLander,
                    miscTools,
                    about
                });
        }
    }
}
