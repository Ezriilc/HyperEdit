using System;
using System.Collections.Generic;
using System.Linq;

namespace HyperEdit.Model
{
    public struct SliderRange
    {
        public float Min { get; private set; }
        public float Max { get; private set; }

        public SliderRange(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }

    public class OrbitEditor
    {
        public interface IEditorType
        {
            Orbit Orbit(Orbit old);
            void SetBody(CelestialBody body);
        }

        public class Simple : IEditorType
        {
            public double Altitude { get; set; }
            public CelestialBody Body { get; set; }

            public Orbit Orbit(Orbit old)
            {
                return CreateOrbit(0, 0, Altitude + Body.Radius, 0, 0, 0, 0, Body);
            }

            public void SetBody(CelestialBody body)
            {
                Body = body;
            }

            public Simple(Orbit orbit)
            {
                Altitude = orbit.altitude;
                Body = orbit.referenceBody;
            }
        }

        public class Complex : IEditorType
        {
            public double Inclination { get; set; }
            public double Eccentricity { get; set; }
            public double SemiMajorAxis { get; set; }
            public double LongitudeAscendingNode { get; set; }
            public double ArgumentOfPeriapsis { get; set; }
            public double MeanAnomalyAtEpoch { get; set; }
            public double Epoch { get; set; }
            public CelestialBody Body { get; set; }

            public Orbit Orbit(Orbit old)
            {
                return CreateOrbit(Inclination, Eccentricity, SemiMajorAxis, LongitudeAscendingNode, ArgumentOfPeriapsis, MeanAnomalyAtEpoch, Epoch, Body);
            }

            public void SetBody(CelestialBody body)
            {
                Body = body;
            }

            public Complex(Orbit orbit)
            {
                Inclination = orbit.inclination;
                Eccentricity = orbit.eccentricity;
                SemiMajorAxis = orbit.semiMajorAxis;
                LongitudeAscendingNode = orbit.LAN;
                ArgumentOfPeriapsis = orbit.argumentOfPeriapsis;
                MeanAnomalyAtEpoch = orbit.meanAnomalyAtEpoch;
                Epoch = orbit.epoch;
                Body = orbit.referenceBody;
            }
        }

        public class Graphical : IEditorType
        {
            public float Inclination { get; set; }
            public SliderRange InclinationRange { get { return new SliderRange(0, 360); } }
            public float Eccentricity { get; set; }
            public SliderRange EccentricityRange { get { return new SliderRange(0, (float)Math.PI / 2 - 0.001f); } }
            public float Periapsis { get; set; }
            public SliderRange PeriapsisRange { get { return new SliderRange(0.01f, 1); } }
            public float LongitudeAscendingNode { get; set; }
            public SliderRange LongitudeAscendingNodeRange { get { return new SliderRange(0, 360); } }
            public float ArgumentOfPeriapsis { get; set; }
            public SliderRange ArgumentOfPeriapsisRange { get { return new SliderRange(0, 360); } }
            public float MeanAnomaly { get; set; }
            public SliderRange MeanAnomalyRange { get { return new SliderRange(0, (float)Math.PI * 2); } }
            public CelestialBody Body { get; set; }

            public Orbit Orbit(Orbit old)
            {
                var soi = Body.Soi();
                var pe = (double)Periapsis;
                var ratio = soi / (Body.Radius + Body.maxAtmosphereAltitude);
                pe = Math.Pow(ratio, pe) / ratio;
                pe *= soi;

                var e = Math.Tan(Eccentricity);
                var semimajor = pe / (1 - e);

                var mep = (double)MeanAnomaly;
                if (semimajor < 0)
                {
                    mep /= Math.PI;
                    mep -= 1;
                    mep *= 5;
                }
                return CreateOrbit(Inclination, e, semimajor, LongitudeAscendingNode, ArgumentOfPeriapsis, mep, 0, Body);
            }

            public void SetBody(CelestialBody body)
            {
                Body = body;
            }

            public Graphical(Orbit orbit)
            {
                Body = orbit.referenceBody;
                Inclination = (float)orbit.inclination;
                LongitudeAscendingNode = (float)orbit.LAN;
                ArgumentOfPeriapsis = (float)orbit.argumentOfPeriapsis;
                var e = (float)Math.Atan(orbit.eccentricity);
                Eccentricity = e;
                var soi = orbit.referenceBody.Soi();
                var ratio = soi / ((float)orbit.referenceBody.Radius + orbit.referenceBody.maxAtmosphereAltitude);
                var semimajor = (float)orbit.semiMajorAxis * (1 - e);
                semimajor /= soi;
                semimajor *= ratio;
                semimajor = (float)Math.Log(semimajor, ratio);
                Periapsis = semimajor;
                var mep = (float)orbit.meanAnomalyAtEpoch;
                if (orbit.semiMajorAxis < 0)
                {
                    mep /= 5;
                    mep += 1;
                    mep *= (float)Math.PI;
                }
                MeanAnomaly = mep;
            }
        }

        public class Velocity : IEditorType
        {
            public enum ChangeDirection
            {
                Prograde,
                Normal,
                Radial,
                North,
                East,
                Up
            }

            public ChangeDirection Direction { get; set; }
            public double Speed { get; set; }

            public Orbit Orbit(Orbit oldOrbit)
            {
                Vector3d velocity;
                switch (Direction)
                {
                    case ChangeDirection.Prograde:
                        velocity = oldOrbit.getOrbitalVelocityAtUT(Planetarium.GetUniversalTime()).normalized * Speed;
                        break;
                    case ChangeDirection.Normal:
                        velocity = oldOrbit.GetOrbitNormal().normalized * Speed;
                        break;
                    case ChangeDirection.Radial:
                        velocity = Vector3d.Cross(oldOrbit.getOrbitalVelocityAtUT(Planetarium.GetUniversalTime()), oldOrbit.GetOrbitNormal()).normalized * Speed;
                        break;
                    case ChangeDirection.North:
                        var upn = oldOrbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).normalized;
                        velocity = Vector3d.Cross(Vector3d.Cross(upn, new Vector3d(0, 0, 1)), upn) * Speed;
                        break;
                    case ChangeDirection.East:
                        var upe = oldOrbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).normalized;
                        velocity = Vector3d.Cross(new Vector3d(0, 0, 1), upe) * Speed;
                        break;
                    case ChangeDirection.Up:
                        velocity = oldOrbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).normalized * Speed;
                        break;
                    default:
                        throw new Exception("Unknown VelChangeDir");
                }
                var tempOrbit = oldOrbit.Clone();
                tempOrbit.UpdateFromStateVectors(oldOrbit.pos, oldOrbit.vel + velocity, oldOrbit.referenceBody, Planetarium.GetUniversalTime());
                return tempOrbit;
            }

            public void SetBody(CelestialBody body)
            {
            }

            public Velocity(Orbit orbit)
            {
                Direction = ChangeDirection.Prograde;
                Speed = 0;
            }
        }

        public class Rendezvous : IEditorType
        {
            public Vessel RendezvousWith { get; set; }
            public double LeadTime { get; set; }

            public Orbit Orbit(Orbit old)
            {
                var o = RendezvousWith.orbit;
                return CreateOrbit(o.inclination, o.eccentricity, o.semiMajorAxis, o.LAN, o.argumentOfPeriapsis, o.meanAnomalyAtEpoch, o.epoch - LeadTime, o.referenceBody);
            }

            public void SetBody(CelestialBody body)
            {
            }

            public void SelectVessel()
            {
                if (FlightGlobals.fetch == null || FlightGlobals.Vessels == null)
                    Extentions.ErrorPopup("Could not get list of vessels");
                else
                    View.WindowHelper.Selector("Select vessel", FlightGlobals.Vessels, v => v.name, v => RendezvousWith = v);
            }

            public Rendezvous(Orbit orbit)
            {
                LeadTime = 10;
            }
        }

        private static Orbit CreateOrbit(double inc, double e, double sma, double lan, double w, double mEp, double epoch, CelestialBody body)
        {
            if (double.IsNaN(inc))
                inc = 0;
            if (double.IsNaN(e))
                e = 0;
            if (double.IsNaN(sma))
                sma = body.Radius + body.maxAtmosphereAltitude + 10000;
            if (double.IsNaN(lan))
                lan = 0;
            if (double.IsNaN(w))
                w = 0;
            if (double.IsNaN(mEp))
                mEp = 0;
            if (double.IsNaN(epoch))
                mEp = Planetarium.GetUniversalTime();

            if (Math.Sign(e - 1) == Math.Sign(sma))
                sma = -sma;

            if (Math.Sign(sma) >= 0)
            {
                while (mEp < 0)
                    mEp += Math.PI * 2;
                while (mEp > Math.PI * 2)
                    mEp -= Math.PI * 2;
            }

            return new Orbit(inc, e, sma, lan, w, mEp, epoch, body);
        }

        private OrbitDriver _currentlyEditing;
        public OrbitDriver CurrentlyEditing
        {
            get { return _currentlyEditing; }
            set { _currentlyEditing = value; Editor = new Simple(value.orbit); }
        }

        public IEditorType Editor { get; set; }

        private static string GetName(OrbitDriver driver)
        {
            if (driver == null)
                return null;
            var body = FlightGlobals.Bodies.FirstOrDefault(cb => cb.orbitDriver != null && cb.orbitDriver == driver);
            if (body != null)
                return body.bodyName;
            var vessel = FlightGlobals.Vessels.FirstOrDefault(v => v.orbitDriver != null && v.orbitDriver == driver);
            if (vessel != null)
                return vessel == FlightGlobals.ActiveVessel ? "Active vessel" : vessel.vesselName;
            if (string.IsNullOrEmpty(driver.name) == false)
                return driver.name;
            return "Unknown";
        }

        public string CurrentlyEditingName
        {
            get
            {
                return GetName(_currentlyEditing);
            }
        }

        public OrbitEditor()
        {
            if (FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.orbit != null)
            {
                CurrentlyEditing = FlightGlobals.ActiveVessel.orbitDriver;
            }
        }

        public void Apply()
        {
            if (CurrentlyEditing != null)
            {
                CurrentlyEditing.orbit.Set(Editor.Orbit(CurrentlyEditing.orbit));
            }
        }

        private static IEnumerable<OrbitDriver> OrderedOrbits()
        {
            var query = (IEnumerable<OrbitDriver>)
                        (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.orbitDriver == null
                             ? new OrbitDriver[0]
                             : new[] { FlightGlobals.ActiveVessel.orbitDriver });
            if (FlightGlobals.fetch != null)
                query = query
                    .Concat(FlightGlobals.Vessels.Select(v => v.orbitDriver))
                    .Concat(FlightGlobals.Bodies.Select(v => v.orbitDriver));
            query = query.Where(o => o != null).Distinct();
            return query;
        }

        public void SelectOrbit()
        {
            if (FlightGlobals.fetch == null)
                Extentions.ErrorPopup("Could not get list of orbits (are you in the flight scene?)");
            else
                View.WindowHelper.Selector("Select orbit", OrderedOrbits(), GetName, o => CurrentlyEditing = o);
        }

        public void SelectBody()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.Bodies == null)
                Extentions.ErrorPopup("Could not get list of bodies (are you in the flight scene?)");
            else
                View.WindowHelper.Selector("Select body", FlightGlobals.Bodies, b => b.bodyName, Editor.SetBody);
        }
    }
}
