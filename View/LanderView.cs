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
            _model.Latitude = GuiTextField("Latitude", new GUIContent("Lat", "Latitude of landing coordinates"), double.TryParse, _model.Latitude);
            _model.Longitude = GuiTextField("Longitude", new GUIContent("Lon", "Longitude of landing coordinates"), double.TryParse, _model.Longitude);
            _model.Altitude = GuiTextField("Altitutde", new GUIContent("Alt", "Altitude of landing coordinates"), double.TryParse, _model.Altitude);
            _model.Landing = GUILayout.Toggle(_model.Landing, new GUIContent("Landing", "Land the ship (or stop landing)"));
            if (GUILayout.Button(new GUIContent("Save", "Save the current location")))
            {
                _model.Save();
            }
            if (GUILayout.Button(new GUIContent("Load", "Load a previously-saved location")))
            {
                _model.Load(ClearTextFields);
            }
            if (GUILayout.Button(new GUIContent("Delete", "Delete a previously-saved location")))
            {
                _model.Delete();
            }
            if (GUILayout.Button(new GUIContent("SetToCurrent", "Set lat/lon to the current position")))
            {
                _model.SetToCurrent();
                ClearTextFields();
            }
        }
    }
}
