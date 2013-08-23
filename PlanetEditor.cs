//
// This file is part of the HyperEdit plugin for Kerbal Space Program, Copyright Erickson Swift, 2013.
// HyperEdit is licensed under the GPL, found in COPYING.txt.
// Currently supported by Team HyperEdit, and Ezriilc.
// Original HyperEdit concept and code by khyperia (no longer involved).
//
// Thanks to Payo for inventing, writing and contributing the PlanetEditor component.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HyperEdit
{
  public class PlanetEditor : Window
  {
    struct PlanetSettings
    {
      double geeASL;
      float atmoshpereTemperatureMultiplier;
      bool atmosphere;
      bool atmosphereContainsOxygen;
      float atmosphereMultiplier;
      double atmosphereScaleHeight;
      UnityEngine.Color atmosphericAmbientColor;

      public static PlanetSettings CreateFromBody(CelestialBody body)
      {
        PlanetSettings result;
        result.geeASL = body.GeeASL;
        result.atmoshpereTemperatureMultiplier = body.atmoshpereTemperatureMultiplier;
        result.atmosphere = body.atmosphere;
        result.atmosphereContainsOxygen = body.atmosphereContainsOxygen;
        result.atmosphereMultiplier = body.atmosphereMultiplier;
        result.atmosphereScaleHeight = body.atmosphereScaleHeight;
        result.atmosphericAmbientColor = body.atmosphericAmbientColor;

        return result;
      }

      public bool Matches(CelestialBody body)
      {
        return
          geeASL == body.GeeASL &&
          atmoshpereTemperatureMultiplier == body.atmoshpereTemperatureMultiplier &&
          atmosphere == body.atmosphere &&
          atmosphereContainsOxygen == body.atmosphereContainsOxygen &&
          atmosphereMultiplier == body.atmosphereMultiplier &&
          atmosphereScaleHeight == body.atmosphereScaleHeight &&
          atmosphericAmbientColor == body.atmosphericAmbientColor;
      }

      public void CopyTo(CelestialBody body)
      {
        body.GeeASL = geeASL;
        body.atmoshpereTemperatureMultiplier = atmoshpereTemperatureMultiplier;
        body.atmosphere = atmosphere;
        body.atmosphereContainsOxygen = atmosphereContainsOxygen;
        body.atmosphereMultiplier = atmosphereMultiplier;
        body.atmosphereScaleHeight = atmosphereScaleHeight;
        body.atmosphericAmbientColor = atmosphericAmbientColor;
      }
    }

    private CelestialBody _body;
    private Dictionary<string, PlanetSettings> _defaults;
    private FieldProxy[] _fields;

    public PlanetEditor()
    {
      EnsureSingleton(this);
      Title = "Planet editor";
      WindowRect = new Rect(100, 200, 200, 5);
      Initialize();
      Refresh();
    }

    private void Initialize()
    {
      _fields = new FieldProxy[0];
      _defaults = new Dictionary<string, PlanetSettings>();

      if (FlightGlobals.fetch == null || FlightGlobals.Bodies.FirstOrDefault() == null)
        return;

      _fields = new FieldProxy[]
      {
        FieldProxy.Create("gravitation", () => DensityToGrav(_body.Radius, _body.GeeASL), v => _body.GeeASL = GravToDensity(_body.Radius, v) ),
        FieldProxy.Create("temperature", () => _body.atmoshpereTemperatureMultiplier, v => _body.atmoshpereTemperatureMultiplier = v),
        FieldProxy.Create("has atmosphere", () => _body.atmosphere, v => _body.atmosphere = v),
        FieldProxy.Create("has O2", () => _body.atmosphereContainsOxygen, v => _body.atmosphereContainsOxygen = v),
        FieldProxy.Create("atmospheric pressur", () => _body.atmosphereMultiplier, v => _body.atmosphereMultiplier = v),
        FieldProxy.Create("atmosphere height", () => _body.atmosphereScaleHeight, v => _body.atmosphereScaleHeight = v),
        FieldProxy.Create("atmosphere color", () => _body.atmosphericAmbientColor, v => _body.atmosphericAmbientColor = v),
      };
    }

    private PlanetSettings SafeGetDefaults(CelestialBody body)
    {
      if (!_defaults.ContainsKey(body.name))
      {
        _defaults[body.name] = PlanetSettings.CreateFromBody(body);
      }

      return _defaults[body.name];
    }

    private double DensityToGrav(double radius, double density)
    {
      // default gravitational effect is 9.81
      double gravitation = density * (9.81 * Math.Pow(radius, 2.0));
      return gravitation;
    }

    private double GravToDensity(double radius, double gravitation)
    {
      // default gravitational effect is 9.81
      double geeASL = gravitation / (9.81 * Math.Pow(radius, 2.0));
      return geeASL;
    }

    private void Refresh()
    {
      Contents = new List<IWindowContent>
                {
                    new CustomDisplay(() => { }),
                    new Button("Close", CloseWindow),
                    new Button("Select planet to edit", SelectPlanet),
                    new Label("Editing: " + (GetName(_body) ?? "Nothing selected"))
                };
      if (_body != null)
      {
        foreach (var field in _fields)
        {
          Contents.Add(field.NewTextBox());
        }

        Contents.Add(new Button("Set", SetPlanet));

        if (!MatchesDefaults())
        {
          Contents.Add(new Button("Revert to Defaults", ResetPlanet));
        }

        if (!IsKerbin())
        {
          Contents.Add(new Button("Copy to Kerbin", CopyToKerbin));
        }
      }

      WindowRect = WindowRect.Set(300, 5);
    }

    private bool IsKerbin()
    {
      return _body == GetKerbin();
    }

    private bool MatchesDefaults()
    {
      var defaults = SafeGetDefaults(_body);

      return defaults.Matches(_body);
    }

    private static string GetName(CelestialBody body)
    {
      if (body == null)
        return null;

      if (string.IsNullOrEmpty(body.name))
        return "Unknown";

      return body.name;
    }

    private void SelectPlanet()
    {
      if (FlightGlobals.fetch == null)
      {
        ErrorPopup.Error("Could not get the list of orbits (are you in the flight scene?)");
        return;
      }
      new Selector<CelestialBody>("Select planet", OrderedBodies(), GetName, SetBody).OpenWindow();
    }

    private IEnumerable<CelestialBody> OrderedBodies()
    {
      if (FlightGlobals.fetch == null)
        yield break;

      foreach (var body in FlightGlobals.Bodies.Where(b => b != null).Distinct())
      {
        SafeGetDefaults(body);
        yield return body;
      }
    }

    private CelestialBody GetKerbin()
    {
      return OrderedBodies().FirstOrDefault(b => b.name == "Kerbin");
    }

    private void ResetPlanet()
    {
      var defaults = SafeGetDefaults(_body);
      defaults.CopyTo(_body);
      _body.CBUpdate();
      Refresh();
    }

    private void CopyToKerbin()
    {
      if (IsKerbin())
        return;

      var settings = PlanetSettings.CreateFromBody(_body);
      var kerbin = GetKerbin();
      settings.CopyTo(kerbin);
      kerbin.CBUpdate();

      if (IsKerbin())
        Refresh();
    }

    private void SetPlanet()
    {
      if (_body == null)
      {
        ErrorPopup.Error("No active body selected");
        return;
      }

      if (_fields.Any(f => !f.CanParseTextBox()))
      {
        ErrorPopup.Error("A planet parameter was not a valid");
        return;
      }

      _fields.ToList().ForEach(f => f.Commit());

      _body.CBUpdate();
      Refresh();
    }

    private void SetBody(CelestialBody body)
    {
      _body = body;
      Refresh();
    }
  }
}