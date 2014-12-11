using System.Linq;
using UnityEngine;

namespace HyperEdit.View
{
    public class OrbitEditorView : View
    {
        Model.OrbitEditor _model;

        public static void Create(Model.OrbitEditor model)
        {
            var view = new OrbitEditorView();
            view._model = model;
            Window.Create("Orbit editor", 200, -1, view.Draw);
        }

        private OrbitEditorView() { }

        private float Slider(GUIContent display, float oldval, Model.SliderRange range, ref bool changed)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(display);
            var newval = GUILayout.HorizontalSlider(oldval, range.Min, range.Max);
            GUILayout.EndHorizontal();
            if (changed == false)
                changed = newval != oldval;
            return newval;
        }

        public override void Draw(Window window)
        {
            base.Draw(window);
            if (GUILayout.Button("Select orbit to edit"))
                _model.SelectOrbit();
            var name = _model.CurrentlyEditingName;
            GUILayout.Label(name == null ? "Nothing selected" : "Editing: " + name);

            var simple = _model.Editor as Model.OrbitEditor.Simple;
            var complex = _model.Editor as Model.OrbitEditor.Complex;
            var graphical = _model.Editor as Model.OrbitEditor.Graphical;
            var velocity = _model.Editor as Model.OrbitEditor.Velocity;
            var rendezvous = _model.Editor as Model.OrbitEditor.Rendezvous;

            if (_model.CurrentlyEditing != null)
            {
                GUILayout.BeginHorizontal();
                if (simple == null ? GUILayout.Button("Simple") : GUILayout.Button("Simple", Settings.PressedButton))
                {
                    _model.Editor = new Model.OrbitEditor.Simple(_model.CurrentlyEditing.orbit);
                    ClearTextFields();
                }
                if (complex == null ? GUILayout.Button("Complex") : GUILayout.Button("Complex", Settings.PressedButton))
                {
                    _model.Editor = new Model.OrbitEditor.Complex(_model.CurrentlyEditing.orbit);
                    ClearTextFields();
                }
                if (graphical == null ? GUILayout.Button("Graphical") : GUILayout.Button("Graphical", Settings.PressedButton))
                {
                    _model.Editor = new Model.OrbitEditor.Graphical(_model.CurrentlyEditing.orbit);
                    ClearTextFields();
                }
                if (velocity == null ? GUILayout.Button("Velocity") : GUILayout.Button("Velocity", Settings.PressedButton))
                {
                    _model.Editor = new Model.OrbitEditor.Velocity(_model.CurrentlyEditing.orbit);
                    ClearTextFields();
                }
                if (FlightGlobals.fetch != null && FlightGlobals.Vessels != null && FlightGlobals.Vessels.Any(v => v.orbitDriver == _model.CurrentlyEditing))
                {
                    if (rendezvous == null ? GUILayout.Button("Rendezvous") : GUILayout.Button("Rendezvous", Settings.PressedButton))
                    {
                        _model.Editor = new Model.OrbitEditor.Rendezvous(_model.CurrentlyEditing.orbit);
                        ClearTextFields();
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (simple != null)
            {
                simple.Altitude = GuiTextField("Altitude", new GUIContent("Altitude", "Altitude of circular orbit"), double.TryParse, simple.Altitude);
                GUILayout.Label(simple.Body == null ? "No body selected" : "Body: " + simple.Body.bodyName);
                if (GUILayout.Button("Select body"))
                {
                    _model.SelectBody();
                    ClearTextFields();
                }
                if (AllValid && GUILayout.Button("Set"))
                    _model.Apply();
            }
            if (complex != null)
            {
                complex.Inclination = GuiTextField("Inclination", new GUIContent("Inclination", "How close to the equator the orbit plane is"), double.TryParse, complex.Inclination);
                complex.Eccentricity = GuiTextField("Eccentricity", new GUIContent("Eccentricity", "How circular the orbit is (0=circular, 0.5=elliptical, 1=parabolic)"), double.TryParse, complex.Eccentricity);
                complex.SemiMajorAxis = GuiTextField("SemiMajorAxis", new GUIContent("Semi-major axis", "Mean radius of the orbit (ish)"), double.TryParse, complex.SemiMajorAxis);
                complex.LongitudeAscendingNode = GuiTextField("LongitudeAscendingNode", new GUIContent("Lon. of asc. node", "Longitude of the place where you cross the equator northwards"), double.TryParse, complex.LongitudeAscendingNode);
                complex.ArgumentOfPeriapsis = GuiTextField("ArgumentOfPeriapsis", new GUIContent("Argument of periapsis", "Rotation of the orbit around the normal"), double.TryParse, complex.ArgumentOfPeriapsis);
                complex.MeanAnomalyAtEpoch = GuiTextField("MeanAnomalyAtEpoch", new GUIContent("Mean anomaly at epoch", "Position along the orbit at the epoch"), double.TryParse, complex.MeanAnomalyAtEpoch);
                complex.Epoch = GuiTextField("Epoch", new GUIContent("Epoch", "Epoch at which mEp is measured"), double.TryParse, complex.Epoch);
                GUILayout.Label(complex.Body == null ? "No body selected" : "Body: " + complex.Body.bodyName);
                if (GUILayout.Button("Select body"))
                {
                    _model.SelectBody();
                    ClearTextFields();
                }
                if (AllValid && GUILayout.Button("Set"))
                    _model.Apply();
            }
            if (graphical != null)
            {
                var changed = false;
                graphical.Inclination = Slider(new GUIContent("Inclination", "How close to the equator the orbit plane is"), graphical.Inclination, graphical.InclinationRange, ref changed);
                graphical.Eccentricity = Slider(new GUIContent("Eccentricity", "How circular the orbit is"), graphical.Eccentricity, graphical.EccentricityRange, ref changed);
                graphical.Periapsis = Slider(new GUIContent("Periapsis", "Lowest point in the orbit"), graphical.Periapsis, graphical.PeriapsisRange, ref changed);
                graphical.LongitudeAscendingNode = Slider(new GUIContent("Lon. of asc. node", "Longitude of the place where you cross the equator northwards"), graphical.LongitudeAscendingNode, graphical.LongitudeAscendingNodeRange, ref changed);
                graphical.ArgumentOfPeriapsis = Slider(new GUIContent("Argument of periapsis", "Rotation of the orbit around the normal"), graphical.ArgumentOfPeriapsis, graphical.ArgumentOfPeriapsisRange, ref changed);
                graphical.MeanAnomaly = Slider(new GUIContent("Mean anomaly", "Position along the orbit"), graphical.MeanAnomaly, graphical.MeanAnomalyRange, ref changed);
                GUILayout.Label(graphical.Body == null ? "No body selected" : "Body: " + graphical.Body.bodyName);
                if (GUILayout.Button("Select body"))
                {
                    _model.SelectBody();
                    ClearTextFields();
                }
                if (changed)
                    _model.Apply();
            }
            if (velocity != null)
            {
                GUILayout.BeginHorizontal();
                foreach (var type in System.Enum.GetValues(typeof(Model.OrbitEditor.Velocity.ChangeDirection)))
                {
                    var value = (Model.OrbitEditor.Velocity.ChangeDirection)type;
                    if (value == velocity.Direction ? GUILayout.Button(value.ToString(), Settings.PressedButton) : GUILayout.Button(value.ToString()))
                        velocity.Direction = value;
                }
                GUILayout.EndHorizontal();
                velocity.Speed = GuiTextField("Speed", new GUIContent("Speed", "How much velocity to add (can be negative)"), double.TryParse, velocity.Speed);
                if (AllValid && GUILayout.Button("Add"))
                    _model.Apply();
            }
            if (rendezvous != null)
            {
                rendezvous.LeadTime = GuiTextField("LeadTime", new GUIContent("Lead time", "How many seconds off to rendezvous at (zero = on top of each other, bad)"), double.TryParse, rendezvous.LeadTime);
                GUILayout.Label(rendezvous.RendezvousWith == null ? "No vessel" : "Vessel: " + rendezvous.RendezvousWith.name);
                if (GUILayout.Button("Select vessel"))
                    rendezvous.SelectVessel();
                if (AllValid && rendezvous.RendezvousWith != null && GUILayout.Button("Rendezvous"))
                    _model.Apply();
            }
        }
    }
}
