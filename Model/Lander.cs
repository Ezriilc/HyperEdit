using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HyperEdit.Model
{
    public static class DoLander
    {
        private const string OldFilename = "landcoords.txt";
        private const string FilenameNoExt = "landcoords";

        public static bool IsLanding()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                return false;
            return FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>() != null;
        }

        public static void ToggleLanding(double latitude, double longitude, double altitude, CelestialBody body, Action<double, double, CelestialBody> onManualEdit)
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
                lander.OnManualEdit = onManualEdit;
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

        private static IEnumerable<LandingCoordinates> DefaultSavedCoords
        {
            get
            {
                if (FlightGlobals.fetch == null || FlightGlobals.Bodies == null ||
                    Planetarium.fetch == null || Planetarium.fetch.Home == null)
                    return new List<LandingCoordinates>();
                var kerbin = Planetarium.fetch.Home;
                var minmus = FlightGlobals.Bodies.FirstOrDefault(b => b.bodyName == "Minmus");
                var list = new List<LandingCoordinates>
                {
                    new LandingCoordinates("Airstrip Island Runway", -1.5179, 288.032, kerbin),
                    new LandingCoordinates("Airstrip Island Beach - Wet", -1.498, -72.088, kerbin),
                    new LandingCoordinates("KSC Launch Pad", -0.097210087, 285.442335999, kerbin),
                    new LandingCoordinates("KSC Runway", -0.04862627, 285.2766345, kerbin),
                    new LandingCoordinates("KSC Beach - Wet", -0.04862627, -74.39, kerbin)
                };
                if (minmus != null)
                {
                    list.Add(new LandingCoordinates("Minmus Flats", 0.562859, 175.968846, minmus));
                }
                return list;
            }
        }

        private static List<LandingCoordinates> SavedCoords
        {
            get
            {
                var path = IoExt.GetPath(FilenameNoExt + ".cfg");
                var oldPath = IoExt.GetPath(OldFilename);
                IEnumerable<LandingCoordinates> query;
                if (System.IO.File.Exists(path))
                {
                    query = ConfigNode.Load(path).nodes.OfType<ConfigNode>().Select(c => new LandingCoordinates(c));
                }
                else if (System.IO.File.Exists(oldPath))
                {
                    query = System.IO.File.ReadAllLines(oldPath).Select(x => new LandingCoordinates(x)).Where(l => string.IsNullOrEmpty(l.Name) == false);
                }
                else
                {
                    query = new LandingCoordinates[0];
                }
                query = query.Union(DefaultSavedCoords);
                return query.ToList();
            }
            set
            {
                var cfg = new ConfigNode(FilenameNoExt);
                foreach (var coord in value)
                {
                    cfg.AddNode(coord.ToConfigNode());
                }
                cfg.Save();
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
            saved.RemoveAll(match => match.Name == name);
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

        struct LandingCoordinates : IEquatable<LandingCoordinates>
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
                if (split.Length < 3)
                {
                    Name = null;
                    Lat = 0;
                    Lon = 0;
                    Body = null;
                    return;
                }
                double dlat, dlon;
                CelestialBody body;
                if (double.TryParse(split[1], out dlat) && double.TryParse(split[2], out dlon))
                {
                    Name = split[0];
                    Lat = dlat;
                    Lon = dlon;
                    if (split.Length >= 4 && Extensions.CbTryParse(split[3], out body))
                    {
                        Body = body;
                    }
                    else
                    {
                        Body = Planetarium.fetch.Home;
                    }
                }
                else
                {
                    Name = null;
                    Lat = 0;
                    Lon = 0;
                    Body = null;
                }
            }

            public LandingCoordinates(ConfigNode node)
            {
                CelestialBody body = null;
                node.TryGetValue("body", ref body, Extensions.CbTryParse);
                Body = body;
                double temp = 0.0;
                node.TryGetValue("lat", ref temp, double.TryParse);
                Lat = temp;
                node.TryGetValue("lon", ref temp, double.TryParse);
                Lon = temp;
                string name = null;
                node.TryGetValue("name", ref name, null);
                Name = name;
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is LandingCoordinates ? Equals((LandingCoordinates)obj) : false;
            }

            public bool Equals(LandingCoordinates other)
            {
                return Name.Equals(other.Name);
            }

            public override string ToString()
            {
                return Name + "," + Lat + "," + Lon + "," + Body.CbToString();
            }

            public ConfigNode ToConfigNode()
            {
                var node = new ConfigNode("coordinate");
                node.AddValue("body", Body.CbToString());
                node.AddValue("lat", Lat);
                node.AddValue("lon", Lon);
                node.AddValue("name", Name);
                return node;
            }
        }
    }

    public class LanderAttachment : MonoBehaviour
    {
        public bool AlreadyTeleported { get; set; }
        public Action<double, double, CelestialBody> OnManualEdit { get; set; }
        public CelestialBody Body { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }

        private readonly object accelLogObject = new object();

        public void Update()
        {
            // 0.2 meters per frame
            var degrees = 0.2 / Body.Radius * (180 / Math.PI);
            var changed = false;
            if (GameSettings.TRANSLATE_UP.GetKey())
            {
                Latitude -= degrees;
                changed = true;
            }
            if (GameSettings.TRANSLATE_DOWN.GetKey())
            {
                Latitude += degrees;
                changed = true;
            }
            if (GameSettings.TRANSLATE_LEFT.GetKey())
            {
                Longitude -= degrees / Math.Cos(Latitude * (Math.PI / 180));
                changed = true;
            }
            if (GameSettings.TRANSLATE_RIGHT.GetKey())
            {
                Longitude += degrees / Math.Cos(Latitude * (Math.PI / 180));
                changed = true;
            }
            if (changed)
            {
                AlreadyTeleported = false;
                OnManualEdit(Latitude, Longitude, Body);
            }
        }

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
                    RateLimitedLogger.Log(accelLogObject,
                        string.Format("(Happening every frame) Soft-lander changed ship velocity this frame by vector {0},{1},{2} (mag {3})",
                        accel.x, accel.y, accel.z, accel.magnitude));
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
                if (TimeWarp.CurrentRateIndex != 0)
                {
                    TimeWarp.SetRate(0, true);
                    Extensions.Log("Set time warp to index 0");
                }
                // HoldVesselUnpack is in display frames, not physics frames

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
