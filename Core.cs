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
        //HyperEdit.Model.PlanetEditor.ApplyFileDefaults();
    }
}

namespace HyperEdit
{
    public delegate bool TryParse<T>(string str, out T value);

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
        ApplicationLauncherButton _appLauncherButton;

        public void Awake()
        {
            GameEvents.OnFlightGlobalsReady.Add(FlightGlobalsReady);
            GameEvents.onGUIApplicationLauncherReady.Add(AddAppLauncher);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveAppLauncher);
        }

        private void FlightGlobalsReady(bool ready)
        {
            if (ready)
                Model.PlanetEditor.ApplyFileDefaults();
        }

        private void AddAppLauncher()
        {
            if (_appLauncherButton != null)
            {
                Extentions.Log("Not adding to ApplicationLauncher, button already exists (yet onGUIApplicationLauncherReady was called?)");
                return;
            }
            var applauncher = ApplicationLauncher.Instance;
            if (applauncher == null)
            {
                Extentions.Log("Cannot add to ApplicationLauncher, instance was null");
                return;
            }
            const ApplicationLauncher.AppScenes scenes =
                ApplicationLauncher.AppScenes.FLIGHT |
                ApplicationLauncher.AppScenes.MAPVIEW |
                ApplicationLauncher.AppScenes.TRACKSTATION;
            var tex = new Texture2D(38, 38, TextureFormat.RGBA32, false);

            for (var x = 0; x < tex.width; x++)
                for (var y = 0; y < tex.height; y++)
                    tex.SetPixel(x, y, new Color(2 * (float)Math.Abs(x - tex.width / 2) / tex.width, 0.25f, 2 * (float)Math.Abs(y - tex.height / 2) / tex.height, 0));
            for (var x = 10; x < 12; x++)
                for (var y = 10; y < tex.height - 10; y++)
                    tex.SetPixel(x, y, new Color(1, 1, 1));
            for (var x = tex.width - 12; x < tex.width - 10; x++)
                for (var y = 10; y < tex.height - 10; y++)
                    tex.SetPixel(x, y, new Color(1, 1, 1));
            for (var x = 12; x < tex.width - 12; x++)
                for (var y = tex.height / 2; y < tex.height / 2 + 2; y++)
                    tex.SetPixel(x, y, new Color(1, 1, 1));

            tex.Apply();
            _appLauncherButton = applauncher.AddModApplication(() =>
                {
                    View.CoreView.Create();
                    //_appLauncherButton.SetFalse();
                }, () =>
                {
                    View.Window.CloseAll();
                }, () =>
                {
                }, () =>
                {
                }, () =>
                {
                }, () =>
                {
                }, scenes, tex);
        }

        private void RemoveAppLauncher()
        {
            var applauncher = ApplicationLauncher.Instance;
            if (applauncher == null)
            {
                Extentions.Log("Cannot remove from ApplicationLauncher, instance was null");
                return;
            }
            if (_appLauncherButton == null)
            {
                return;
            }
            applauncher.RemoveModApplication(_appLauncherButton);
            _appLauncherButton = null;
        }

        public void Update()
        {
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.H))
            {
                if (View.Window.GameObject.GetComponents<View.Window>().Any(w => w._title == "HyperEdit"))
                {
                    View.Window.CloseAll();
                    _appLauncherButton.SetFalse();
                }
                else
                {
                    View.CoreView.Create();
                    _appLauncherButton.SetTrue();
                }
            }
        }
    }

    public static class SiSuffix
    {
        private static readonly Dictionary<string, double> Suffixes = new Dictionary<string, double>
        {
            { "Y", 1e24 },
            { "Z", 1e21 },
            { "E", 1e18 },
            { "P", 1e15 },
            { "T", 1e12 },
            { "G", 1e9 },
            { "M", 1e6 },
            { "k", 1e3 },
            { "h", 1e2 },
            { "da", 1e1 },

            { "d", 1e-1 },
            { "c", 1e-2 },
            { "m", 1e-3 },
            { "u", 1e-6 },
            { "n", 1e-9 },
            { "p", 1e-12 },
            { "f", 1e-15 },
            { "a", 1e-18 },
            { "z", 1e-21 },
            { "y", 1e-24 }
        };

        public static bool TryParse(string s, out float value)
        {
            double dval;
            var success = TryParse(s, out dval);
            value = (float)dval;
            return success;
        }

        public static bool TryParse(string s, out double value)
        {
            s = s.Trim();
            double multiplier;
            var suffix = Suffixes.FirstOrDefault(suf => s.EndsWith(suf.Key, StringComparison.Ordinal));
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

        /*
        // Not currently used.  Si suffixes are unnecessary and confusing.  Possibly useful with modification for clarity and practicality.
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
        */
    }

    public static class Extentions
    {
        public static void Log(string message)
        {
            Debug.Log("HyperEdit: " + message);
        }

        public static void TryGetValue<T>(this ConfigNode node, string key, ref T value, TryParse<T> tryParse)
        {
            var strvalue = node.GetValue(key);
            if (strvalue == null)
                return;
            T temp;
            if (tryParse(strvalue, out temp) == false)
                return;
            value = temp;
        }

        public static void ErrorPopup(string message)
        {
            PopupDialog.SpawnPopupDialog("Error", message, "Close", true, HighLogic.Skin);
        }

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

        public static void DynamicSetOrbit(this OrbitDriver orbit, Orbit newOrbit)
        {
            var vessel = orbit.vessel;
            var body = orbit.celestialBody;
            if (vessel != null)
                vessel.SetOrbit(newOrbit);
            else if (body != null)
                body.SetOrbit(newOrbit);
            else
                HardsetOrbit(orbit, newOrbit);
        }

        public static void SetOrbit(this Vessel vessel, Orbit newOrbit)
        {
            if (newOrbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).magnitude > newOrbit.referenceBody.sphereOfInfluence)
            {
                ErrorPopup("Destination position was above the sphere of influence");
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
                Extentions.Log("OrbitPhysicsManager.HoldVesselUnpack threw NullReferenceException");
            }

            var allVessels = FlightGlobals.fetch == null ? (IEnumerable<Vessel>)new[] { vessel } : FlightGlobals.Vessels;
            foreach (var v in allVessels.Where(v => v.packed == false))
                v.GoOnRails();

            var oldBody = vessel.orbitDriver.orbit.referenceBody;

            HardsetOrbit(vessel.orbitDriver, newOrbit);

            vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
            vessel.orbitDriver.vel = vessel.orbit.vel;

            var newBody = vessel.orbitDriver.orbit.referenceBody;
            if (newBody != oldBody)
            {
                var evnt = new GameEvents.HostedFromToAction<Vessel, CelestialBody>(vessel, oldBody, newBody);
                GameEvents.onVesselSOIChanged.Fire(evnt);
            }
        }

        public static void RealCbUpdate(this CelestialBody body)
        {
            body.CBUpdate();
            body.SetupAtmosphere();
            body.resetTimeWarpLimits();

            // CBUpdate doesn't update hillSphere
            // http://en.wikipedia.org/wiki/Hill_sphere
            var orbit = body.orbit;
            var cubedRoot = Math.Pow(body.Mass / orbit.referenceBody.Mass, 1.0 / 3.0);
            body.hillSphere = orbit.semiMajorAxis * (1.0 - orbit.eccentricity) * cubedRoot;

            // Nor sphereOfInfluence
            // http://en.wikipedia.org/wiki/Sphere_of_influence_(astrodynamics)
            body.sphereOfInfluence = orbit.semiMajorAxis * Math.Pow(body.Mass / orbit.referenceBody.Mass, 2.0 / 5.0);
        }

        public static void SetOrbit(this CelestialBody body, Orbit newOrbit)
        {
            var oldBody = body.referenceBody;
            HardsetOrbit(body.orbitDriver, newOrbit);
            if (oldBody != newOrbit.referenceBody)
            {
                oldBody.orbitingBodies.Remove(body);
                newOrbit.referenceBody.orbitingBodies.Add(body);
            }
            body.RealCbUpdate();
        }

        private static void HardsetOrbit(OrbitDriver orbitDriver, Orbit newOrbit)
        {
            var orbit = orbitDriver.orbit;
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
            if (orbit.referenceBody != newOrbit.referenceBody)
            {
                if (orbitDriver.OnReferenceBodyChange != null)
                    orbitDriver.OnReferenceBodyChange(newOrbit.referenceBody);
            }
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
            if (float.IsNaN(radius) || float.IsInfinity(radius) || radius < 0 || radius > 200000000000f)
                radius = 200000000000f; // jool apo = 72,212,238,387
            return radius;
        }

        private static Dictionary<string, KeyCode> _keyCodeNames;

        public static Dictionary<string, KeyCode> KeyCodeNames
        {
            get
            {
                return _keyCodeNames ?? (_keyCodeNames =
                    Enum.GetNames(typeof(KeyCode)).Distinct().ToDictionary(k => k, k => (KeyCode)Enum.Parse(typeof(KeyCode), k)));
            }
        }

        public static bool KeyCodeTryParse(string str, out KeyCode[] value)
        {
            var split = str.Split('-', '+');
            if (split.Length == 0)
            {
                value = null;
                return false;
            }
            value = new KeyCode[split.Length];
            for (int i = 0; i < split.Length; i++)
            {
                if (KeyCodeNames.TryGetValue(split[i], out value[i]) == false)
                {
                    return false;
                }
            }
            return true;
        }

        public static string VesselToString(this Vessel vessel)
        {
            if (FlightGlobals.fetch != null && FlightGlobals.ActiveVessel == vessel)
                return "Active vessel";
            return vessel.vesselName;
        }

        public static string KeyCodeToString(this KeyCode[] values)
        {
            return string.Join("-", values.Select(v => v.ToString()).ToArray());
        }

        public static string OrbitDriverToString(this OrbitDriver driver)
        {
            if (driver == null)
                return null;
            var body = FlightGlobals.Bodies.FirstOrDefault(cb => cb.orbitDriver != null && cb.orbitDriver == driver);
            if (body != null)
                return body.bodyName;
            var vessel = FlightGlobals.Vessels.FirstOrDefault(v => v.orbitDriver != null && v.orbitDriver == driver);
            if (vessel != null)
                return vessel.VesselToString();
            if (string.IsNullOrEmpty(driver.name) == false)
                return driver.name;
            return "Unknown";
        }

        public static bool CbTryParse(string bodyName, out CelestialBody body)
        {
            body = FlightGlobals.Bodies == null ? null : FlightGlobals.Bodies.FirstOrDefault(cb => cb.name == bodyName);
            return body != null;
        }

        private static string TrimUnityColor(string value)
        {
            value = value.Trim();
            if (value.StartsWith("RGBA", StringComparison.OrdinalIgnoreCase))
                value = value.Substring(4).Trim();
            value = value.Trim('(', ')');
            return value;
        }

        public static bool ColorTryParse(string value, out Color color)
        {
            color = new Color();
            string parseValue = TrimUnityColor(value);
            if (parseValue == null)
                return false;
            string[] values = parseValue.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length == 3 || values.Length == 4)
            {
                if (!float.TryParse(values[0], out color.r) ||
                    !float.TryParse(values[1], out color.g) ||
                    !float.TryParse(values[2], out color.b))
                    return false;
                if (values.Length == 3 && !float.TryParse(values[3], out color.a))
                    return false;
                return true;
            }
            return false;
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
        var other = obj as KSPAddonFixed;
        return other != null && Equals(other);
    }

    public bool Equals(KSPAddonFixed other)
    {
        return once == other.once && startup == other.startup && type == other.type;
    }

    public override int GetHashCode()
    {
        return startup.GetHashCode() ^ once.GetHashCode() ^ type.GetHashCode();
    }
}
