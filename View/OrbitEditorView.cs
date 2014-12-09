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

        private float Slider(string display, float oldval, Model.SliderRange range, ref bool changed)
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

            if (simple != null)
            {
                simple.Altitude = GuiTextField("Altitude", "Altitude", double.TryParse, simple.Altitude);
                simple.Body = GuiTextField("Body", "Body", Extentions.CbTryParse, simple.Body, b => b.bodyName);
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
                complex.Inclination = GuiTextField("Inclination", "Inclination", double.TryParse, complex.Inclination);
                complex.Eccentricity = GuiTextField("Eccentricity", "Eccentricity", double.TryParse, complex.Eccentricity);
                complex.SemiMajorAxis = GuiTextField("SemiMajorAxis", "Semi-major axis", double.TryParse, complex.SemiMajorAxis);
                complex.LongitudeAscendingNode = GuiTextField("LongitudeAscendingNode", "Lon. of asc. node", double.TryParse, complex.LongitudeAscendingNode);
                complex.ArgumentOfPeriapsis = GuiTextField("ArgumentOfPeriapsis", "Argument of periapsis", double.TryParse, complex.ArgumentOfPeriapsis);
                complex.MeanAnomalyAtEpoch = GuiTextField("MeanAnomalyAtEpoch", "Mean anomaly at epoch", double.TryParse, complex.MeanAnomalyAtEpoch);
                complex.Epoch = GuiTextField("Epoch", "Epoch", double.TryParse, complex.Epoch);
                complex.Body = GuiTextField("Body", "Body", Extentions.CbTryParse, complex.Body, b => b.bodyName);
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
                graphical.Inclination = Slider("Inclination", graphical.Inclination, graphical.InclinationRange, ref changed);
                graphical.Eccentricity = Slider("Eccentricity", graphical.Eccentricity, graphical.EccentricityRange, ref changed);
                graphical.Periapsis = Slider("Semi-major axis", graphical.Periapsis, graphical.PeriapsisRange, ref changed);
                graphical.LongitudeAscendingNode = Slider("Lon. of asc. node", graphical.LongitudeAscendingNode, graphical.LongitudeAscendingNodeRange, ref changed);
                graphical.ArgumentOfPeriapsis = Slider("Argument of periapsis", graphical.ArgumentOfPeriapsis, graphical.ArgumentOfPeriapsisRange, ref changed);
                graphical.MeanAnomaly = Slider("Mean anomaly", graphical.MeanAnomaly, graphical.MeanAnomalyRange, ref changed);
                GUILayout.Label(graphical.Body == null ? "No body selected" :  "Body: " + graphical.Body.bodyName);
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
                    if (value == velocity.Direction ? GUILayout.Button(value.ToString(), Settings.PressedButton) :  GUILayout.Button(value.ToString()))
                        velocity.Direction = value;
                }
                GUILayout.EndHorizontal();
                velocity.Speed = GuiTextField("Speed", "Speed", double.TryParse, velocity.Speed);
                if (AllValid && GUILayout.Button("Add"))
                    _model.Apply();
            }
            if (rendezvous != null)
            {
                rendezvous.LeadTime = GuiTextField("LeadTime", "Lead time", double.TryParse, rendezvous.LeadTime);
                GUILayout.Label(rendezvous.RendezvousWith == null ? "No vessel" : "Vessel: " + rendezvous.RendezvousWith.name);
                if (GUILayout.Button("Select vessel"))
                    rendezvous.SelectVessel();
                if (AllValid && rendezvous.RendezvousWith != null && GUILayout.Button("Rendezvous"))
                    _model.Apply();
            }
        }
    }
}
