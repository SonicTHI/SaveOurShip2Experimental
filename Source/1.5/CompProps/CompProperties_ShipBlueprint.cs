using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
	public class CompProperties_ShipBlueprint : CompProperties
	{
		public EnemyShipDef shipDef;
		public CompProperties_ShipBlueprint()
		{
			this.compClass = typeof(CompShipBlueprint);
		}
	}
}
