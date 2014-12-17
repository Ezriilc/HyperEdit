using UnityEngine;

namespace HyperEdit.View
{
    public static class AboutWindow
    {
        public static void Create()
        {
            Window.Create("About", 500, 200, w =>
                {
                    GUILayout.Label(AboutContents);
                });
        }

        private const string AboutContents = @"For support and contact information, please visit: http://www.kerbaltek.com/

This is a highly eccentric plugin, so there may be lots of bugs and explosions - please tell us if you find any.

Created by:
khyperia (original creator, code)
Ezriilc (web)
sirkut (code)
payo (code [Planet Editor])
forecaster (graphics, logo)

GPL license. Opensource at https://github.com/Ezriilc/HyperEdit";
    }
}
