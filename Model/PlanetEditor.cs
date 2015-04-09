using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace HyperEdit.Model
{
    public static class PlanetEditor
    {
        private static Dictionary<string, PlanetSettings> _defaultSettings = new Dictionary<string, PlanetSettings>();

        public struct PlanetSettings
        {
            public double GeeASL { get; set; }

            //public float AtmoshpereTemperatureMultiplier { get; set; }

            public bool Atmosphere { get; set; }

            public bool AtmosphereContainsOxygen { get; set; }

            //public float AtmosphereMultiplier { get; set; }

            //public double AtmosphereScaleHeight { get; set; }

            public Color AtmosphericAmbientColor { get; set; }

            public double SphereOfInfluence { get; set; }

            public double RotationPeriod { get; set; }

            public bool TidallyLocked { get; set; }

            public Orbit Orbit { get; set; }

            public PlanetSettings(
                double geeAsl,
                //float atmoshpereTemperatureMultiplier,
                bool atmosphere,
                bool atmosphereContainsOxygen,
                //float atmosphereMultiplier,
                //double atmosphereScaleHeight,
                Color atmosphericAmbientColor,
                double sphereOfInfluence,
                double rotationPeriod,
                bool tidallyLocked,
                Orbit orbit) : this()
            {
                GeeASL = geeAsl;
                //AtmoshpereTemperatureMultiplier = atmoshpereTemperatureMultiplier;
                Atmosphere = atmosphere;
                AtmosphereContainsOxygen = atmosphereContainsOxygen;
                //AtmosphereMultiplier = atmosphereMultiplier;
                //AtmosphereScaleHeight = atmosphereScaleHeight;
                AtmosphericAmbientColor = atmosphericAmbientColor;
                SphereOfInfluence = sphereOfInfluence;
                RotationPeriod = rotationPeriod;
                TidallyLocked = tidallyLocked;
                Orbit = orbit;
            }

            public PlanetSettings(CelestialBody body)
                : this()
            {
                GeeASL = body.GeeASL;
                //AtmoshpereTemperatureMultiplier = body.atmoshpereTemperatureMultiplier;
                Atmosphere = body.atmosphere;
                AtmosphereContainsOxygen = body.atmosphereContainsOxygen;
                //AtmosphereMultiplier = body.atmosphereMultiplier;
                //AtmosphereScaleHeight = body.atmosphereScaleHeight;
                AtmosphericAmbientColor = body.atmosphericAmbientColor;
                SphereOfInfluence = body.sphereOfInfluence;
                RotationPeriod = body.rotationPeriod;
                TidallyLocked = body.tidallyLocked;
                Orbit = body.orbitDriver == null ? null : body.orbitDriver.orbit.Clone();

                if (_defaultSettings.ContainsKey(body.bodyName) == false)
                {
                    _defaultSettings.Add(body.bodyName, this);
                }
            }

            public bool Matches(CelestialBody body)
            {
                return GeeASL == body.GeeASL &&
                    //AtmoshpereTemperatureMultiplier == body.atmoshpereTemperatureMultiplier &&
                    Atmosphere == body.atmosphere &&
                    AtmosphereContainsOxygen == body.atmosphereContainsOxygen &&
                    //AtmosphereMultiplier == body.atmosphereMultiplier &&
                    //AtmosphereScaleHeight == body.atmosphereScaleHeight &&
                    AtmosphericAmbientColor == body.atmosphericAmbientColor &&
                    SphereOfInfluence == body.sphereOfInfluence &&
                    RotationPeriod == body.rotationPeriod &&
                    TidallyLocked == body.tidallyLocked;
            }

            public void CopyTo(CelestialBody body, bool setOrbit)
            {
                body.GeeASL = GeeASL;
                //body.atmoshpereTemperatureMultiplier = AtmoshpereTemperatureMultiplier;
                body.atmosphere = Atmosphere;
                body.atmosphereContainsOxygen = AtmosphereContainsOxygen;
                //body.atmosphereMultiplier = AtmosphereMultiplier;
                //body.atmosphereScaleHeight = AtmosphereScaleHeight;
                body.atmosphericAmbientColor = AtmosphericAmbientColor;
                body.sphereOfInfluence = SphereOfInfluence;
                body.rotationPeriod = RotationPeriod;
                body.tidallyLocked = TidallyLocked;
                if (setOrbit && body.orbitDriver != null && Orbit != null)
                    body.SetOrbit(Orbit);
                body.CBUpdate();
            }

            public static ConfigNode GetConfig(CelestialBody body)
            {
                var node = new ConfigNode(body.bodyName);
                node.AddValue("GeeASL", body.GeeASL);
                //node.AddValue("atmoshpereTemperatureMultiplier", body.atmoshpereTemperatureMultiplier);
                node.AddValue("atmosphere", body.atmosphere);
                node.AddValue("atmosphereContainsOxygen", body.atmosphereContainsOxygen);
                //node.AddValue("atmosphereMultiplier", body.atmosphereMultiplier);
                //node.AddValue("atmosphereScaleHeight", body.atmosphereScaleHeight);
                node.AddValue("atmosphericAmbientColor", body.atmosphericAmbientColor);
                node.AddValue("sphereOfInfluence", body.sphereOfInfluence);
                node.AddValue("rotationPeriod", body.rotationPeriod);
                node.AddValue("tidallyLocked", body.tidallyLocked);

                if (body.orbitDriver == null)
                    return node;
                var orbit = body.orbitDriver.orbit;
                node.AddValue("inclination", orbit.inclination);
                node.AddValue("eccentricity", orbit.eccentricity);
                node.AddValue("semiMajorAxis", orbit.semiMajorAxis);
                node.AddValue("LAN", orbit.LAN);
                node.AddValue("argumentOfPeriapsis", orbit.argumentOfPeriapsis);
                node.AddValue("meanAnomalyAtEpoch", orbit.meanAnomalyAtEpoch);
                node.AddValue("orbitEpoch", orbit.epoch);
                node.AddValue("orbitBody", orbit.referenceBody.bodyName);
                return node;
            }

            public static void ApplyConfig(ConfigNode node, CelestialBody body)
            {
                node.TryGetValue("GeeASL", ref body.GeeASL, double.TryParse);
                //node.TryGetValue("atmoshpereTemperatureMultiplier", ref body.atmoshpereTemperatureMultiplier, float.TryParse);
                node.TryGetValue("atmosphere", ref body.atmosphere, bool.TryParse);
                node.TryGetValue("atmosphereContainsOxygen", ref body.atmosphereContainsOxygen, bool.TryParse);
                //node.TryGetValue("atmosphereMultiplier", ref body.atmosphereMultiplier, float.TryParse);
                //node.TryGetValue("atmosphereScaleHeight", ref body.atmosphereScaleHeight, double.TryParse);
                node.TryGetValue("atmosphericAmbientColor", ref body.atmosphericAmbientColor, Extentions.ColorTryParse);
                node.TryGetValue("sphereOfInfluence", ref body.sphereOfInfluence, double.TryParse);
                node.TryGetValue("rotationPeriod", ref body.rotationPeriod, double.TryParse);
                node.TryGetValue("tidallyLocked", ref body.tidallyLocked, bool.TryParse);
                body.CBUpdate();

                if (body.orbitDriver == null)
                    return;
                var orbit = body.orbitDriver.orbit.Clone();
                node.TryGetValue("inclination", ref orbit.inclination, double.TryParse);
                node.TryGetValue("eccentricity", ref orbit.eccentricity, double.TryParse);
                node.TryGetValue("semiMajorAxis", ref orbit.semiMajorAxis, double.TryParse);
                node.TryGetValue("LAN", ref orbit.LAN, double.TryParse);
                node.TryGetValue("argumentOfPeriapsis", ref orbit.argumentOfPeriapsis, double.TryParse);
                node.TryGetValue("meanAnomalyAtEpoch", ref orbit.meanAnomalyAtEpoch, double.TryParse);
                node.TryGetValue("orbitEpoch", ref orbit.epoch, double.TryParse);
                node.TryGetValue("orbitBody", ref orbit.referenceBody, Extentions.CbTryParse);
                body.SetOrbit(orbit);
            }
        }

        private static CelestialBody _kerbin;

        public static CelestialBody Kerbin
        {
            get
            {
                return _kerbin ??
                    (_kerbin = FlightGlobals.fetch == null ? null :
                        FlightGlobals.fetch.bodies.FirstOrDefault(cb => cb.bodyName == "Kerbin"));
            }
        }

        public static void ResetToDefault(CelestialBody body)
        {
            try
            {
                var defaultCb = _defaultSettings[body.bodyName];
                defaultCb.CopyTo(body, true);
            }
            catch (KeyNotFoundException)
            {
                Extentions.Log("Defaults for celestial body " + body.bodyName + " not found");
            }
        }

        public static void SavePlanet(CelestialBody body)
        {
            var cfg = PlanetSettings.GetConfig(body);
            cfg.Save(KSP.IO.IOUtils.GetFilePathFor(typeof(HyperEditBehaviour), cfg.name + ".cfg"));
        }

        public static void ApplyFileDefaults()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.Bodies == null)
            {
                Extentions.Log("Could not apply planet defaults: FlightGlobals.Bodies was null");
                return;
            }
            foreach (var body in FlightGlobals.Bodies)
            {
                new PlanetSettings(body); // trigger default settings check
                var filename = body.bodyName + ".cfg";
                if (KSP.IO.File.Exists<HyperEditBehaviour>(filename) == false)
                    continue;
                var filepath = KSP.IO.IOUtils.GetFilePathFor(typeof(HyperEditBehaviour), filename);
                var cfg = ConfigNode.Load(filepath);
                Extentions.Log("Applying saved config for " + body.bodyName);
                PlanetSettings.ApplyConfig(cfg, body);
            }
        }
    }
}
