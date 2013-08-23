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

[KSPAddonFixed(KSPAddon.Startup.MainMenu, true, typeof(HyperEditModule))]
public class HyperEditModule : MonoBehaviour
{
    public HyperEditModule()
    {
        HyperEdit.Immortal.AddImmortal<HyperEdit.HyperEditBehaviour>();
    }
}

namespace HyperEdit
{
    public static class Immortal
    {
        private static GameObject _gameObject;
        public static T AddImmortal<T>() where T : Component
        {
            if (_gameObject == null)
            {
                _gameObject = new GameObject("HyperEditImmortal", typeof(T));
                UnityEngine.Object.DontDestroyOnLoad(_gameObject);
            }
            return _gameObject.GetComponent<T>() ?? _gameObject.AddComponent<T>();
        }
    }
    public class HyperEditBehaviour : MonoBehaviour
    {
        private static Krakensbane _krakensbane;
        private static readonly HyperEditWindow HyperEditWindow = new HyperEditWindow();

        public static Krakensbane Krakensbane
        {
            get { return _krakensbane ?? (_krakensbane = (Krakensbane)FindObjectOfType(typeof(Krakensbane))); }
        }

        public void Update()
        {
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.H))
                HyperEditWindow.OpenWindow();
        }
    }

    public static class ErrorPopup
    {
        public static void Error(string message)
        {
            PopupDialog.SpawnPopupDialog("Error", message, "Close", true, HighLogic.Skin);
        }
    }

    public class HyperEditWindow : Window
    {
        public HyperEditWindow()
        {
            EnsureSingleton(this);
            Title = "HyperEdit";
            WindowRect = new Rect(50, 50, 100, 100);
            Contents = new List<IWindowContent>
                {
                    new Button("Ship Lander", new Lander().OpenWindow),
                    new Button("Orbit Editor", new OrbitEditor().OpenWindow),
                    new Button("Planet Editor", new PlanetEditor().OpenWindow),
                    new Button("Misc Tools", new MiscTools().OpenWindow),
                    new Button("HyperEdit Help", new HelpWindow().OpenWindow),
                    new Button("Close All", CloseAll)
                };
        }
    }

    public static class Settings
    {
        private static GUIStyle _pressedButton;
        public static GUIStyle PressedButton
        {
            get
            {
                return _pressedButton ?? (_pressedButton = new GUIStyle(HighLogic.Skin.button)
                    {
                        normal = HighLogic.Skin.button.active,
                        hover = HighLogic.Skin.button.active,
                        active = HighLogic.Skin.button.normal
                    });
            }
        }
    }

/*
public static class Si_DISABLED // Not currently used.  Si suffixes are unnecessary and confusing.  Possibly useful with modification for clarity and practicality.
    {
        private static readonly Dictionary<string, double> Suffixes = new Dictionary<string, double>
            {
                {"Y", 1e24},
                {"Z", 1e21},
                {"E", 1e18},
                {"P", 1e15},
                {"T", 1e12},
                {"G", 1e9},
                {"M", 1e6},
                {"k", 1e3},
                {"h", 1e2},
                {"da", 1e1},

                {"d", 1e-1},
                {"c", 1e-2},
                {"m", 1e-3},
                {"u", 1e-6},
                {"n", 1e-9},
                {"p", 1e-12},
                {"f", 1e-15},
                {"a", 1e-18},
                {"z", 1e-21},
                {"y", 1e-24}
            };

        public static bool TryParse(string s, out double value)
        {
            s = s.Trim();
            double multiplier;
            var suffix = Suffixes.FirstOrDefault(suf => s.EndsWith(suf.Key));
            if (suffix.Key != null)
            {
                s = s.Substring(0, s.Length - suffix.Key.Length);
                multiplier = suffix.Value;
            }
            else
                multiplier = 1.0;
            if (double.TryParse(s, out value) == false)
                return false;
            value *= multiplier;
            return true;
        }

        public static string ToString(this double value)
        {
            var log = Math.Log10(Math.Abs(value));
            var minDiff = double.MaxValue;
            var minSuffix = new KeyValuePair<string, double>("", 1);
            foreach (var suffix in Suffixes.Concat(new[] { new KeyValuePair<string, double>("", 1) }))
            {
                var diff = Math.Abs(log - Math.Log10(suffix.Value));
                if (diff < minDiff)
                {
                    minDiff = diff;
                    minSuffix = suffix;
                }
            }
            value /= minSuffix.Value;
            return value.ToString("F") + minSuffix.Key;
        }
    }
*/
    public static class Extentions
    {
        public static bool ActiveVesselNullcheck(this Window window)
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
            {
                ErrorPopup.Error("Could not find the active vessel (are you in the flight scene?)");
                window.CloseWindow();
                return true;
            }
            return false;
        }

        public static Vessel GetVessel(this Orbit orbit)
        {
            return FlightGlobals.fetch == null ? null : FlightGlobals.Vessels.FirstOrDefault(v => v.orbitDriver != null && v.orbit == orbit);
        }

        public static CelestialBody GetPlanet(this Orbit orbit)
        {
            return FlightGlobals.fetch == null ? null : FlightGlobals.Bodies.FirstOrDefault(v => v.orbitDriver != null && v.orbit == orbit);
        }

        public static void Set(this Orbit orbit, Orbit newOrbit)
        {
            var vessel = FlightGlobals.fetch == null ? null : FlightGlobals.Vessels.FirstOrDefault(v => v.orbitDriver != null && v.orbit == orbit);
            var body = FlightGlobals.fetch == null ? null : FlightGlobals.Bodies.FirstOrDefault(v => v.orbitDriver != null && v.orbit == orbit);
            if (vessel != null)
                WarpShip(vessel, newOrbit);
            else if (body != null)
                WarpPlanet(body, newOrbit);
            else
                HardsetOrbit(orbit, newOrbit);
        }

        private static void WarpShip(Vessel vessel, Orbit newOrbit)
        {
            if (newOrbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).magnitude > newOrbit.referenceBody.sphereOfInfluence)
            {
                ErrorPopup.Error("Destination position was above the sphere of influence");
                return;
            }

            vessel.Landed = false;
            vessel.Splashed = false;
            vessel.landedAt = string.Empty;
            var parts = vessel.parts;
            if (parts != null)
            {
                var clamps = parts.Where(p => p.Modules != null && p.Modules.OfType<LaunchClamp>().Any()).ToList();
                foreach (var clamp in clamps)
                    clamp.Die();
            }

            try
            {
                OrbitPhysicsManager.HoldVesselUnpack(60);
            }
            catch (NullReferenceException)
            {
            }

            foreach (var v in (FlightGlobals.fetch == null ? (IEnumerable<Vessel>)new[] { vessel } : FlightGlobals.Vessels).Where(v => v.packed == false))
                v.GoOnRails();

            HardsetOrbit(vessel.orbit, newOrbit);

            vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
            vessel.orbitDriver.vel = vessel.orbit.vel;
        }

        private static void WarpPlanet(CelestialBody body, Orbit newOrbit)
        {
            var oldBody = body.referenceBody;
            HardsetOrbit(body.orbit, newOrbit);
            if (oldBody != newOrbit.referenceBody)
            {
                oldBody.orbitingBodies.Remove(body);
                newOrbit.referenceBody.orbitingBodies.Add(body);
            }
            body.CBUpdate();
        }

        private static void HardsetOrbit(Orbit orbit, Orbit newOrbit)
        {
            orbit.inclination = newOrbit.inclination;
            orbit.eccentricity = newOrbit.eccentricity;
            orbit.semiMajorAxis = newOrbit.semiMajorAxis;
            orbit.LAN = newOrbit.LAN;
            orbit.argumentOfPeriapsis = newOrbit.argumentOfPeriapsis;
            orbit.meanAnomalyAtEpoch = newOrbit.meanAnomalyAtEpoch;
            orbit.epoch = newOrbit.epoch;
            orbit.referenceBody = newOrbit.referenceBody;
            orbit.Init();
            orbit.UpdateFromUT(Planetarium.GetUniversalTime());
        }

        public static void Teleport(this Krakensbane krakensbane, Vector3d offset)
        {
            foreach (var vessel in FlightGlobals.Vessels.Where(v => v.packed == false && v != FlightGlobals.ActiveVessel))
                vessel.GoOnRails();
            krakensbane.setOffset(offset);
        }

        public static Rect Set(this Rect rect, int width, int height)
        {
            return new Rect(rect.xMin, rect.yMin, width, height);
        }

        public static Orbit Clone(this Orbit o)
        {
            return new Orbit(o.inclination, o.eccentricity, o.semiMajorAxis, o.LAN,
                             o.argumentOfPeriapsis, o.meanAnomalyAtEpoch, o.epoch, o.referenceBody);
        }

        public static float Soi(this CelestialBody body)
        {
            var radius = (float)(body.sphereOfInfluence * 0.95);
            if (Planetarium.fetch != null && body == Planetarium.fetch.Sun || float.IsNaN(radius) || float.IsInfinity(radius) || radius < 0 || radius > 200000000000f)
                radius = 200000000000f; // jool apo = 72,212,238,387
            return radius;
        }

        public static string Aggregate(this IEnumerable<string> source, string middle)
        {
            return source.Aggregate("", (total, part) => total + middle + part).Substring(middle.Length);
        }
    }
}

// Credit to "Majiir" for "KSPAddonFixed" : KSPAddon with equality checking using an additional type parameter. Fixes the issue where AddonLoader prevents multiple start-once addons with the same start scene.
public class KSPAddonFixed : KSPAddon, IEquatable<KSPAddonFixed>
{
    private readonly Type type;

    public KSPAddonFixed(KSPAddon.Startup startup, bool once, Type type)
        : base(startup, once)
    {
        this.type = type;
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() != this.GetType()) { return false; }
        return Equals((KSPAddonFixed)obj);
    }

    public bool Equals(KSPAddonFixed other)
    {
        if (this.once != other.once) { return false; }
        if (this.startup != other.startup) { return false; }
        if (this.type != other.type) { return false; }
        return true;
    }

    public override int GetHashCode()
    {
        return this.startup.GetHashCode() ^ this.once.GetHashCode() ^ this.type.GetHashCode();
    }
}
