using System.Linq;

namespace HyperEdit.Model
{
    public class HeatEditor : PartModule
    {
        private double? lockedHeat;

        public static void Enable()
        {
            if (FlightGlobals.fetch == null || FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.parts == null)
                return;
            foreach (var part in FlightGlobals.ActiveVessel.parts)
            {
                if (!part.Modules.OfType<HeatEditor>().Any())
                {
                    Extensions.Log("Adding HeatEditor to " + part.name);
                    part.AddModule("HeatEditor");
                }
            }
        }

        [KSPEvent(guiActive = true, guiName = "Heat editor")]
        public void MakeHeatEditor()
        {
            var view = CreateView();
            View.Window.Create("Heat editor: " + part.name, false, false, 300, -1, w => view.Draw());
        }

        private View.IView CreateView()
        {
            var sliderLocked = new View.ConditionalView(() => lockedHeat.HasValue,
                new View.DynamicSliderView("Temp", "Current temperature of the part", () => lockedHeat.Value / part.maxTemp, v => lockedHeat = v * part.maxTemp));
            var sliderUnlocked = new View.ConditionalView(() => !lockedHeat.HasValue,
                new View.DynamicSliderView("Temp", "Current temperature of the part", () => part.temperature / part.maxTemp, v => part.temperature = v * part.maxTemp));
            var lockButton = new View.DynamicToggleView("Lock heat", "Locks the temperature of the part to the slider value", () => lockedHeat.HasValue, () => true,
                b => lockedHeat = b ? part.temperature : (double?)null);
            return new View.VerticalView(new View.IView[] { sliderLocked, sliderUnlocked, lockButton });
        }
        
        public void FixedUpdate()
        {
            if (lockedHeat.HasValue)
            {
                part.temperature = lockedHeat.Value;
            }
        }
    }
}
