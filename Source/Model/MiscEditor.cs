using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HyperEdit.Model
{
    public static class MiscEditor
    {
        public static void DestroyVessel()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.Vessels == null)
                View.WindowHelper.Error("Could not get list of vessels");
            else
                View.WindowHelper.Selector("Destroy...", FlightGlobals.Vessels, v => v.vesselName, v => v.Die());
        }

        public static double UniversalTime
        {
            get { return Planetarium.GetUniversalTime(); }
            set { Planetarium.SetUniversalTime(value); Extensions.Log("Set Planetarium.UniversalTime to " + value); }
        }

        // time intervals in seconds
        static readonly int Hour = 3600;
        static readonly int Day = 6 * Hour;
        static readonly int Year = 426 * Day;

        public static double IncrementYear(double multiplier) { UniversalTime += Year * multiplier; return UniversalTime; }
        public static double DecrementYear(double multiplier) { UniversalTime -= Year * multiplier; return UniversalTime; }
        public static double IncrementDay(double multiplier) { UniversalTime += Day * multiplier; return UniversalTime; }
        public static double DecrementDay(double multiplier) { UniversalTime -= Day * multiplier; return UniversalTime; }
        public static double IncrementHour(double multiplier) { UniversalTime += Hour * multiplier; return UniversalTime; }
        public static double DecrementHour(double multiplier) { UniversalTime -= Hour * multiplier; return UniversalTime; }

        public static void AlignSemiMajorAxis()
        {
            View.SmaAlignerView.Create();
        }

        public static void RefillVesselResources()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                return;
            RefillVesselResources(FlightGlobals.ActiveVessel);
        }

        public static IEnumerable<KeyValuePair<string, double>> GetResources()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                return new KeyValuePair<string, double>[0];
            return GetResources(FlightGlobals.ActiveVessel);
        }

        public static IEnumerable<KeyValuePair<string, double>> GetResources(Vessel vessel)
        {
            if (vessel.parts == null)
                return new KeyValuePair<string, double>[0];
            return vessel.parts
                .SelectMany(part => part.Resources.Cast<PartResource>())
                .GroupBy(p => p.resourceName)
                .Select(g => new KeyValuePair<string, double>(g.Key, g.Sum(x => x.amount) / g.Sum(x => x.maxAmount)));
        }

        public static void SetResource(string key, double value)
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                return;
            SetResource(FlightGlobals.ActiveVessel, key, value);
        }

        private static readonly object SetResourceLogObject = new object();
        private static void SetResource(Vessel vessel, string key, double value)
        {
            if (vessel.parts == null)
                return;
            foreach (var part in vessel.parts)
            {
                //foreach(PartResource resource in part.Resources)
                int resourceCount = part.Resources.Count;
                for(int i = 0; i < resourceCount; ++i) {
                    PartResource resource = part.Resources[i];
                   if (resource.resourceName == key)
                    {
                        part.TransferResource(resource.info.id, resource.maxAmount * value - resource.amount);
                        RateLimitedLogger.Log(SetResourceLogObject,
                            $"Set part \"{part.partName}\"'s resource \"{resource.resourceName}\" to {value*100}% by requesting {resource.maxAmount*value - resource.amount} from it");
                    }
                }
            }
        }

        public static void RefillVesselResources(Vessel vessel)
        {
            if (vessel.parts == null)
                return;
            foreach (var part in vessel.parts)
            {
                //foreach(PartResource resource in part.Resources)
                int resourceCount = part.Resources.Count;
                for(int i = 0; i < resourceCount; ++i) {
                    PartResource resource = part.Resources[i];

                    part.TransferResource(resource.info.id, resource.maxAmount - resource.amount);
                    Extensions.Log(
                        $"Refilled part \"{part.partName}\"'s resource \"{resource.resourceName}\" by requesting {resource.maxAmount - resource.amount} from it");
                }
            }
        }

        public static KeyCode[] BoostButtonKey
        {
            get {
              return BoostListener.Fetch.Keys;
            }
            set {
              BoostListener.Fetch.Keys = value;
              //Save value to config file
            }
        }

        public static double BoostButtonSpeed
        {
            get { return BoostListener.Fetch.Speed; }
            set { BoostListener.Fetch.Speed = value; }
        }
    }

    public class BoostListener : MonoBehaviour
    {
        private static BoostListener _fetch;

        public static BoostListener Fetch
        {
            get
            {
                if (_fetch == null)
                {
                    var go = new GameObject("HyperEditBoostListener");
                    DontDestroyOnLoad(go);
                    _fetch = go.AddComponent<BoostListener>();
                }
                return _fetch;
            }
        }

        private bool _doBoost;
        private readonly object _boostLogObject = new object();

        public KeyCode[] Keys { get; set; } = { KeyCode.RightControl, KeyCode.B };

        public double Speed { get; set; }

        public void Update()
        {
            _doBoost = Keys.Length > 0 && Keys.All(Input.GetKey);
        }

        public void FixedUpdate()
        {
            if (_doBoost == false)
                return;
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
            {
                _doBoost = false;
                return;
            }
            var vessel = FlightGlobals.ActiveVessel;
            var toAdd = vessel.transform.up;
            toAdd *= (float)Speed;
            vessel.ChangeWorldVelocity(toAdd);
            RateLimitedLogger.Log(_boostLogObject,
                $"Booster changed vessel's velocity by {toAdd.x},{toAdd.y},{toAdd.z} (mag {toAdd.magnitude})");
        }
    }
}
