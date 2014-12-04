//
// This file is part of the HyperEdit plugin for Kerbal Space Program, Copyright Erickson Swift, 2013.
// HyperEdit is licensed under the GPL, found in COPYING.txt.
// Currently supported by Team HyperEdit, and Ezriilc.
// Original HyperEdit concept and code by khyperia (no longer involved).
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HyperEdit
{
    public class OrbitEditor : Window
    {
        private OrbitDriver _orbit;
        private VelChangeDir _velChangeDir = VelChangeDir.Prograde;

        public OrbitEditor()
        {
            EnsureSingleton(this);
            Title = "Orbit editor";
            WindowRect = new Rect(100, 200, 200, 5);
            SwitchModes(0);
        }

        private void OrbitAliveCheck()
        {
            if (_orbit == null)
            {
                if (FlightGlobals.fetch != null && FlightGlobals.fetch.activeVessel != null)
                    _orbit = FlightGlobals.fetch.activeVessel.orbitDriver;
                if (_orbit != null)
                    SwitchModes(EditMode.Simple);
                return;
            }
            if (IsOrbitAlive())
                return;
            _orbit = null;
            SwitchModes(EditMode.Simple);
        }

        private bool IsOrbitAlive()
        {
            if (FlightGlobals.fetch == null)
                return false;
            if (FlightGlobals.Vessels.Any(o => o.orbitDriver == _orbit))
                return true;
            if (FlightGlobals.Bodies.Any(o => o.orbitDriver == _orbit))
                return true;
            return false;
        }

        private void SwitchModes(EditMode mode)
        {
            Contents = new List<IWindowContent>
                {
                    new CustomDisplay(OrbitAliveCheck),
                    new Button("Close", CloseWindow),
                    new Button("Select orbit to edit", SelectOrbit),
                    new Label("Editing: " + (GetName(_orbit) ?? "Nothing selected"))
                };
            if (_orbit != null)
            {
                Contents.Add(new ListSelect<EditMode>(
                    FlightGlobals.fetch != null && FlightGlobals.fetch.vessels.Select(v => v.orbitDriver).Contains(_orbit) ?
                    new[] { EditMode.Simple, EditMode.Complex, EditMode.Graphical, EditMode.Velocity, EditMode.Rendezvous } :
                    new[] { EditMode.Simple, EditMode.Complex, EditMode.Graphical, EditMode.Velocity },
                    e => e.ToString(), SwitchModes) { Selected = mode });
                var orbit = _orbit.orbit;
                switch (mode)
                {
                    case EditMode.Simple:
                        Contents.Add(new TextBox("altitude", orbit.altitude.ToString()));
                        Contents.Add(new TextBox("body", orbit.referenceBody.bodyName));
                        Contents.Add(new Button("Select body", SelectBody));
                        Contents.Add(new Button("Set", SetSimple));
                        break;
                    case EditMode.Complex:
                        Contents.Add(new TextBox("inc", orbit.inclination.ToString()));
                        Contents.Add(new TextBox("e", orbit.eccentricity.ToString()));
                        Contents.Add(new TextBox("sma", orbit.semiMajorAxis.ToString()));
                        Contents.Add(new TextBox("lan", orbit.LAN.ToString()));
                        Contents.Add(new TextBox("w", orbit.argumentOfPeriapsis.ToString()));
                        Contents.Add(new TextBox("mEp", orbit.meanAnomalyAtEpoch.ToString()));
                        Contents.Add(new TextBox("epoch", orbit.epoch.ToString()));
                        Contents.Add(new TextBox("body", orbit.referenceBody.bodyName));
                        Contents.Add(new Button("Set", SetComplex));
                        break;
                    case EditMode.Graphical:
                        Contents.Add(new Button("Select body", SelectBodyImmediate));
                        Contents.Add(new Slider("inc", 0, 360, FindField<Slider, float>("inc"), SliderUpdate));
                        Contents.Add(new Slider("e", 0, Mathf.PI / 2 - 0.001f, FindField<Slider, float>("e"), SliderUpdate));
                        Contents.Add(new Slider("pe", 0.01f, 1, FindField<Slider, float>("pe"), SliderUpdate));
                        Contents.Add(new Slider("lan", 0, 360, FindField<Slider, float>("lan"), SliderUpdate));
                        Contents.Add(new Slider("w", 0, 360, FindField<Slider, float>("w"), SliderUpdate));
                        Contents.Add(new Slider("mEp", 0, Mathf.PI * 2, FindField<Slider, float>("mEp"), SliderUpdate));
                        RefreshSlider();
                        break;
                    case EditMode.Velocity:
                        _velChangeDir = VelChangeDir.Prograde;
                        Contents.Add(new ListSelect<VelChangeDir>((VelChangeDir[])Enum.GetValues(typeof(VelChangeDir)), v => v.ToString(), x => _velChangeDir = x) { Selected = _velChangeDir });
                        Contents.Add(new TextBox("speed", "0", OnVelChangeSet));
                        break;
                    case EditMode.Rendezvous:
                        if (FlightGlobals.fetch == null || FlightGlobals.fetch.vessels == null)
                            Contents.Add(new Label("Error: No vessels found"));
                        else
                        {
                            Contents.Add(new Button("Select vessel", SelectRendezvous));
                            Contents.Add(new TextBox("Lead time", "0.1"));
                        }
                        break;
                }
            }
            WindowRect = WindowRect.Set(300, 5);
        }

        private void OnVelChangeSet(string s)
        {
            double speed;
            if (double.TryParse(s, out speed) == false)
            {
                ErrorPopup.Error("Speed was not a number");
                return;
            }
            Vector3d velocity;
            switch (_velChangeDir)
            {
                case VelChangeDir.Prograde:
                    velocity = _orbit.orbit.getOrbitalVelocityAtUT(Planetarium.GetUniversalTime()).normalized * speed;
                    break;
                case VelChangeDir.Normal:
                    velocity = _orbit.orbit.GetOrbitNormal().normalized * speed;
                    break;
                case VelChangeDir.Radial:
                    velocity = Vector3d.Cross(_orbit.orbit.getOrbitalVelocityAtUT(Planetarium.GetUniversalTime()), _orbit.orbit.GetOrbitNormal()).normalized * speed;
                    break;
                case VelChangeDir.North:
                    var upn = _orbit.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).normalized;
                    velocity = Vector3d.Cross(Vector3d.Cross(upn, new Vector3d(0, 0, 1)), upn) * speed;
                    break;
                case VelChangeDir.East:
                    var upe = _orbit.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).normalized;
                    velocity = Vector3d.Cross(new Vector3d(0, 0, 1), upe) * speed;
                    break;
                case VelChangeDir.Up:
                    velocity = _orbit.orbit.getRelativePositionAtUT(Planetarium.GetUniversalTime()).normalized * speed;
                    break;
                default:
                    ErrorPopup.Error("Unknown VelChangeDir");
                    return;
            }
            var tempOrbit = _orbit.orbit.Clone();
            tempOrbit.UpdateFromStateVectors(_orbit.orbit.pos, _orbit.orbit.vel + velocity, _orbit.referenceBody, Planetarium.GetUniversalTime());
            _orbit.orbit.Set(tempOrbit);
        }

        private static string GetName(OrbitDriver orbit)
        {
            if (orbit == null)
                return null;
            var body = FlightGlobals.Bodies.FirstOrDefault(cb => cb.orbitDriver != null && cb.orbitDriver == orbit);
            if (body != null)
                return body.bodyName;
            var vessel = FlightGlobals.Vessels.FirstOrDefault(v => v.orbitDriver != null && v.orbitDriver == orbit);
            if (vessel != null)
                return vessel == FlightGlobals.ActiveVessel ? "Active vessel" : vessel.vesselName;
            if (string.IsNullOrEmpty(orbit.name) == false)
                return orbit.name;
            return "Unknown";
        }

        private void SelectOrbit()
        {
            if (FlightGlobals.fetch == null)
            {
                ErrorPopup.Error("Could not get the list of orbits (are you in the flight scene?)");
                return;
            }
            new Selector<OrbitDriver>("Select orbit", OrderedOrbits(), GetName, o =>
                {
                    _orbit = o;
                    SwitchModes(0);
                }).OpenWindow();
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

        private void SelectRendezvous()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.Vessels == null)
            {
                ErrorPopup.Error("Could not get the list of orbits (are you in the flight scene?)");
                return;
            }
            new Selector<Vessel>("Select vessel to rendezvous with", FlightGlobals.Vessels.Where(v => v.orbitDriver != _orbit), v => v.name, RendezvousWith).OpenWindow();
        }

        private void RendezvousWith(Vessel vessel)
        {
            double leadTime;
            if (double.TryParse(FindField<TextBox, string>("Lead time"), out leadTime) == false)
            {
                ErrorPopup.Error("Lead time was not a number");
                return;
            }
            var targetOrbit = vessel.orbit.Clone();
            targetOrbit.epoch -= leadTime;
            _orbit.orbit.Set(targetOrbit);
        }

        private void SliderUpdate(float value)
        {
            var body = _orbit.referenceBody;
            var soi = body.Soi();
            var pe = (double)FindField<Slider, float>("pe");
            var ratio = soi / (body.Radius + body.maxAtmosphereAltitude);
            pe = Math.Pow(ratio, pe) / ratio;
            pe *= soi;

            var e = Math.Tan(FindField<Slider, float>("e"));
            var semimajor = pe / (1 - e);

            var mep = FindField<Slider, float>("mEp");
            if (semimajor < 0)
            {
                mep /= Mathf.PI;
                mep -= 1;
                mep *= 5;
            }

            _orbit.orbit.Set(CreateOrbit(FindField<Slider, float>("inc"),
                                         e,
                                         semimajor,
                                         FindField<Slider, float>("lan"),
                                         FindField<Slider, float>("w"),
                                         mep,
                                         _orbit.orbit.GetVessel() == null ? Planetarium.GetUniversalTime() : _orbit.orbit.epoch,
                                         body));
        }

        private void RefreshSlider()
        {
            var body = _orbit.referenceBody;
            var orbit = _orbit.orbit;
            SetField<Slider, float>("inc", (float)orbit.inclination);
            SetField<Slider, float>("lan", (float)orbit.LAN);
            SetField<Slider, float>("w", (float)orbit.argumentOfPeriapsis);

            var e = (float)Math.Atan(orbit.eccentricity);
            SetField<Slider, float>("e", e);

            var soi = body.Soi();
            var ratio = soi / ((float)body.Radius + body.maxAtmosphereAltitude);
            var semimajor = (float)orbit.semiMajorAxis * (1 - e);
            semimajor /= soi;
            semimajor *= ratio;
            semimajor = (float)Math.Log(semimajor, ratio);
            SetField<Slider, float>("pe", semimajor);

            var mep = (float)orbit.meanAnomalyAtEpoch;
            if (orbit.semiMajorAxis < 0)
            {
                mep /= 5;
                mep += 1;
                mep *= Mathf.PI;
            }

            SetField<Slider, float>("mEp", mep);
        }

        private void SetComplex()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.Bodies == null)
            {
                ErrorPopup.Error("Could not get the list of planets (are you in the flight scene?)");
                return;
            }

            double inc, e, sma, lan, w, mEp, epoch;
            var epochText = FindField<TextBox, string>("epoch");
            if (epochText.ToLower() == "now")
                epoch = Planetarium.GetUniversalTime();
            else if (string.IsNullOrEmpty(epochText))
                epoch = _orbit.orbit.epoch;
            else if (double.TryParse(epochText, out epoch) == false)
            {
                ErrorPopup.Error("An orbital parameter was not a number");
                return;
            }
            if (double.TryParse(FindField<TextBox, string>("inc"), out inc) == false ||
                double.TryParse(FindField<TextBox, string>("e"), out e) == false ||
                double.TryParse(FindField<TextBox, string>("sma"), out sma) == false ||
                double.TryParse(FindField<TextBox, string>("lan"), out lan) == false ||
                double.TryParse(FindField<TextBox, string>("w"), out w) == false ||
                double.TryParse(FindField<TextBox, string>("mEp"), out mEp) == false)
            {
                ErrorPopup.Error("An orbital parameter was not a number");
                return;
            }

            var body = FlightGlobals.Bodies.FirstOrDefault(c => c.bodyName.ToLower() == (FindField<TextBox, string>("body") ?? "").ToLower());
            if (body == null)
            {
                ErrorPopup.Error("Unknown body");
                return;
            }
            _orbit.orbit.Set(CreateOrbit(inc, e, sma, lan, w, mEp, epoch, body));
        }

        private void SetSimple()
        {
            double altitude;
            if (double.TryParse(FindField<TextBox, string>("altitude"), out altitude) == false)
            {
                ErrorPopup.Error("Altitude was not a number");
                return;
            }
            var body = FlightGlobals.Bodies.FirstOrDefault(c => c.bodyName.ToLower() == (FindField<TextBox, string>("body") ?? "").ToLower());
            if (body == null)
            {
                ErrorPopup.Error("Unknown body");
                return;
            }
            _orbit.orbit.Set(CreateOrbit(0, 0, altitude + body.Radius, 0, 0, 0, 0, body));
        }

        private void SelectBody()
        {
            new Selector<CelestialBody>("Select body", FlightGlobals.Bodies, cb => cb.bodyName, cb => SetField<TextBox, string>("body", cb.bodyName)).OpenWindow();
        }

        private void SelectBodyImmediate()
        {
            new Selector<CelestialBody>("Select body", FlightGlobals.Bodies, cb => cb.bodyName, SetBody).OpenWindow();
        }

        private void SetBody(CelestialBody body)
        {
            var orbit = _orbit.orbit.Clone();
            orbit.referenceBody = body;
            var soi = body.Soi();
            if (orbit.PeA < 50000 || Math.Abs(orbit.ApR) > soi * 0.75)
                orbit = CreateOrbit(orbit.inclination, 0, (body.Radius + soi) / 2, orbit.LAN, orbit.argumentOfPeriapsis, orbit.meanAnomalyAtEpoch, orbit.epoch, body);
            _orbit.orbit.Set(orbit);
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

        private enum VelChangeDir
        {
            Prograde,
            Normal,
            Radial,
            North,
            East,
            Up
        }

        private enum EditMode
        {
            Simple,
            Complex,
            Graphical,
            Velocity,
            Rendezvous
        }
    }
}