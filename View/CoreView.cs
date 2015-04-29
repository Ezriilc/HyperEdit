using System;
using UnityEngine;

namespace HyperEdit.View
{
    public static class CoreView
    {
        public static Action Create()
        {
            var view = View();
            return () => Window.Create("HyperEdit", true, true, 100, -1, w => view.Draw());
        }

        public static IView View()
        {
            var orbitEditorView = OrbitEditorView.Create();
            var planetEditorView = PlanetEditorView.Create();
            var landerView = LanderView.Create();
            var miscEditorView = MiscEditorView.Create();
            var aboutView = AboutWindow.Create();

            var closeAll = new ButtonView("Close all", "Closes all windows", Window.CloseAll);
            var orbitEditor = new ButtonView("Orbit Editor", "Opens the Orbit Editor window", orbitEditorView);
            var planetEditor = new ButtonView("Planet Editor", "Opens the Planet Editor window", planetEditorView);
            var shipLander = new ButtonView("Ship Lander", "Opens the Ship Lander window", landerView);
            var miscTools = new ButtonView("Misc Tools", "Opens the Misc Tools window", miscEditorView);
            var about = new ButtonView("About", "Opens the About window", aboutView);

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
