using System;
using UnityEngine;

namespace HyperEdit.View
{
    public static class PlanetEditorView
    {
        public static void Create()
        {
            var view = View();
            Window.Create("Planet editor", true, true, 400, -1, w => view.Draw());
        }

        public static IView View()
        {
            CelestialBody body = null;

            var geeAsl = new TextBoxView<double>("Gravity", "Multiplier strength of gravity", 1, SiSuffix.TryParse);
            var temperature = new TextBoxView<double>("Temperature", "Temperature multiplier of atmosphere", 1, SiSuffix.TryParse);
            var atmosphere = new ToggleView("Has atmosphere", "Toggles if the body has an atmosphere", true);
            var atmosphereMultiplier = new TextBoxView<double>("Atmosphere pressure", "Atmosphere pressure", 1, SiSuffix.TryParse);
            var atmosphereScaleHeight = new TextBoxView<double>("Atmosphere height", "Scale height of the atmosphere", 1, SiSuffix.TryParse);
            var atmosphereContainsOxygen = new ToggleView("Atmosphere contains oxygen", "If jet engine intakes work", true);
            var atmosphereAmbientColor = new TextBoxView<Color>("Atmosphere color", "Color of the light emitted by the atmosphere (doesn't change actual color)", new Color(1, 1, 1), Extentions.ColorTryParse);
            var sphereOfInfluence = new TextBoxView<double>("Sphere of influence", "Radius of the SOI of the planet", 1, SiSuffix.TryParse);
            var rotationPeriod = new TextBoxView<double>("Rotation period", "Seconds per revolution of the planet", 1, SiSuffix.TryParse);
            var tidallyLocked = new ToggleView("Tidally locked", "If rotation period is equal to orbital period", true);

            Action<CelestialBody> onSelect = cb =>
            {
                body = cb;
                geeAsl.Object = body.GeeASL;
                temperature.Object = body.atmoshpereTemperatureMultiplier;
                atmosphere.Value = body.atmosphere;
                atmosphereMultiplier.Object = body.atmosphereMultiplier;
                atmosphereScaleHeight.Object = body.atmosphereScaleHeight;
                atmosphereContainsOxygen.Value = body.atmosphereContainsOxygen;
                atmosphereAmbientColor.Object = body.atmosphericAmbientColor;
                sphereOfInfluence.Object = body.sphereOfInfluence;
                rotationPeriod.Object = body.rotationPeriod;
                tidallyLocked.Value = body.tidallyLocked;
            };

            var selectBody = new ConditionalView(() => FlightGlobals.fetch != null && FlightGlobals.Bodies != null,
                                 new ListSelectView<CelestialBody>(() => FlightGlobals.Bodies, onSelect, cb => cb.bodyName));

            var apply = new ConditionalView(() => geeAsl.Valid &&
                            temperature.Valid &&
                            atmosphereMultiplier.Valid &&
                            atmosphereScaleHeight.Valid &&
                            atmosphereAmbientColor.Valid &&
                            sphereOfInfluence.Valid &&
                            rotationPeriod.Valid,
                            new ButtonView("Apply", "Applies the changes to the body", () =>
                    {
                        new Model.PlanetEditor.PlanetSettings(
                            geeAsl.Object,
                            (float)temperature.Object,
                            atmosphere.Value,
                            atmosphereContainsOxygen.Value,
                            (float)atmosphereMultiplier.Object,
                            atmosphereScaleHeight.Object,
                            atmosphereAmbientColor.Object,
                            sphereOfInfluence.Object,
                            rotationPeriod.Object,
                            tidallyLocked.Value,
                            body.orbit).CopyTo(body, false);
                    }));

            var editFields = new ConditionalView(() => body != null, new VerticalView(new IView[]
                    {
                        geeAsl,
                        temperature,
                        atmosphere,
                        atmosphereMultiplier,
                        atmosphereScaleHeight,
                        atmosphereContainsOxygen,
                        atmosphereAmbientColor,
                        sphereOfInfluence,
                        rotationPeriod,
                        tidallyLocked,
                        apply,
                    }));

            var resetToDefault = new ConditionalView(() => body != null,
                                     new ButtonView("Reset to defaults", "Reset the selected planet to defaults",
                                         () => Model.PlanetEditor.ResetToDefault(body)));

            var copyToKerbin = new ConditionalView(() => body != null && body != Model.PlanetEditor.Kerbin,
                                   new ButtonView("Copy to kerbin", "Copies the selected planet's settings to kerbin",
                                       () => new Model.PlanetEditor.PlanetSettings(body).CopyTo(Model.PlanetEditor.Kerbin, false)));

            var savePlanet = new ConditionalView(() => body != null,
                                 new ButtonView("Save planet to config file", "Saves the current configuration of the planet to a file, so it stays edited even after a restart. Delete the file named the planet's name in ./GameData/Kerbaltek/PluginData/HyperEdit/ to undo.",
                                     () => Model.PlanetEditor.SavePlanet(body)));

            var reloadDefaults = new ConditionalView(() => FlightGlobals.fetch != null && FlightGlobals.Bodies != null,
                                     new ButtonView("Reload config files", "Reloads the planet .cfg files in ./GameData/Kerbaltek/PluginData/HyperEdit/",
                                         Model.PlanetEditor.ApplyFileDefaults));

            return new VerticalView(new IView[]
                {
                    selectBody,
                    editFields,
                    resetToDefault,
                    copyToKerbin,
                    savePlanet,
                    reloadDefaults
                });
        }
    }
}
