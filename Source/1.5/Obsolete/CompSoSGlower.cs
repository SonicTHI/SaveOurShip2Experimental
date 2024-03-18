using System;
using Verse;

namespace RimWorld
{
	/*class CompSoSGlower : CompGlower
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
						CompSoShipLight part = thing.TryGetComp<CompSoShipLight>();
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
	}*/
}
