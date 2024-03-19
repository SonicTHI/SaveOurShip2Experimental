using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
	class ShipMoveBlueprint : Thing
	{
		public Sketch shipSketch;

		public ShipMoveBlueprint(Sketch sketch)
		{
			shipSketch = sketch;
			this.def = ResourceBank.ThingDefOf.ShipMoveBlueprint;
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			this.shipSketch.DrawGhost(drawLoc.ToIntVec3(), Sketch.SpawnPosType.Unchanged, false, null);
		}

		public void DrawGhost(IntVec3 drawLoc, bool flip = false)
		{
			this.shipSketch.DrawGhost(drawLoc, Sketch.SpawnPosType.Unchanged, false, null);
		}
	}
}
