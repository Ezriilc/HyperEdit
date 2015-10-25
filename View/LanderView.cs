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

        public static IView View()
        {
            var bodySelector = new ListSelectView<CelestialBody>("Planet", () => FlightGlobals.fetch == null ? null : FlightGlobals.fetch.bodies, null, Extensions.CbToString);
            bodySelector.CurrentlySelected = FlightGlobals.fetch == null ? null : FlightGlobals.ActiveVessel == null ? Planetarium.fetch.Home : FlightGlobals.ActiveVessel.mainBody;
            var lat = new TextBoxView<double>("Lat", "Latitude of landing coordinates", 0, double.TryParse);
            var lon = new TextBoxView<double>("Lon", "Longitude of landing coordinates", 0, double.TryParse);
            var alt = new TextBoxView<double>("Alt", "Altitude of landing coordinates", 20, SiSuffix.TryParse);
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
                    new ConditionalView(() => FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.mainBody != bodySelector.CurrentlySelected,
                        new LabelView("Landing on a body other than the current one is not recommended.",
                        "This causes lots of explosions, it's advisable to teleport to an orbit above the planet, then land on it directly")),
                    new DynamicToggleView("Landing", "Land the ship (or stop landing)", Model.DoLander.IsLanding,
                        isValid, b => Model.DoLander.ToggleLanding(lat.Object, lon.Object, alt.Object, bodySelector.CurrentlySelected, load)),
                    new ConditionalView(() => Model.DoLander.IsLanding(), new LabelView(HelpString(), "Moves the landing vessel's coordinates slightly")),
                    new ConditionalView(() => !Model.DoLander.IsLanding(), new ButtonView("Land here", "Stops the vessel and slowly lowers it to the ground (without teleporting)", () => Model.DoLander.LandHere())),
                    new ConditionalView(isValid, new ButtonView("Save", "Save the entered location", () => Model.DoLander.AddSavedCoords(lat.Object, lon.Object, bodySelector.CurrentlySelected))),
                    new ButtonView("Load", "Load a previously-saved location", () => Model.DoLander.Load(load)),
                    new ButtonView("Delete", "Delete a previously-saved location", Model.DoLander.Delete),
                    new ButtonView("Set to current", "Set lat/lon to the current position", () => Model.DoLander.SetToCurrent(load)),
                    new ListSelectView<Vessel>("Set lat/lon to", Model.DoLander.LandedVessels, select => Model.DoLander.SetToLanded(load, select), Extensions.VesselToString),
                });
        }

        private static string HelpString()
        {
            return string.Format("Use {0},{1},{2},{3} to fine-tune landing coordinates",
                GameSettings.TRANSLATE_UP.primary,
                GameSettings.TRANSLATE_DOWN.primary,
                GameSettings.TRANSLATE_LEFT.primary,
                GameSettings.TRANSLATE_RIGHT.primary);
        }
    }
}
