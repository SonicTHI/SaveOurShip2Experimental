using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class CompProps_ShipBlueprint : CompProperties
	{
		public SpaceShipDef shipDef;
		public bool flip = false;
		public CompProps_ShipBlueprint()
		{
			compClass = typeof(CompShipBlueprint);
		}
	}
}
