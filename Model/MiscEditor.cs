using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HyperEdit.Model
{
    public class MiscEditor
    {
        public void DestroyVessel()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.Vessels == null)
                Extentions.ErrorPopup("Could not get list of vessels");
            else
                View.WindowHelper.Selector("Destroy...", FlightGlobals.Vessels, v => v.name, v => v.Die());
        }

        public double UniversalTime
        {
            get { return Planetarium.GetUniversalTime(); }
            set { Planetarium.SetUniversalTime(value); }
        }

        public void AlignSemiMajorAxis()
        {
            View.View.CreateView(new SmaAligner());
        }

        public void RefillVesselResources()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                return;
            RefillVesselResources(FlightGlobals.ActiveVessel);
        }

        public IEnumerable<KeyValuePair<string, double>> GetResources()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                return new KeyValuePair<string, double>[0];
            return GetResources(FlightGlobals.ActiveVessel);
        }

        public IEnumerable<KeyValuePair<string, double>> GetResources(Vessel vessel)
        {
            if (vessel.parts == null)
                return new KeyValuePair<string, double>[0];
            return vessel.parts
                .SelectMany(part => part.Resources.Cast<PartResource>())
                .GroupBy(p => p.resourceName)
                .Select(g => new KeyValuePair<string, double>(g.Key, g.Sum(x => x.amount) / g.Sum(x => x.maxAmount)));
        }

        public void SetResource(string key, double value)
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                return;
            SetResource(FlightGlobals.ActiveVessel, key, value);
        }

        private void SetResource(Vessel vessel, string key, double value)
        {
            if (vessel.parts == null)
                return;
            foreach (var part in vessel.parts)
                foreach (PartResource resource in part.Resources)
                    if (resource.resourceName == key)
                        part.TransferResource(resource.info.id, resource.maxAmount * value - resource.amount);
        }

        public void RefillVesselResources(Vessel vessel)
        {
            if (vessel.parts == null)
                return;
            foreach (var part in vessel.parts)
                foreach (PartResource resource in part.Resources)
                    part.TransferResource(resource.info.id, resource.maxAmount - resource.amount);
        }

        public KeyCode[] BoostButtonKey
        {
            get { return BoostListener.Fetch.Keys; }
            set { BoostListener.Fetch.Keys = value; }
        }

        public double BoostButtonSpeed
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
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    _fetch = go.AddComponent<BoostListener>();
                }
                return _fetch;
            }
        }

        private bool _doBoost = false;

        KeyCode[] _keys = new[] { KeyCode.LeftControl, KeyCode.B };

        public KeyCode[] Keys
        {
            get { return _keys; }
            set { _keys = value; }
        }

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
        }
    }
}