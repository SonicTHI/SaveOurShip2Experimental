using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
	class SectionLayer_ShipHeat : SectionLayer_Things
	{
		public SectionLayer_ShipHeat(Section section) : base(section)
		{
			base.requireAddToMapMesh = false;
			base.relevantChangeTypes = MapMeshFlagDefOf.Buildings;
		}

		public override void DrawLayer()
		{
			Designator_Build val = Find.DesignatorManager.SelectedDesignator as Designator_Build;
			if (val != null)
			{
				ThingDef val2 = ((Designator_Place)val).PlacingDef as ThingDef;
				if (val2 != null && val2.comps.OfType<CompProperties_ShipHeat>().Any())
				{
					base.DrawLayer();
				}
			}
		}

		protected override void TakePrintFrom(Thing t)
		{
			Building val = t as Building;
			if (val != null && ((ThingWithComps)val).TryGetComp<CompShipHeat>()!=null)
			{
				((ThingWithComps)val).TryGetComp<CompShipHeat>().PrintForGrid(this);
			}
		}
	}
}
