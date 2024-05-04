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

