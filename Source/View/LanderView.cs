using System;

namespace HyperEdit.View
{
  public static class LanderView
  {

    private static bool _autoOpenLander;
    private static ConfigNode _hyperEditConfig;

    public static Action Create()
    {
      var view = View();
      return () => Window.Create("Lander", true, true, 200, -1, w => view.Draw());
    }

    // Use myTryParse to validate the string, and, if it is 0, to set it to 0.001f
    static bool myTryParse(string str, out double d)
    {
      double d1;
      bool b = double.TryParse(str, out d1);
      if (!b)
      {
        d = 0.001f;
        return false;
      }
      if (d1 == 0)
        d1 = 0.001d;
      d = d1;
      return true;
    }
    /*
    static bool lonTryParse(string str, out double result) {
      result = null;
      double d1;
      //Extensions.DegreeFix(str, 0);
      bool b = double.TryParse(str, out d1);

      if (!b) {
        result = 0.001f;
        return false;
      } else {
        
        d1 = Extensions.DegreeFix(result, 0);

        return true;
      }
      return true;

    }
    */

    static bool latTryParse(string str, out double d)
    {
      double d1;
      double highLimit = 89.9d;
      double lowLimit = -89.9d;
      bool b = double.TryParse(str, out d1);
      if (!b)
      {
        d = 0.001f;
        return false;
      }
      if (d1 == 0)
      {
        d = 0.001d;
        return true;
      }
      if (d1 > highLimit)
      {
        d = highLimit;
        return false;
      }
      if (d1 < lowLimit)
      {
        d = lowLimit;
        return false;
      }
      //d = d1;
      d = Extensions.DegreeFix(d1, 0); //checking for massive values
      return true;
    }
    static bool altTryParse(string str, out double d)
    {
      double d1;
      double lowLimit = 0.0d;
      bool b = Model.SiSuffix.TryParse(str, out d1);
      if (!b)
      {
        d = 0.001f;
        return false;
      }
      if (d1 == 0)
      {
        d = 0.001d;
        return true;
      }
      if (d1 < lowLimit)
      {
        d = lowLimit;
        return false;
      }
      d = d1;
      return true;
    }

    private static void ReloadConfig()
    {
      var hypereditCfg = IoExt.GetPath("hyperedit.cfg");
      if (System.IO.File.Exists(hypereditCfg))
      {
        _hyperEditConfig = ConfigNode.Load(hypereditCfg);
        _hyperEditConfig.name = "hyperedit";
      }
      else
      {
        _hyperEditConfig = new ConfigNode("hyperedit");
      }

      var autoOpenLanderValue = true;
      _hyperEditConfig.TryGetValue("AutoOpenLander", ref autoOpenLanderValue, bool.TryParse);
      AutoOpenLander = autoOpenLanderValue;
    }

    public static bool AutoOpenLander
    {
      get { return _autoOpenLander; }
      set
      {
        if (_autoOpenLander == value)
          return;
        _autoOpenLander = value;
        _hyperEditConfig.SetValue("AutoOpenLander", value.ToString(), true);
        _hyperEditConfig.Save();
      }
    }

    public static IView View()
    {
      // Load Auto Open status.
      ReloadConfig();

      var setAutoOpen = new DynamicToggleView("Auto Open", "Open this view when entering the Flight or Tracking Center scenes.",
          () => AutoOpenLander, () => true, v => AutoOpenLander = v);
      var bodySelector = new ListSelectView<CelestialBody>("Body", () => FlightGlobals.fetch == null ? null : FlightGlobals.fetch.bodies, null, Extensions.CbToString);
      bodySelector.CurrentlySelected = FlightGlobals.fetch == null ? null : FlightGlobals.ActiveVessel == null ? Planetarium.fetch.Home : FlightGlobals.ActiveVessel.mainBody;
      var lat = new TextBoxView<double>("Lat", "Latitude (North/South). Between +90 (North) and -90 (South).", 0.001d, latTryParse);
      var lon = new TextBoxView<double>("Lon", "Longitude (East/West). Converts to less than 360 degrees.", 0.001d, myTryParse);
      var alt = new TextBoxView<double>("Alt", "Altitude (Up/Down). Distance above the surface.", 20, altTryParse);
      var setRot = new ToggleView("Force Rotation",
          "Rotates vessel such that up on the vessel is up when landing. Otherwise, the current orientation is kept relative to the body.",
          true);
      Func<bool> isValid = () => lat.Valid && lon.Valid && alt.Valid;
      Action<double, double, double, CelestialBody> load = (latVal, lonVal, altVal, body) =>
      {
        lat.Object = latVal;
        lon.Object = lonVal;
        alt.Object = altVal;
        bodySelector.CurrentlySelected = body;
      };

      // Load last entered values.
      Model.DoLander.LoadLast(load);

      return new VerticalView(new IView[]
          {
            setAutoOpen,
            bodySelector,
            new ConditionalView(() => FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.mainBody != bodySelector.CurrentlySelected, new LabelView("Landing on a different body is not recommended.", "This may destroy the vessel. Use the Orbit Editor to orbit the body first, then land on it.")),
            lat,
            new ConditionalView(() => !lat.Valid, new LabelView("Latitude must be a number from 0 to (+/-)89.9.", "Values too close to the poles ((+/-)90) can crash KSP, values beyond that are invalid for a latitude.")),
            lon,
            alt,
            new ConditionalView(() => alt.Object < 0, new LabelView("Altitude must be a positive number.", "This may destroy the vessel. Values less than 0 are sub-surface.")),
            setRot,
            new ConditionalView(() => !isValid(), new ButtonView("Cannot Land", "Entered location is invalid. Correct items in red.", null)),
            new ConditionalView(() => !Model.DoLander.IsLanding() && isValid(), new ButtonView("Land", "Teleport to entered location, then slowly lower to surface.", () => Model.DoLander.ToggleLanding(lat.Object, lon.Object, alt.Object, bodySelector.CurrentlySelected, setRot.Value, load))),
            new ConditionalView(() => Model.DoLander.IsLanding(), new ButtonView("Drop (CAUTION!)", "Release vessel to gravity.", () => Model.DoLander.ToggleLanding(lat.Object, lon.Object, alt.Object, bodySelector.CurrentlySelected, setRot.Value, load))),
            new ConditionalView(() => Model.DoLander.IsLanding(), new LabelView("LANDING IN PROGRESS.", "Vessel is being lowered to the surface.")),
            //Launch button here
            new ConditionalView(() => Model.DoLander.IsLanding(), new LabelView(changeHelpString(), "Change location slightly.")),
            new ConditionalView(() => !Model.DoLander.IsLanding(), new ButtonView("Land Here", "Stop at current location, then slowly lower to surface.", () => Model.DoLander.LandHere(load))),
            new ListSelectView<Vessel>("Set to vessel", Model.DoLander.LandedVessels, select => Model.DoLander.SetToLanded(load, select), Extensions.VesselToString),
            new ButtonView("Current", "Set to current location.", () => Model.DoLander.SetToCurrent(load)),
            new ConditionalView(isValid, new ButtonView("Save", "Save the entered location.", () => Model.DoLander.AddSavedCoords(lat.Object, lon.Object, alt.Object, bodySelector.CurrentlySelected))),
            new ButtonView("Load", "Load a saved location.", () => Model.DoLander.Load(load)),
            new ButtonView("Delete", "Delete a saved location.", Model.DoLander.Delete),
          });
    }

    private static string changeHelpString()
    {
      return
          $"Use {GameSettings.TRANSLATE_UP.primary},{GameSettings.TRANSLATE_DOWN.primary},{GameSettings.TRANSLATE_LEFT.primary},{GameSettings.TRANSLATE_RIGHT.primary} to fine-tune location.";
    }
  }
}
