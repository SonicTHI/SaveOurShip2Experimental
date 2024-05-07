using System;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_ShipBay : CompProperties
	{
		public bool beam = false;
		public int weight = 0;
		public int maxShuttleSize = 1;
		public int borderSize = 0;
		public float repairBonus = 1;
		public float autoRepair = 0.4f;
		public float repairUpTo = 0.5f;
		[NoTranslate]
		public string graphicPath;
		[Unsaved]
		public Material roofGraphic;
		public CompProps_ShipBay()
		{
			compClass = typeof(CompShipBay);
		}

		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);
			LongEventHandler.ExecuteWhenFinished((Action)(() => roofGraphic = MaterialPool.MatFrom(graphicPath)));
		}
	}
}

