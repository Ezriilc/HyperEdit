﻿using System.Collections.Generic;
using System.Linq;

namespace HyperEdit.Model
{
    public static class PlanetEditor
    {
        private static bool _haveAppliedDefaults;
        private static readonly Dictionary<string, PlanetSettings> DefaultSettings = new Dictionary<string, PlanetSettings>();

        public struct PlanetSettings
        {
            // Included fields for copy-paste:
            /*
                double GeeASL
                bool   ocean
                bool   atmosphere
                bool   atmosphereContainsOxygen
                double atmosphereDepth
                double atmosphereTemperatureSeaLevel
                double atmospherePressureSeaLevel
                double atmosphereMolarMass
                double atmosphereAdiabaticIndex
                bool   rotates
                double rotationPeriod
                double initialRotation
                bool   tidallyLocked
            */

            public double GeeASL { get; set; }
            public bool ocean { get; set; }
            public bool atmosphere { get; set; }
            public bool atmosphereContainsOxygen { get; set; }
            public double atmosphereDepth { get; set; }
            public double atmosphereTemperatureSeaLevel { get; set; }
            public double atmospherePressureSeaLevel { get; set; }
            public double atmosphereMolarMass { get; set; }
            public double atmosphereAdiabaticIndex { get; set; }
            public bool rotates { get; set; }
            public double rotationPeriod { get; set; }
            public double initialRotation { get; set; }
            public bool tidallyLocked { get; set; }
            public Orbit orbit { get; set; }

            public PlanetSettings(
                double GeeASL,
                bool ocean,
                bool atmosphere,
                bool atmosphereContainsOxygen,
                double atmosphereDepth,
                double atmosphereTemperatureSeaLevel,
                double atmospherePressureSeaLevel,
                double atmosphereMolarMass,
                double atmosphereAdiabaticIndex,
                bool rotates,
                double rotationPeriod,
                double initialRotation,
                bool tidallyLocked,
                Orbit orbit) : this()
            {
                this.GeeASL = GeeASL;
                this.ocean = ocean;
                this.atmosphere = atmosphere;
                this.atmosphereContainsOxygen = atmosphereContainsOxygen;
                this.atmosphereDepth = atmosphereDepth;
                this.atmosphereTemperatureSeaLevel = atmosphereTemperatureSeaLevel;
                this.atmospherePressureSeaLevel = atmospherePressureSeaLevel;
                this.atmosphereMolarMass = atmosphereMolarMass;
                this.atmosphereAdiabaticIndex = atmosphereAdiabaticIndex;
                this.rotates = rotates;
                this.rotationPeriod = rotationPeriod;
                this.initialRotation = initialRotation;
                this.tidallyLocked = tidallyLocked;
                this.orbit = orbit;
            }

            public PlanetSettings(CelestialBody body)
                : this()
            {
                GeeASL = body.GeeASL;
                ocean = body.ocean;
                atmosphere = body.atmosphere;
                atmosphereContainsOxygen = body.atmosphereContainsOxygen;
                atmosphereDepth = body.atmosphereDepth;
                atmosphereTemperatureSeaLevel = body.atmosphereTemperatureSeaLevel;
                atmospherePressureSeaLevel = body.atmospherePressureSeaLevel;
                atmosphereMolarMass = body.atmosphereMolarMass;
                atmosphereAdiabaticIndex = body.atmosphereAdiabaticIndex;
                rotates = body.rotates;
                rotationPeriod = body.rotationPeriod;
                initialRotation = body.initialRotation;
                tidallyLocked = body.tidallyLocked;
                orbit = body.orbitDriver?.orbit.Clone();

                if (DefaultSettings.ContainsKey(body.bodyName) == false)
                {
                    DefaultSettings.Add(body.bodyName, this);
                }
            }

            public void CopyTo(CelestialBody body, bool setOrbit)
            {
                body.GeeASL = GeeASL;
                body.ocean = ocean;
                body.atmosphere = atmosphere;
                body.atmosphereContainsOxygen = atmosphereContainsOxygen;
                body.atmosphereDepth = atmosphereDepth;
                body.atmosphereTemperatureSeaLevel = atmosphereTemperatureSeaLevel;
                body.atmospherePressureSeaLevel = atmospherePressureSeaLevel;
                body.atmosphereMolarMass = atmosphereMolarMass;
                body.atmosphereAdiabaticIndex = atmosphereAdiabaticIndex;
                body.rotates = rotates;
                body.rotationPeriod = rotationPeriod;
                body.initialRotation = initialRotation;
                body.tidallyLocked = tidallyLocked;

                if (setOrbit && body.orbitDriver != null && orbit != null)
                    body.SetOrbit(orbit);

                body.RealCbUpdate();

                Extensions.Log($"Set body \"{body.bodyName}\"'s parameters to:\n{GetConfig(body)}");
            }

            public static ConfigNode GetConfig(CelestialBody body)
            {
                var node = new ConfigNode(body.bodyName);

                node.AddValue("GeeASL", body.GeeASL);
                node.AddValue("ocean", body.ocean);
                node.AddValue("atmosphere", body.atmosphere);
                node.AddValue("atmosphereContainsOxygen", body.atmosphereContainsOxygen);
                node.AddValue("atmosphereDepth", body.atmosphereDepth);
                node.AddValue("atmosphereTemperatureSeaLevel", body.atmosphereTemperatureSeaLevel);
                node.AddValue("atmospherePressureSeaLevel", body.atmospherePressureSeaLevel);
                node.AddValue("atmosphereMolarMass", body.atmosphereMolarMass);
                node.AddValue("atmosphereAdiabaticIndex", body.atmosphereAdiabaticIndex);
                node.AddValue("rotates", body.rotates);
                node.AddValue("rotationPeriod", body.rotationPeriod);
                node.AddValue("initialRotation", body.initialRotation);
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
                node.TryGetValue("ocean", ref body.ocean, bool.TryParse);
                node.TryGetValue("atmosphere", ref body.atmosphere, bool.TryParse);
                node.TryGetValue("atmosphereContainsOxygen", ref body.atmosphereContainsOxygen, bool.TryParse);
                node.TryGetValue("atmosphereDepth", ref body.atmosphereDepth, double.TryParse);
                node.TryGetValue("atmosphereTemperatureSeaLevel", ref body.atmosphereTemperatureSeaLevel, double.TryParse);
                node.TryGetValue("atmospherePressureSeaLevel", ref body.atmospherePressureSeaLevel, double.TryParse);
                node.TryGetValue("atmosphereMolarMass", ref body.atmosphereMolarMass, double.TryParse);
                node.TryGetValue("atmosphereAdiabaticIndex", ref body.atmosphereAdiabaticIndex, double.TryParse);
                node.TryGetValue("rotates", ref body.rotates, bool.TryParse);
                node.TryGetValue("rotationPeriod", ref body.rotationPeriod, double.TryParse);
                node.TryGetValue("initialRotation", ref body.initialRotation, double.TryParse);
                node.TryGetValue("tidallyLocked", ref body.tidallyLocked, bool.TryParse);

                if (body.orbitDriver != null)
                {
                    var orbit = body.orbitDriver.orbit.Clone();
                    node.TryGetValue("inclination", ref orbit.inclination, double.TryParse);
                    node.TryGetValue("eccentricity", ref orbit.eccentricity, double.TryParse);
                    node.TryGetValue("semiMajorAxis", ref orbit.semiMajorAxis, double.TryParse);
                    node.TryGetValue("LAN", ref orbit.LAN, double.TryParse);
                    node.TryGetValue("argumentOfPeriapsis", ref orbit.argumentOfPeriapsis, double.TryParse);
                    node.TryGetValue("meanAnomalyAtEpoch", ref orbit.meanAnomalyAtEpoch, double.TryParse);
                    node.TryGetValue("orbitEpoch", ref orbit.epoch, double.TryParse);
                    node.TryGetValue("orbitBody", ref orbit.referenceBody, Extensions.CbTryParse);
                    body.SetOrbit(orbit);
                }

                body.RealCbUpdate();

                Extensions.Log($"Set body \"{body.bodyName}\"'s parameters to:\n{GetConfig(body)}");
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
                var defaultCb = DefaultSettings[body.bodyName];
                defaultCb.CopyTo(body, true);
            }
            catch (KeyNotFoundException)
            {
                Extensions.Log("Defaults for celestial body " + body.bodyName + " not found");
            }
        }

        public static void SavePlanet(CelestialBody body)
        {
            PlanetSettings.GetConfig(body).Save();
        }

        public static void TryApplyFileDefaults()
        {
            if (_haveAppliedDefaults)
                return;
            ApplyFileDefaults();
        }

        public static void ApplyFileDefaults()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.Bodies == null)
            {
                Extensions.Log("Could not apply planet defaults: FlightGlobals.Bodies was null");
                return;
            }
            _haveAppliedDefaults = true;
            foreach (var body in FlightGlobals.Bodies)
            {
                new PlanetSettings(body); // trigger default settings check
                var filepath = IoExt.GetPath(body.bodyName + ".cfg");
                if (System.IO.File.Exists(filepath) == false)
                    continue;
                var cfg = ConfigNode.Load(filepath);
                Extensions.Log("Applying saved config for " + body.bodyName);
                PlanetSettings.ApplyConfig(cfg, body);
            }
        }
    }
}
