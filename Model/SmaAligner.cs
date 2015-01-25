using System.Collections.Generic;
using System.Linq;

namespace HyperEdit.Model
{
    public static class SmaAligner
    {
        public static List<Vessel> AvailableVessels { get { return FlightGlobals.fetch != null && FlightGlobals.Vessels != null ? FlightGlobals.Vessels : new List<Vessel>(); } }

        public static void Align(List<Vessel> vesselsToAlign)
        {
            vesselsToAlign.RemoveAll(v => AvailableVessels.All(a => a.id != v.id));

            var averageSma = vesselsToAlign.Average(v => v.orbit.semiMajorAxis);
            foreach (var vessel in vesselsToAlign)
            {
                var orbit = vessel.orbit.Clone();
                orbit.semiMajorAxis = averageSma;
                vessel.SetOrbit(orbit);
            }
        }
    }
}
