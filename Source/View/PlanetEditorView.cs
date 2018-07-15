using System;

using UnityEngine;

namespace HyperEdit.View
{
    public static class PlanetEditorView
    {
        public static Action Create()
        {
            var view = View();
            return () => Window.Create("Planet editor", true, true, 400, -1, w => view.Draw());
        }

        public static IView View()
        {
            CelestialBody body = null;

            var geeAsl = new TextBoxView<double>("Gravity multiplier", "1.0 is kerbin, 0.5 is half of kerbin's gravity, etc.", 1, Model.SiSuffix.TryParse);
            var ocean = new ToggleView("Has ocean", "Does weird things to the ocean if off", false);
            var atmosphere = new ToggleView("Has atmosphere", "Toggles if the planet has atmosphere or not", false);
            var atmosphereContainsOxygen = new ToggleView("Atmosphere contains oxygen", "Whether jet engines work or not", false);
            var atmosphereDepth = new TextBoxView<double>("Atmosphere depth", "Theoretically atmosphere height. In reality, doesn't work too well.", 1, Model.SiSuffix.TryParse);
            var atmosphereTemperatureSeaLevel = new TextBoxView<double>("atmosphereTemperatureSeaLevel", "New 1.0 field. Unknown what this does.", 1, Model.SiSuffix.TryParse);
            var atmosphereTemperatureLapseRate = new TextBoxView<double>("atmosphereTemperatureLapseRate", "Unknown", 1, Model.SiSuffix.TryParse);
            var atmospherePressureSeaLevel = new TextBoxView<double>("atmospherePressureSeaLevel", "New 1.0 field. Unknown what this does.", 1, Model.SiSuffix.TryParse);
            var atmDensityASL = new TextBoxView<double>("atmDensityASL", "New 1.4-ish field. Unknown what this does.", 1, Model.SiSuffix.TryParse);
            var atmosphereGasMassLapseRate = new TextBoxView<double>("atmosphereGasMassLapseRate", "New 1.4-ish field. Unknown what this does.", 1, Model.SiSuffix.TryParse);
            var atmosphereMolarMass = new TextBoxView<double>("atmosphereMolarMass", "New 1.0 field. Unknown what this does.", 1, Model.SiSuffix.TryParse);
            var atmosphereAdiabaticIndex = new TextBoxView<double>("atmosphereAdiabaticIndex", "New 1.0 field. Unknown what this does.", 1, Model.SiSuffix.TryParse);
            var radiusAtmoFactor = new TextBoxView<double>("radiusAtmoFactor", "Unknown", 1, Model.SiSuffix.TryParse);
            var atmosphereUsePressureCurve = new ToggleView("Use atmospheric pressure curve", "Unknown", false);
            var atmospherePressureCurveIsNormalized = new ToggleView("Atmospheric pressure curve is normalized", "Unknown", false);
            var atmospherePressureCurve = new TextAreaView<String>("atmospherePressureCurve", "Atmosphere pressure curve", "", Model.SiSuffix.TryParseFloatCurve);
            var atmosphereUseTemperatureCurve = new ToggleView("Use atmospheric temperature curve", "Unknown", false);
            var atmosphereTemperatureCurveIsNormalized = new ToggleView("Atmospheric temperature curve is normalized", "Unknown", false);
            var atmosphereTemperatureCurve = new TextAreaView<String>("atmosphereTemperatureCurve", "Atmosphere temperature curve", "", Model.SiSuffix.TryParseFloatCurve);
            var atmosphereTemperatureSunMultCurve = new TextAreaView<String>("atmosphereTemperatureSunMultCurve", "Atmosphere temperature sun mult curve", "", Model.SiSuffix.TryParseFloatCurve);
            var rotates = new ToggleView("Rotates", "If the planet rotates.", false);
            var rotationPeriod = new TextBoxView<double>("Rotation period", "Rotation period of the planet, in seconds.", 1, Model.SiSuffix.TryParse);
            var initialRotation = new TextBoxView<double>("Initial rotation", "Absolute rotation in degrees of the planet at time=0", 1, Model.SiSuffix.TryParse);
            var tidallyLocked = new ToggleView("Tidally locked", "If the planet is tidally locked. Overrides Rotation Period.", false);

            Action<CelestialBody> onSelect = cb =>
            {
                body = cb;
                geeAsl.Object = body.GeeASL;
                ocean.Value = body.ocean;
                atmosphere.Value = body.atmosphere;
                atmosphereContainsOxygen.Value = body.atmosphereContainsOxygen;
                atmosphereDepth.Object = body.atmosphereDepth;
                atmosphereTemperatureSeaLevel.Object = body.atmosphereTemperatureSeaLevel;
                atmosphereTemperatureLapseRate.Object = body.atmosphereTemperatureLapseRate;
                atmospherePressureSeaLevel.Object = body.atmospherePressureSeaLevel;
                atmDensityASL.Object = body.atmDensityASL;
                atmosphereGasMassLapseRate.Object = body.atmosphereGasMassLapseRate;
                atmosphereMolarMass.Object = body.atmosphereMolarMass;
                atmosphereAdiabaticIndex.Object = body.atmosphereAdiabaticIndex;
                radiusAtmoFactor.Object = body.radiusAtmoFactor;
                atmosphereUsePressureCurve.Value = body.atmosphereUsePressureCurve;
                atmospherePressureCurveIsNormalized.Value = body.atmospherePressureCurveIsNormalized;
                atmospherePressureCurve.Object = JsonUtility.ToJson(body.atmospherePressureCurve, true);
                atmosphereUseTemperatureCurve.Value = body.atmosphereUseTemperatureCurve;
                atmosphereTemperatureCurveIsNormalized.Value = body.atmosphereTemperatureCurveIsNormalized;
                atmosphereTemperatureCurve.Object = JsonUtility.ToJson(body.atmospherePressureCurve, true);
                atmosphereTemperatureSunMultCurve.Object = JsonUtility.ToJson(body.atmosphereTemperatureSunMultCurve, true);
                rotates.Value = body.rotates;
                rotationPeriod.Object = body.rotationPeriod;
                initialRotation.Object = body.initialRotation;
                tidallyLocked.Value = body.tidallyLocked;
            };

            var selectBody = new ConditionalView(() => FlightGlobals.fetch != null && FlightGlobals.Bodies != null,
                                 new ListSelectView<CelestialBody>("Selected body", () => FlightGlobals.Bodies, onSelect, Extensions.CbToString));

            var apply = new ConditionalView(() =>
                            geeAsl.Valid &&
                            atmosphereDepth.Valid &&
                            atmosphereTemperatureSeaLevel.Valid &&
                            atmosphereTemperatureLapseRate.Valid &&
                            atmospherePressureSeaLevel.Valid &&
                            atmDensityASL.Valid &&
                            atmosphereGasMassLapseRate.Valid &&
                            atmosphereMolarMass.Valid &&
                            atmosphereAdiabaticIndex.Valid &&
                            radiusAtmoFactor.Valid &&
                            atmospherePressureCurve.Valid &&
                            atmosphereTemperatureCurve.Valid &&
                            atmosphereTemperatureSunMultCurve.Valid &&
                            rotationPeriod.Valid &&
                            initialRotation.Valid,
                            new ButtonView("Apply", "Applies the changes to the body", () =>
                    {
                        new Model.PlanetEditor.PlanetSettings(
                            geeAsl.Object,
                            ocean.Value,
                            atmosphere.Value,
                            atmosphereContainsOxygen.Value,
                            atmosphereDepth.Object,
                            atmosphereTemperatureSeaLevel.Object,
                            atmosphereTemperatureLapseRate.Object,
                            atmospherePressureSeaLevel.Object,
                            atmDensityASL.Object,
                            atmosphereGasMassLapseRate.Object,
                            atmosphereMolarMass.Object,
                            atmosphereAdiabaticIndex.Object,
                            radiusAtmoFactor.Object,
                            atmosphereUsePressureCurve.Value,
                            atmospherePressureCurveIsNormalized.Value,
                            JsonUtility.FromJson<FloatCurve>(atmospherePressureCurve.Object),
                            atmosphereUseTemperatureCurve.Value,
                            atmosphereTemperatureCurveIsNormalized.Value,
                            JsonUtility.FromJson<FloatCurve>(atmosphereTemperatureCurve.Object),
                            JsonUtility.FromJson<FloatCurve>(atmosphereTemperatureSunMultCurve.Object),
                            rotates.Value,
                            rotationPeriod.Object,
                            initialRotation.Object,
                            tidallyLocked.Value,
                            body.orbit).CopyTo(body, false);
                    }));

            var editFields = new ConditionalView(() => body != null, new VerticalView(new IView[]
                    {
                        new ScrollView(new VerticalView(new IView[]
                        {
                            geeAsl,
                            ocean,
                            atmosphere,
                            atmosphereContainsOxygen,
                            atmosphereDepth,
                            atmosphereTemperatureSeaLevel,
                            atmosphereTemperatureLapseRate,
                            atmospherePressureSeaLevel,
                            atmDensityASL,
                            atmosphereGasMassLapseRate,
                            atmosphereMolarMass,
                            atmosphereAdiabaticIndex,
                            radiusAtmoFactor,
                            atmosphereUsePressureCurve,
                            atmospherePressureCurveIsNormalized,
                            atmospherePressureCurve,
                            atmosphereUseTemperatureCurve,
                            atmosphereTemperatureCurveIsNormalized,
                            atmosphereTemperatureCurve,
                            atmosphereTemperatureSunMultCurve,
                            rotates,
                            rotationPeriod,
                            initialRotation,
                            tidallyLocked
                }), GUILayout.MinHeight(600), GUILayout.MaxHeight(Screen.height - 200)),
                        apply
                    }
            ));

            var resetToDefault = new ConditionalView(() => body != null,
                                     new ButtonView("Reset to defaults", "Reset the selected planet to defaults",
                                         () => { Model.PlanetEditor.ResetToDefault(body); onSelect(body); }));

            var copyToKerbin = new ConditionalView(() => body != null && body != Model.PlanetEditor.Kerbin,
                                   new ButtonView("Copy to kerbin", "Copies the selected planet's settings to kerbin",
                                       () => new Model.PlanetEditor.PlanetSettings(body).CopyTo(Model.PlanetEditor.Kerbin, false)));

            var savePlanet = new ConditionalView(() => body != null,
                                 new ButtonView("Save planet to config file", "Saves the current configuration of the planet to a file, so it stays edited even after a restart. Delete the file named the planet's name in " + IoExt.GetPath(null) + " to undo.",
                                     () => Model.PlanetEditor.SavePlanet(body)));

            var reloadDefaults = new ConditionalView(() => FlightGlobals.fetch != null && FlightGlobals.Bodies != null,
                                     new ButtonView("Reload config files", "Reloads the planet .cfg files in " + IoExt.GetPath(null),
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
