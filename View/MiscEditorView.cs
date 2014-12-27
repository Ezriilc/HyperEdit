using System;
using UnityEngine;

namespace HyperEdit.View
{
    public class MiscEditorView : View
    {
        Model.MiscEditor _model;

        public static void Create(Model.MiscEditor model)
        {
            var view = new MiscEditorView();
            view._model = model;
            Window.Create("Misc tools", true, true, 300, -1, view.Draw);
        }

        private MiscEditorView()
        {
        }

        public override void Draw(Window window)
        {
            base.Draw(window);
            GUILayout.Label(new GUIContent("Resources", "Set amounts of various resources contained on the active vessel"));
            foreach (var resource in _model.GetResources())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(resource.Key);
                var newval = (double)GUILayout.HorizontalSlider((float)resource.Value, 0, 1);
                if (Math.Abs(newval - resource.Value) > float.Epsilon)
                {
                    _model.SetResource(resource.Key, newval);
                }
                GUILayout.EndHorizontal();
            }
            var newUT = GuiTextFieldSettable("UniversalTime", new GUIContent("Time", "Set time (aka UniversalTime)"), SiSuffix.TryParse, _model.UniversalTime);
            if (newUT.HasValue)
                _model.UniversalTime = newUT.Value;
            if (GUILayout.Button(new GUIContent("Align SMAs", "Open the semi-major axis aligner window")))
                _model.AlignSemiMajorAxis();
            if (GUILayout.Button(new GUIContent("Destroy a vessel", "Select a vessel to destroy")))
                _model.DestroyVessel();

            _model.BoostButtonKey = GuiTextField("BoostButtonKey", new GUIContent("Boost button key", "Sets the keybinding used for the boost button"),
                Extentions.KeyCodeTryParse, _model.BoostButtonKey, Extentions.KeyCodeToString);
            _model.BoostButtonSpeed = GuiTextField("BoostButtonSpeed", new GUIContent("Boost button speed", "Sets the dV applied per frame when the boost button is held down"),
                SiSuffix.TryParse, _model.BoostButtonSpeed);
        }
    }
}
