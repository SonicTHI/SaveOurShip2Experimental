using System;
using Verse;

namespace RimWorld
{
    class CompSoSGlower : CompGlower
    {
        protected override void SetGlowColorInternal(ColorInt? color)
        {
            base.SetGlowColorInternal(color);
            if (color.HasValue)
            {
                foreach (object selected in Find.Selector.SelectedObjectsListForReading)
                {
                    if (selected is ThingWithComps thing)
                    {
                        CompSoShipPart part = thing.TryGetComp<CompSoShipPart>();
                        if (part.myLight != null)
                        {
                            CompShipLight light = part.myLight.TryGetComp<CompShipLight>();
                            if (light != null)
                                light.UpdateColors(color.Value);
                        }
                    }
                }
            }
        }
    }
}
