//
// This file is part of the HyperEdit plugin for Kerbal Space Program, Copyright Erickson Swift, 2013.
// HyperEdit is licensed under the GPL, found in COPYING.txt.
// Currently supported by Team HyperEdit, and Ezriilc.
// Original HyperEdit concept and code by khyperia (no longer involved).
//

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace HyperEdit
{
    public class SmaAligner : Window
    {
        private readonly List<Guid> _selectedVessels = new List<Guid>();

        public SmaAligner()
        {
            EnsureSingleton(this);
            Title = "Semi-Major axis aligner";
            WindowRect = new Rect(100, 200, 300, 400);
            Contents = new List<IWindowContent>
                {
                    new Scroller(new IWindowContent[]{new CustomDisplay(VesselList)}),
                    new Button("Align semi-major axises", Align),
                    new Button("Cancel", CloseSma)
                };
        }

        private void VesselList()
        {
            if (FlightGlobals.fetch == null)
            {
                CloseSma();
                return;
            }
            foreach (var vessel in FlightGlobals.Vessels)
            {
                var contains = _selectedVessels.Contains(vessel.id);
                if (contains != GUILayout.Toggle(contains, vessel.vesselName))
                    if (contains)
                        _selectedVessels.Remove(vessel.id);
                    else
                        _selectedVessels.Add(vessel.id);
            }
            _selectedVessels.RemoveAll(g => FlightGlobals.Vessels.All(v => v.id != g));
        }

        private void Align()
        {
            if (FlightGlobals.fetch == null)
            {
                CloseSma();
                return;
            }
            var vessels = FlightGlobals.Vessels.Where(v => _selectedVessels.Contains(v.id)).ToArray();
            if (vessels.Length == 0)
            {
                ErrorPopup.Error("No vessels selected");
                return;
            }
            var averageSma = vessels.Average(v => v.orbit.semiMajorAxis);
            foreach (var vessel in vessels)
            {
                var orbit = vessel.orbit.Clone();
                orbit.semiMajorAxis = averageSma;
                vessel.orbit.Set(orbit);
            }
        }

        private void CloseSma()
        {
            _selectedVessels.Clear();
            CloseWindow();
        }
    }
}