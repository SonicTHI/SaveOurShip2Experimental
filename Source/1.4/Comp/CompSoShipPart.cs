using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using SaveOurShip2;

namespace RimWorld
{
	[StaticConstructorOnStartup]
	public class CompSoShipPart : ThingComp
	{
		//CompSoShipPart types that are cached:
		//xml tagged building shipPart: anything attachable - walls, hullfoam, plating, engines, corners, hardpoints, spinal barrels
		//SoShipPart props isHull: walls, airlocks, walllikes, hullfoam fills, wrecks form from these, spawns ship terrain beneath
		//SoShipPart props isPlating: plating, airlocks - can not be placed under buildings, hullfoam fills, wrecks form from these, spawns ship terrain beneath, count as 1 for weight calcs
		//SoShipPart props isHardpoint: spawns ship terrain beneath, reduce damage for turrets
		//SoShipPart props hermetic: hold air in vacuum - walls, airlocks, corners, engines, hullfoam, extenders, spinal barrels
		//SoShipPart: other parts that are cached - not attached, no corePath (bridges)
		//other tags:
		//SoShipPart props roof: forces and spawns ship roof above (should only be isPlating but currently also walls, walllikes)
		//SoShipPart props mechanoid, archotech, wreckage, foam: override for plating/roof type

		//toggles
		//CacheOff: set on map load to not cause massive joining calcs, proper parts assign to MapShipCells, after that map is cached
		//AirlockBugFlag: set on ship move/remove, when removing things in bulk caches are copied over or deleted

		//roof textures
		public static GraphicData roofedData = new GraphicData();
		public static GraphicData roofedDataMech = new GraphicData();
		public static Graphic roofedGraphicTile;
		public static Graphic roofedGraphicTileMech;
		static CompSoShipPart()
		{
			roofedData.texPath = "Things/Building/Ship/Ship_Roof";
			roofedData.graphicClass = typeof(Graphic_Single);
			roofedData.shaderType = ShaderTypeDefOf.MetaOverlay;
			roofedGraphicTile = new Graphic_256(roofedData.Graphic);
			roofedDataMech.texPath = "Things/Building/Ship/Ship_RoofMech";
			roofedDataMech.graphicClass = typeof(Graphic_Single);
			roofedDataMech.shaderType = ShaderTypeDefOf.MetaOverlay;
			roofedGraphicTileMech = new Graphic_256(roofedDataMech.Graphic);
		}

		bool isTile;
		bool isMechTile;
		bool isArchoTile;
		bool isFoamTile; //no gfx for foam roof

		public HashSet<IntVec3> cellsUnder;
		public bool FoamFill = false;
		public bool ArchoConvert = false;
		Map map;
		public ShipHeatMapComp mapComp;
		Faction fac;
		public CompProperties_SoShipPart Props
		{
			get
			{
				return (CompProperties_SoShipPart)props;
			}
		}
		public override string CompInspectStringExtra()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (Prefs.DevMode)
			{
				int index = mapComp.ShipIndexOnVec(parent.Position);
				int path = -1;
				if (index != -1)
				{
					path = mapComp.MapShipCells[parent.Position].Item2;
				}
				if (parent.def.building.shipPart) //proper parts
					stringBuilder.Append("shipIndex: " + index + " / corePath: " + path);
				else //other parts
				{
					stringBuilder.Append("shipIndex: " + index);
					if (parent is Building_ShipBridge && parent == mapComp.ShipsOnMapNew[index].Core)
						stringBuilder.Append(" PRIMARY CORE");
				}
			}
			return stringBuilder.ToString();
		}
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			map = parent.Map;
			mapComp = map.GetComponent<ShipHeatMapComp>();
			fac = parent.Faction;
			cellsUnder = parent.OccupiedRect().ToHashSet();

			if (!parent.def.building.shipPart) //other parts
			{
				if (mapComp.CacheOff || ShipInteriorMod2.AirlockBugFlag)
					return;
				foreach (IntVec3 vec in cellsUnder) //if any part spawned on ship
				{
					int shipIndex = mapComp.ShipIndexOnVec(vec);
					if (shipIndex != -1)
					{
						mapComp.ShipsOnMapNew[shipIndex].AddToCache(parent as Building);
						return;
					}
				}
				return;
			}
			//proper parts
			isTile = parent.def == ResourceBank.ThingDefOf.ShipHullTile;
			isMechTile = parent.def == ResourceBank.ThingDefOf.ShipHullTileMech;
			isArchoTile = parent.def == ResourceBank.ThingDefOf.ShipHullTileArchotech;
			isFoamTile = parent.def == ResourceBank.ThingDefOf.ShipHullfoamTile;
			if (!respawningAfterLoad && Props.AnyPart)
			{
				if (!ShipInteriorMod2.AirlockBugFlag)
				{
					foreach (IntVec3 v in cellsUnder) //clear floor on construction
					{
						RemoveOtherTerrain(v);
					}
				}
				foreach (IntVec3 v in cellsUnder) //set terrain
				{
					SetShipTerrain(v);
				}
			}
			if (ShipInteriorMod2.AirlockBugFlag) //MoveShip - cache is off, dont make roof or floor
			{
				return;
			}
			if (Props.roof)
			{
				foreach (IntVec3 pos in cellsUnder) //set roof
				{
					var oldRoof = map.roofGrid.RoofAt(pos);
					if (!ShipInteriorMod2.IsRoofDefAirtight(oldRoof) && Props.hermetic)
						map.roofGrid.SetRoof(pos, ResourceBank.RoofDefOf.RoofShip);
					else
						map.roofGrid.SetRoof(pos, RoofDefOf.RoofConstructed);
				}
			}

			HashSet<IntVec3> cellsToMerge = new HashSet<IntVec3>();
			foreach (IntVec3 vec in cellsUnder) //init cells if not already in ShipCells
			{
				if (!mapComp.MapShipCells.ContainsKey(vec))
				{
					mapComp.MapShipCells.Add(vec, new Tuple<int, int>(-1, -1));
				}
				else
				{
					cellsToMerge.Add(vec);
				}
			}
			if (mapComp.CacheOff) //on mapinit - cache is off
			{
				cellsToMerge.Clear();
				return;
			}
			//plating or shipPart: chk all cardinal, if any plating or shipPart has valid shipIndex, set to this
			//plating or shipPart with different or no shipIndex: merge connected to this ship
			foreach (IntVec3 vec in GenAdj.CellsAdjacentCardinal(parent))
			{
				if (!mapComp.MapShipCells.ContainsKey(vec))
					continue;
				cellsToMerge.Add(vec);
			}
			if (cellsToMerge.Any()) //if any other parts under or adj, merge in order to: largest ship, any ship, wreck
			{
				cellsToMerge.AddRange(cellsUnder);
				mapComp.CheckAndMerge(cellsToMerge);
			}
			else //else make new ship/wreck
			{
				mapComp.ShipsOnMapNew.Add(parent.thingIDNumber, new SoShipCache());
				mapComp.ShipsOnMapNew[parent.thingIDNumber].RebuildCache(parent as Building);
			}
		}

		public void PreDeSpawn(DestroyMode mode) //called in building.destroy, before comps get removed
		{
			//Log.Warning("despawn " + parent);
			if (ShipInteriorMod2.AirlockBugFlag) //disable on moveship, detach destruction
				return;

			List<IntVec3> areaDestroyed = new List<IntVec3>();
			HashSet<Building> buildings = new HashSet<Building>();
			foreach (IntVec3 vec in cellsUnder) //check if other floor or hull on any vec
			{
				bool partExists = false;
				foreach (Thing t in vec.GetThingList(parent.Map))
				{
					if (t is Building b && b != parent)
					{
						if (b.def.building.shipPart)
						{
							partExists = true;
						}
						else
						{
							if (b is Building_ShipBridge br)
								br.terminate = true;
							buildings.Add(b);
						}
					}
				}
				if (!partExists) //no shippart remains, remove from area
				{
					areaDestroyed.Add(vec);
				}
			}

			if (mapComp.CacheOff)
			{
				foreach (IntVec3 vec in areaDestroyed)
				{
					mapComp.MapShipCells.Remove(vec);
				}
				return;
			}

			int shipIndex = mapComp.ShipIndexOnVec(parent.Position);
			if (shipIndex == -1)
				return;

			var ship = mapComp.ShipsOnMapNew[shipIndex];
			ship.RemoveFromCache(parent as Building, mode);
			if (!parent.def.building.shipPart || ArchoConvert)
			{
				return;
			}
			else if ((mode == DestroyMode.KillFinalize || mode == DestroyMode.KillFinalizeLeavingsOnly) && ship.FoamDistributors.Any() && parent.def.Size == IntVec2.One && (Props.Hull && ShipInteriorMod2.AnyAdjRoomNotOutside(parent.Position, map) || (Props.Plating && !ShipInteriorMod2.ExposedToOutside(parent.Position.GetRoom(map)))))
			{
				//replace part with foam, no detach checks
				foreach (CompHullFoamDistributor dist in ship.FoamDistributors.Where(d => d.parent.TryGetComp<CompRefuelable>().Fuel > 0 && d.parent.TryGetComp<CompPowerTrader>().PowerOn))
				{
					dist.parent.TryGetComp<CompRefuelable>().ConsumeFuel(1);
					FoamFill = true;
					return;
				}
			}
			foreach (IntVec3 vec in areaDestroyed)
			{
				int path = mapComp.MapShipCells[vec].Item2;
				if (ship.LastSafePath > path)
					ship.LastSafePath = path;
				bool replaceCore = false;
				if (path == 0)
					replaceCore = true;
				ship.Area.Remove(vec);
				mapComp.MapShipCells.Remove(vec);
				if (replaceCore) //tile under bridge was hit before bridge
				{
					ship.ReplaceCore();
				}
			}
			foreach (Building b in buildings) //remove other buildings that are no longer supported by this ship
			{
				bool onShip = false;
				foreach (IntVec3 v in GenAdj.CellsOccupiedBy(b))
				{
					if (mapComp.ShipIndexOnVec(v) == shipIndex)
					{
						onShip = true;
						break;
					}
				}
				if (!onShip)
				{
					ship.RemoveFromCache(b, mode);
				}
			}
			if (areaDestroyed.Any())
				ship.CheckForDetach(areaDestroyed);
		}
		public override void PostDeSpawn(Map map) //proper parts only - terrain, roof removal, foam replacers
		{
			base.PostDeSpawn(map);
			if (!Props.AnyPart)
				return;

			foreach (IntVec3 pos in cellsUnder)
			{
				bool stillHasTile = false;
				foreach(Thing t in pos.GetThingList(map))
				{
					var shipComp = t.TryGetComp<CompSoShipPart>();
					if (shipComp != null && (shipComp.Props.isPlating || shipComp.Props.isHardpoint))
					{
						stillHasTile = true;
						break;
					}
				}
				if (!stillHasTile)
				{
					TerrainDef manualTerrain = map.terrainGrid.TerrainAt(pos); //RimWorld freaks out about regions if we don't do the leavings manually
					map.terrainGrid.RemoveTopLayer(pos, false);
					List<ThingDefCountClass> list = manualTerrain.CostListAdjusted(null);
					for (int i = 0; i < list.Count; i++)
					{
						ThingDefCountClass thingDefCountClass = list[i];
						int num = GenMath.RoundRandom((float)thingDefCountClass.count * manualTerrain.resourcesFractionWhenDeconstructed);
						if (num > 0)
						{
							Thing thing = ThingMaker.MakeThing(thingDefCountClass.thingDef);
							thing.stackCount = num;
							//Log.Message(string.Format("Spawning wrecks {0} at {1}", thing.def.defName, pos));
							GenPlace.TryPlaceThing(thing, pos, map, ThingPlaceMode.Near);
						}
					}
					if (Props.roof)
						map.roofGrid.SetRoof(pos, null);
				}
			}
			if (FoamFill)
			{
				Thing replacer;
				if (Props.isHull)
					replacer = ThingMaker.MakeThing(ResourceBank.ThingDefOf.HullFoamWall);
				else
					replacer = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipHullfoamTile);

				replacer.SetFaction(fac);
				GenPlace.TryPlaceThing(replacer, cellsUnder.First(), map, ThingPlaceMode.Direct);
			}
			else if (ArchoConvert)
			{
				Thing replacer = ThingMaker.MakeThing(ShipInteriorMod2.archoConversions[parent.def]);
				replacer.Rotation = parent.Rotation;
				replacer.Position = parent.Position;
				replacer.SetFaction(fac);

				replacer.SpawnSetup(parent.Map, false);
				FleckMaker.ThrowSmoke(replacer.DrawPos, parent.Map, 2);
			}
		}
		public override void PostDraw()
		{
			base.PostDraw();
			if (!Props.roof || !parent.Spawned)
				return;
			if ((Find.PlaySettings.showRoofOverlay || parent.Position.Fogged(parent.Map)) && parent.Position.Roofed(parent.Map))
			{
				foreach (Thing t in parent.Position.GetThingList(parent.Map).Where(t => t is Building))
				{
					var heatComp = t.TryGetComp<CompShipHeat>();
					if (heatComp != null && heatComp.Props.showOnRoof)// t.def.altitudeLayer == AltitudeLayer.WorldClipper)
					{
						return;
					}
				}
				if (isTile)
				{
					Graphics.DrawMesh(roofedGraphicTile.MeshAt(parent.Rotation), new Vector3(parent.DrawPos.x, 0, parent.DrawPos.z), Quaternion.identity, roofedGraphicTile.MatSingleFor(parent), 0);
				}
				else if (isMechTile || isArchoTile)
				{
					Graphics.DrawMesh(roofedGraphicTileMech.MeshAt(parent.Rotation), new Vector3(parent.DrawPos.x, 0, parent.DrawPos.z), Quaternion.identity, roofedGraphicTileMech.MatSingleFor(parent), 0);
				}
			}
		}
		public virtual void SetShipTerrain(IntVec3 v)
		{
			if (!map.terrainGrid.TerrainAt(v).layerable)
			{
				if (Props.archotech)
					map.terrainGrid.SetTerrain(v, ResourceBank.TerrainDefOf.FakeFloorInsideShipArchotech);
				else if (Props.mechanoid)
					map.terrainGrid.SetTerrain(v, ResourceBank.TerrainDefOf.FakeFloorInsideShipMech);
				else if (Props.foam)
					map.terrainGrid.SetTerrain(v, ResourceBank.TerrainDefOf.FakeFloorInsideShipFoam);
				else if (Props.wreckage)
					map.terrainGrid.SetTerrain(v, ResourceBank.TerrainDefOf.ShipWreckageTerrain);
				else
					map.terrainGrid.SetTerrain(v, ResourceBank.TerrainDefOf.FakeFloorInsideShip);
			}
		}
		public virtual void RemoveOtherTerrain(IntVec3 v)
		{
			if (map.terrainGrid.TerrainAt(v).layerable && map.terrainGrid.TerrainAt(v) != ResourceBank.TerrainDefOf.FakeFloorInsideShipArchotech && map.terrainGrid.TerrainAt(v) != ResourceBank.TerrainDefOf.FakeFloorInsideShipMech && map.terrainGrid.TerrainAt(v) != ResourceBank.TerrainDefOf.FakeFloorInsideShipFoam && map.terrainGrid.TerrainAt(v) != ResourceBank.TerrainDefOf.ShipWreckageTerrain && map.terrainGrid.TerrainAt(v) != ResourceBank.TerrainDefOf.FakeFloorInsideShip)
			{
				map.terrainGrid.RemoveTopLayer(v);
			}
		}
	}
}