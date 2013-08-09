//
// This file is part of the HyperEdit plugin for Kerbal Space Program, Copyright Erickson Swift, 2013.
// HyperEdit is licensed under the GPL, found in COPYING.txt.
// Currently supported by Team HyperEdit, and Ezriilc.
// Original HyperEdit concept and code by khyperia (no longer involved).
//

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HyperEdit
{
    public class MiscTools : Window
    {
        public MiscTools()
        {
            EnsureSingleton(this);
            Title = "Misc tools";
            WindowRect = new Rect(100, 200, 250, 100);
            Contents = new List<IWindowContent>
                {
                    new Button("Close", CloseWindow),
                    new Button("Refill ship resources", RefillResources),
                    new TextBox("Time", Planetarium.fetch == null ? "" : Planetarium.GetUniversalTime().ToSiString(), SetUniversalTime),
                    new Button("Align SMA", new SmaAligner().OpenWindow),
                    new Button("Destroy a vessel", DestroyVessel)
                };
        }

        private static void DestroyVessel()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.Vessels == null)
            {
                ErrorPopup.Error("Could not get the list of orbits (are you in the flight scene?)");
                return;
            }
            new Selector<Vessel>("Select vessel to destroy", FlightGlobals.Vessels, v => v.vesselName, DestroyVessel).OpenWindow();
        }

        private static void DestroyVessel(Vessel vessel)
        {
            vessel.Die();
        }

        private static void SetUniversalTime(string s)
        {
            if (Planetarium.fetch == null)
            {
                ErrorPopup.Error("Could not find a universe to set the time (are you in the flight scene?)");
                return;
            }
            double time;
            if (Si.TryParse(s, out time) == false)
            {
                ErrorPopup.Error("Time was not a number");
                return;
            }
            Planetarium.SetUniversalTime(time);
        }

        private void RefillResources()
        {
            if (this.ActiveVesselNullcheck())
                return;
            if (FlightGlobals.ActiveVessel.parts == null)
            {
                ErrorPopup.Error("Cound not find the parts on the active vessel");
                return;
            }
            foreach (var part in FlightGlobals.ActiveVessel.parts)
                foreach (PartResource resource in part.Resources)
                    part.TransferResource(resource.info.id, resource.maxAmount - resource.amount);
        }
    }
}