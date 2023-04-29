using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class PlaceWorker_ShipProjectileInterceptorRadius : PlaceWorker
    {
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
		{
			if (def.Size.x < 5)
            {
                GenDraw.DrawCircleOutline(center.ToVector3Shifted(), 10);
                GenDraw.DrawCircleOutline(center.ToVector3Shifted(), 20);
            }
			else
            {
                GenDraw.DrawCircleOutline(center.ToVector3Shifted(), 20);
                GenDraw.DrawCircleOutline(center.ToVector3Shifted(), 60);
            }
		}
	}
}
