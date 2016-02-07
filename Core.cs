﻿using System;
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
        private Action createCoreView = null;

		// Following is a minimal API added by Linuxgurugamer
		// This is used to call HyperEdit from other mods
		public static bool activeVesselOnlyAPI = false;
		//
		// This function shows Hyperedit.  If activeVesselOnly is true
		// then HyperEdit will only work on the active vessel
		// The function first calls the hideHyperEditAPI to be sure
		// that no other HyperEdit windows are open
		//
		public  void showHyperEditAPI(bool activeVesselOnly)
		{
			Extensions.Log("showHyperEdit");
			// First call the hideHyperEditAPI() to delete the old views, if they exist
			hideHyperEditAPI();
			CreateAPICoreView();
			_appLauncherButton.SetTrue();
			activeVesselOnlyAPI = activeVesselOnly;
		}
		//
		// This function will hide HyperEdit and all it's windows
		//
		public void hideHyperEditAPI(bool noop = false)
		{
			if (createCoreView != null) {
				View.Window.CloseAll ();
				createCoreView = null;
			}
			activeVesselOnlyAPI = false;
		}
		//
		// This is called by the API when showing HyperEdit as called from another mod
		// This will open and make available a subset of the HyperEdit windows
		//
		private void CreateAPICoreView()
		{
			Extensions.Log("CreateHolodeckCoreView");
			if (createCoreView == null)
			{
				createCoreView = View.CoreView.Create (this, true, false, true, false, false, false);
			}
			createCoreView();
		}

        private void CreateCoreView()
        {
			Extensions.Log("CreateCoreView");
            if (createCoreView == null)
            {
                createCoreView = View.CoreView.Create(this);
            }
            createCoreView();
        }

        public void Awake()
        {
            View.Window.AreWindowsOpenChange += AreWindowsOpenChange;
            GameEvents.onGUIApplicationLauncherReady.Add(AddAppLauncher);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(RemoveAppLauncher);
            ReloadConfig();
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
            {
                _hyperEditConfig = new ConfigNode("hyperedit");
            }

            var value = true;
            _hyperEditConfig.TryGetValue("UseAppLauncherButton", ref value, bool.TryParse);
            UseAppLauncherButton = value;
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
            get
            {
                return _useAppLauncherButton;
            }
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
                Extensions.Log("Not adding to ApplicationLauncher, button already exists (yet onGUIApplicationLauncherReady was called?)");
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
                    CreateCoreView();
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
                Extensions.Log("Cannot remove from ApplicationLauncher, instance was null");
                return;
            }
            if (_appLauncherButton == null)
            {
                return;
            }
            applauncher.RemoveModApplication(_appLauncherButton);
            _appLauncherButton = null;
        }

        public void FixedUpdate()
        {
            Model.PlanetEditor.TryApplyFileDefaults();
        }

        public void Update()
        {
            RateLimitedLogger.Update();
			// Linuxgurugamer added following to make sure HyperEdit is not visible in the editors.  Following the logic in the AddAppLauncher function above
			if (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.TRACKSTATION) {
				if ((Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.RightAlt)) && Input.GetKeyDown (KeyCode.H)) {
					if (View.Window.GameObject.GetComponents<View.Window> ().Any (w => w._title == "HyperEdit")) {
						if (_appLauncherButton == null)
							View.Window.CloseAll ();
						else
							_appLauncherButton.SetFalse ();
					} else {
						if (_appLauncherButton == null)
							CreateCoreView ();
						else
							_appLauncherButton.SetTrue ();
					}
				}
			} else {
				hideHyperEditAPI ();
			}
        }
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

        public static string GetPath(string path)
        {
            if (path == null)
                return RootDir;
            return System.IO.Path.Combine(RootDir, path);
        }

        public static bool Save(this ConfigNode config)
        {
            return config.Save(GetPath(config.name + ".cfg"));
        }
    }

    public static class RateLimitedLogger
    {
        private const int MaxFrequency = 100; // measured in number of frames

        class Countdown
        {
            public string lastMessage;
            public int framesLeft;
            public bool needsPrint;

            public Countdown(string msg, int frames)
            {
                lastMessage = msg;
                framesLeft = frames;
                needsPrint = false;
            }
        }

        private static readonly Dictionary<object, Countdown> messages = new Dictionary<object, Countdown>();

        public static void Update()
        {
            List<object> toRemove = null;
            foreach (var kvp in messages)
            {
                if (kvp.Value.framesLeft == 0)
                {
                    if (kvp.Value.needsPrint)
                    {
                        kvp.Value.needsPrint = false;
                        kvp.Value.framesLeft = MaxFrequency;
                        Extensions.Log(kvp.Value.lastMessage);
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
                    kvp.Value.framesLeft--;
                }
            }
            if (toRemove != null)
            {
                foreach (var key in toRemove)
                {
                    messages.Remove(key);
                }
            }
        }

        public static void Log(object key, string message)
        {
            Countdown countdown;
            if (messages.TryGetValue(key, out countdown))
            {
                countdown.needsPrint = true;
                countdown.lastMessage = message;
            }
            else
            {
                Extensions.Log(message);
                messages[key] = new Countdown(message, MaxFrequency);
            }
        }
    }

    public static class Extensions
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
            if (tryParse == null)
            {
                // `T` better be `string`...
                value = (T)(object)strvalue;
                return;
            }
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
            var cubedRoot = Math.Pow(body.Mass / orbit.referenceBody.Mass, 1.0 / 3.0);
            body.hillSphere = orbit.semiMajorAxis * (1.0 - orbit.eccentricity) * cubedRoot;

            // Nor sphereOfInfluence
            // http://en.wikipedia.org/wiki/Sphere_of_influence_(astrodynamics)
            body.sphereOfInfluence = orbit.semiMajorAxis * Math.Pow(body.Mass / orbit.referenceBody.Mass, 2.0 / 5.0);
        }

        public static void PrepVesselTeleport(this Vessel vessel)
        {
            if (vessel.Landed)
            {
                vessel.Landed = false;
                Extensions.Log("Set ActiveVessel.Landed = false");
            }
            if (vessel.Splashed)
            {
                vessel.Splashed = false;
                Extensions.Log("Set ActiveVessel.Splashed = false");
            }
            if (vessel.landedAt != string.Empty)
            {
                vessel.landedAt = string.Empty;
                Extensions.Log("Set ActiveVessel.landedAt = \"\"");
            }
            var parts = vessel.parts;
            if (parts != null)
            {
                int killcount = 0;
                foreach (var part in parts.Where(part => part.Modules.OfType<LaunchClamp>().Any()).ToList())
                {
                    killcount++;
                    part.Die();
                }
                if (killcount != 0)
                {
                    Extensions.Log(string.Format("Removed {0} launch clamps from {1}", killcount, vessel.vesselName));
                }
            }
        }

        public static double Soi(this CelestialBody body)
        {
            var radius = body.sphereOfInfluence * 0.95;
            if (double.IsNaN(radius) || double.IsInfinity(radius) || radius < 0 || radius > 200000000000)
                radius = 200000000000; // jool apo = 72,212,238,387
            return radius;
        }

        public static double Mod(this double x, double y)
        {
            var result = x % y;
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
