using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HyperEdit.Model
{
    public static class DoLander
    {
        private const string Filename = "landcoords.txt";

        public static bool IsLanding()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                return false;
            return FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>() != null;
        }

        public static void ToggleLanding(double latitude, double longitude, double altitude)
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                return;
            var lander = FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>();
            if (lander == null)
            {
                lander = FlightGlobals.ActiveVessel.gameObject.AddComponent<LanderAttachment>();
                lander.Latitude = latitude;
                lander.Longitude = longitude;
                lander.Altitude = altitude;
            }
            else
            {
                UnityEngine.Object.Destroy(lander);
            }
        }

        public static void LandHere()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                return;
            var lander = FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>();
            if (lander == null)
            {
                lander = FlightGlobals.ActiveVessel.gameObject.AddComponent<LanderAttachment>();
                lander.AlreadyTeleported = true;
            }
        }

        private static List<LandingCoordinates> SavedCoords
        {
            get
            {
                return KSP.IO.File.Exists<HyperEditBehaviour>(Filename)
                    ? KSP.IO.File.ReadAllLines<HyperEditBehaviour>(Filename).Select(x => new LandingCoordinates(x)).Where(l => string.IsNullOrEmpty(l.Name) == false).ToList()
                        : new List<LandingCoordinates>();
            }
            set
            {
                KSP.IO.File.WriteAllText<HyperEditBehaviour>(string.Join(Environment.NewLine, value.Select(l => l.ToString()).ToArray()), Filename);
            }
        }

        public static void AddSavedCoords(double latitude, double longitude)
        {
            View.WindowHelper.Prompt("Save as...", s => AddSavedCoords(s, latitude, longitude));
        }

        private static void AddSavedCoords(string name, double latitude, double longitude)
        {
            var saved = SavedCoords;
            saved.Add(new LandingCoordinates(name, latitude, longitude));
            SavedCoords = saved;
        }

        public static void Load(Action<double, double> onLoad)
        {
            View.WindowHelper.Selector("Load...", SavedCoords, c => c.Name, c =>
                {
                    onLoad(c.Lat, c.Lon);
                });
        }

        public static void Delete()
        {
            var coords = SavedCoords;
            View.WindowHelper.Selector("Delete...", coords, c => c.Name, toDelete =>
                {
                    coords.Remove(toDelete);
                    SavedCoords = coords;
                });
        }

        public static void SetToCurrent(Action<double, double> onLoad)
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                return;
            onLoad(FlightGlobals.ActiveVessel.latitude, FlightGlobals.ActiveVessel.longitude);
        }

        public static void SetToLanded(Action<double, double> onLoad)
        {
            if (View.LanderView.LandingBeside == null || View.LanderView.LandingBeside == null)
                return;

            //work out Logitude + 50m
            double FiftyMOfLong = (360 * 40) / (View.LanderView.LandingBeside.orbit.referenceBody.Radius * 2 * Math.PI) ;
            onLoad(View.LanderView.LandingBeside.latitude, View.LanderView.LandingBeside.longitude + FiftyMOfLong);
        }

        struct LandingCoordinates
        {
            public string Name { get; set; }

            public double Lat { get; set; }

            public double Lon { get; set; }

            public LandingCoordinates(string name, double lat, double lon)
                : this()
            {
                Name = name;
                Lat = lat;
                Lon = lon;
            }

            public LandingCoordinates(string value)
                : this()
            {
                var split = value.Split(',');
                if (split.Length < 3)
                {
                    Name = null;
                    Lat = 0;
                    Lon = 0;
                }
                double dlat, dlon;
                if (double.TryParse(split[1], out dlat) && double.TryParse(split[2], out dlon))
                {
                    Name = split[0];
                    Lat = dlat;
                    Lon = dlon;
                }
                else
                {
                    Name = null;
                    Lat = 0;
                    Lon = 0;
                }
            }

            public override string ToString()
            {
                return Name + "," + Lat + "," + Lon;
            }
        }
    }

    public class LanderAttachment : MonoBehaviour
    {
        public bool AlreadyTeleported { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }

        public void FixedUpdate()
        {
            var vessel = GetComponent<Vessel>();
            if (vessel != FlightGlobals.ActiveVessel)
            {
                Destroy(this);
                return;
            }
            if (AlreadyTeleported)
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
                var pqs = vessel.mainBody.pqsController;
                if (pqs == null)
                {
                    Destroy(this);
                    return;
                }
                var alt = pqs.GetSurfaceHeight(
                    QuaternionD.AngleAxis(Longitude, Vector3d.down) *
                    QuaternionD.AngleAxis(Latitude, Vector3d.forward) * Vector3d.right) -
                    pqs.radius;
                alt = Math.Max(alt, 0); // Underwater!
                if (vessel.Landed)
                    vessel.Landed = false;
                else if (vessel.Splashed)
                    vessel.Splashed = false;
                foreach (var part in vessel.parts.Where(part => part.Modules.OfType<LaunchClamp>().Any()).ToList())
                    part.Die();
                TimeWarp.SetRate(0, true); // HoldVesselUnpack is in display frames, not physics frames

                var teleportPosition = vessel.mainBody.GetWorldSurfacePosition(Latitude, Longitude, alt + Altitude);
                var teleportVelocity = vessel.mainBody.getRFrmVel(teleportPosition);
                // convert from world space to "normal" space
                teleportPosition = (teleportPosition - vessel.GetWorldPos3D()).xzy + vessel.orbit.pos;
                teleportVelocity = (teleportVelocity - vessel.GetObtVelocity()).xzy + vessel.orbit.vel;
                // counter for the momentary fall when on rails (about one second)
                teleportVelocity += teleportPosition.normalized * (vessel.mainBody.gravParameter / teleportPosition.sqrMagnitude);

                var orbit = vessel.orbitDriver.orbit.Clone();
                orbit.UpdateFromStateVectors(teleportPosition, teleportVelocity, orbit.referenceBody, Planetarium.GetUniversalTime());
                vessel.SetOrbit(orbit);

                AlreadyTeleported = true;
            }
        }
    }
}