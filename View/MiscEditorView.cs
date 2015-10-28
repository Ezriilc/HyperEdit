using System;
using UnityEngine;

namespace HyperEdit.View
{
    public class MiscEditorView
    {
        public static Action Create()
        {
            var view = View();
            return () => Window.Create("Misc tools", true, true, 300, -1, w => view.Draw());
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
                    if (Math.Abs(newval - resource.Value) > 0.001)
                    {
                        Model.MiscEditor.SetResource(resource.Key, newval);
                    }
                    GUILayout.EndHorizontal();
                }
            };
            var tetris = TetrisView.Create();
            return new VerticalView(new IView[]
                {
                    new LabelView("Resources", "Set amounts of various resources contained on the active vessel"),
                    new CustomView(resources),
                    new TextBoxView<double>("Time", "Set time (aka UniversalTime)",
                        Model.MiscEditor.UniversalTime, SiSuffix.TryParse, null, v => Model.MiscEditor.UniversalTime = v),
                    new ButtonView("Align SMAs", "Open the semi-major axis aligner window", Model.MiscEditor.AlignSemiMajorAxis),
                    new ButtonView("Destroy a vessel", "Select a vessel to destroy", Model.MiscEditor.DestroyVessel),
                    new ButtonView("Enable heat editor", "Attaches a button to each part's right click menu to let you change their temperatures", Model.HeatEditor.Enable),
                    new TextBoxView<KeyCode[]>("Boost button key", "Sets the keybinding used for the boost button",
                        Model.MiscEditor.BoostButtonKey, Extensions.KeyCodeTryParse, Extensions.KeyCodeToString, v => Model.MiscEditor.BoostButtonKey = v),
                    new TextBoxView<double>("Boost button speed", "Sets the dV applied per frame when the boost button is held down",
                        Model.MiscEditor.BoostButtonSpeed, SiSuffix.TryParse, null, v => Model.MiscEditor.BoostButtonSpeed = v),
                    new ButtonView("Tetris", "Use QE to rotate, AD to move, S to move down, W to drop", tetris)
                });
        }
    }

    public static class TetrisView
    {
        public static Action Create()
        {
            var view = View();
            return () => Window.Create("Tetris", true, true, 300, -1, w => view.Draw());
        }

        private static bool tetrisViewed;
        private static Model.Tetris keyboardDrivenTetris;

        public static void UpdateTetris()
        {
            if (!tetrisViewed || keyboardDrivenTetris == null)
                return;
            var moveLeft = Input.GetKeyDown(KeyCode.A);
            var moveRight = Input.GetKeyDown(KeyCode.D);
            var rotLeft = Input.GetKeyDown(KeyCode.Q);
            var rotRight = Input.GetKeyDown(KeyCode.E);
            var down = Input.GetKeyDown(KeyCode.S);
            var drop = Input.GetKeyDown(KeyCode.W);
            keyboardDrivenTetris.RunUpdate(moveLeft, moveRight, rotLeft, rotRight, down, drop);
            tetrisViewed = false;
        }

        public static IView View()
        {
            var texture = new Texture2D(200, 300, TextureFormat.RGB24, false);
            var tetris = new Model.Tetris(texture, 10);
            keyboardDrivenTetris = tetris;
            return new CustomView(() =>
            {
                tetrisViewed = true;
                GUILayout.Box(tetris.Render());
            });
        }
    }
}
