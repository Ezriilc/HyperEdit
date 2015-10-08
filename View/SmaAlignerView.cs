using UnityEngine;
using System.Collections.Generic;

namespace HyperEdit.View
{
    public static class SmaAlignerView
    {
        public static void Create()
        {
            var view = View();
            Window.Create("SMA Aligner", true, true, 300, -1, w => view.Draw());
        }

        public static IView View()
        {
            Vector2 scrollPos = new Vector2(0, 0);
            List<Vessel> vesselsToAlign = new List<Vessel>();
            var vesselList = new CustomView(() =>
                {
                    scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.MinHeight(300));
                    vesselsToAlign.RemoveAll(v => !Model.SmaAligner.AvailableVessels.Contains(v));
                    foreach (var vessel in Model.SmaAligner.AvailableVessels)
                    {
                        var alreadyIn = vesselsToAlign.Contains(vessel);
                        var newIn = GUILayout.Toggle(alreadyIn, vessel.vesselName);
                        if (alreadyIn == false && newIn == true)
                            vesselsToAlign.Add(vessel);
                        if (alreadyIn == true && newIn == false)
                            vesselsToAlign.Remove(vessel);
                    }
                    GUILayout.EndScrollView();
                });
            var align = new ConditionalView(() => vesselsToAlign.Count > 1,
                            new ButtonView("Align", "Sets all semi-major axes of selected vessels to be equal, so they all have the same period",
                                () => Model.SmaAligner.Align(vesselsToAlign)));

            return new VerticalView(new IView[]
                {
                    vesselList,
                    align
                });
        }
    }
}