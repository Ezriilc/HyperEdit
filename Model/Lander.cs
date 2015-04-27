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

        public static void ToggleLanding(double latitude, double longitude, double altitude, CelestialBody body)
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null || body == null)
                return;
            var lander = FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>();
            if (lander == null)
            {
                lander = FlightGlobals.ActiveVessel.gameObject.AddComponent<LanderAttachment>();
                lander.Latitude = latitude;
                lander.Longitude = longitude;
                lander.Altitude = altitude;
                lander.Body = body;
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
                var path = IoExt.GetPath(Filename);
                return System.IO.File.Exists(path)
                    ? System.IO.File.ReadAllLines(path).Select(x => new LandingCoordinates(x)).Where(l => string.IsNullOrEmpty(l.Name) == false).ToList()
                        : new List<LandingCoordinates>();
            }
            set
            {
                var path = IoExt.GetPath(Filename);
                System.IO.File.WriteAllText(string.Join(Environment.NewLine, value.Select(l => l.ToString()).ToArray()), path);
            }
        }

        public static void AddSavedCoords(double latitude, double longitude, CelestialBody body)
        {
            if (body == null)
                return;
            View.WindowHelper.Prompt("Save as...", s => AddSavedCoords(s, latitude, longitude, body));
        }

        private static void AddSavedCoords(string name, double latitude, double longitude, CelestialBody body)
        {
            var saved = SavedCoords;
            saved.Add(new LandingCoordinates(name, latitude, longitude, body));
            SavedCoords = saved;
        }

        public static void Load(Action<double, double, CelestialBody> onLoad)
        {
            View.WindowHelper.Selector("Load...", SavedCoords, c => c.Name, c =>
                {
                    onLoad(c.Lat, c.Lon, c.Body);
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

        public static void SetToCurrent(Action<double, double, CelestialBody> onLoad)
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                return;
            onLoad(FlightGlobals.ActiveVessel.latitude, FlightGlobals.ActiveVessel.longitude, FlightGlobals.ActiveVessel.mainBody);
        }

        public static IEnumerable<Vessel> LandedVessels()
        {
            return FlightGlobals.fetch == null ? null : FlightGlobals.Vessels.Where(v => v.Landed);
        }

        public static void SetToLanded(Action<double, double, CelestialBody> onLoad, Vessel landingBeside)
        {
            if (landingBeside == null)
                return;

            //work out Logitude + 50m
            double FiftyMOfLong = (360 * 40) / (landingBeside.orbit.referenceBody.Radius * 2 * Math.PI);
            onLoad(landingBeside.latitude, landingBeside.longitude + FiftyMOfLong, landingBeside.mainBody);
        }

        struct LandingCoordinates
        {
            public string Name { get; set; }

            public double Lat { get; set; }

            public double Lon { get; set; }
            public CelestialBody Body { get; set; }

            public LandingCoordinates(string name, double lat, double lon, CelestialBody body)
                : this()
            {
                Name = name;
                Lat = lat;
                Lon = lon;
                Body = body;
            }

            public LandingCoordinates(string value)
                : this()
            {
                var split = value.Split(',');
                if (split.Length < 4)
                {
                    Name = null;
                    Lat = 0;
                    Lon = 0;
                    Body = null;
                    return;
                }
                double dlat, dlon;
                CelestialBody body;
                if (double.TryParse(split[1], out dlat) && double.TryParse(split[2], out dlon) && Extensions.CbTryParse(split[3], out body))
                {
                    Name = split[0];
                    Lat = dlat;
                    Lon = dlon;
                    Body = body;
                }
                else
                {
                    Name = null;
                    Lat = 0;
                    Lon = 0;
                    Body = null;
                }
            }

            public override string ToString()
            {
                return Name + "," + Lat + "," + Lon + "," + Body.CbToString();
            }
        }
    }

    public class LanderAttachment : MonoBehaviour
    {
        public bool AlreadyTeleported { get; set; }
        public CelestialBody Body { get; set; }
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
                var pqs = Body.pqsController;
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

                var teleportPosition = Body.GetRelSurfacePosition(Latitude, Longitude, alt + Altitude);
                var teleportVelocity = Body.getRFrmVel(teleportPosition + Body.position);
                // convert from world space to orbit space
                teleportPosition = teleportPosition.xzy;
                teleportVelocity = teleportVelocity.xzy;
                // counter for the momentary fall when on rails (about one second)
                teleportVelocity += teleportPosition.normalized * (Body.gravParameter / teleportPosition.sqrMagnitude);

                var orbit = vessel.orbitDriver.orbit.Clone();
                orbit.UpdateFromStateVectors(teleportPosition, teleportVelocity, Body, Planetarium.GetUniversalTime());
                vessel.SetOrbit(orbit);

                AlreadyTeleported = true;
            }
        }
    }
}