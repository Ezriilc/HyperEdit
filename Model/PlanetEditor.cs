using System.Collections.Generic;
using UnityEngine;

namespace HyperEdit.Model
{
    public class PlanetEditor
    {
        private static Dictionary<string, PlanetSettings> _defaultSettings = new Dictionary<string, PlanetSettings>();

        public struct PlanetSettings
        {
            public double GeeASL { get; set; }
            public float AtmoshpereTemperatureMultiplier { get; set; }
            public bool Atmosphere { get; set; }
            public bool AtmosphereContainsOxygen { get; set; }
            public float AtmosphereMultiplier { get; set; }
            public double AtmosphereScaleHeight { get; set; }
            public Color AtmosphericAmbientColor { get; set; }
            public double SphereOfInfluence { get; set; }
            public double RotationPeriod { get; set; }
            public bool TidallyLocked { get; set; }

            public PlanetSettings(CelestialBody body)
            {
                GeeASL = body.GeeASL;
                AtmoshpereTemperatureMultiplier = body.atmoshpereTemperatureMultiplier;
                Atmosphere = body.atmosphere;
                AtmosphereContainsOxygen = body.atmosphereContainsOxygen;
                AtmosphereMultiplier = body.atmosphereMultiplier;
                AtmosphereScaleHeight = body.atmosphereScaleHeight;
                AtmosphericAmbientColor = body.atmosphericAmbientColor;
                SphereOfInfluence = body.sphereOfInfluence;
                RotationPeriod = body.rotationPeriod;
                TidallyLocked = body.tidallyLocked;

                if (_defaultSettings.ContainsKey(body.bodyName) == false)
                {
                    _defaultSettings.Add(body.bodyName, this);
                }
            }

            public bool Matches(CelestialBody body)
            {
                return
                  GeeASL == body.GeeASL &&
                  AtmoshpereTemperatureMultiplier == body.atmoshpereTemperatureMultiplier &&
                  Atmosphere == body.atmosphere &&
                  AtmosphereContainsOxygen == body.atmosphereContainsOxygen &&
                  AtmosphereMultiplier == body.atmosphereMultiplier &&
                  AtmosphereScaleHeight == body.atmosphereScaleHeight &&
                  AtmosphericAmbientColor == body.atmosphericAmbientColor &&
                  SphereOfInfluence == body.sphereOfInfluence &&
                  RotationPeriod == body.rotationPeriod &&
                  TidallyLocked == body.tidallyLocked;
            }

            public void CopyTo(CelestialBody body)
            {
                body.GeeASL = GeeASL;
                body.atmoshpereTemperatureMultiplier = AtmoshpereTemperatureMultiplier;
                body.atmosphere = Atmosphere;
                body.atmosphereContainsOxygen = AtmosphereContainsOxygen;
                body.atmosphereMultiplier = AtmosphereMultiplier;
                body.atmosphereScaleHeight = AtmosphereScaleHeight;
                body.atmosphericAmbientColor = AtmosphericAmbientColor;
                body.sphereOfInfluence = SphereOfInfluence;
                body.rotationPeriod = RotationPeriod;
                body.tidallyLocked = TidallyLocked;
                body.CBUpdate();
            }
        }

        private PlanetSettings _currentSettings;
        public PlanetSettings CurrentSettings
        {
            get { return _currentSettings; }
            set { _currentSettings = value; }
        }

        private CelestialBody _currentBody;
        public CelestialBody CurrentBody
        {
            get { return _currentBody; }
            set { _currentBody = value; _currentSettings = new PlanetSettings(value); }
        }

        public void SelectPlanet()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.Bodies == null)
                View.WindowHelper.Error("Could not get list of planets");
            else
                View.WindowHelper.Selector("Select planet", FlightGlobals.Bodies, b => b.bodyName, b => CurrentBody = b);
        }

        public void Apply()
        {
            if (_currentBody == null)
                return;
            _currentSettings.CopyTo(_currentBody);
        }

        public bool IsNotDefault
        {
            get
            {
                if (_currentBody == null)
                    return false;
                try
                {
                    var defaultCb = _defaultSettings[_currentBody.bodyName];
                    return !defaultCb.Matches(_currentBody);
                }
                catch (KeyNotFoundException)
                {
                    Debug.Log("Defaults for celestial body " + _currentBody.bodyName + " not found");
                    return false;
                }
            }
        }

        public void ResetToDefault()
        {
            if (_currentBody == null)
                return;
            try
            {
                var defaultCb = _defaultSettings[_currentBody.bodyName];
                _currentSettings = defaultCb;
                Apply();
            }
            catch (KeyNotFoundException)
            {
                Debug.Log("Defaults for celestial body " + _currentBody.bodyName + " not found");
            }
        }
    }
}
