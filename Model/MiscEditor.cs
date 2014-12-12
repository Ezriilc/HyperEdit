using System;
using System.Collections.Generic;
using System.Linq;

namespace HyperEdit.Model
{
    public class MiscEditor
    {
        public void DestroyVessel()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.Vessels == null)
                View.WindowHelper.Error("Could not get list of vessels");
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
    }
}