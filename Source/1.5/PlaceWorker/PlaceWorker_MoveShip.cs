using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class PlaceWorker_MoveShip : PlaceWorker
	{
		public override void DrawGhost(ThingDef def, IntVec3 loc, Rot4 rot, Color ghostCol, Thing thing = null)
		{
			if (thing is ShipMoveBlueprint ship)
			{
				ship.DrawGhost(loc);
			}
		}

		public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{
			if (thing is ShipMoveBlueprint ship)
			{
				bool targetMapLarger = false; //if target map is larger, allow only up to origin map size
				Map originMap = ShipInteriorMod2.shipOriginMap;
				if (originMap != null && (originMap.Size.x < map.Size.x || originMap.Size.z < map.Size.z))
				{
					targetMapLarger = true;
				}
				AcceptanceReport result = true;
				foreach (SketchEntity current in ship.shipSketch.Entities.Concat(ship.extenderSketch?.Entities))
				{
					IntVec3 vec = loc + current.pos;
					if (!vec.InBounds(map))
					{
						result = false;
						break;
					}
					if (GenGrid.InNoBuildEdgeArea(vec, map) || current.IsSpawningBlocked(vec, map) || map.roofGrid.Roofed(vec) || (targetMapLarger && (vec.x > originMap.Size.x || vec.z > originMap.Size.z)))
					{
						current.DrawGhost(vec, new Color(0.8f, 0.2f, 0.2f, 0.3f));
						result = false;
						continue;
					}
					foreach (Thing t in vec.GetThingList(map))
					{
						if (t is Building b)
						{
							if (b.def.passability == Traversability.Impassable || b is Building_SteamGeyser)
							{
								current.DrawGhost(vec, new Color(0.8f, 0.2f, 0.2f, 0.3f));
								result = false;
								break;
							}
						}
					}
				}
				foreach (SketchEntity current in ship.conflictSketch?.Entities) //nothing allowed in this
				{
					IntVec3 vec = loc + current.pos;
					if (!vec.InBounds(map))
					{
						result = false;
						break;
					}
					if (vec.GetThingList(map).Any())
					{
						result = false;
						break;
					}
				}
				return result;
			}
			return true;
		}
	}
}
