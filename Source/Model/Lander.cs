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
    private const string RecentEntryName = "Most Recent";

    public static bool IsLanding()
    {
      if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
      {
        return false;
      }

      return FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>() != null;
    }

    public static void ToggleLanding(double latitude, double longitude, double altitude, CelestialBody body,
        bool setRotation, Action<double, double, double, CelestialBody> onManualEdit)
    {
      if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null || body == null)
      {
        return;
      }

      Extensions.Log("HyperEdit.Model.ToggleLanding");
      Extensions.Log("-----------------------------");

      var lander = FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>();
      if (lander == null)
      {
        Model.DoLander.AddLastCoords(latitude, longitude, altitude, body);
        lander = FlightGlobals.ActiveVessel.gameObject.AddComponent<LanderAttachment>();

        Extensions.Log("Latitude : " + latitude.ToString() );
        Extensions.Log("Longitude: " + longitude.ToString() );
        Extensions.Log("Altitude : " + altitude.ToString() );
        Extensions.Log("Body     : " + body.ToString());
        Extensions.Log("B-Radius : " + body.Radius.ToString());
        Extensions.Log("B-Depth  : " + body.atmosphereDepth.ToString());
        
        if (latitude == 0.0f)
        {
          latitude = 0.001;
        }

        if (longitude == 0.0f)
        {
          longitude = 0.001;
        }

        lander.Latitude = latitude;
        lander.Longitude = longitude;

        lander.InterimAltitude = body.Radius + body.atmosphereDepth + 10000d;

        lander.Altitude = altitude;
        lander.SetRotation = setRotation;
        lander.Body = body;
        lander.OnManualEdit = onManualEdit;

        Extensions.Log("NEW:");
        Extensions.Log("lander = " + lander.ToString());
        Extensions.Log("intermAltitude = " + lander.InterimAltitude);

      }
      else
      {
        Extensions.Log("Unity destroy lander");
        UnityEngine.Object.Destroy(lander);
      }
    }

    public static void LandHere(Action<double, double, double, CelestialBody> onManualEdit)
    {
      if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
      {
        return;
      }

      var vessel = FlightGlobals.ActiveVessel;
      var lander = vessel.GetComponent<LanderAttachment>();
      if (lander == null)
      {
        Extensions.Log("LandHere");
        Extensions.Log("--------");

        Extensions.Log("Vessel Latitude : " + vessel.latitude.ToString() );
        Extensions.Log("Vessel Longitude: " + vessel.longitude.ToString() );
        Extensions.Log("Vessel Altitude : " + vessel.altitude.ToString());

        lander = vessel.gameObject.AddComponent<LanderAttachment>();
        lander.Latitude = vessel.latitude;
        lander.Longitude = vessel.longitude;
        lander.SetRotation = false;
        lander.Body = vessel.mainBody;
        lander.OnManualEdit = onManualEdit;
        lander.AlreadyTeleported = false;
        lander.SetAltitudeToCurrent();

        Extensions.Log("UPDATE: lander:");
        Extensions.Log("lander = " + lander);

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
                    new LandingCoordinates("KSC Launch Pad", -0.0972, 285.4423, 20, kerbin),
                    new LandingCoordinates("KSC Runway", -0.0486, 285.2823, 20, kerbin),
                    new LandingCoordinates("KSC Beach - Wet", -0.04862627, 285.666, 20, kerbin),
                    new LandingCoordinates("Airstrip Island Runway", -1.518, 288.1, 35, kerbin),
                    new LandingCoordinates("Airstrip Island Beach - Wet", -1.518, 287.9503, 20, kerbin)
                };
        if (minmus != null)
        {
          list.Add(new LandingCoordinates("Minmus Flats", 0.562859, 175.968846, 20, minmus));
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

    public static void AddLastCoords(double latitude, double longitude, double altitude, CelestialBody body)
    {
      if (body == null)
      {
        return;
      }

      AddSavedCoords(RecentEntryName, latitude, longitude, altitude, body);
    }

    public static void AddSavedCoords(double latitude, double longitude, double altitude, CelestialBody body)
    {
      if (body == null)
      {
        return;
      }

      View.WindowHelper.Prompt("Save as...", s => AddSavedCoords(s, latitude, longitude, altitude, body));
    }

    private static void AddSavedCoords(string name, double latitude, double longitude, double altitude, CelestialBody body)
    {
      var saved = SavedCoords;
      saved.RemoveAll(match => match.Name == name);
      saved.Add(new LandingCoordinates(name, latitude, longitude, altitude, body));
      SavedCoords = saved;
    }

    public static void LoadLast(Action<double, double, double, CelestialBody> onLoad)
    {
      var lastC = SavedCoords.Find(c => c.Name == RecentEntryName);
      onLoad(lastC.Lat, lastC.Lon, lastC.Alt, lastC.Body);
    }

    public static void Load(Action<double, double, double, CelestialBody> onLoad)
    {
      View.WindowHelper.Selector("Load...", SavedCoords, c => c.Name, c => onLoad(c.Lat, c.Lon, c.Alt, c.Body));
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

    public static void SetToCurrent(Action<double, double, double, CelestialBody> onLoad)
    {
      if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null)
      {
        return;
      }

      onLoad(FlightGlobals.ActiveVessel.latitude, FlightGlobals.ActiveVessel.longitude, FlightGlobals.ActiveVessel.altitude,
          FlightGlobals.ActiveVessel.mainBody);
    }

    public static IEnumerable<Vessel> LandedVessels()
    {
      return FlightGlobals.fetch == null ? null : FlightGlobals.Vessels.Where(v => v.Landed);
    }

    public static void SetToLanded(Action<double, double, double, CelestialBody> onLoad, Vessel landingBeside)
    {
      if (landingBeside == null)
      {
        return;
      }

      //work out Longitude + 50m
      var fiftyMOfLong = (360 * 40) / (landingBeside.orbit.referenceBody.Radius * 2 * Math.PI);

      Extensions.Log("SetToLanded:: fiftyMOfLong=" + fiftyMOfLong);
      Extensions.Log("landingBeside: " + landingBeside);

      onLoad(landingBeside.latitude, landingBeside.longitude + fiftyMOfLong, landingBeside.altitude, landingBeside.mainBody);
    }

    private struct LandingCoordinates : IEquatable<LandingCoordinates>
    {
      public string Name { get; }
      public double Lat { get; }
      public double Lon { get; }
      public double Alt { get; }
      public CelestialBody Body { get; }

      public LandingCoordinates(string name, double lat, double lon, double alt, CelestialBody body)
          : this()
      {
        Name = name;
        Lat = lat;
        Lon = lon;
        Alt = alt;
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
          Alt = 20;
          Body = null;
          return;
        }
        double dlat, dlon, dalt;
        if (double.TryParse(split[1], out dlat) && double.TryParse(split[2], out dlon) && double.TryParse(split[2], out dalt))
        {
          Name = split[0];
          Lat = dlat;
          Lon = dlon;
          Alt = dalt;
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
          Alt = 20;
          Body = null;
        }
      }

      public LandingCoordinates(ConfigNode node)
      {
        CelestialBody body = null;
        node.TryGetValue("body", ref body, Extensions.CbTryParse);
        Body = body;
        var temp = 0.0;
        var tempAlt = 20.0;
        node.TryGetValue("lat", ref temp, double.TryParse);
        Lat = temp;
        node.TryGetValue("lon", ref temp, double.TryParse);
        Lon = temp;
        node.TryGetValue("alt", ref tempAlt, double.TryParse);
        Alt = tempAlt;
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
        return obj is LandingCoordinates && Equals((LandingCoordinates)obj);
      }

      public bool Equals(LandingCoordinates other)
      {
        return Name.Equals(other.Name);
      }

      public override string ToString()
      {
        return Name + "," + Lat + "," + Lon + "," + Alt + "," + Body.CbToString();
      }

      public ConfigNode ToConfigNode()
      {
        var node = new ConfigNode("coordinate");
        node.AddValue("name", Name);
        node.AddValue("body", Body.CbToString());
        node.AddValue("lat", Lat);
        node.AddValue("lon", Lon);
        node.AddValue("alt", Alt);

        //Extensions.Log("Checking ToConfigNode: " + node);

        return node;
      }
    }
  }

  public class LanderAttachment : MonoBehaviour
  {
    public bool AlreadyTeleported { get; set; }
    public Action<double, double, double, CelestialBody> OnManualEdit { get; set; }
    public CelestialBody Body { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public bool SetRotation { get; set; }
    public double InterimAltitude { get; set; }

    private readonly object _accelLogObject = new object();
    private bool teleportedToLandingAlt = false;
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
          QuaternionD.AngleAxis(Longitude, Vector3d.down) *
          QuaternionD.AngleAxis(Latitude, Vector3d.forward) * Vector3d.right) -
                pqs.radius;
      Extensions.Log("SetAltitudeToCurrent:: alt (pqs.GetSurfaceHeight) = " + alt);

      alt = Math.Max(alt, 0); // Underwater!
      Altitude = GetComponent<Vessel>().altitude - alt;

      Extensions.Log("SetAltitudeToCurrent::");
      Extensions.Log(" alt = Math.Max(alt, 0) := " + alt);
      Extensions.Log(" <Vessel>.altitude      := " + Altitude);

    }

    public void Update()
    {
      // 0.2 meters per frame
      //var degrees = 0.2 / Body.Radius * (180 / Math.PI);
      var degrees = 0.5 / Body.Radius * (180 / Math.PI);

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

      if (Latitude == 0)
      {
        Latitude = 0.0001;
      }
      if (Longitude == 0)
      {
        Longitude = 0.0001;
      }
      if (changed)
      {
        AlreadyTeleported = false;
        teleportedToLandingAlt = false;
        OnManualEdit(Latitude, Longitude, Altitude, Body);
      }
    }

    public void FixedUpdate()
    {
      //Extensions.Log("LanderAttachment.FixedUpdate");
      //Extensions.Log("----------------------------");

      var vessel = GetComponent<Vessel>();
      if (vessel != FlightGlobals.ActiveVessel)
      {
        Destroy(this);
        return;
      }

      if (AlreadyTeleported)
      {
        //Extensions.Log("FixedUpdate: AlreadyTeleported");
        if (vessel.LandedOrSplashed)
        {
          Destroy(this);
        }
        else
        {
          var accel = (vessel.srf_velocity + vessel.upAxis) * -0.5;
          vessel.ChangeWorldVelocity(accel);
          /*
          RateLimitedLogger.Log(_accelLogObject,
              $"(Happening every frame) Soft-lander changed ship velocity this frame by vector {accel.x},{accel.y},{accel.z} (mag {accel.magnitude})");
          */
        }
      }
      else
      {
        //Debug.Log("FixedUpdate: not AlreadyTeleported");
        var pqs = Body.pqsController;
        if (pqs == null)
        {
          Destroy(this);
          return;
        }

        var alt = pqs.GetSurfaceHeight(Body.GetRelSurfaceNVector(Latitude, Longitude)) - Body.Radius;
        var tmpAlt = Body.TerrainAltitude(Latitude, Longitude);

        double landHeight = 0;

        landHeight = FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude;
        var checkAlt = FlightGlobals.ActiveVessel.altitude;
        var checkPQSAlt = FlightGlobals.ActiveVessel.pqsAltitude;

        Extensions.Log("m1. Body.Radius  = " + Body.Radius);
        Extensions.Log("m2. PQS SurfaceHeight = " + pqs.GetSurfaceHeight(Body.GetRelSurfaceNVector(Latitude, Longitude)) );
        Extensions.Log("alt ( m2 - m1 ) = " + alt);
        Extensions.Log(".GetRelSurfaceNVector = " + Body.GetRelSurfaceNVector(Latitude, Longitude));
        Extensions.Log("Body.TerrainAltitude = " + tmpAlt);

        alt = Math.Max(alt, 0d); // Underwater!

        Extensions.Log("alt > 0.00 = " + alt);

        if (TimeWarp.CurrentRateIndex != 0)
        {
          TimeWarp.SetRate(0, true);
          Extensions.Log("Set time warp to index 0");
        }
        // HoldVesselUnpack is in display frames, not physics frames

        Extensions.Log("alt: " + alt.ToString() + "   Altitude: " + Altitude);
        Extensions.Log("Latitude: " + Latitude.ToString() + "   Longitude: " + Longitude.ToString());
        Extensions.Log("TerrainAltitude: " + tmpAlt);

        //var teleportPosition = Body.GetRelSurfacePosition(Latitude, Longitude, alt +Altitude);
        Extensions.Log("Old teleportPosition: " + Body.GetRelSurfacePosition(Latitude, Longitude, alt + Altitude));

        Vector3d teleportPosition;
        Vector3d tpTest;

        if (!teleportedToLandingAlt)
        {
          Extensions.Log("teleportedToLandingAlt == false, ");
          Extensions.Log("interimAltitude: " + InterimAltitude );
          Extensions.Log("Altitude: " + Altitude);

          if (InterimAltitude > Altitude)
          {
            if (Planetarium.GetUniversalTime() - lastUpdate >= 0.5)
            {
              InterimAltitude = InterimAltitude / 10;

              Extensions.Log("Planetarium.GetUniversalTime (): " + Planetarium.GetUniversalTime().ToString() + "   lastUpdate: " + lastUpdate.ToString());
              Extensions.Log("intermAltitude: " + InterimAltitude.ToString());

              teleportPosition = Body.GetRelSurfacePosition(Latitude, Longitude, alt + InterimAltitude);
              tpTest = Body.GetWorldSurfacePosition(Latitude, Longitude, alt + InterimAltitude);

              Extensions.Log("1. teleportPosition = " + teleportPosition);
              Extensions.Log("1. tpTest = " + tpTest);

              if (lastUpdate != 0)
              {
                InterimAltitude = Altitude;
              }
              lastUpdate = Planetarium.GetUniversalTime();

            }
            else
            {
              Extensions.Log("teleportPositionAltitude (no time change):");
              Extensions.Log("alt: " + alt.ToString() + " | intermAltitude: " + InterimAltitude.ToString());

              teleportPosition = Body.GetRelSurfacePosition(Latitude, Longitude, alt + InterimAltitude);
              tpTest = Body.GetWorldSurfacePosition(Latitude, Longitude, alt + InterimAltitude);

              Extensions.Log("2. teleportPosition = " + teleportPosition);
              Extensions.Log("2. tpTest = " + tpTest);
            }
          }
          else
          {
            Extensions.Log("teleportedToLandingAlt set to true");

            teleportedToLandingAlt = true;
            teleportPosition = Body.GetRelSurfacePosition(Latitude, Longitude, alt + Altitude);

            tpTest = Body.GetWorldSurfacePosition(Latitude, Longitude, alt + Altitude);

            Extensions.Log("3. teleportPosition = " + teleportPosition);
            Extensions.Log("3. tpTest = " + tpTest);
          }
        }
        else
        {
          Extensions.Log("teleportedToLandingAlt == true");

          teleportPosition = Body.GetRelSurfacePosition(Latitude, Longitude, alt + Altitude);

          tpTest = Body.GetWorldSurfacePosition(Latitude, Longitude, alt + Altitude);

          Extensions.Log("4. teleportPosition = " + teleportPosition);
          Extensions.Log("4. tpTest = " + tpTest);
        }

        var teleportVelocity = Vector3d.Cross(Body.angularVelocity, teleportPosition);

        //var teleportVelocity = Vector3d.Cross(Vector3d.down, teleportPosition.normalized)*
        //                       (Math.Cos(L atitude*(Math.PI/180))*teleportPosition.magnitude*
        //                        (Math.PI*2)/(Body.rotationPeriod));

        // convert from world space to orbit space
        
        teleportPosition = teleportPosition.xzy;
        teleportVelocity = teleportVelocity.xzy;
        

        Extensions.Log("teleportPosition(xzy): " + teleportPosition);
        Extensions.Log("teleportVelocity(xzy): " + teleportVelocity);
        Extensions.Log("Body                 : " + Body);

        // counter for the momentary fall when on rails (about one second)
        teleportVelocity += teleportPosition.normalized * (Body.gravParameter / teleportPosition.sqrMagnitude);

        Quaternion rotation;
        if (SetRotation)
        {
          var from = Vector3d.up;
          //var to = teleportPosition.xzy.normalized;
          var to = teleportPosition.normalized;
          rotation = Quaternion.FromToRotation(from, to);
        }
        else
        {
          var oldUp = vessel.orbit.pos.xzy.normalized;
          //var newUp = teleportPosition.xzy.normalized;
          var newUp = teleportPosition.normalized;
          rotation = Quaternion.FromToRotation(oldUp, newUp) * vessel.vesselTransform.rotation;
        }

        var orbit = vessel.orbitDriver.orbit.Clone();
        orbit.UpdateFromStateVectors(teleportPosition, teleportVelocity, Body, Planetarium.GetUniversalTime());

        Extensions.Log("FINAL:");
        Extensions.Log("orbit    = " + orbit);
        Extensions.Log("rotation = " + rotation);
        Extensions.Log("teleportedToLandingAlt = " + teleportedToLandingAlt);
        Extensions.Log("vessel: " + vessel);

        vessel.SetOrbit(orbit);
        vessel.SetRotation(rotation);
        if (teleportedToLandingAlt)
        {
          AlreadyTeleported = true;
          Extensions.Log(" :ALREADY TELEPORTED:");
        }
      }
    }
  }
}