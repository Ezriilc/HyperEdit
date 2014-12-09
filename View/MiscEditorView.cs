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
            if (GUILayout.Button("Refill ship resources")) _model.RefillVesselResources();
            var newUT = GuiTextFieldSettable("UniversalTime", "Global time", double.TryParse, _model.UniversalTime);
            if (newUT.HasValue)
                _model.UniversalTime = newUT.Value;
            if (GUILayout.Button("Align SMAs"))
                _model.AlignSemiMajorAxis();
            if (GUILayout.Button("Destroy a vessel"))
                _model.DestroyVessel();
        }
    }
}
