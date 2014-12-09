using UnityEngine;

namespace HyperEdit.View
{
    public class PlanetEditorView : View
    {
        Model.PlanetEditor _model;
        public static void Create(Model.PlanetEditor model)
        {
            var view = new PlanetEditorView();
            view._model = model;
            Window.Create("Planet editor", 400, -1, view.Draw);
        }

        private PlanetEditorView() { }

        public override void Draw(Window window)
        {
            base.Draw(window);
            if (GUILayout.Button("Select planet"))
                _model.SelectPlanet();
            GUILayout.Label(_model.CurrentBody == null ? "Nothing selected" : "Editing: " + _model.CurrentBody.bodyName);
            if (_model.CurrentBody != null)
            {
                var settings = _model.CurrentSettings;
                settings.GeeASL = GuiTextField("GeeASL", "Gravity", double.TryParse, settings.GeeASL);
                settings.AtmoshpereTemperatureMultiplier = GuiTextField("Temperature", "Temperature", float.TryParse, settings.AtmoshpereTemperatureMultiplier);
                settings.Atmosphere = GUILayout.Toggle(settings.Atmosphere, "Has atmosphere");
                settings.AtmosphereMultiplier = GuiTextField("AtmosphereMultiplier", "Atmosphere pressure", float.TryParse, settings.AtmosphereMultiplier);
                settings.AtmosphereScaleHeight = GuiTextField("AtmosphereScaleHeight", "Atmosphere height", double.TryParse, settings.AtmosphereScaleHeight);
                settings.AtmosphericAmbientColor = GuiTextField("AtmosphericAmbientColor", "Atmosphere color", Extentions.ColorTryParse, settings.AtmosphericAmbientColor);
                settings.SphereOfInfluence = GuiTextField("SphereOfInfluence", "Sphere of influence", double.TryParse, settings.SphereOfInfluence);
                settings.RotationPeriod = GuiTextField("RotationPeriod", "Rotation period", double.TryParse, settings.RotationPeriod);
                settings.TidallyLocked = GUILayout.Toggle(settings.TidallyLocked, "tidally locked");
                _model.CurrentSettings = settings;
                if (AllValid && GUILayout.Button("Apply"))
                {
                    _model.Apply();
                    ClearTextFields();
                }
                if (_model.IsNotDefault)
                {
                    if (GUILayout.Button("Reset to defaults"))
                    {
                        _model.ResetToDefault();
                        ClearTextFields();
                    }
                }
            }
        }
    }
}
