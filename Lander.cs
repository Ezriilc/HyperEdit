//
// This file is part of the HyperEdit plugin for Kerbal Space Program, Copyright Erickson Swift, 2013.
// HyperEdit is licensed under the GPL, found in COPYING.txt.
// Currently supported by Team HyperEdit, and Ezriilc.
// Original HyperEdit concept and code by khyperia (no longer involved).
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HyperEdit
{
    public class Lander : Window
    {
        private const string Filename = "landcoords.txt";

        public Lander()
        {
            EnsureSingleton(this);
            Title = "Ship Lander";
            WindowRect = new Rect(100, 200, 150, 5);
            Contents = new List<IWindowContent>
                {
                    new Button("Close", CloseWindow),
                    new TextBox("Lat", "0"),
                    new TextBox("Lon", "0"),
                    new TextBox("Alt", "50"),
                    new Button("Land/Drop", LandAtTarget),
                    new Button("Save", SaveCoords),
                    new Button("Load", LoadCoords),
                    new Button("Delete", DeleteCoords),
                    new Button("Set To Current", SetCurrent)
                };
        }

        private void SetCurrent()
        {
            if (this.ActiveVesselNullcheck())
                return;
            SetField<TextBox, string>("Lat", FlightGlobals.ActiveVessel.latitude.ToString());
            SetField<TextBox, string>("Lon", FlightGlobals.ActiveVessel.longitude.ToString());
        }

        private static void DeleteCoords()
        {
            new Selector<string[]>("Delete Location", LandingCoords, l => l[0], OnDeleteCoords).OpenWindow();
        }

        private static void OnDeleteCoords(string[] line)
        {
            var coords = LandingCoords;
            coords.RemoveAll(l => l[0] == line[0]);
            LandingCoords = coords;
        }

        private void LoadCoords()
        {
            new Selector<string[]>("Load Location", LandingCoords, l => l[0], OnLoadCoords).OpenWindow();
        }

        private void OnLoadCoords(string[] line)
        {
            SetField<TextBox, string>("Lat", line[1]);
            SetField<TextBox, string>("Lon", line[2]);
        }

        private void SaveCoords()
        {
            new Prompt("Save Location as...", SaveCoordsNamed).OpenWindow();
        }

        private void SaveCoordsNamed(string s)
        {
            var line = new[] { s, FindField<TextBox, string>("Lat"), FindField<TextBox, string>("Lon") };
            var presaved = LandingCoords;
            var preexist = presaved.FirstOrDefault(l => l.Length > 0 && l[0].ToLower() == s.ToLower());
            if (preexist == null)
                presaved.Add(line);
            else
                for (var i = 0; i < line.Length; i++)
                    preexist[i] = line[i];
            LandingCoords = presaved;
        }

        private static List<string[]> LandingCoords
        {
            get
            {
                return KSP.IO.File.Exists<HyperEditBehaviour>(Filename)
                           ? KSP.IO.File.ReadAllLines<HyperEditBehaviour>(Filename).Select(l => l.Split(',').Select(s => s.Trim()).ToArray()).Where(l => l.Length == 3).ToList()
                           : new List<string[]>();
            }
            set
            {
                KSP.IO.File.WriteAllLines<HyperEditBehaviour>(value.Select(l => l.Aggregate(",")).ToArray(), Filename);
            }
        }

        private void LandAtTarget()
        {
            if (this.ActiveVesselNullcheck())
                return;
            double latitude, longitude, altitude;
            if (double.TryParse(FindField<TextBox, string>("Lat"), out latitude) == false ||
                double.TryParse(FindField<TextBox, string>("Lon"), out longitude) == false ||
                double.TryParse(FindField<TextBox, string>("Alt"), out altitude) == false)
            {
                ErrorPopup.Error("Landing parameter was not a number");
                return;
            }
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
            {
                ErrorPopup.Error("Could not find active vessel");
                return;
            }
            var lander = FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>();
            if (lander == null)
            {
                lander = FlightGlobals.ActiveVessel.gameObject.AddComponent<LanderAttachment>();
                lander.Latitude = latitude;
                lander.Longitude = longitude;
                lander.Altitude = altitude;
            }
            else
                UnityEngine.Object.Destroy(lander);
        }
    }

    public class LanderAttachment : MonoBehaviour
    {
        private bool _alreadyTeleported;
        public double Latitude;
        public double Longitude;
        public double Altitude;

        public void FixedUpdate()
        {
            var vessel = GetComponent<Vessel>();
            if (vessel != FlightGlobals.ActiveVessel)
            {
                Destroy(this);
                return;
            }
            if (_alreadyTeleported)
            {
                if (vessel.LandedOrSplashed)
                {
                    Destroy(this);
                }
                else
                {
                    var accel = (vessel.srf_velocity + vessel.upAxis) * -0.5;
                    vessel.ChangeWorldVelocity(accel);
                }
            }
            else
            {
                var alt = vessel.mainBody.pqsController.GetSurfaceHeight(
                    QuaternionD.AngleAxis(Longitude, Vector3d.down) *
                    QuaternionD.AngleAxis(Latitude, Vector3d.forward) * Vector3d.right) -
                          vessel.mainBody.pqsController.radius;
                alt = Math.Max(alt, 0); // Underwater!
                var diff = vessel.mainBody.GetWorldSurfacePosition(Latitude, Longitude, alt + Altitude) - vessel.GetWorldPos3D();
                if (vessel.Landed)
                    vessel.Landed = false;
                else if (vessel.Splashed)
                    vessel.Splashed = false;
                foreach (var part in vessel.parts.Where(part => part.Modules.OfType<LaunchClamp>().Any()).ToList())
                    part.Die();
                HyperEditBehaviour.Krakensbane.Teleport(diff);
                vessel.ChangeWorldVelocity(-vessel.obt_velocity);
                _alreadyTeleported = true;
            }
        }
    }
}
