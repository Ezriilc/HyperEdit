using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HyperEdit.Model {
  public static class DoLander {
    private const string OldFilename = "landcoords.txt";
    private const string FilenameNoExt = "landcoords";
    private const string RecentEntryName = "Most Recent";

    public static bool IsLanding() {
      if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null) {
        return false;
      }

      return FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>() != null;
    }

    public static void ToggleLanding(double latitude, double longitude, double altitude, CelestialBody body,
        bool setRotation, Action<double, double, double, CelestialBody> onManualEdit) {
      if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null || body == null) {
        return;
      }

      Extensions.Log("HyperEdit.Model.ToggleLanding");
      Extensions.Log("-----------------------------");

      var lander = FlightGlobals.ActiveVessel.GetComponent<LanderAttachment>();
      if (lander == null) {
        Model.DoLander.AddLastCoords(latitude, longitude, altitude, body);
        lander = FlightGlobals.ActiveVessel.gameObject.AddComponent<LanderAttachment>();
        
        if (latitude == 0.0f) {
          latitude = 0.001;
        }

        if (longitude == 0.0f) {
          longitude = 0.001;
        }

        lander.Latitude = latitude;
        lander.Longitude = longitude;

        lander.InterimAltitude = body.Radius + body.atmosphereDepth + 10000d; //Altitude threshold

        lander.Altitude = altitude;
        lander.SetRotation = setRotation;
        lander.Body = body;
        lander.OnManualEdit = onManualEdit;

        Extensions.Log("Latitude : " + latitude.ToString());
        Extensions.Log("Longitude: " + longitude.ToString());
        Extensions.Log("Altitude : " + altitude.ToString());
        Extensions.Log("Body     : " + body.ToString());
        Extensions.Log("B-Radius : " + body.Radius.ToString());
        //Extensions.Log("B-Depth  : " + body.atmosphereDepth.ToString());
        Extensions.Log("NEW:");
        Extensions.Log("lander = " + lander.ToString());
        Extensions.Log("interimAltitude = " + lander.InterimAltitude);
        Extensions.Log("-----------------------------");

      } else {
        //lander != null
        Extensions.Log("Unity destroy lander");
        UnityEngine.Object.Destroy(lander);
      }
    }

    public static void LandHere(Action<double, double, double, CelestialBody> onManualEdit) {
      if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null) {
        return;
      }

      var vessel = FlightGlobals.ActiveVessel;
      var lander = vessel.GetComponent<LanderAttachment>();
      if (lander == null) {
        Extensions.Log("LandHere");
        Extensions.Log("-----------------------------");
        
        Extensions.Log("Vessel Latitude : " + vessel.latitude.ToString());
        Extensions.Log("Vessel Longitude: " + vessel.longitude.ToString());
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
        Extensions.Log("-----------------------------");

      }
    }

    private static IEnumerable<LandingCoordinates> DefaultSavedCoords {
      get {
        var kerbin = Planetarium.fetch?.Home;
        var minmus = FlightGlobals.fetch?.bodies?.FirstOrDefault(b => b.bodyName == "Minmus");
        if (kerbin == null) {
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
        if (minmus != null) {
          list.Add(new LandingCoordinates("Minmus Flats", 0.562859, 175.968846, 20, minmus));
        }
        return list;
      }
    }

    private static List<LandingCoordinates> SavedCoords {
      get {
        var path = IoExt.GetPath(FilenameNoExt + ".cfg");
        var oldPath = IoExt.GetPath(OldFilename);
        IEnumerable<LandingCoordinates> query;
        if (System.IO.File.Exists(path)) {
          query = ConfigNode.Load(path).nodes.OfType<ConfigNode>().Select(c => new LandingCoordinates(c));
        } else if (System.IO.File.Exists(oldPath)) {
          query =
              System.IO.File.ReadAllLines(oldPath)
                  .Select(x => new LandingCoordinates(x))
                  .Where(l => string.IsNullOrEmpty(l.Name) == false);
        } else {
          query = new LandingCoordinates[0];
        }
        query = query.Union(DefaultSavedCoords);
        return query.ToList();
      }
      set {
        var cfg = new ConfigNode(FilenameNoExt);
        foreach (var coord in value) {
          cfg.AddNode(coord.ToConfigNode());
        }
        cfg.Save();
      }
    }

    public static void AddLastCoords(double latitude, double longitude, double altitude, CelestialBody body) {
      if (body == null) {
        return;
      }

      AddSavedCoords(RecentEntryName, latitude, longitude, altitude, body);
    }

    public static void AddSavedCoords(double latitude, double longitude, double altitude, CelestialBody body) {
      if (body == null) {
        return;
      }

      View.WindowHelper.Prompt("Save as...", s => AddSavedCoords(s, latitude, longitude, altitude, body));
    }

    private static void AddSavedCoords(string name, double latitude, double longitude, double altitude, CelestialBody body) {
      var saved = SavedCoords;
      saved.RemoveAll(match => match.Name == name);
      saved.Add(new LandingCoordinates(name, latitude, longitude, altitude, body));
      SavedCoords = saved;
    }

    public static void LoadLast(Action<double, double, double, CelestialBody> onLoad) {
      var lastC = SavedCoords.Find(c => c.Name == RecentEntryName);
      //double-check coords are correct (so that we don't load invalid data!)
      onLoad(Extensions.DegreeFix(lastC.Lat,0) , lastC.Lon, lastC.Alt, lastC.Body);
    }

    public static void Load(Action<double, double, double, CelestialBody> onLoad) {
      View.WindowHelper.Selector("Load...", SavedCoords, c => c.Name, c => onLoad(c.Lat, c.Lon, c.Alt, c.Body));
    }

    public static void Delete() {
      var coords = SavedCoords;
      View.WindowHelper.Selector("Delete...", coords, c => c.Name, toDelete => {
        coords.Remove(toDelete);
        SavedCoords = coords;
      });
    }

    public static void SetToCurrent(Action<double, double, double, CelestialBody> onLoad) {
      if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null) {
        return;
      }

      //FlightGlobals.ActiveVessel.altitude is incorrect.
      var Body = FlightGlobals.ActiveVessel.mainBody;
      var Latitude = FlightGlobals.ActiveVessel.latitude;
      var Longitude = FlightGlobals.ActiveVessel.longitude;
      var alt = FlightGlobals.ActiveVessel.radarAltitude;
      /*
      var pqs = FlightGlobals.ActiveVessel.mainBody.pqsController;

      if (pqs != null) {
        var alt = pqs.GetSurfaceHeight(Body.GetRelSurfaceNVector(Latitude, Longitude)) - Body.Radius;
      } else {
        var alt = FlightGlobals.ActiveVessel.radarAltitude;
      }
      */

      onLoad(Latitude, Longitude, alt, Body);
    }

    public static IEnumerable<Vessel> LandedVessels() {
      return FlightGlobals.fetch == null ? null : FlightGlobals.Vessels.Where(v => v.Landed);
    }

    public static void SetToLanded(Action<double, double, double, CelestialBody> onLoad, Vessel landingBeside) {
      if (landingBeside == null) {
        return;
      }

      //doing this here for brevity and correct altitude display.
      var Body = landingBeside.mainBody;
      var Latitude = landingBeside.latitude;
      var Longitude = landingBeside.longitude;
      var alt = landingBeside.radarAltitude;
      
      onLoad(Latitude, Longitude, alt, Body);
    }

    private struct LandingCoordinates : IEquatable<LandingCoordinates> {
      public string Name { get; }
      public double Lat { get; }
      public double Lon { get; }
      public double Alt { get; }
      public CelestialBody Body { get; }

      public LandingCoordinates(string name, double lat, double lon, double alt, CelestialBody body)
          : this() {
        Name = name;
        Lat = lat;
        Lon = lon;
        Alt = alt;
        Body = body;
      }

      public LandingCoordinates(string value)
          : this() {
        var split = value.Split(',');
        if (split.Length < 3) {
          Name = null;
          Lat = 0;
          Lon = 0;
          Alt = 20;
          Body = null;
          return;
        }
        double dlat, dlon, dalt;
        if (double.TryParse(split[1], out dlat) && double.TryParse(split[2], out dlon) && double.TryParse(split[2], out dalt)) {
          Name = split[0];
          Lat = dlat;
          Lon = dlon;
          Alt = dalt;
          CelestialBody body;
          if (split.Length >= 4 && Extensions.CbTryParse(split[3], out body)) {
            Body = body;
          } else {
            Body = Planetarium.fetch.Home;
          }
        } else {
          Name = null;
          Lat = 0;
          Lon = 0;
          Alt = 20;
          Body = null;
        }
      }

      public LandingCoordinates(ConfigNode node) {
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

      public override int GetHashCode() {
        return Name.GetHashCode();
      }

      public override bool Equals(object obj) {
        return obj is LandingCoordinates && Equals((LandingCoordinates)obj);
      }

      public bool Equals(LandingCoordinates other) {
        return Name.Equals(other.Name);
      }

      public override string ToString() {
        return Name + "," + Lat + "," + Lon + "," + Alt + "," + Body.CbToString();
      }

      public ConfigNode ToConfigNode() {
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

  public class LanderAttachment : MonoBehaviour {
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
    //private double altAGL = 0; // Need to work out these in relation
    //private double altASL = 0; // to land or sea.

    /// <summary>
    /// Sets the vessel altitude to the current calculation.
    /// </summary>
    public void SetAltitudeToCurrent() {
      var pqs = Body.pqsController;
      if (pqs == null) {
        Destroy(this);
        return;
      }
      var alt = pqs.GetSurfaceHeight(
          QuaternionD.AngleAxis(Longitude, Vector3d.down) *
          QuaternionD.AngleAxis(Latitude, Vector3d.forward) * Vector3d.right) -
                pqs.radius;
      Extensions.Log("SetAltitudeToCurrent:: alt (pqs.GetSurfaceHeight) = " + alt);

      alt = Math.Max(alt, 0); // Underwater!
      /*
       * I'm not sure whether this is correct to zero the altitude as there are times on certain bodies
       * where the altitude of the surface is below sea level...wish I could remember where it was that
       * I found this.
       * 
       * Also HyperEdit used to allow you to land underwater for things like submarines!
       */

      Altitude = GetComponent<Vessel>().altitude - alt;

      Extensions.Log("SetAltitudeToCurrent::");
      Extensions.Log(" alt = Math.Max(alt, 0) := " + alt);
      Extensions.Log(" <Vessel>.altitude      := " + Altitude);

    }

    public void Update() {

      //Testing whether to kill TimeWarp
      if (TimeWarp.CurrentRateIndex != 0) {
        TimeWarp.SetRate(0, true);
        Extensions.Log("Update: Kill TimeWarp");
      }

      // 0.2 meters per frame
      var degrees = 0.2 / Body.Radius * (180 / Math.PI);

      var changed = false;
      if (GameSettings.TRANSLATE_UP.GetKey()) {
        Latitude -= degrees;
        changed = true;
      }
      if (GameSettings.TRANSLATE_DOWN.GetKey()) {
        Latitude += degrees;
        changed = true;
      }
      if (GameSettings.TRANSLATE_LEFT.GetKey()) {
        Longitude -= degrees / Math.Cos(Latitude * (Math.PI / 180));
        changed = true;
      }
      if (GameSettings.TRANSLATE_RIGHT.GetKey()) {
        Longitude += degrees / Math.Cos(Latitude * (Math.PI / 180));
        changed = true;
      }

      if (Latitude == 0) {
        Latitude = 0.0001;
      }
      if (Longitude == 0) {
        Longitude = 0.0001;
      }
      if (changed) {
        AlreadyTeleported = false;
        teleportedToLandingAlt = false;
        OnManualEdit(Latitude, Longitude, Altitude, Body);
      }
    }

    public void FixedUpdate() {
      var vessel = GetComponent<Vessel>();

      if (vessel != FlightGlobals.ActiveVessel) {
        Destroy(this);
        return;
      }

      if (TimeWarp.CurrentRateIndex != 0) {
        TimeWarp.SetRate(0, true);
        Extensions.Log("Kill time warp for safety reasons!");
      }

      if (AlreadyTeleported) {
        
        if (vessel.LandedOrSplashed) {
          Destroy(this);
        } else {
          var accel = (vessel.srf_velocity + vessel.upAxis) * -0.5;
          vessel.ChangeWorldVelocity(accel);
          /*
          RateLimitedLogger.Log(_accelLogObject,
              $"(Happening every frame) Soft-lander changed ship velocity this frame by vector {accel.x},{accel.y},{accel.z} (mag {accel.magnitude})");
          */
        }
      } else {
        //NOT AlreadyTeleported
        //Still calculating
        var pqs = Body.pqsController;
        if (pqs == null) {
          // The sun has no terrain.  Everthing else has a PQScontroller.
          Destroy(this);
          return;
        }

        var alt = pqs.GetSurfaceHeight(Body.GetRelSurfaceNVector(Latitude, Longitude)) - Body.Radius;
        var tmpAlt = Body.TerrainAltitude(Latitude, Longitude);

        double landHeight = FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude;

        double finalAltitude = 0.0; //trying to isolate this for debugging!

        var checkAlt = FlightGlobals.ActiveVessel.altitude;
        var checkPQSAlt = FlightGlobals.ActiveVessel.pqsAltitude;
        double terrainAlt = GetTerrainAltitude();

        Extensions.ALog("-------------------");
        Extensions.ALog("m1. Body.Radius  = ", Body.Radius);
        Extensions.ALog("m2. PQS SurfaceHeight = ", pqs.GetSurfaceHeight(Body.GetRelSurfaceNVector(Latitude, Longitude)));
        Extensions.ALog("alt ( m2 - m1 ) = ", alt);
        Extensions.ALog("Body.TerrainAltitude = ", tmpAlt);
        Extensions.ALog("checkAlt    = ", checkAlt);
        Extensions.ALog("checkPQSAlt = ", checkPQSAlt);
        Extensions.ALog("landheight  = ", landHeight);
        Extensions.ALog("terrainAlt  = ", terrainAlt);
        Extensions.ALog("-------------------");
        Extensions.ALog("Latitude: ", Latitude, "Longitude: ", Longitude);
        Extensions.ALog("-------------------");

        alt = Math.Max(alt, 0d); // Make sure we're not underwater!
        
        // HoldVesselUnpack is in display frames, not physics frames
        
        Vector3d teleportPosition;

        if (!teleportedToLandingAlt) {
          Extensions.ALog("teleportedToLandingAlt == false");
          Extensions.ALog("interimAltitude: ", InterimAltitude);
          Extensions.ALog("Altitude: ", Altitude);

          if (InterimAltitude > Altitude) {
            
            if (Planetarium.GetUniversalTime() - lastUpdate >= 0.5) {
              InterimAltitude = InterimAltitude / 10;
              terrainAlt = GetTerrainAltitude();

              if (InterimAltitude < terrainAlt) {
                InterimAltitude = terrainAlt + Altitude;
              }

              //InterimAltitude = terrainAlt + Altitude;
              
              teleportPosition = Body.GetWorldSurfacePosition(Latitude, Longitude, InterimAltitude) - Body.position;

              Extensions.ALog("1. teleportPosition = ", teleportPosition);
              Extensions.ALog("1. interimAltitude: ", InterimAltitude);

              if (lastUpdate != 0) {
                InterimAltitude = Altitude;
              }
              lastUpdate = Planetarium.GetUniversalTime();

            } else {
              Extensions.Log("teleportPositionAltitude (no time change):");
              
              teleportPosition = Body.GetWorldSurfacePosition(Latitude, Longitude, alt + InterimAltitude) - Body.position;
              
              Extensions.ALog("2. teleportPosition = ", teleportPosition);
              Extensions.ALog("2. alt: ", alt);
              Extensions.ALog("2. interimAltitude: ", InterimAltitude);
            }
          } else {
            //InterimAltitude <= Altitude
            Extensions.Log("3. teleportedToLandingAlt sets to true");

            landHeight = FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude;
            terrainAlt = GetTerrainAltitude();

            //trying to find the correct altitude here.
            
            if (checkPQSAlt > terrainAlt) {
              alt = checkPQSAlt;
            } else {
              alt = terrainAlt;
            }

            if (alt == 0.0) {
              //now what?
            }

            /*
             * landHeight factors into the final altitude somehow. Possibly.
             */

            teleportedToLandingAlt = true;
            //finalAltitude = alt + Altitude;
            if (alt < 0) {
              finalAltitude = Altitude;
            } else if (alt > 0) {

              finalAltitude = alt + Altitude;
            } else {
              finalAltitude = alt + Altitude;
            }
            
            teleportPosition = Body.GetWorldSurfacePosition(Latitude, Longitude, finalAltitude) - Body.position;

            Extensions.ALog("3. teleportPosition = ", teleportPosition);
            Extensions.ALog("3. alt = ", alt, "Altitude = ", Altitude, "InterimAltitude = ", InterimAltitude);
            Extensions.ALog("3. TerrainAlt = ", terrainAlt, "landHeight = ", landHeight);
          }
        } else {
          /*
           * With the current way of calculating, it seems like this part of the conditional
           * never gets called. (Well not so far in my (@fronbow) testing.
           */

          Extensions.Log("teleportedToLandingAlt == true");

          landHeight = FlightGlobals.ActiveVessel.altitude - FlightGlobals.ActiveVessel.pqsAltitude;
          terrainAlt = GetTerrainAltitude();

          Extensions.ALog("4. finalAltitude = ", finalAltitude);
          /*
           * Depending on finalAltitude, we might not need to calculate it again here.
           */
           
          //finalAltitude = alt + Altitude;
          if (alt < 0) {
            finalAltitude = Altitude;
          } else if (alt > 0) {
            finalAltitude = alt + Altitude;
          } else {
            finalAltitude = alt + Altitude;
          }

          //teleportPosition = Body.GetRelSurfacePosition(Latitude, Longitude, finalAltitude);
          teleportPosition = Body.GetWorldSurfacePosition(Latitude, Longitude, finalAltitude) - Body.position;

          Extensions.ALog("4. teleportPosition = ", teleportPosition);
          Extensions.ALog("4. alt = ", alt, "Altitude = ", Altitude, "InterimAltitude = ", InterimAltitude);
          Extensions.ALog("4. TerrainAlt = ", terrainAlt, "landHeight = ", landHeight);
          Extensions.ALog("4. finalAltitude = ", finalAltitude);
        }

        var teleportVelocity = Vector3d.Cross(Body.angularVelocity, teleportPosition);
        
        // convert from world space to orbit space

        teleportPosition = teleportPosition.xzy;
        teleportVelocity = teleportVelocity.xzy;
        
        Extensions.ALog("0. teleportPosition(xzy): ", teleportPosition);
        Extensions.ALog("0. teleportVelocity(xzy): ", teleportVelocity);
        Extensions.ALog("0. Body                 : ", Body);

        // counter for the momentary fall when on rails (about one second)
        teleportVelocity += teleportPosition.normalized * (Body.gravParameter / teleportPosition.sqrMagnitude);

        Quaternion rotation;
        
        
        if (SetRotation) {
          // Need to check vessel and find up for the root command pod
          vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false); //hopefully this disables SAS as it causes unknown results!

          var from = Vector3d.up; //Sensible default for all vessels

          if (vessel.displaylandedAt == "Runway" || vessel.vesselType.ToString() == "Plane") {
            from = vessel.vesselTransform.up;
          }


          var to = teleportPosition.xzy.normalized;
          rotation = Quaternion.FromToRotation(from, to);
        } else {
          var oldUp = vessel.orbit.pos.xzy.normalized;
          var newUp = teleportPosition.xzy.normalized;
          rotation = Quaternion.FromToRotation(oldUp, newUp) * vessel.vesselTransform.rotation;
        }

        var orbit = vessel.orbitDriver.orbit.Clone();
        orbit.UpdateFromStateVectors(teleportPosition, teleportVelocity, Body, Planetarium.GetUniversalTime());
        
        vessel.SetOrbit(orbit);
        vessel.SetRotation(rotation);

        if (teleportedToLandingAlt) {
          AlreadyTeleported = true;
          Extensions.Log(" :FINISHED TELEPORTING:");
        }
      }
    }

    /// <summary>
    ///  Returns the ground's altitude above sea level at this geo position.
    /// </summary>
    /// <returns></returns>
    /// <remarks>Borrowed this from the kOS mod with slight modification</remarks>
    /// <see cref="https://github.com/KSP-KOS/KOS/blob/develop/src/kOS/Suffixed/GeoCoordinates.cs"/>
    public Double GetTerrainAltitude() {
      double alt = 0.0;
      PQS bodyPQS = Body.pqsController;
      if (bodyPQS != null) // The sun has no terrain.  Everything else has a PQScontroller.
      {
        // The PQS controller gives the theoretical ideal smooth surface curve terrain.
        // The actual ground that exists in-game that you land on, however, is the terrain
        // polygon mesh which is built dynamically from the PQS controller's altitude values,
        // and it only approximates the PQS controller.  The discrepancy between the two
        // can be as high as 20 meters on relatively mild rolling terrain and is probably worse
        // in mountainous terrain with steeper slopes.  It also varies with the user terrain detail
        // graphics setting.

        // Therefore the algorithm here is this:  Get the PQS ideal terrain altitude first.
        // Then try using RayCast to get the actual terrain altitude, which will only work
        // if the LAT/LONG is near the active vessel so the relevant terrain polygons are
        // loaded.  If the RayCast hit works, it overrides the PQS altitude.

        // PQS controller ideal altitude value:
        // -------------------------------------

        // The vector the pqs GetSurfaceHeight method expects is a vector in the following
        // reference frame:
        //     Origin = body center.
        //     X axis = LATLNG(0,0), Y axis = LATLNG(90,0)(north pole), Z axis = LATLNG(0,-90).
        // Using that reference frame, you tell GetSurfaceHeight what the "up" vector is pointing through
        // the spot on the surface you're querying for.
        var bodyUpVector = new Vector3d(1, 0, 0);
        bodyUpVector = QuaternionD.AngleAxis(Latitude, Vector3d.forward/*around Z axis*/) * bodyUpVector;
        bodyUpVector = QuaternionD.AngleAxis(Longitude, Vector3d.down/*around -Y axis*/) * bodyUpVector;

        alt = bodyPQS.GetSurfaceHeight(bodyUpVector) - bodyPQS.radius;

        // Terrain polygon raycasting:
        // ---------------------------
        const double HIGH_AGL = 1000.0;
        const double POINT_AGL = 800.0;
        const int TERRAIN_MASK_BIT = 15;

        // a point hopefully above the terrain:
        Vector3d worldRayCastStart = Body.GetWorldSurfacePosition(Latitude, Longitude, alt + HIGH_AGL);
        // a point a bit below it, to aim down to the terrain:
        Vector3d worldRayCastStop = Body.GetWorldSurfacePosition(Latitude, Longitude, alt + POINT_AGL);
        RaycastHit hit;
        if (Physics.Raycast(worldRayCastStart, (worldRayCastStop - worldRayCastStart), out hit, float.MaxValue, 1 << TERRAIN_MASK_BIT)) {
          // Ensure hit is on the topside of planet, near the worldRayCastStart, not on the far side.
          if (Mathf.Abs(hit.distance) < 3000) {
            // Okay a hit was found, use it instead of PQS alt:
            alt = ((alt + HIGH_AGL) - hit.distance);
          }
        }
      }
      return alt;
    }

  }
}