using UnityEngine;

namespace HyperEdit.View
{
    public class SmaAlignerView : View
    {
        Vector2 _scrollPos;

        Model.SmaAligner _model;

        public static void Create(Model.SmaAligner model)
        {
            var view = new SmaAlignerView();
            view._model = model;
            Window.Create("SMA Aligner", true, true, 200, -1, view.Draw);
        }

        private SmaAlignerView()
        {
        }

        public override void Draw(Window window)
        {
            base.Draw(window);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.MinHeight(300));
            foreach (var vessel in _model.AvailableVessels)
            {
                var alreadyIn = _model.VesselsToAlign.Contains(vessel);
                var newIn = GUILayout.Toggle(alreadyIn, vessel.name);
                if (alreadyIn == false && newIn == true)
                    _model.VesselsToAlign.Add(vessel);
                if (alreadyIn == true && newIn == false)
                    _model.VesselsToAlign.Remove(vessel);
            }
            GUILayout.EndScrollView();
            if (AllValid && GUILayout.Button(new GUIContent("Align", "Sets all semi-major axes of selected vessels to be equal, so they all have the same period")))
                _model.Align();
        }
    }
}