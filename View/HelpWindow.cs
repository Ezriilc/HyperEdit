using UnityEngine;

namespace HyperEdit.View
{
    public static class HelpWindow
    {
        public static void Create()
        {
            Window.Create("Help", 500, 400, w =>
            {
                GUILayout.Label(HelpContents);
            });
        }

        private const string HelpContents = @"Main window:
Lander:
'Land/Drop': teleports you to latitude/longitude at altitude above the terrain and slowly lowers you to the ground.  As the ship slowly descends, you have full attitude control, and pressing 'Land/Drop' again drops the ship, releasing it to gravity (DANGER!).
'Save Location': saves the currently displayed coordinates to: './GameData/Kerbaltek/PluginData/HyperEdit/landcoords.txt', prompting you to name the location.
'Load Location': loads a coordinate set from the list of saves.
'Delete Location': permanently (!) deletes a coordinate set from the saves.
'Set to current location (sorta) LOOSELY sets lat/lon to the vessel's current position.  This is rather inexact, and we plan to fix that.

Orbit Editor:
Select orbit: Chooses the orbit to edit. This is not the destination, as might be suggested by planetary names - if you select a planet, you are actually editing that planet's orbit
Simple: Teleports yourself to a circular, equatorial orbit at the specified altitude
Complex: Edit raw Keplarian orbital components (see Wikipedia)
Graphical: Use sliders to edit Keplarian orbital components and see results immediately (advised to use map view)
Velocity: Edit instantaneous velocity of the orbit
Rendezvous (only when editing a vessel's orbit): Teleport to (nearly) the same orbit as another ship, at 'lead time' seconds before the other ship

Planet Editor:
Mostly self-explanatory.  Gravitation values are automatically adjusted for the radius of the planet.  More info coming soon (right Payo?).

Misc Tools:
'Refill ship resources': sets all resources, e.g. fuel, power, etc., to their maximum capacity.  Watch out for that sudden change in mass!
'Time': sets the Universal Time of your save game, in seconds.  WARNING: Changing the time this way may cause your MET clock to begin counting BACKWARDS.  If so, you won't be able to control your ship until you correct it, or start another flight.  Messing with it enough has been known to correct it, and yes, we're planning to fix that.
'Destroy a vessel': kills the vessel you select. (Killing the active vessel has... interesting results)
'Align SMA': sets many orbits' semi major axis to be equal, which makes their period be exactly the same - useful for satellite constellations
(note: if the active vessel is one of the satellites you are setting, it is advised to go into non-physical warp, since if the ship runs physics for even one frame, the alignment messes up)
(also, Bad Things(tm) will happen if you choose satellites on different planets or ones that are landed)

Input Field Si unit suffixes have been REMOVED from HyperEdit as of version 1.2.4, for KSP 0.21.1.  The numbers shown are the numbers used, more or less.

For more help, support and contact information, please visit: http://www.KerbaltekAerospace.com .  This is a highly eccentric plugin, so there may be lots of bugs and explosions - please tell us if you find any.
";
    }
}
