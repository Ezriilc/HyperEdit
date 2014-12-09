using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HyperEdit.Model
{
    public class Lander
    {
        private const string Filename = "landcoords.txt";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }

        public Lander()
        {
            Latitude = 0;
            Longitude = 0;
            Altitude = 50;
        }

        public bool Landing
        {
            get
            {
                if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                    return false;
                return FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>() != null;
            }
            set
            {
                if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                    return;
                var lander = FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>();
                if (value == (lander != null))
                    return;
                if (lander == null)
                {
                    lander = FlightGlobals.ActiveVessel.gameObject.AddComponent<LanderAttachment>();
                    lander.Latitude = Latitude;
                    lander.Longitude = Longitude;
                    lander.Altitude = Altitude;
                }
                else
                {
                    UnityEngine.Object.Destroy(lander);
                }
            }
        }

        struct LandingCoordinates
        {
            public string Name { get; set; }
            public double Lat { get; set; }
            public double Lon { get; set; }

            public LandingCoordinates(string name, double lat, double lon)
            {
                Name = name;
                Lat = lat;
                Lon = lon;
            }

            public LandingCoordinates(string value)
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

        private List<LandingCoordinates> SavedCoords
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

        public void Save()
        {
            View.WindowHelper.Prompt("Save as...", Save);
        }

        public void Save(string name)
        {
            var saved = SavedCoords;
            saved.Add(new LandingCoordinates(name, Latitude, Longitude));
            SavedCoords = saved;
        }

        public void Load(Action onLoad)
        {
            View.WindowHelper.Selector("Load...", SavedCoords, c => c.Name, c =>
            {
                Latitude = c.Lat;
                Longitude = c.Lon;
                onLoad();
            });
        }

        public void Delete()
        {
            var coords = SavedCoords;
            View.WindowHelper.Selector("Delete...", coords, c => c.Name, toDelete =>
            {
                coords.Remove(toDelete);
                SavedCoords = coords;
            });
        }

        public void SetToCurrent()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
                return;
            Longitude = FlightGlobals.ActiveVessel.longitude;
            Latitude = FlightGlobals.ActiveVessel.latitude;
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
}