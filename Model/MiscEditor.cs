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