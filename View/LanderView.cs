using UnityEngine;

namespace HyperEdit.View
{
    public class LanderView : View
    {
        Model.Lander _model;
        public static void Create(Model.Lander model)
        {
            var view = new LanderView();
            view._model = model;
            Window.Create("Lander", 200, -1, view.Draw);
        }

        private LanderView() { }

        public override void Draw(Window window)
        {
            base.Draw(window);
            _model.Latitude = GuiTextField("Latitude", "Lat", double.TryParse, _model.Latitude);
            _model.Longitude = GuiTextField("Longitude", "Lon", double.TryParse, _model.Longitude);
            _model.Altitude = GuiTextField("Altitutde", "Alt", double.TryParse, _model.Altitude);
            _model.Landing = GUILayout.Toggle(_model.Landing, "Landing");
            if (GUILayout.Button("Save"))
            {
                _model.Save();
            }
            if (GUILayout.Button("Load"))
            {
                _model.Load(ClearTextFields);
            }
            if (GUILayout.Button("Delete"))
            {
                _model.Delete();
            }
            if (GUILayout.Button("SetToCurrent"))
            {
                _model.SetToCurrent();
                ClearTextFields();
            }
        }
    }
}
