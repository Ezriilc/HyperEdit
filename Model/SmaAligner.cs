using System.Collections.Generic;
using System.Linq;

namespace HyperEdit.Model
{
    public class SmaAligner
    {
        public List<Vessel> VesselsToAlign { get; private set; }

        public List<Vessel> AvailableVessels { get { return FlightGlobals.fetch != null && FlightGlobals.Vessels != null ? FlightGlobals.Vessels : new List<Vessel>(); } }

        public SmaAligner()
        {
            VesselsToAlign = new List<Vessel>();
        }

        public void Align()
        {
            VesselsToAlign.RemoveAll(v => AvailableVessels.All(a => a.id != v.id));

            var averageSma = VesselsToAlign.Average(v => v.orbit.semiMajorAxis);
            foreach (var vessel in VesselsToAlign)
            {
                var orbit = vessel.orbit.Clone();
                orbit.semiMajorAxis = averageSma;
                vessel.SetOrbit(orbit);
            }
        }
    }
}
