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
            Window.Create("Misc tools", 300, -1, view.Draw);
        }

        private MiscEditorView() { }

        public override void Draw(Window window)
        {
            base.Draw(window);
            if (GUILayout.Button(new GUIContent("Refill ship resources", "Refill all resources (fuel/power/etc) to max value"))) _model.RefillVesselResources();
            var newUT = GuiTextFieldSettable("UniversalTime", new GUIContent("Time", "Set time (aka UniversalTime)"), double.TryParse, _model.UniversalTime);
            if (newUT.HasValue)
                _model.UniversalTime = newUT.Value;
            if (GUILayout.Button(new GUIContent("Align SMAs", "Open the semi-major axis aligner window")))
                _model.AlignSemiMajorAxis();
            if (GUILayout.Button(new GUIContent("Destroy a vessel", "Select a vessel to destroy")))
                _model.DestroyVessel();
        }
    }
}
