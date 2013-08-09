//
// This file is part of the HyperEdit plugin for Kerbal Space Program, Copyright Erickson Swift, 2013.
// HyperEdit is licensed under the GPL, found in COPYING.txt.
// Currently supported by Team HyperEdit, and Ezriilc.
// Original HyperEdit concept and code by khyperia (no longer involved).
//

using System.Collections.Generic;
using UnityEngine;

namespace HyperEdit
{
    public class HelpWindow : Window
    {
        public HelpWindow()
        {
            EnsureSingleton(this);
            Title = "Help";
            WindowRect = new Rect(Screen.width / 2 - 250, Screen.height / 2 - 200, 500, 400);
            Contents = new List<IWindowContent>
                {
                    new Scroller(new[]{(IWindowContent)new Label(HelpContents) }),
                    new Button("Close", CloseWindow)
                };
        }

        private const string HelpContents = @"Main window:
Edit an orbit: Changes the orbit of any ship or planet in the Kerbin system.
Land your ship: Teleports the current ship to a set of coordinates, and gently sets it down on the surface.
Misc tools: Random functions The Creator has found useful to include, like refilling ship resources, setting the time, and more.

Orbit editor window:
Select orbit: Chooses the orbit to edit. This is not the destination, as might be suggested by planetary names - if you select a planet, you are actually editing that planet's orbit
Simple: Teleports yourself to a circular, equatorial orbit at the specified altitude
Complex: Edit raw Keplarian orbital components (see Wikipedia)
Graphical: Use sliders to edit Keplarian orbital components and see results immediately (advised to use map view)
Velocity: Edit instantaneous velocity of the orbit
Rendezvous (only when editing a vessel's orbit): Teleport to (nearly) the same orbit as another ship, at 'lead time' seconds before the other ship

Lander window:
Pressing land teleports you to latitude/longitude at altitude above the terrain and slowly lowers you to the ground.
Pressing land again cancels the landing (you'll probably fall and explode, so it's advised to do something to prevent that right after canceling)
Save coordinates saves the currently entered coordinates to disk, prompting you to name it
Load coordinates loads a coordinate pair from disk by name
Delete coordinates deletes a coordinate pair off the disk
Set to current position sets lat/lon to the current vessel's position (useful for saving a spot you're at)

Misc editor:
Refill ship resources sets all resources on your ship to their maximum capacity.
Time sets the Universal Time of your save game, in seconds.
Destroy a vessel kills the vessel you select. (Killing the active vessel has... interesting results)
Align SMA sets many orbits' semi major axis to be equal, which makes their period be exactly the same - useful for satellite constellations
(note: if the active vessel is one of the satellites you are setting, it is advised to go into non-physical warp, since if the ship runs physics for even one frame, the alignment messes up)
(also, Bad Things(tm) will happen if you choose satellites on different planets or ones that are landed)

Input Fields:
All input numbers can be followed by a Metric SI multiplier.  These suffixes move the decimal point right (multiply) or left (divide), anywhere from 1 to 24 places, according to this chart.
Examples:
da means *10
so...
199da = 1990
And:
d means /10
so...
199d = 19.9

Multiply:
Y = 24
Z = 21
E = 18
P = 15
T = 12
G = 9
M = 6
k = 3
h = 2
da = 1

Divide:
d = 1
c = 2
m = 3
u = 6
n = 9
p = 12
f = 15
a = 18
z = 21
y = 24

More information on Metric SI multipliers here: http://en.wikipedia.org/wiki/Metric_prefix

For more help, support and contact information, please visit: http://www.KerbaltekAerospace.com .  This is a highly eccentric plugin, so there may be lots of bugs and explosions - please tell us if you find any.";
    }
}
