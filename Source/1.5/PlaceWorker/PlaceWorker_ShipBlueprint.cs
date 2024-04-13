using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class PlaceWorker_ShipBlueprint : PlaceWorker
	{
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
		{
			Map map = Find.CurrentMap;
			var comp = def.GetCompProperties<CompProps_ShipBlueprint>();
			if (comp == null)
				return;
			List<IntVec3> postitions = new List<IntVec3>();
			Color color = Color.cyan;
			bool clear = true;
			foreach (IntVec3 v in GenerateBlueprintSketch(comp.shipDef))
			{
				IntVec3 pos = new IntVec3(center.x + v.x + 1 ,0, center.z + v.z + 1);
				if (!pos.InBounds(map))
					return;
				postitions.Add(pos);
				if (clear && !ShipInteriorMod2.CanPlaceShipOnVec(pos, map, true))
					clear = false;
			}
			if (!clear)
				color = Color.red;
			GenDraw.DrawFieldEdges(postitions, color);
		}

		public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
		{/*
			var comp = def.blueprintDef.GetCompProperties<CompProperties_ShipBlueprint>();
			if (comp == null)
				return false;
			foreach (IntVec3 v in GenerateBlueprintSketch(comp.shipDef))
			{
				IntVec3 pos = new IntVec3(loc.x + v.x + 1, 0, loc.z + v.z + 1);
				if (!pos.InBounds(map))
					return false;
				if (ValidPosition(pos, map))
					return false;
			}
			//var comp = thing.TryGetComp<CompShipBlueprint>();
			/*
			if (thing is ShipSpawnBlueprint ship)
			{
				//CellRect rect = ship.shipSketch.OccupiedRect.MovedBy(loc);
				foreach (SketchEntity current in ship.shipSketch.Entities)
				{
					if (!current.OccupiedRect.MovedBy(loc).InBounds(map))
						return false;
					if (current.IsSpawningBlocked(current.pos + loc, map))
						return false;
					if (map.fogGrid.IsFogged(current.pos + loc))
						return false;
					if (map.roofGrid.Roofed(current.pos + loc))
					{
						current.DrawGhost(current.pos + loc, new Color(0.8f, 0.2f, 0.2f, 0.35f));
					}
				}
			}*/
			return true;
		}
		public List<IntVec3> GenerateBlueprintSketch(SpaceShipDef shipDef)
		{
			HashSet<IntVec3> positions = new HashSet<IntVec3>();
			foreach (ShipShape shape in shipDef.parts)
			{
				if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
				{
					ThingDef d = ThingDef.Named(shape.shapeOrDef);
					if (d.building != null && d.building.shipPart)
					{
						IntVec3 pos = new IntVec3(shape.x, 0, shape.z);
						if (d.Size.x > 1 || d.Size.z > 1)
						{
							if (shape.rot == Rot4.North || shape.rot == Rot4.South)
							{
								pos.x -= (d.Size.x - 1) / 2;
								pos.z -= (d.Size.z - 1) / 2;
							}
							else
							{
								pos.x -= (d.Size.z - 1) / 2;
								pos.z -= (d.Size.x - 1) / 2;
							}
							if (d.size.z % 2 == 0 && d.size.x % 2 != 0)
							{
								if (shape.rot == Rot4.South)
									pos.z -= 1;
								else if (shape.rot == Rot4.West)
									pos.x -= 1;
							}
							for (int i = 0; i < d.Size.x; i++)
							{
								for (int j = 0; j < d.Size.z; j++)
								{
									IntVec3 adjPos;
									if (shape.rot == Rot4.North || shape.rot == Rot4.South)
										adjPos = new IntVec3(pos.x + i, 0, pos.z + j);
									else
										adjPos = new IntVec3(pos.x + j, 0, pos.z + i);
									positions.Add(adjPos);
								}
							}
						}
						else
							positions.Add(pos);
					}
				}
			}
			return positions.ToList();
		}
	}
}
