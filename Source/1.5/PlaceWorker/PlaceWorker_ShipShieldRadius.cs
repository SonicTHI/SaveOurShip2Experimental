using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public class PlaceWorker_ShipShieldRadius : PlaceWorker
	{
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
		{
			GenDraw.DrawCircleOutline(center.ToVector3Shifted(), def.GetCompProperties<CompProps_ShipHeat>().shieldMin);
			GenDraw.DrawCircleOutline(center.ToVector3Shifted(), def.GetCompProperties<CompProps_ShipHeat>().shieldMax);
		}
	}
}
