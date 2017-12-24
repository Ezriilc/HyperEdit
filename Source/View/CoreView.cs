using System;

namespace HyperEdit.View
{
    public static class CoreView
    {
        public static Action Create(HyperEditBehaviour hyperedit)
        {
            var view = View(hyperedit);
            return () => Window.Create("HyperEdit", true, true, 120, -1, w => view.Draw());
        }

        public static IView View(HyperEditBehaviour hyperedit)
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
      //var debugMenu = new ButtonView("KSP Debug Menu", "Opens the KSP Debug Toolbar (also available with Mod+F12)", () => DebugToolbar.toolbarShown = true); // !DebugToolbar.toolbarShown);
            var about = new ButtonView("About", "Opens the About window", aboutView);
            var appLauncher = new DynamicToggleView("H-Button",
                "Enables or disables the AppLauncher button (top right H button)",
                () => hyperedit.UseAppLauncherButton, () => true, v => hyperedit.UseAppLauncherButton = v);

            return new VerticalView(new IView[]
            {
                closeAll,
                orbitEditor,
                planetEditor,
                shipLander,
                miscTools,
                //debugMenu,
                about,
                appLauncher
            });
        }
    }
}