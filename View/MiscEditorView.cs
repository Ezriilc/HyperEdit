using System;
using UnityEngine;

namespace HyperEdit.View
{
    public class MiscEditorView
    {
        public static void Create()
        {
            Window.Create("Misc tools", true, true, 300, -1, w => View().Draw());
        }

        public static IView View()
        {
            Action resources = () =>
            {
                foreach (var resource in Model.MiscEditor.GetResources())
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(resource.Key);
                    var newval = (double)GUILayout.HorizontalSlider((float)resource.Value, 0, 1);
                    if (Math.Abs(newval - resource.Value) > 0.01)
                    {
                        Model.MiscEditor.SetResource(resource.Key, newval);
                    }
                    GUILayout.EndHorizontal();
                }
            };
            return new VerticalView(new IView[]
                {
                    new LabelView("Resources", "Set amounts of various resources contained on the active vessel"),
                    new CustomView(resources),
                    new TextBoxView<double>("Time", "Set time (aka UniversalTime)",
                        Model.MiscEditor.UniversalTime.ToString(), SiSuffix.TryParse, v => Model.MiscEditor.UniversalTime = v),
                    new ButtonView("Align SMAs", "Open the semi-major axis aligner window", () => true, Model.MiscEditor.AlignSemiMajorAxis),
                    new ButtonView("Destroy a vessel", "Select a vessel to destroy", () => true, Model.MiscEditor.DestroyVessel),
                    new TextBoxView<KeyCode[]>("Boost button key", "Sets the keybinding used for the boost button",
                        Model.MiscEditor.BoostButtonKey.KeyCodeToString(), Extentions.KeyCodeTryParse, v => Model.MiscEditor.BoostButtonKey = v),
                    new TextBoxView<double>("Boost button speed", "Sets the dV applied per frame when the boost button is held down",
                        Model.MiscEditor.BoostButtonSpeed.ToString(), SiSuffix.TryParse, v => Model.MiscEditor.BoostButtonSpeed = v),
                });
        }
    }
}
