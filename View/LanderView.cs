using System;

namespace HyperEdit.View
{
    public static class LanderView
    {
        public static Action Create()
        {
            var view = View();
            return () => Window.Create("Lander", true, true, 200, -1, w => view.Draw());
        }
		// Use myTryParse to validate the string, and, if it is 0, to set it to 0.001f
		static bool myTryParse(string str, out double d)
		{
			double d1;
			bool b = double.TryParse (str, out d1);
			if (!b) {
				d = 0.001f;
				return false;
			}
			if (d1 == 0)
				d1 = 0.001d;
			d = d1;
			return true;
		}
        public static IView View()
        {
            var bodySelector = new ListSelectView<CelestialBody>("Planet", () => FlightGlobals.fetch == null ? null : FlightGlobals.fetch.bodies, null, Extensions.CbToString);
            bodySelector.CurrentlySelected = FlightGlobals.fetch == null ? null : FlightGlobals.ActiveVessel == null ? Planetarium.fetch.Home : FlightGlobals.ActiveVessel.mainBody;
			var lat = new TextBoxView<double>("Lat", "Latitude of landing coordinates", 0.001d, myTryParse);
			var lon = new TextBoxView<double>("Lon", "Longitude of landing coordinates", 0.001d, myTryParse);
            var alt = new TextBoxView<double>("Alt", "Altitude of landing coordinates", 20, Model.SiSuffix.TryParse);
            var setRot = new ToggleView("Set rotation",
                "If set, rotates the vessel such that up on the vessel is up when landing. Otherwise, the same orientation is kept as before teleporting, relative to the planet",
                false);
            Func<bool> isValid = () => lat.Valid && lon.Valid && alt.Valid;
            Action<double, double, CelestialBody> load = (latVal, lonVal, body) =>
            {
                lat.Object = latVal;
                lon.Object = lonVal;
                bodySelector.CurrentlySelected = body;
            };

            return new VerticalView(new IView[]
                {
                    lat,
                    lon,
                    alt,
                    bodySelector,
                    setRot,
                    new ConditionalView(() => FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.mainBody != bodySelector.CurrentlySelected,
                        new LabelView("Landing on a body other than the current one is not recommended.",
                            "This causes lots of explosions, it's advisable to teleport to an orbit above the planet, then land on it directly")),
                    new ConditionalView(() => lat.Valid && (lat.Object < -89.9 || lat.Object > 89.9),
                        new LabelView("Setting latitude to -90 or 90 degrees (or near it) is dangerous, try 89.9 degrees",
                            "(This warning also appears when latitude is past 90 degrees)")),
                    new DynamicToggleView("Landing", "Land the ship (or stop landing)", Model.DoLander.IsLanding,
                        isValid, b => Model.DoLander.ToggleLanding(lat.Object, lon.Object, alt.Object, bodySelector.CurrentlySelected, setRot.Value, load)),
                    new ConditionalView(() => Model.DoLander.IsLanding(), new LabelView(HelpString(), "Moves the landing vessel's coordinates slightly")),
                    new ConditionalView(() => !Model.DoLander.IsLanding(), new ButtonView("Land here", "Stops the vessel and slowly lowers it to the ground (without teleporting)", () => Model.DoLander.LandHere(load))),
                    new ConditionalView(isValid, new ButtonView("Save", "Save the entered location", () => Model.DoLander.AddSavedCoords(lat.Object, lon.Object, bodySelector.CurrentlySelected))),
                    new ButtonView("Load", "Load a previously-saved location", () => Model.DoLander.Load(load)),
                    new ButtonView("Delete", "Delete a previously-saved location", Model.DoLander.Delete),
                    new ButtonView("Set to current", "Set lat/lon to the current position", () => Model.DoLander.SetToCurrent(load)),
                    new ListSelectView<Vessel>("Set lat/lon to", Model.DoLander.LandedVessels, select => Model.DoLander.SetToLanded(load, select), Extensions.VesselToString),
                });
        }

        private static string HelpString()
        {
            return
                $"Use {GameSettings.TRANSLATE_UP.primary},{GameSettings.TRANSLATE_DOWN.primary},{GameSettings.TRANSLATE_LEFT.primary},{GameSettings.TRANSLATE_RIGHT.primary} to fine-tune landing coordinates";
        }
    }
}
