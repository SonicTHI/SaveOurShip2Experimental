using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class ApparelHolographic : Apparel
	{
		public ThingDef apparelToMimic;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look<ThingDef>(ref apparelToMimic, "apparelToMimic");
		}

		public override Color DrawColor => (Wearer != null ? Wearer.health.hediffSet.GetFirstHediff<HediffPawnIsHologram>().consciousnessSource.TryGetComp<CompBuildingConsciousness>().HologramColor : base.DrawColor);
	}
}
