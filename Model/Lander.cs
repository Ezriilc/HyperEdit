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

        public static void ToggleLanding(double latitude, double longitude, double altitude, CelestialBody body,
            bool setRotation, Action<double, double, CelestialBody> onManualEdit)
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null || body == null)
                return;
			
			//Debug.Log ("HyperEdit.Model.ToggleLanding");
            var lander = FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>();
            if (lander == null)
            {
                lander = FlightGlobals.ActiveVessel.gameObject.AddComponent<LanderAttachment>();
				//Debug.Log ("ToggleLanding Latitude: " + latitude.ToString () + "   Longitude: " + longitude.ToString() + "   Altitude: " + altitude.ToString());
			
				if (latitude == 0.0f)
					latitude = 0.001;
				if (longitude == 0.0f)
					longitude = 0.001;
                lander.Latitude = latitude;
                lander.Longitude = longitude;

				lander.intermAltitude = body.Radius + body.atmosphereDepth + 10000d;
				                        
				lander.Altitude = altitude;
                lander.SetRotation = setRotation;
                lander.Body = body;
                lander.OnManualEdit = onManualEdit;
            }
            else
            {
                UnityEngine.Object.Destroy(lander);
            }
        }

        public static void LandHere(Action<double, double, CelestialBody> onManualEdit)
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                return;
            var vessel = FlightGlobals.ActiveVessel;
            var lander = vessel.GetComponent<LanderAttachment>();
            if (lander == null)
            {
				//Debug.Log ("LandHere Latitude: " + vessel.latitude.ToString () + "   Longitude: " + vessel.longitude.ToString());
                lander = vessel.gameObject.AddComponent<LanderAttachment>();
                lander.Latitude = vessel.latitude;
                lander.Longitude = vessel.longitude;
                lander.SetRotation = false;
                lander.Body = vessel.mainBody;
                lander.OnManualEdit = onManualEdit;
                lander.AlreadyTeleported = false;
                lander.SetAltitudeToCurrent();
            }
        }

        private static IEnumerable<LandingCoordinates> DefaultSavedCoords
        {
            get
            {
                var kerbin = Planetarium.fetch?.Home;
                var minmus = FlightGlobals.fetch?.bodies?.FirstOrDefault(b => b.bodyName == "Minmus");
                if (kerbin == null)
                {
                    return new List<LandingCoordinates>();
                }
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
                    query =
                        System.IO.File.ReadAllLines(oldPath)
                            .Select(x => new LandingCoordinates(x))
                            .Where(l => string.IsNullOrEmpty(l.Name) == false);
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
            View.WindowHelper.Selector("Load...", SavedCoords, c => c.Name, c => onLoad(c.Lat, c.Lon, c.Body));
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
            onLoad(FlightGlobals.ActiveVessel.latitude, FlightGlobals.ActiveVessel.longitude,
                FlightGlobals.ActiveVessel.mainBody);
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
            var fiftyMOfLong = (360*40)/(landingBeside.orbit.referenceBody.Radius*2*Math.PI);
            onLoad(landingBeside.latitude, landingBeside.longitude + fiftyMOfLong, landingBeside.mainBody);
        }

        private struct LandingCoordinates : IEquatable<LandingCoordinates>
        {
            public string Name { get; }
            public double Lat { get; }
            public double Lon { get; }
            public CelestialBody Body { get; }

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
                if (double.TryParse(split[1], out dlat) && double.TryParse(split[2], out dlon))
                {
                    Name = split[0];
                    Lat = dlat;
                    Lon = dlon;
                    CelestialBody body;
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
                var temp = 0.0;
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
                return obj is LandingCoordinates && Equals((LandingCoordinates) obj);
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
        public bool SetRotation { get; set; }

        private readonly object _accelLogObject = new object();
		private bool teleportedToLandingAlt = false;
		public double intermAltitude { get; set; }
		private double lastUpdate = 0;

        public void SetAltitudeToCurrent()
        {
            var pqs = Body.pqsController;
            if (pqs == null)
            {
                Destroy(this);
                return;
            }
            var alt = pqs.GetSurfaceHeight(
                QuaternionD.AngleAxis(Longitude, Vector3d.down)*
                QuaternionD.AngleAxis(Latitude, Vector3d.forward)*Vector3d.right) -
                      pqs.radius;
            alt = Math.Max(alt, 0); // Underwater!
            Altitude = GetComponent<Vessel>().altitude - alt;
        }

        public void Update()
        {
			//Debug.Log ("LanderAttachment.Update");
            // 0.2 meters per frame
            var degrees = 0.2/Body.Radius*(180/Math.PI);
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
                Longitude -= degrees/Math.Cos(Latitude*(Math.PI/180));
                changed = true;
            }
            if (GameSettings.TRANSLATE_RIGHT.GetKey())
            {
                Longitude += degrees/Math.Cos(Latitude*(Math.PI/180));
                changed = true;
            }
			if (Latitude == 0)
				Latitude = 0.0001;
			if (Longitude == 0)
				Longitude = 0.0001;
            if (changed)
            {
                AlreadyTeleported = false;
				teleportedToLandingAlt = false;
                OnManualEdit(Latitude, Longitude, Body);
            }
        }

        public void FixedUpdate()
        {
			//Debug.Log ("LanderAttachment.FixedUpdate");

            var vessel = GetComponent<Vessel>();
            if (vessel != FlightGlobals.ActiveVessel)
            {
                Destroy(this);
                return;
            }
            if (AlreadyTeleported)
            {
				//Debug.Log ("FixedUpdate: AlreadyTeleported");
                if (vessel.LandedOrSplashed)
                {
                    Destroy(this);
                }
                else
                {
                    var accel = (vessel.srf_velocity + vessel.upAxis)*-0.5;
                    vessel.ChangeWorldVelocity(accel);
                    RateLimitedLogger.Log(_accelLogObject,
                        $"(Happening every frame) Soft-lander changed ship velocity this frame by vector {accel.x},{accel.y},{accel.z} (mag {accel.magnitude})");
                }
            }
            else
            {
				//Debug.Log ("FixedUpdate: not AlreadyTeleported");
                var pqs = Body.pqsController;
                if (pqs == null)
                {
                    Destroy(this);
                    return;
                }
                var alt = pqs.GetSurfaceHeight(Body.GetRelSurfaceNVector(Latitude, Longitude)) - Body.Radius;
                alt = Math.Max(alt, 0); // Underwater!
                if (TimeWarp.CurrentRateIndex != 0)
                {
                    TimeWarp.SetRate(0, true);
                    Extensions.Log("Set time warp to index 0");
                }
                // HoldVesselUnpack is in display frames, not physics frames
				//Debug.Log("alt: " + alt.ToString() + "   Altitude: " + Altitude.ToString());
				//Debug.Log ("Latitude: " + Latitude.ToString () + "   Longitude: " + Longitude.ToString ());

				//var teleportPosition = Body.GetRelSurfacePosition(Latitude, Longitude, alt +Altitude);
				Vector3d teleportPosition;
				if (!teleportedToLandingAlt) {
					//Debug.Log("teleportedToLandingAlt == false, intermAltitude: " + intermAltitude.ToString() + "  Altitude: " + Altitude.ToString());
					if (intermAltitude > Altitude) {
						if (Planetarium.GetUniversalTime () - lastUpdate >= 0.5 ) {
							intermAltitude = intermAltitude / 10;
							//Debug.Log("Planetarium.GetUniversalTime (): " + Planetarium.GetUniversalTime ().ToString() + "   lastUpdate: " + lastUpdate.ToString());
							//Debug.Log("intermAltitude: " + intermAltitude.ToString());
							teleportPosition = Body.GetRelSurfacePosition(Latitude, Longitude, alt + intermAltitude);
							if (lastUpdate != 0)
								intermAltitude = Altitude;
							lastUpdate = Planetarium.GetUniversalTime();
							
						} else {
							//Debug.Log("teleportPositionAltitude (no time change): alt: " + alt.ToString() + "   intermAltitude: " + intermAltitude.ToString());
							teleportPosition = Body.GetRelSurfacePosition(Latitude, Longitude, alt + intermAltitude);
						}
					}
					else {
						//Debug.Log("teleportedToLandingAlt set to true");
						teleportedToLandingAlt = true;
						teleportPosition = Body.GetRelSurfacePosition(Latitude, Longitude, alt +Altitude);
					}
				}
				else
					teleportPosition = Body.GetRelSurfacePosition(Latitude, Longitude, alt +Altitude);
				
                var teleportVelocity = Vector3d.Cross(Body.angularVelocity, teleportPosition);
                //var teleportVelocity = Vector3d.Cross(Vector3d.down, teleportPosition.normalized)*
                //                       (Math.Cos(L atitude*(Math.PI/180))*teleportPosition.magnitude*
                //                        (Math.PI*2)/(Body.rotationPeriod));

                // convert from world space to orbit space
                teleportPosition = teleportPosition.xzy;
                teleportVelocity = teleportVelocity.xzy;
                // counter for the momentary fall when on rails (about one second)
                teleportVelocity += teleportPosition.normalized*(Body.gravParameter/teleportPosition.sqrMagnitude);

                Quaternion rotation;
                if (SetRotation)
                {
                    var from = Vector3d.up;
                    var to = teleportPosition.xzy.normalized;
                    rotation = Quaternion.FromToRotation(from, to);
                }
                else
                {
                    var oldUp = vessel.orbit.pos.xzy.normalized;
                    var newUp = teleportPosition.xzy.normalized;
                    rotation = Quaternion.FromToRotation(oldUp, newUp)*vessel.vesselTransform.rotation;
                }

                var orbit = vessel.orbitDriver.orbit.Clone();
                orbit.UpdateFromStateVectors(teleportPosition, teleportVelocity, Body, Planetarium.GetUniversalTime());
                vessel.SetOrbit(orbit);
                vessel.SetRotation(rotation);
				if (teleportedToLandingAlt)
	                AlreadyTeleported = true;
            }
        }
    }
}