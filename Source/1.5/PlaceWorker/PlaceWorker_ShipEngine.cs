using SaveOurShip2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld
{
	class PlaceWorker_ShipEngine : PlaceWorker
	{
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			if (ShipInteriorMod2.HasSoS2CK)
				return AcceptanceReport.WasAccepted;
			CompEngineTrail engineprev = null;
			var mapComp = map.GetComponent<ShipHeatMapComp>();
			if (mapComp.ShipsOnMapNew.Values.Any(s => s.Engines.Any()))
			{
				//prefer player owned non wreck ships
				if (mapComp.ShipsOnMapNew.Values.Any(s => s.Engines.Any() && !s.IsWreck && s.Faction == Faction.OfPlayer))
					engineprev = mapComp.ShipsOnMapNew.Values.Where(s => s.Engines.Any() && !s.IsWreck && s.Faction == Faction.OfPlayer).First().Engines.First();
				else if (mapComp.ShipsOnMapNew.Values.Any(s => s.Engines.Any()))
					engineprev = mapComp.ShipsOnMapNew.Values.First(s => s.Engines.Any()).Engines.First();
			}
			if (engineprev != null && engineprev.parent.Rotation != rot)
				return AcceptanceReport.WasRejected;
			return AcceptanceReport.WasAccepted;
		}
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
		{
			List<IntVec3> rect = GenAdj.CellsOccupiedBy(center, rot, def.size).ToList();
			int minx = 99999;
			int minz = 99999;
			foreach (IntVec3 vec in rect)
			{
				if (vec.x < minx)
					minx = vec.x;
				if (vec.z < minz)
					minz = vec.z;
			}
			CellRect rectToKill;
			if (def.size.z > 3)
			{
				rectToKill = new CellRect(minx, minz, rot.IsHorizontal ? def.size.z : def.size.x, rot.IsHorizontal ? def.size.x : def.size.z).MovedBy(CompEngineTrail.killOffsetL[rot.AsInt]).ExpandedBy(2);
			}
			else
			{
				rectToKill = new CellRect(minx, minz, rot.IsHorizontal ? def.size.z : def.size.x, rot.IsHorizontal ? def.size.x : def.size.z).MovedBy(CompEngineTrail.killOffset[rot.AsInt]).ExpandedBy(1);
			}
			if (rot.IsHorizontal)
				rectToKill.Width = rectToKill.Width * 2 - 3;
			else
				rectToKill.Height = rectToKill.Height * 2 - 3;
			GenDraw.DrawFieldEdges(rectToKill.Cells.ToList(), Color.red);
		}
	}
}
