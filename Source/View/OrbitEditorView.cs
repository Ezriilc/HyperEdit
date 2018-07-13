using System;
using System.Collections.Generic;

namespace HyperEdit.View
{
    public static class OrbitEditorView
    {
        public static Action Create()
        {
            var view = View();
            return () => Window.Create("Orbit Editor", true, true, 300, -1, w => view.Draw());
        }

        // Also known as "closure hell"
        public static IView View()
        {
            ListSelectView<OrbitDriver> currentlyEditing = null;
            Action<OrbitDriver> onCurrentlyEditingChange = null;

            var setToCurrentOrbit = new ButtonView("Set to current orbit", "Sets all the fields of the editor to reflect the orbit of the currently selected vessel",
                () => onCurrentlyEditingChange(currentlyEditing.CurrentlySelected));

            var referenceSelector = new ListSelectView<CelestialBody>("Reference body", () => FlightGlobals.fetch == null ? null : FlightGlobals.fetch.bodies, null, Extensions.CbToString);

            #region Simple
            var simpleAltitude = new TextBoxView<double>("Altitude", "Altitude of circular orbit", 110000, Model.SiSuffix.TryParse);
            var simpleApply = new ConditionalView(() => simpleAltitude.Valid && referenceSelector.CurrentlySelected != null,
                                  new ButtonView("Apply", "Sets the orbit", () =>
                    {
                        Model.OrbitEditor.Simple(currentlyEditing.CurrentlySelected, simpleAltitude.Object, referenceSelector.CurrentlySelected);

                        currentlyEditing.ReInvokeOnSelect();
                    }));
            var simple = new VerticalView(new IView[]
                {
                    simpleAltitude,
                    referenceSelector,
                    simpleApply,
                    setToCurrentOrbit
                });
            #endregion

            #region Complex
            var complexInclination = new TextBoxView<double>("Inclination", "How close to the equator the orbit plane is", 0, double.TryParse);
            var complexEccentricity = new TextBoxView<double>("Eccentricity", "How circular the orbit is (0=circular, 0.5=elliptical, 1=parabolic)", 0, double.TryParse);
            var complexSemiMajorAxis = new TextBoxView<double>("Semi-major axis", "Mean radius of the orbit (ish)", 10000000, Model.SiSuffix.TryParse);
            var complexLongitudeAscendingNode = new TextBoxView<double>("Lon. of asc. node", "Longitude of the place where you cross the equator northwards", 0, double.TryParse);
            var complexArgumentOfPeriapsis = new TextBoxView<double>("Argument of periapsis", "Rotation of the orbit around the normal", 0, double.TryParse);
            var complexMeanAnomalyAtEpoch = new TextBoxView<double>("Mean anomaly at epoch", "Position along the orbit at the epoch", 0, double.TryParse);
            var complexEpoch = new TextBoxView<double>("Epoch", "Epoch at which mEp is measured", 0, Model.SiSuffix.TryParse);
            var complexEpochNow = new ButtonView("Set epoch to now", "Sets the Epoch field to the current time", () => complexEpoch.Object = Planetarium.GetUniversalTime());
            var complexApply = new ConditionalView(() => complexInclination.Valid &&
                                   complexEccentricity.Valid &&
                                   complexSemiMajorAxis.Valid &&
                                   complexLongitudeAscendingNode.Valid &&
                                   complexArgumentOfPeriapsis.Valid &&
                                   complexMeanAnomalyAtEpoch.Valid &&
                                   complexEpoch.Valid &&
                                   referenceSelector.CurrentlySelected != null,
                                   new ButtonView("Apply", "Sets the orbit", () =>
                    {
                        Model.OrbitEditor.Complex(currentlyEditing.CurrentlySelected,
                            complexInclination.Object,
                            complexEccentricity.Object,
                            complexSemiMajorAxis.Object,
                            complexLongitudeAscendingNode.Object,
                            complexArgumentOfPeriapsis.Object,
                            complexMeanAnomalyAtEpoch.Object,
                            complexEpoch.Object,
                            referenceSelector.CurrentlySelected);

                        currentlyEditing.ReInvokeOnSelect();
                    }));
            var complex = new VerticalView(new IView[]
                {
                    complexInclination,
                    complexEccentricity,
                    complexSemiMajorAxis,
                    complexLongitudeAscendingNode,
                    complexArgumentOfPeriapsis,
                    complexMeanAnomalyAtEpoch,
                    complexEpoch,
                    complexEpochNow,
                    referenceSelector,
                    complexApply,
                    setToCurrentOrbit
                });
            #endregion

            #region Graphical
            SliderView graphicalInclination = null;
            SliderView graphicalEccentricity = null;
            SliderView graphicalPeriapsis = null;
            SliderView graphicalLongitudeAscendingNode = null;
            SliderView graphicalArgumentOfPeriapsis = null;
            SliderView graphicalMeanAnomaly = null;
            double graphicalEpoch = 0;

            Action<double> graphicalOnChange = ignored =>
            {
                Model.OrbitEditor.Graphical(currentlyEditing.CurrentlySelected,
                    graphicalInclination.Value,
                    graphicalEccentricity.Value,
                    graphicalPeriapsis.Value,
                    graphicalLongitudeAscendingNode.Value,
                    graphicalArgumentOfPeriapsis.Value,
                    graphicalMeanAnomaly.Value,
                    graphicalEpoch);

                currentlyEditing.ReInvokeOnSelect();
            };

            graphicalInclination = new SliderView("Inclination", "How close to the equator the orbit plane is", graphicalOnChange);
            graphicalEccentricity = new SliderView("Eccentricity", "How circular the orbit is", graphicalOnChange);
            graphicalPeriapsis = new SliderView("Periapsis", "Lowest point in the orbit", graphicalOnChange);
            graphicalLongitudeAscendingNode = new SliderView("Lon. of asc. node", "Longitude of the place where you cross the equator northwards", graphicalOnChange);
            graphicalArgumentOfPeriapsis = new SliderView("Argument of periapsis", "Rotation of the orbit around the normal", graphicalOnChange);
            graphicalMeanAnomaly = new SliderView("Mean anomaly", "Position along the orbit", graphicalOnChange);

            var graphical = new VerticalView(new IView[]
                {
                    graphicalInclination,
                    graphicalEccentricity,
                    graphicalPeriapsis,
                    graphicalLongitudeAscendingNode,
                    graphicalArgumentOfPeriapsis,
                    graphicalMeanAnomaly,
                    setToCurrentOrbit
                });
            #endregion

            #region Velocity
            var velocitySpeed = new TextBoxView<double>("Speed", "dV to apply", 0, Model.SiSuffix.TryParse);
            var velocityDirection = new ListSelectView<Model.OrbitEditor.VelocityChangeDirection>("Direction", () => Model.OrbitEditor.AllVelocityChanges);
            var velocityApply = new ConditionalView(() => velocitySpeed.Valid,
                                    new ButtonView("Apply", "Adds the selected velocity to the orbit", () =>
                    {
                        Model.OrbitEditor.Velocity(currentlyEditing.CurrentlySelected, velocityDirection.CurrentlySelected, velocitySpeed.Object);
                    }));
            var velocity = new VerticalView(new IView[]
                {
                    velocitySpeed,
                    velocityDirection,
                    velocityApply
                });
            #endregion

            #region Rendezvous
            var rendezvousLeadTime = new TextBoxView<double>("Lead time", "How many seconds off to rendezvous at (zero = on top of each other, bad)", 1, Model.SiSuffix.TryParse);
            var rendezvousVessel = new ListSelectView<Vessel>("Target vessel", () => FlightGlobals.fetch == null ? null : FlightGlobals.fetch.vessels, null, Extensions.VesselToString);
            var rendezvousApply = new ConditionalView(() => rendezvousLeadTime.Valid && rendezvousVessel.CurrentlySelected != null,
                                      new ButtonView("Apply", "Rendezvous", () =>
                    {
                        Model.OrbitEditor.Rendezvous(currentlyEditing.CurrentlySelected, rendezvousLeadTime.Object, rendezvousVessel.CurrentlySelected);
                    }));
            // rendezvous gets special ConditionalView to force only editing of planets
            var rendezvous = new ConditionalView(() => currentlyEditing.CurrentlySelected != null && currentlyEditing.CurrentlySelected.vessel != null,
                                 new VerticalView(new IView[]
                    {
                        rendezvousLeadTime,
                        rendezvousVessel,
                        rendezvousApply
                    }));
            #endregion

            #region CurrentlyEditing
            onCurrentlyEditingChange = newEditing =>
            {
                if (newEditing == null)
                {
                    return;
                }
                {
                    double altitude;
                    CelestialBody body;
                    Model.OrbitEditor.GetSimple(newEditing, out altitude, out body);
                    simpleAltitude.Object = altitude;
                    referenceSelector.CurrentlySelected = body;
                }
                {
                    double inclination;
                    double eccentricity;
                    double semiMajorAxis;
                    double longitudeAscendingNode;
                    double argumentOfPeriapsis;
                    double meanAnomalyAtEpoch;
                    double epoch;
                    CelestialBody body;
                    Model.OrbitEditor.GetComplex(newEditing,
                        out inclination,
                        out eccentricity,
                        out semiMajorAxis,
                        out longitudeAscendingNode,
                        out argumentOfPeriapsis,
                        out meanAnomalyAtEpoch,
                        out epoch,
                        out body);
                    complexInclination.Object = inclination;
                    complexEccentricity.Object = eccentricity;
                    complexSemiMajorAxis.Object = semiMajorAxis;
                    complexLongitudeAscendingNode.Object = longitudeAscendingNode;
                    complexArgumentOfPeriapsis.Object = argumentOfPeriapsis;
                    complexMeanAnomalyAtEpoch.Object = meanAnomalyAtEpoch;
                    complexEpoch.Object = epoch;
                    referenceSelector.CurrentlySelected = body;
                }
                {
                    double inclination;
                    double eccentricity;
                    double periapsis;
                    double longitudeAscendingNode;
                    double argumentOfPeriapsis;
                    double meanAnomaly;
                    Model.OrbitEditor.GetGraphical(newEditing,
                        out inclination,
                        out eccentricity,
                        out periapsis,
                        out longitudeAscendingNode,
                        out argumentOfPeriapsis,
                        out meanAnomaly,
                        out graphicalEpoch);
                    graphicalInclination.Value = inclination;
                    graphicalEccentricity.Value = eccentricity;
                    graphicalPeriapsis.Value = periapsis;
                    graphicalLongitudeAscendingNode.Value = longitudeAscendingNode;
                    graphicalArgumentOfPeriapsis.Value = argumentOfPeriapsis;
                    graphicalMeanAnomaly.Value = meanAnomaly;
                }
                {
                    Model.OrbitEditor.VelocityChangeDirection direction;
                    double speed;
                    Model.OrbitEditor.GetVelocity(newEditing, out direction, out speed);
                    velocityDirection.CurrentlySelected = direction;
                    velocitySpeed.Object = speed;
                }
            };

            currentlyEditing = new ListSelectView<OrbitDriver>("Currently editing", Model.OrbitEditor.OrderedOrbits, onCurrentlyEditingChange, Extensions.OrbitDriverToString);

            if (FlightGlobals.fetch != null && FlightGlobals.fetch.activeVessel != null && FlightGlobals.fetch.activeVessel.orbitDriver != null)
            {
                currentlyEditing.CurrentlySelected = FlightGlobals.fetch.activeVessel.orbitDriver;
            }
            #endregion

            var savePlanet = new ButtonView("Save planet", "Saves the current orbit of the planet to a file, so it stays edited even after a restart. Delete the file named the planet's name in " + IoExt.GetPath(null) + " to undo.",
                                 () => Model.PlanetEditor.SavePlanet(currentlyEditing.CurrentlySelected.celestialBody));
            var resetPlanet = new ButtonView("Reset to defaults", "Reset the selected planet to defaults",
                                  () => Model.PlanetEditor.ResetToDefault(currentlyEditing.CurrentlySelected.celestialBody));

            var planetButtons = new ConditionalView(() => currentlyEditing.CurrentlySelected?.celestialBody != null,
                                    new VerticalView(new IView[]
                    {
                        savePlanet,
                        resetPlanet
                    }));

            var tabs = new TabView(new List<KeyValuePair<string, IView>>()
                {
                    new KeyValuePair<string, IView>("Simple", simple),
                    new KeyValuePair<string, IView>("Complex", complex),
                    new KeyValuePair<string, IView>("Graphical", graphical),
                    new KeyValuePair<string, IView>("Velocity", velocity),
                    new KeyValuePair<string, IView>("Rendezvous", rendezvous),
                });

            return new VerticalView(new IView[]
                {
                    currentlyEditing,
                    planetButtons,
                    new ConditionalView(() => currentlyEditing.CurrentlySelected != null, tabs)
                });
        }
    }
}
