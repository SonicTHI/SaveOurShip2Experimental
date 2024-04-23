using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public class CompShipBlueprint : ThingComp
	{
		public CompProps_ShipBlueprint Props
		{
			get { return props as CompProps_ShipBlueprint; }
		}
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			Command_Action place1 = new Command_Action
			{

				action = delegate
				{
					SpawnShipDefBlueprint(Props.shipDef, this.parent.Position, this.parent.Map, 1);
				},
				defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.BlueprintPlace1"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.BlueprintPlace1Desc"),
				icon = ContentFinder<Texture2D>.Get("Things/Building/Ship/HullPlate")
			};
			Command_Action place2 = new Command_Action
			{

				action = delegate
				{
					SpawnShipDefBlueprint(Props.shipDef, this.parent.Position, this.parent.Map, 2);
				},
				defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.BlueprintPlace2"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.BlueprintPlace2Desc"),
				icon = ContentFinder<Texture2D>.Get("Things/Building/Ship/ShipBeamModular_east")
			};
			Command_Action place3 = new Command_Action
			{

				action = delegate
				{
					SpawnShipDefBlueprint(Props.shipDef, this.parent.Position, this.parent.Map, 3);
				},
				defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.BlueprintPlace3"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.BlueprintPlace3Desc"),
				icon = ContentFinder<Texture2D>.Get("Things/Building/Ship/Ship_Bridge_Mini_south")
			};
			if (!ResearchProjectDef.Named("ShipBasics").IsFinished)
			{
				place1.Disable(TranslatorFormattedStringExtensions.Translate("SoS.BlueprintDisabled"));
				place2.Disable(TranslatorFormattedStringExtensions.Translate("SoS.BlueprintDisabled"));
				place3.Disable(TranslatorFormattedStringExtensions.Translate("SoS.BlueprintDisabled"));
			}
			yield return place1;
			yield return place2;
			yield return place3;
		}
		public void SpawnShipDefBlueprint(SpaceShipDef shipDef, IntVec3 pos, Map map, int tier)
		{
			//get area
			HashSet<IntVec3> Area = new HashSet<IntVec3>();
			HashSet<IntVec3> Plating = new HashSet<IntVec3>();
			foreach (ShipShape shape in shipDef.parts.Where(s => DefDatabase<ThingDef>.GetNamedSilentFail(s.shapeOrDef) != null))
			{
				ThingDef def = ThingDef.Named(shape.shapeOrDef);
				if (!def.IsBuildingArtificial)
					continue;
				IntVec3 v = new IntVec3(pos.x + 1, 0, pos.z + 1);
				if (def.size.x == 1 && def.size.z == 1)
				{
					v = new IntVec3(pos.x + shape.x + 1, 0, pos.z + shape.z + 1);
					Area.Add(v);
					var comp = def.GetCompProperties<CompProps_ShipCachePart>();
					if (comp != null && comp.Plating)
						Plating.Add(v);
					continue;
				}
				for (int i = 0; i < def.size.x; i++)
				{
					for (int j = 0; j < def.size.z; j++)
					{
						int adjx = 0;
						int adjz = 0;
						if (shape.rot == Rot4.North || shape.rot == Rot4.South)
						{
							adjx = i - (def.size.x / 2);
							adjz = j - (def.size.z / 2);
							if (shape.rot == Rot4.North && def.size.z % 2 == 0)
								adjz += 1;
						}
						else
						{
							adjx = j - (def.size.x / 2);
							adjz = i - (def.size.z / 2);
							if (def.size.x != def.size.z && def.size.z != 4)
							{
								adjx -= 1;
								adjz += 1;
							}
							if (shape.rot == Rot4.East && def.size.z % 2 == 0)
								adjx += 1;
						}
						int x = v.x + shape.x + adjx;
						int z = v.z + shape.z + adjz;
						Area.Add(new IntVec3(x, 0, z));
					}
				}
			}
			//check
			foreach (IntVec3 v in Area)
			{
				RoofDef roof = map.roofGrid.RoofAt(v);
				if (!v.InBounds(map) || v.InNoBuildEdgeArea(map) || (roof != null && roof.isThickRoof) || !v.GetTerrain(map).affordances.Contains(TerrainAffordanceDefOf.Heavy) || v.Fogged(map) || v.GetThingList(map).Any(t => t is Building b && b.Faction != Faction.OfPlayer))
				{
					Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.BlueprintFailed"), parent, MessageTypeDefOf.NegativeEvent);
					return;
				}
			}
			//place
			foreach (ShipShape shape in shipDef.parts.Where(s => DefDatabase<ThingDef>.GetNamedSilentFail(s.shapeOrDef) != null))
			{
				ThingDef def = ThingDef.Named(shape.shapeOrDef);
				if (!def.IsBuildingArtificial || !def.IsResearchFinished || !def.BuildableByPlayer)
					continue;
				IntVec3 v = new IntVec3(pos.x + shape.x + 1, 0, pos.z + shape.z + 1);
				var comp = def.GetCompProperties<CompProps_ShipCachePart>();
				//tier 1: plating and hull not on plating
				//tier 2: hull (all, placecheck prevents dupes/overwrites)
				//tier 3: rest, non ship part
				if (tier == 1 && (comp != null && (comp.Plating || (comp.isHull && !Plating.Contains(v)))) || (tier == 2 && def.building.shipPart && comp != null && !comp.Plating) || tier == 3 && !def.building.shipPart)
				{
					if (GenConstruct.CanPlaceBlueprintAt(def, v, shape.rot, map))
					{
						ThingDef stuff = GenStuff.DefaultStuffFor(def);
						if (def.MadeFromStuff)
						{
							if (shape.stuff != null)
								stuff = ThingDef.Named(shape.stuff);
						}
						GenConstruct.PlaceBlueprintForBuild(def, v, map, shape.rot, Faction.OfPlayer, stuff);
					}
				}
			}
			if (tier == 3) //place core
			{
				ThingDef def = ThingDef.Named(shipDef.core.shapeOrDef);
				IntVec3 v = new IntVec3(pos.x + shipDef.core.x + 1, 0, pos.z + shipDef.core.z + 1);
				if (GenConstruct.CanPlaceBlueprintAt(def, v, shipDef.core.rot, map))
					GenConstruct.PlaceBlueprintForBuild(def, v, map, shipDef.core.rot, Faction.OfPlayer, null);
			}
		}
	}
}