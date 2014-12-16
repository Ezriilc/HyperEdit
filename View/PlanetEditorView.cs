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
                settings.GeeASL = GuiTextField("GeeASL", new GUIContent("Gravity", "Multiplier strength of gravity"), SiSuffix.TryParse, settings.GeeASL);
                settings.AtmoshpereTemperatureMultiplier = GuiTextField("Temperature", new GUIContent("Temperature", "Temperature multiplier of atmosphere"), SiSuffix.TryParse, settings.AtmoshpereTemperatureMultiplier);
                settings.Atmosphere = GUILayout.Toggle(settings.Atmosphere, new GUIContent("Has atmosphere", "Toggles if the body has an atmosphere"));
                settings.AtmosphereMultiplier = GuiTextField("AtmosphereMultiplier", new GUIContent("Atmosphere pressure", "Atmosphere pressure"), SiSuffix.TryParse, settings.AtmosphereMultiplier);
                settings.AtmosphereScaleHeight = GuiTextField("AtmosphereScaleHeight", new GUIContent("Atmosphere height", "Scale height of the atmosphere"), SiSuffix.TryParse, settings.AtmosphereScaleHeight);
                settings.AtmosphericAmbientColor = GuiTextField("AtmosphericAmbientColor", new GUIContent("Atmosphere color", "Color of the light emitted by the atmosphere (doesn't change actual color)"), Extentions.ColorTryParse, settings.AtmosphericAmbientColor);
                settings.SphereOfInfluence = GuiTextField("SphereOfInfluence", new GUIContent("Sphere of influence", "Radius of the SOI of the planet"), SiSuffix.TryParse, settings.SphereOfInfluence);
                settings.RotationPeriod = GuiTextField("RotationPeriod", new GUIContent("Rotation period", "Seconds per revolution of the planet"), SiSuffix.TryParse, settings.RotationPeriod);
                settings.TidallyLocked = GUILayout.Toggle(settings.TidallyLocked, new GUIContent("Tidally locked", "If rotation period is equal to orbital period"));
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
                if (GUILayout.Button(new GUIContent("Save planet to config file",
                    "Saves the current configuration of the planet to a file, so it stays edited even after a restart. Delete the file named the planet's name in ./GameData/Kerbaltek/PluginData/HyperEdit/ to undo.")))
                {
                    _model.SavePlanet();
                }
            }
            if (GUILayout.Button(new GUIContent("Reload config files", "Reloads the planet .cfg files in ./GameData/Kerbaltek/PluginData/HyperEdit/")))
            {
                Model.PlanetEditor.ApplyFileDefaults();
            }
        }
    }
}