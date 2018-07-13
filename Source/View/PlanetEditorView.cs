using System;

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
            var atmospherePressureSeaLevel = new TextBoxView<double>("atmospherePressureSeaLevel", "New 1.0 field. Unknown what this does.", 1, Model.SiSuffix.TryParse);
            var atmosphereMolarMass = new TextBoxView<double>("atmosphereMolarMass", "New 1.0 field. Unknown what this does.", 1, Model.SiSuffix.TryParse);
            var atmosphereAdiabaticIndex = new TextBoxView<double>("atmosphereAdiabaticIndex", "New 1.0 field. Unknown what this does.", 1, Model.SiSuffix.TryParse);
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
                atmospherePressureSeaLevel.Object = body.atmospherePressureSeaLevel;
                atmosphereMolarMass.Object = body.atmosphereMolarMass;
                atmosphereAdiabaticIndex.Object = body.atmosphereAdiabaticIndex;
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
                            atmospherePressureSeaLevel.Valid &&
                            atmosphereMolarMass.Valid &&
                            atmosphereAdiabaticIndex.Valid &&
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
                            atmospherePressureSeaLevel.Object,
                            atmosphereMolarMass.Object,
                            atmosphereAdiabaticIndex.Object,
                            rotates.Value,
                            rotationPeriod.Object,
                            initialRotation.Object,
                            tidallyLocked.Value,
                            body.orbit).CopyTo(body, false);
                    }));

            var editFields = new ConditionalView(() => body != null, new VerticalView(new IView[]
                    {
                        geeAsl,
                        ocean,
                        atmosphere,
                        atmosphereContainsOxygen,
                        atmosphereDepth,
                        atmosphereTemperatureSeaLevel,
                        atmospherePressureSeaLevel,
                        atmosphereMolarMass,
                        atmosphereAdiabaticIndex,
                        rotates,
                        rotationPeriod,
                        initialRotation,
                        tidallyLocked,
                        apply,
                    }));

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
