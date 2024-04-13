using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class CompProps_ShipBlueprint : CompProperties
	{
		public SpaceShipDef shipDef;
		public CompProps_ShipBlueprint()
		{
			this.compClass = typeof(CompShipBlueprint);
		}
	}
}
