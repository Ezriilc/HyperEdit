using System;
using System.Collections.Generic;
using System.Linq;

namespace HyperEdit.Model
{
    public static class OrbitEditor
    {
        public static IEnumerable<OrbitDriver> OrderedOrbits()
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

        public static void Simple(OrbitDriver currentlyEditing, double altitude, CelestialBody body)
        {
            SetOrbit(currentlyEditing, CreateOrbit(0, 0, altitude + body.Radius, 0, 0, 0, 0, body));
        }

        public static void GetSimple(OrbitDriver currentlyEditing, out double altitude, out CelestialBody body)
        {
            const int min = 1000;
            const int defaultAlt = 100000;
            body = currentlyEditing.orbit.referenceBody;
            altitude = currentlyEditing.orbit.semiMajorAxis - body.Radius;
            if (altitude > min)
                return;
            altitude = currentlyEditing.orbit.ApA;
            if (altitude > min)
                return;
            altitude = defaultAlt;
        }

        public static void Complex(OrbitDriver currentlyEditing, double inclination, double eccentricity,
            double semiMajorAxis, double longitudeAscendingNode, double argumentOfPeriapsis,
            double meanAnomalyAtEpoch, double epoch, CelestialBody body)
        {
            SetOrbit(currentlyEditing, CreateOrbit(inclination, eccentricity, semiMajorAxis,
                longitudeAscendingNode, argumentOfPeriapsis, meanAnomalyAtEpoch, epoch, body));
        }

        public static void GetComplex(OrbitDriver currentlyEditing, out double inclination, out double eccentricity,
            out double semiMajorAxis, out double longitudeAscendingNode, out double argumentOfPeriapsis,
            out double meanAnomalyAtEpoch, out double epoch, out CelestialBody body)
        {
            inclination = currentlyEditing.orbit.inclination;
            eccentricity = currentlyEditing.orbit.eccentricity;
            semiMajorAxis = currentlyEditing.orbit.semiMajorAxis;
            longitudeAscendingNode = currentlyEditing.orbit.LAN;
            argumentOfPeriapsis = currentlyEditing.orbit.argumentOfPeriapsis;
            meanAnomalyAtEpoch = currentlyEditing.orbit.meanAnomalyAtEpoch;
            epoch = currentlyEditing.orbit.epoch;
            body = currentlyEditing.orbit.referenceBody;
        }

        public static void Graphical(OrbitDriver currentlyEditing, double inclination, double eccentricity,
            double periapsis, double longitudeAscendingNode, double argumentOfPeriapsis,
            double meanAnomaly, double epoch)
        {
            var body = currentlyEditing.orbit.referenceBody;
            var soi = body.Soi();
            var ratio = soi / (body.Radius + body.atmosphereDepth + 1000);
            periapsis = Math.Pow(ratio, periapsis) / ratio;
            periapsis *= soi;

            eccentricity *= Math.PI / 2 - 0.001;

            eccentricity = Math.Tan(eccentricity);
            var semimajor = periapsis / (1 - eccentricity);

            if (semimajor < 0)
            {
                meanAnomaly -= 0.5;
                meanAnomaly *= eccentricity * 4; // 4 is arbitrary constant
            }

            inclination *= 360;
            longitudeAscendingNode *= 360;
            argumentOfPeriapsis *= 360;
            meanAnomaly *= 2 * Math.PI;

            SetOrbit(currentlyEditing, CreateOrbit(inclination, eccentricity, semimajor, longitudeAscendingNode, argumentOfPeriapsis, meanAnomaly, epoch, body));
        }

        public static void GetGraphical(OrbitDriver currentlyEditing, out double inclination, out double eccentricity,
            out double periapsis, out double longitudeAscendingNode, out double argumentOfPeriapsis,
            out double meanAnomaly, out double epoch)
        {
            inclination = currentlyEditing.orbit.inclination / 360;
            inclination = inclination.Mod(1);
            longitudeAscendingNode = currentlyEditing.orbit.LAN / 360;
            longitudeAscendingNode = longitudeAscendingNode.Mod(1);
            argumentOfPeriapsis = currentlyEditing.orbit.argumentOfPeriapsis / 360;
            argumentOfPeriapsis = argumentOfPeriapsis.Mod(1);
            var eTemp = Math.Atan(currentlyEditing.orbit.eccentricity);
            eccentricity = eTemp / (Math.PI / 2 - 0.001);
            var soi = currentlyEditing.orbit.referenceBody.Soi();
            var ratio = soi / (currentlyEditing.orbit.referenceBody.Radius + currentlyEditing.orbit.referenceBody.atmosphereDepth + 1000);
            var semimajor = currentlyEditing.orbit.semiMajorAxis * (1 - currentlyEditing.orbit.eccentricity);
            semimajor /= soi;
            semimajor *= ratio;
            semimajor = Math.Log(semimajor, ratio);
            periapsis = semimajor;
            meanAnomaly = currentlyEditing.orbit.meanAnomalyAtEpoch;
            meanAnomaly /= (2 * Math.PI);
            if (currentlyEditing.orbit.semiMajorAxis < 0)
            {
                meanAnomaly /= currentlyEditing.orbit.eccentricity * 4;
                meanAnomaly += 0.5;
            }
            epoch = currentlyEditing.orbit.epoch;
        }

        public enum VelocityChangeDirection
        {
            Prograde,
            Normal,
            Radial,
            North,
            East,
            Up
        }

        public static VelocityChangeDirection[] AllVelocityChanges = Enum.GetValues(typeof(VelocityChangeDirection)).Cast<VelocityChangeDirection>().ToArray();

        public static void Velocity(OrbitDriver currentlyEditing, VelocityChangeDirection direction, double speed)
        {
            Vector3d velocity;
            switch (direction)
            {
                case VelocityChangeDirection.Prograde:
                    velocity = currentlyEditing.orbit.getOrbitalVelocityAtUT(Planetarium.GetUniversalTime()).normalized * speed;
                    break;
                case VelocityChangeDirection.Normal:
                    velocity = currentlyEditing.orbit.GetOrbitNormal().normalized * speed;
                    break;
                case VelocityChangeDirection.Radial:
                    velocity = Vector3d.Cross(currentlyEditing.orbit.getOrbitalVelocityAtUT(Planetarium.GetUniversalTime()), currentlyEditing.orbit.GetOrbitNormal()).normalized * speed;
                    break;
                case VelocityChangeDirection.North:
                    var upn = currentlyEditing.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).normalized;
                    velocity = Vector3d.Cross(Vector3d.Cross(upn, new Vector3d(0, 0, 1)), upn) * speed;
                    break;
                case VelocityChangeDirection.East:
                    var upe = currentlyEditing.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).normalized;
                    velocity = Vector3d.Cross(new Vector3d(0, 0, 1), upe) * speed;
                    break;
                case VelocityChangeDirection.Up:
                    velocity = currentlyEditing.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).normalized * speed;
                    break;
                default:
                    throw new Exception("Unknown VelChangeDir");
            }
            var tempOrbit = currentlyEditing.orbit.Clone();
            tempOrbit.UpdateFromStateVectors(currentlyEditing.orbit.pos, currentlyEditing.orbit.vel + velocity, currentlyEditing.orbit.referenceBody, Planetarium.GetUniversalTime());
            SetOrbit(currentlyEditing, tempOrbit);
        }

        public static void GetVelocity(OrbitDriver currentlyEditing, out VelocityChangeDirection direction, out double speed)
        {
            direction = VelocityChangeDirection.Prograde;
            speed = 0;
        }

        public static void Rendezvous(OrbitDriver currentlyEditing, double leadTime, Vessel target)
        {
            SetOrbit(currentlyEditing, CreateOrbit(
                target.orbit.inclination,
                target.orbit.eccentricity,
                target.orbit.semiMajorAxis,
                target.orbit.LAN,
                target.orbit.argumentOfPeriapsis,
                target.orbit.meanAnomalyAtEpoch,
                target.orbit.epoch - leadTime,
                target.orbit.referenceBody));
        }

        private static void SetOrbit(OrbitDriver currentlyEditing, Orbit orbit)
        {
            currentlyEditing.DynamicSetOrbit(orbit);
        }

        private static Orbit CreateOrbit(double inc, double e, double sma, double lan, double w, double mEp, double epoch, CelestialBody body)
        {
            if (double.IsNaN(inc))
                inc = 0;
            if (double.IsNaN(e))
                e = 0;
            if (double.IsNaN(sma))
                sma = body.Radius + body.atmosphereDepth + 10000;
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

        public static void DynamicSetOrbit(this OrbitDriver orbit, Orbit newOrbit)
        {
            var vessel = orbit.vessel;
            var body = orbit.celestialBody;
            if (vessel != null)
                vessel.SetOrbit(newOrbit);
            else if (body != null)
                body.SetOrbit(newOrbit);
            else
                HardsetOrbit(orbit, newOrbit);
        }

        public static void SetOrbit(this Vessel vessel, Orbit newOrbit)
        {
            var destinationMagnitude = newOrbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).magnitude;
            if (destinationMagnitude > newOrbit.referenceBody.sphereOfInfluence)
            {
                Extensions.ErrorPopup("Destination position was above the sphere of influence");
                return;
            }
            if (destinationMagnitude < newOrbit.referenceBody.Radius)
            {
                Extensions.ErrorPopup("Destination position was below the surface");
                return;
            }

            vessel.PrepVesselTeleport();

            try
            {
                OrbitPhysicsManager.HoldVesselUnpack(60);
            }
            catch (NullReferenceException)
            {
                Extensions.Log("OrbitPhysicsManager.HoldVesselUnpack threw NullReferenceException");
            }

            var allVessels = FlightGlobals.fetch == null ? (IEnumerable<Vessel>)new[] { vessel } : FlightGlobals.Vessels;
            foreach (var v in allVessels.Where(v => v.packed == false))
                v.GoOnRails();

            var oldBody = vessel.orbitDriver.orbit.referenceBody;

            HardsetOrbit(vessel.orbitDriver, newOrbit);

            vessel.orbitDriver.pos = vessel.orbit.pos.xzy;
            vessel.orbitDriver.vel = vessel.orbit.vel;

            var newBody = vessel.orbitDriver.orbit.referenceBody;
            if (newBody != oldBody)
            {
                var evnt = new GameEvents.HostedFromToAction<Vessel, CelestialBody>(vessel, oldBody, newBody);
                GameEvents.onVesselSOIChanged.Fire(evnt);
            }
        }

        public static void SetOrbit(this CelestialBody body, Orbit newOrbit)
        {
            var oldBody = body.referenceBody;
            HardsetOrbit(body.orbitDriver, newOrbit);
            if (oldBody != newOrbit.referenceBody)
            {
                oldBody.orbitingBodies.Remove(body);
                newOrbit.referenceBody.orbitingBodies.Add(body);
            }
            body.RealCbUpdate();
        }

        private static readonly object HardsetOrbitLogObject = new object();

        private static void HardsetOrbit(OrbitDriver orbitDriver, Orbit newOrbit)
        {
            var orbit = orbitDriver.orbit;
            orbit.inclination = newOrbit.inclination;
            orbit.eccentricity = newOrbit.eccentricity;
            orbit.semiMajorAxis = newOrbit.semiMajorAxis;
            orbit.LAN = newOrbit.LAN;
            orbit.argumentOfPeriapsis = newOrbit.argumentOfPeriapsis;
            orbit.meanAnomalyAtEpoch = newOrbit.meanAnomalyAtEpoch;
            orbit.epoch = newOrbit.epoch;
            orbit.referenceBody = newOrbit.referenceBody;
            orbit.Init();
            orbit.UpdateFromUT(Planetarium.GetUniversalTime());
            if (orbit.referenceBody != newOrbit.referenceBody)
            {
                if (orbitDriver.OnReferenceBodyChange != null)
                    orbitDriver.OnReferenceBodyChange(newOrbit.referenceBody);
            }
            RateLimitedLogger.Log(HardsetOrbitLogObject,
                string.Format("Orbit \"{0}\" changed to: inc={1} ecc={2} sma={3} lan={4} argpe={5} mep={6} epoch={7} refbody={8}",
                orbitDriver.OrbitDriverToString(), orbit.inclination, orbit.eccentricity, orbit.semiMajorAxis,
                orbit.LAN, orbit.argumentOfPeriapsis, orbit.meanAnomalyAtEpoch, orbit.epoch, orbit.referenceBody.CbToString()));
        }

        public static Orbit Clone(this Orbit o)
        {
            return new Orbit(o.inclination, o.eccentricity, o.semiMajorAxis, o.LAN,
                o.argumentOfPeriapsis, o.meanAnomalyAtEpoch, o.epoch, o.referenceBody);
        }
    }
}
