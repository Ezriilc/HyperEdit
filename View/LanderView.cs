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
            var lat = new TextBoxView<double>("Lat", "Latitude of landing coordinates", 0, double.TryParse);
            var lon = new TextBoxView<double>("Lon", "Longitude of landing coordinates", 0, double.TryParse);
            var alt = new TextBoxView<double>("Alt", "Altitude of landing coordinates", 20, SiSuffix.TryParse);
            Func<bool> isValid = () => lat.Valid && lon.Valid && alt.Valid;
            Action<double, double> load = (latVal, lonVal) =>
            {
                lat.Object = latVal;
                lon.Object = lonVal;
            };

            var landBesideSelector = new ListSelectView<Vessel>("Land Next To", Model.DoLander.LandedVessels, null, Extentions.VesselToString);

            if (FlightGlobals.fetch != null && FlightGlobals.fetch.activeVessel != null && FlightGlobals.fetch.activeVessel.orbitDriver != null)
            {
                landBesideSelector.CurrentlySelected = FlightGlobals.fetch.activeVessel;
            }

            return new VerticalView(new IView[]
                {
                    lat,
                    lon,
                    alt,
                    new DynamicToggleView("Landing", "Land the ship (or stop landing)", Model.DoLander.IsLanding,
                        isValid, b => Model.DoLander.ToggleLanding(lat.Object, lon.Object, alt.Object)),
                    new ConditionalView(() => !Model.DoLander.IsLanding(), new ButtonView("Land here", "Stops the vessel and slowly lowers it to the ground (without teleporting)", () => Model.DoLander.LandHere())),
                    new ConditionalView(isValid, new ButtonView("Save", "Save the current location", () => Model.DoLander.AddSavedCoords(lat.Object, lon.Object))),
                    new ButtonView("Load", "Load a previously-saved location", () => Model.DoLander.Load(load)),
                    new ButtonView("Delete", "Delete a previously-saved location", Model.DoLander.Delete),
                    new ButtonView("SetToCurrent", "Set lat/lon to the current position", () => Model.DoLander.SetToCurrent(load)),
                    landBesideSelector,
                    new ButtonView("Set to Land Next To", "Set lat/lon to the Land Beside Vessel", () => Model.DoLander.SetToLanded(load, landBesideSelector.CurrentlySelected)),
                });
        }
    }
}
