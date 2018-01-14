using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HyperEdit {
  public static class Utils {

    const double PI = Math.PI;

    ///   Borrowed from https://github.com/KSP-KOS/KOS.
    /// <summary>
    ///   Fix the strange too-large or too-small angle degrees that are sometimes
    ///   returned by KSP, normalizing them into a constrained 360 degree range.
    /// </summary>
    /// <param name="inAngle">input angle in degrees</param>
    /// <param name="rangeStart">
    ///   Bottom of 360 degree range to normalize to.
    ///   ( 0 means the range [0..360]), while -180 means [-180,180] )
    /// </param>
    /// <returns>the same angle, normalized to the range given.</returns>
    public static double DegreeFix(double inAngle, double rangeStart) {
      double rangeEnd = rangeStart + 360.0;
      double outAngle = inAngle;
      while (outAngle > rangeEnd)
        outAngle -= 360.0;
      while (outAngle < rangeStart)
        outAngle += 360.0;
      return outAngle;
    }

    public static double DegreesToRadians(double degrees) {
      return degrees * PI / 180;
    }

    public static double RadiansToDegrees(double radians) {
      return radians * 180 / PI;
    }

    /// <summary>
    /// Get destination latitude point for use with fine-tuning.
    /// <para>See http://www.movable-type.co.uk/scripts/latlong.html</para>
    /// <para>(Destination point given distance and bearing from start point)</para>
    /// </summary>
    /// <param name="latStart">The starting latitude</param>
    /// <param name="lonStart">The starting longitude</param>
    /// <param name="bearing">The direction</param>
    /// <param name="distance">The distance to move in metres.</param>
    /// <param name="radius">The current Body's radius</param>
    /// <returns></returns>
    public static double DestinationLatitude(double latStart, double lonStart, double bearing, double distance, double radius) {

      distance = distance / 100; //this should equate to metres

      latStart = PI / 180 * latStart;
      lonStart = PI / 180 * lonStart;
      bearing = PI / 180 * bearing;

      var latEnd = Math.Asin(Math.Sin(latStart) * Math.Cos(distance / radius) +
        Math.Cos(latStart) * Math.Sin(distance / radius) * Math.Cos(bearing));
      var lonEnd = lonStart + Math.Atan2(Math.Sin(bearing) * Math.Sin(distance / radius) * Math.Cos(latStart),
        Math.Cos(distance / radius) - Math.Sin(latStart) * Math.Sin(latEnd));

      return latEnd;
    }

    /// <summary>
    /// Get destination longitude point for use with fine-tuning.
    /// <para>See http://www.movable-type.co.uk/scripts/latlong.html</para>
    /// <para>(Destination point given distance and bearing from start point)</para>
    /// </summary>
    /// <param name="latStart">The starting latitude</param>
    /// <param name="lonStart">The starting longitude</param>
    /// <param name="bearing">The direction</param>
    /// <param name="distance">The distance to move in metres.</param>
    /// <param name="radius">The current Body's radius</param>
    /// <returns></returns>
    public static double DestinationLongitude(double latStart, double lonStart, double bearing, double distance, double radius) {

      distance = distance / 100; //this should equate to metres

      latStart = PI / 180 * latStart;
      lonStart = PI / 180 * lonStart;
      bearing = PI / 180 * bearing;

      var latEnd = Math.Asin(Math.Sin(latStart) * Math.Cos(distance / radius) +
        Math.Cos(latStart) * Math.Sin(distance / radius) * Math.Cos(bearing));
      var lonEnd = lonStart + Math.Atan2(Math.Sin(bearing) * Math.Sin(distance / radius) * Math.Cos(latStart),
        Math.Cos(distance / radius) - Math.Sin(latStart) * Math.Sin(latEnd));

      return lonEnd;
    }

    public static double DestinationLatitudeRad(double latStart, double lonStart, double bearing, double distance, double radius) {

      distance = distance / 100; //this should equate to metres

      var latEnd = Math.Asin(Math.Sin(latStart) * Math.Cos(distance / radius) +
        Math.Cos(latStart) * Math.Sin(distance / radius) * Math.Cos(bearing));
      var lonEnd = lonStart + Math.Atan2(Math.Sin(bearing) * Math.Sin(distance / radius) * Math.Cos(latStart),
        Math.Cos(distance / radius) - Math.Sin(latStart) * Math.Sin(latEnd));

      return latEnd;
    }

    public static double DestinationLongitudeRad(double latStart, double lonStart, double bearing, double distance, double radius) {

      distance = distance / 100; //this should equate to metres

      var latEnd = Math.Asin(Math.Sin(latStart) * Math.Cos(distance / radius) +
        Math.Cos(latStart) * Math.Sin(distance / radius) * Math.Cos(bearing));
      var lonEnd = lonStart + Math.Atan2(Math.Sin(bearing) * Math.Sin(distance / radius) * Math.Cos(latStart),
        Math.Cos(distance / radius) - Math.Sin(latStart) * Math.Sin(latEnd));

      return lonEnd;
    }


  }
}
