using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.UI.Screens;
using System.Diagnostics;

[assembly: System.Reflection.AssemblyTitle("HyperEdit")]
[assembly: System.Reflection.AssemblyDescription("A plugin mod for Kerbal Space Program")]
[assembly: System.Reflection.AssemblyCompany("Kerbaltek")]
[assembly: System.Reflection.AssemblyCopyright("Erickson Swift")]
[assembly: System.Reflection.AssemblyVersion("1.5.3.0")]

[KSPAddon(KSPAddon.Startup.SpaceCentre, true)] // Determines when plugin starts.

public class HyperEditModule : MonoBehaviour
{
    public HyperEditModule()
    {
        HyperEdit.Immortal.AddImmortal<HyperEdit.HyperEditBehaviour>();
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
        private ConfigNode _hyperEditConfig;
        private bool _useAppLauncherButton;
        private ApplicationLauncherButton _appLauncherButton;
        private Action _createCoreView;
        private Action _createLanderView;
        private bool _autoOpenLanderValue;

        public HyperEditBehaviour() // Constructor.  Don't init data here cuz Unity, do so in Awake();
        {
//            Extensions.Log("[" + this.GetInstanceID().ToString("X") + "][" + Time.time.ToString("0.0000") + "]: Constructor()");
        }

        public void Awake() // Called after scene (designated w/ KSPAddon) loads, but before Start().  Init data here.
        {
//            Extensions.Log("[" + this.GetInstanceID().ToString("X") + "][" + Time.time.ToString("0.0000") + "]: Awake()");
            View.Window.AreWindowsOpenChange += AreWindowsOpenChange;
            GameEvents.onGUIApplicationLauncherReady.Add(AddAppLauncher);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveAppLauncher);
            GameEvents.onLevelWasLoaded.Add(SceneUpdate);
        }

        public void Start() // Called after all mods are Awake().
        {
//            Extensions.Log("[" + this.GetInstanceID().ToString("X") + "][" + Time.time.ToString("0.0000") + "]: Start()");
            ReloadConfig();
        }

        private void CreateCoreView()
        {
            ReloadConfig();
            if (_createCoreView == null)
                _createCoreView = View.CoreView.Create(this);
            _createCoreView();
            if (_autoOpenLanderValue == true && !View.Window.GameObject.GetComponents<View.Window>().Any(w => w.Title == "Lander"))
                CreateLanderView();
        }

        private void CreateLanderView()
        {
            if (_createLanderView == null)
                _createLanderView = View.LanderView.Create();
            _createLanderView();
        }

        // SceneUpdate() fires only when the scene changes.
        private void SceneUpdate(GameScenes data)
        {
            ReloadConfig();
            if (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                if (_autoOpenLanderValue == true && !View.Window.GameObject.GetComponents<View.Window>().Any(w => w.Title == "Lander"))
                    CreateLanderView();
            } else {
                View.Window.CloseAll();
            }
        }

        // FixedUpdate() fires every physics time step.
        public void FixedUpdate() => Model.PlanetEditor.TryApplyFileDefaults();

        // Update() fires every frame.
        public void Update ()
		{
			RateLimitedLogger.Update ();
            if ((Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.RightAlt)) && Input.GetKeyDown (KeyCode.H)) {
                // Linuxgurugamer added this scene check to keep HyperEdit off in the editors.
                if (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                {
                    if (View.Window.GameObject.GetComponents<View.Window>().Any(w => w.Title == "HyperEdit")) {
                        if (_appLauncherButton == null)
                            View.Window.CloseAll();
                        else
                            _appLauncherButton.SetFalse();
					} else {
                        if (_appLauncherButton == null)
                            CreateCoreView();
                        else
                            _appLauncherButton.SetTrue();
                    }
				}
			}
        }

        private void ReloadConfig()
        {
            var hypereditCfg = IoExt.GetPath("hyperedit.cfg");
            if (System.IO.File.Exists(hypereditCfg))
            {
                _hyperEditConfig = ConfigNode.Load(hypereditCfg);
                _hyperEditConfig.name = "hyperedit";
            }
            else
                _hyperEditConfig = new ConfigNode("hyperedit");

            var launcherButtonValue = true;
            _hyperEditConfig.TryGetValue("UseAppLauncherButton", ref launcherButtonValue, bool.TryParse);
            UseAppLauncherButton = launcherButtonValue;

            _hyperEditConfig.TryGetValue("AutoOpenLander", ref _autoOpenLanderValue, bool.TryParse);
        }

        private void AreWindowsOpenChange(bool isOpen)
        {
            if (_appLauncherButton != null)
            {
                if (isOpen)
                    _appLauncherButton.SetTrue(false);
                else
                    _appLauncherButton.SetFalse(false);
            }
        }

        public bool UseAppLauncherButton
        {
            get { return _useAppLauncherButton; }
            set
            {
                if (_useAppLauncherButton == value)
                    return;
                _useAppLauncherButton = value;
                if (value)
                {
                    AddAppLauncher();
                }
                else
                {
                    RemoveAppLauncher();
                }
                _hyperEditConfig.SetValue("UseAppLauncherButton", value.ToString(), true);
                _hyperEditConfig.Save();
            }
        }

        private void AddAppLauncher()
        {
            if (_useAppLauncherButton == false)
                return;
            if (_appLauncherButton != null)
            {
                Extensions.Log(
                    "Not adding to ApplicationLauncher, button already exists (yet onGUIApplicationLauncherReady was called?)");
                return;
            }
            var applauncher = ApplicationLauncher.Instance;
            if (applauncher == null)
            {
                Extensions.Log("Cannot add to ApplicationLauncher, instance was null");
                return;
            }
            const ApplicationLauncher.AppScenes scenes =
                ApplicationLauncher.AppScenes.FLIGHT |
                ApplicationLauncher.AppScenes.MAPVIEW |
                ApplicationLauncher.AppScenes.TRACKSTATION;
            var tex = new Texture2D(38, 38, TextureFormat.RGBA32, false);

            for (var x = 0; x < tex.width; x++)
                for (var y = 0; y < tex.height; y++)
                    tex.SetPixel(x, y,
                        new Color(2 * (float)Math.Abs(x - tex.width / 2) / tex.width, 0.25f,
                            2 * (float)Math.Abs(y - tex.height / 2) / tex.height, 0));
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
            _appLauncherButton = applauncher.AddModApplication(
                CreateCoreView, // onTrue
                View.Window.CloseAll, // onFalse
                () => { }, // onHover
                () => { }, // onHoverOut
                () => { }, // onEnable
                () => { }, // onDisable
                scenes, // visibleInScenes
                tex // texture
            );
        }

        private void RemoveAppLauncher()
        {
            var applauncher = ApplicationLauncher.Instance;
            if (applauncher == null)
            {
                Extensions.Log("Cannot remove from ApplicationLauncher, instance was null");
                return;
            }
            if (_appLauncherButton == null)
                return;
            applauncher.RemoveModApplication(_appLauncherButton);
            _appLauncherButton = null;
        }

    // End of class.
    }

    public static class IoExt
    {
        private static readonly string RootDir = System.IO.Path.ChangeExtension(typeof(IoExt).Assembly.Location, null);

        static IoExt()
        {
            if (!System.IO.Directory.Exists(RootDir))
                System.IO.Directory.CreateDirectory(RootDir);
            Extensions.Log("Using \"" + RootDir + "\" as root config directory");
        }

        public static string GetPath(string path) => path == null ? RootDir : System.IO.Path.Combine(RootDir, path);

        public static void Save(this ConfigNode config) => config.Save(GetPath(config.name + ".cfg"));
    }

    public static class RateLimitedLogger
    {
        private const int MaxFrequency = 100; // measured in number of frames

        private class Countdown
        {
            public string LastMessage;
            public int FramesLeft;
            public bool NeedsPrint;

            public Countdown(string msg, int frames)
            {
                LastMessage = msg;
                FramesLeft = frames;
                NeedsPrint = false;
            }
        }

        private static readonly Dictionary<object, Countdown> Messages = new Dictionary<object, Countdown>();

        public static void Update()
        {
            List<object> toRemove = null;
            foreach (var kvp in Messages)
            {
                if (kvp.Value.FramesLeft == 0)
                {
                    if (kvp.Value.NeedsPrint)
                    {
                        kvp.Value.NeedsPrint = false;
                        kvp.Value.FramesLeft = MaxFrequency;
                        Extensions.Log(kvp.Value.LastMessage);
                    }
                    else
                    {
                        if (toRemove == null)
                            toRemove = new List<object>();
                        toRemove.Add(kvp.Key);
                    }
                }
                else
                {
                    kvp.Value.FramesLeft--;
                }
            }
            if (toRemove != null)
            {
                foreach (var key in toRemove)
                {
                    Messages.Remove(key);
                }
            }
        }

        public static void Log(object key, string message)
        {
            Countdown countdown;
            if (Messages.TryGetValue(key, out countdown))
            {
                countdown.NeedsPrint = true;
                countdown.LastMessage = message;
            }
            else
            {
                Extensions.Log(message);
                Messages[key] = new Countdown(message, MaxFrequency);
            }
        }
    }

    public static class Extensions
    {
		[ConditionalAttribute("DEBUG")]
        public static void Log(string message)
        {
            UnityEngine.Debug.Log("HyperEdit: " + message);
        }

        public static void TryGetValue<T>(this ConfigNode node, string key, ref T value, TryParse<T> tryParse)
        {
            var strvalue = node.GetValue(key);
            if (strvalue == null)
                return;
            if (tryParse == null)
            {
                // `T` better be `string`...
                value = (T) (object) strvalue;
                return;
            }
            T temp;
            if (tryParse(strvalue, out temp) == false)
                return;
            value = temp;
        }

        private static GUIStyle _pressedButton;

        public static GUIStyle PressedButton => _pressedButton ?? (_pressedButton = new GUIStyle(HighLogic.Skin.button)
        {
            normal = HighLogic.Skin.button.active,
            hover = HighLogic.Skin.button.active,
            active = HighLogic.Skin.button.normal
        });

        public static void RealCbUpdate(this CelestialBody body)
        {
            body.CBUpdate();
            try
            {
                body.resetTimeWarpLimits();
            }
            catch (NullReferenceException)
            {
                Log("resetTimeWarpLimits threw NRE " + (TimeWarp.fetch == null ? "as expected" : "unexpectedly"));
            }

            // CBUpdate doesn't update hillSphere
            // http://en.wikipedia.org/wiki/Hill_sphere
            var orbit = body.orbit;
            var cubedRoot = Math.Pow(body.Mass/orbit.referenceBody.Mass, 1.0/3.0);
            body.hillSphere = orbit.semiMajorAxis*(1.0 - orbit.eccentricity)*cubedRoot;

            // Nor sphereOfInfluence
            // http://en.wikipedia.org/wiki/Sphere_of_influence_(astrodynamics)
            body.sphereOfInfluence = orbit.semiMajorAxis*Math.Pow(body.Mass/orbit.referenceBody.Mass, 2.0/5.0);
        }

        public static void PrepVesselTeleport(this Vessel vessel)
        {
            if (vessel.Landed)
            {
                vessel.Landed = false;
                Log("Set ActiveVessel.Landed = false");
            }
            if (vessel.Splashed)
            {
                vessel.Splashed = false;
                Log("Set ActiveVessel.Splashed = false");
            }
            if (vessel.landedAt != string.Empty)
            {
                vessel.landedAt = string.Empty;
                Log("Set ActiveVessel.landedAt = \"\"");
            }
            var parts = vessel.parts;
            if (parts != null)
            {
                var killcount = 0;
                foreach (var part in parts.Where(part => part.Modules.OfType<LaunchClamp>().Any()).ToList())
                {
                    killcount++;
                    part.Die();
                }
                if (killcount != 0)
                {
                    Log($"Removed {killcount} launch clamps from {vessel.vesselName}");
                }
            }
        }

        public static double Soi(this CelestialBody body)
        {
            var radius = body.sphereOfInfluence*0.95;
            if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0 || radius > 200000000000)
                radius = 200000000000; // jool apo = 72,212,238,387
            return radius;
        }

        public static double Mod(this double x, double y)
        {
            var result = x%y;
            if (result < 0)
                result += y;
            return result;
        }

        public static string VesselToString(this Vessel vessel)
        {
            if (FlightGlobals.fetch != null && FlightGlobals.ActiveVessel == vessel)
                return "Active vessel";
            return vessel.vesselName;
        }

        public static string OrbitDriverToString(this OrbitDriver driver)
        {
            if (driver == null)
                return null;
            if (driver.celestialBody != null)
                return driver.celestialBody.bodyName;
            if (driver.vessel != null)
                return driver.vessel.VesselToString();
            if (!string.IsNullOrEmpty(driver.name))
                return driver.name;
            return "Unknown";
        }

        private static Dictionary<string, KeyCode> _keyCodeNames;

        public static Dictionary<string, KeyCode> KeyCodeNames
        {
            get
            {
                return _keyCodeNames ?? (_keyCodeNames =
                    Enum.GetNames(typeof(KeyCode))
                        .Distinct()
                        .ToDictionary(k => k, k => (KeyCode) Enum.Parse(typeof(KeyCode), k)));
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

        public static string KeyCodeToString(this KeyCode[] values)
        {
            return string.Join("-", values.Select(v => v.ToString()).ToArray());
        }

        public static string CbToString(this CelestialBody body)
        {
            return body.bodyName;
        }

        public static bool CbTryParse(string bodyName, out CelestialBody body)
        {
            body = FlightGlobals.Bodies == null ? null : FlightGlobals.Bodies.FirstOrDefault(cb => cb.name == bodyName);
            return body != null;
        }

        public static void ClearGuiFocus()
        {
            GUIUtility.keyboardControl = 0;
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
            string[] values = parseValue.Split(new[] {',', ' '}, StringSplitOptions.RemoveEmptyEntries);
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
