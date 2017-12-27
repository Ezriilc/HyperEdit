using System;
using UnityEngine;

namespace HyperEdit.View {
  public class MiscEditorView {
    private static ConfigNode _toggleRes;
    private static int vwidth = 300; //View width (needed for scrollviews)
    private static int vheight = -1; //View height
    private static Vector2 scrollPosition;

    public static Action Create() {
      var view = View();
      //return () => Window.Create("Misc tools", true, true, 300, -1, w => view.Draw());
      return () => Window.Create("Misc tools", true, true, vwidth, vheight, w => view.Draw());
    }

    public static IView View() {
      ReloadConfig();

      Action resources = () => {
        //Using the Vertical to set the box height.
        GUILayout.BeginVertical(GUILayout.Height(100));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(140));

        foreach (var resource in Model.MiscEditor.GetResources()) {
          GUILayout.BeginHorizontal();
          GUILayout.Label(resource.Key);
          var newval = (double)GUILayout.HorizontalSlider((float)resource.Value, 0, 1);
          if (Math.Abs(newval - resource.Value) > 0.001) {
            Model.MiscEditor.SetResource(resource.Key, newval);
          }
          //Just trying an idea
          //toggleRes = GUILayout.Toggle(toggleRes[resource.Key], "lock");
          //toggleRes = GUILayout.Toggle(toggleRes, "lock");
          /*
           * It'd be nice to lock inf resources for specific vessels, or maybe just any vessel?
           */

          //GUILayout.FlexibleSpace();
          GUILayout.Space(5);
          GUILayout.EndHorizontal();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndScrollView();
        GUILayout.EndVertical();

      };
      return new VerticalView(new IView[]
      {
                new LabelView("Resources", "Set amounts of various resources contained on the active vessel"),
                new CustomView(resources),
                new TextBoxView<double>("Time", "Set time (aka UniversalTime)",
                    Model.MiscEditor.UniversalTime, Model.SiSuffix.TryParse, null,
                    v => Model.MiscEditor.UniversalTime = v),
                new ButtonView("Align SMAs", "Open the semi-major axis aligner window",
                    Model.MiscEditor.AlignSemiMajorAxis),
                new ButtonView("Destroy a vessel", "Select a vessel to destroy", Model.MiscEditor.DestroyVessel),
                new TextBoxView<KeyCode[]>("Boost button key", "Sets the keybinding used for the boost button",
                    Model.MiscEditor.BoostButtonKey, Extensions.KeyCodeTryParse, Extensions.KeyCodeToString,
                    v => Model.MiscEditor.BoostButtonKey = v),
                new TextBoxView<double>("Boost button speed",
                    "Sets the dV applied per frame when the boost button is held down",
                    Model.MiscEditor.BoostButtonSpeed, Model.SiSuffix.TryParse, null,
                    v => Model.MiscEditor.BoostButtonSpeed = v)
      });
    }

    private static void ReloadConfig() {
      var hypereditCfg = IoExt.GetPath("miscoptions.cfg");
      if (System.IO.File.Exists(hypereditCfg)) {
        _toggleRes = ConfigNode.Load(hypereditCfg);
        _toggleRes.name = "miscoptions";
      } else {
        _toggleRes = new ConfigNode("miscoptions");
      }

      //var autoOpenLanderValue = true;
      //_toggleRes.TryGetValue("AutoOpenLander", ref autoOpenLanderValue, bool.TryParse);
      //AutoOpenLander = autoOpenLanderValue;


    }
  }
}