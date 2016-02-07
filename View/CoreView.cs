using System;
using System.Collections.Generic;

namespace HyperEdit.View
{
	public static class CoreView 
    {
		//
		// These functions were modified to support the API by LinuxGuruGame
		// There is no functionality change for HyperEdit when called normally
		// The changes allow specifying which edit windows should be available
		//
		public static Action Create(HyperEditBehaviour hyperedit, bool showOrbit = true, bool showPlanet = true, bool showShipLander = true, bool showMisc = true, bool showAbout = true, bool showApplauncher = true)
        {
			var view = View(hyperedit, showOrbit, showPlanet, showShipLander, showMisc, showAbout, showApplauncher);
            return () => Window.Create("HyperEdit", true, true, 120, -1, w => view.Draw());
        }

		public static IView View(HyperEditBehaviour hyperedit, bool showOrbit, bool showPlanet, bool showShipLander, bool showMisc, bool showAbout, bool showApplauncher)
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
            var appLauncher = new DynamicToggleView("H-Button", "Enables or disables the AppLauncher button (top right H button)",
                () => hyperedit.UseAppLauncherButton, () => true, v => hyperedit.UseAppLauncherButton = v);


			var views = new IView[7 - (showOrbit?0:1) - (showPlanet?0:1) - (showShipLander?0:1) - (showMisc?0:1) - (showAbout?0:1) - (showApplauncher?0:1)];
			int cnt = 0;
			views[cnt++] = closeAll;
			if (showOrbit) views[cnt++] = orbitEditor;
			if (showPlanet) views[cnt++] = planetEditor;
			if (showShipLander) views[cnt++] = shipLander;
			if (showMisc) views[cnt++] = miscTools;
			if (showAbout) views[cnt++] = about;
			if (showApplauncher) views[cnt++] = appLauncher;
			return new VerticalView(views);
               
        }
    }
}
