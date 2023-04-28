﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        //SoShipPart props isHull: walls, walllikes, hullfoam fills, wrecks form from these, spawns ship terrain beneath
        //SoShipPart props isPlating: plating, airlocks - can not be placed under buildings, hullfoam fills, wrecks form from these, spawns ship terrain beneath, count as 1 for weight calcs
        //SoShipPart props isHardpoint: spawns ship terrain beneath, reduce damage for turrets
        //SoShipPart props hermetic: hold air in vacuum - walls, airlocks, corners, engines, hullfoam, extenders, spinal barrels
        //SoShipPart props canLight: can spawn a wall light - powered walls (basic, mech, archo)
        //SoShipPart props light: def of the wall light to be attached
        //SoShipPart: other parts that are cached - not attached, no corePath (bridges)
        //other tags:
        //SoShipPart props roof: forces and spawns ship roof above (should only be isPlating but currently also walls, walllikes)
        //SoShipPart props mechanoid, archotech, wreckage, foam: override for plating type

        //roof textures
        public static GraphicData roofedData = new GraphicData();
        public static GraphicData roofedDataMech = new GraphicData();
        public static Graphic roofedGraphicTile;
        public static Graphic roofedGraphicTileMech;
        //icon
        public static Texture2D ShipWallLightIcon;
        public static Texture2D ShipSunLightIcon;
        public static Texture2D DiscoModeIcon;
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
            ShipWallLightIcon = (Texture2D)GraphicDatabase.Get<Graphic_Single>("Things/Building/Ship/ShipWallLightIcon").MatSingle.mainTexture;
            ShipSunLightIcon = (Texture2D)GraphicDatabase.Get<Graphic_Single>("Things/Building/Ship/ShipSunLightIcon").MatSingle.mainTexture;
            DiscoModeIcon = (Texture2D)GraphicDatabase.Get<Graphic_Single>("Things/Building/Ship/DiscoModeIcon").MatSingle.mainTexture;
        }

        bool isTile;
        bool isMechTile;
        bool isArchoTile;
        bool isFoamTile; //no gfx for foam roof

        public bool hasLight = false;
        public bool sunLight = false;
        public ColorInt lightColor = new ColorInt(Color.white);
        public Building myLight = null;
        public bool discoMode = false;

        List<IntVec3> cellsUnder;
        Map map;
        public ShipHeatMapComp mapComp;
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
                if (!mapComp.ShipCells.ContainsKey(parent.Position))
                {
                    stringBuilder.Append("cache is null for this pos!");
                }
                var shipCells = mapComp.ShipCells[parent.Position];
                int index = -1;
                int path = -1;
                if (shipCells != null)
                {
                    index = shipCells.Item1;
                    path = shipCells.Item2;
                }
                if (parent.def.building.shipPart)
                    stringBuilder.Append("shipIndex: " + index + " / corePath: " + path);
                else
                    stringBuilder.Append("shipIndex: " + index);
            }
            return stringBuilder.ToString();
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            isTile = parent.def == ResourceBank.ThingDefOf.ShipHullTile;
            isMechTile = parent.def == ResourceBank.ThingDefOf.ShipHullTileMech;
            isArchoTile = parent.def == ResourceBank.ThingDefOf.ShipHullTileArchotech;
            isFoamTile = parent.def == ResourceBank.ThingDefOf.ShipHullfoamTile;
            map = parent.Map;
            mapComp = map.GetComponent<ShipHeatMapComp>();
            cellsUnder = new List<IntVec3>();
            //plating - check adj, merge
            //hull - check on + adj, merge
            //other - check on + adj, merge
            HashSet<IntVec3> cellsToMerge = new HashSet<IntVec3>();
            foreach (IntVec3 vec in GenAdj.CellsOccupiedBy(parent))
            {
                cellsUnder.Add(vec);
            }
            if (!respawningAfterLoad && (Props.isPlating || Props.isHardpoint || Props.isHull)) //set terrain
            {
                foreach (IntVec3 v in cellsUnder)
                {
                    SetShipTerrain(v);
                }
            }
            if (ShipInteriorMod2.AirlockBugFlag) //MoveShip - cache is off
            {
                if (hasLight) //Despawned light in MoveShip - regenerate manually so we don't get power bugs
                    SpawnLight(lightColor, sunLight);
                return;
            }
            foreach (IntVec3 vec in cellsUnder) //init cells if not already in ShipCells
            {
                if (!mapComp.ShipCells.ContainsKey(vec))
                {
                    mapComp.ShipCells.Add(vec, null);
                }
            }
            if (Props.roof)
            {
                foreach (IntVec3 pos in cellsUnder)
                {
                    var oldRoof = map.roofGrid.RoofAt(pos);
                    if (!ShipInteriorMod2.IsRoofDefAirtight(oldRoof))
                        map.roofGrid.SetRoof(pos, ResourceBank.RoofDefOf.RoofShip);
                }
            }
            if (mapComp.CacheOff) //on load, enemy ship spawn - cache is off
            {
                return;
            }
            //plating or shipPart: chk all cardinal, if any plating or shipPart has valid shipIndex, set to this
            //plating or shipPart with different or no shipIndex: merge connected to this ship
            foreach (IntVec3 vec in GenAdj.CellsAdjacentCardinal(parent))
            {
                if (!mapComp.ShipCells.ContainsKey(vec))
                    continue;
                cellsToMerge.Add(vec);
            }
            if (cellsToMerge.Any()) //if any other parts under or adj, merge in order to: largest ship, any ship, wreck
            {
                int mergeToIndex;
                cellsToMerge.AddRange(cellsUnder);
                IntVec3 mergeTo = IntVec3.Zero;
                int mass = 0;
                foreach (IntVec3 vec in cellsToMerge) //find largest ship
                {
                    int shipIndex = mapComp.ShipIndexOnVec(vec);
                    if (shipIndex != -1 && mapComp.ShipsOnMapNew[shipIndex].Mass > mass)
                    {
                        mergeTo = vec;
                        mass = mapComp.ShipsOnMapNew[shipIndex].Mass;
                    }
                }
                if (mergeTo != IntVec3.Zero)
                {
                    mergeToIndex = mapComp.ShipCells[mergeTo].Item1;
                }
                else //no ships, attach to wreck
                {
                    mergeTo = cellsToMerge.FirstOrDefault();
                    mergeToIndex = mergeTo.GetThingList(parent.Map).FirstOrDefault(b => b.TryGetComp<CompSoShipPart>() != null).thingIDNumber;
                }
                mapComp.AttachAll(mergeTo, mergeToIndex);
            }
            else //else make new entry
            {
                foreach (IntVec3 vec in cellsUnder)
                    mapComp.ShipCells[vec] = new Tuple<int, int>(parent.thingIDNumber, -1);
            }
        }
        public void PreDeSpawn() //called in building.destroy, before comps get removed
        {
            if (ShipInteriorMod2.AirlockBugFlag) //moveship cleans entire area, caches //td
            {
                return;
            }

            if(!mapComp.ShipCells.ContainsKey(parent.Position) || !mapComp.ShipsOnMapNew.ContainsKey(mapComp.ShipCells[parent.Position].Item1))
            {
                return;
            }

            var ship = mapComp.ShipsOnMapNew[mapComp.ShipCells[parent.Position].Item1];
            HashSet<Building> buildings = new HashSet<Building>();
            foreach (IntVec3 vec in cellsUnder) //check if any other ship parts exist, if not remove ship area
            {
                bool partExists = false;
                foreach (Thing t in vec.GetThingList(parent.Map))
                {
                    if (t is Building b && b != parent)
                    {
                        if (b.TryGetComp<CompSoShipPart>() != null)
                        {
                            partExists = true;
                        }
                        else
                        {
                            buildings.Add(b);
                        }
                    }
                }
                if (!partExists) //no shippart remains, remove from area, remove non ship buildings if fully off ship
                {
                    ship.Area.Remove(vec);
                    ship.AreaDestroyed.Add(vec);
                    mapComp.ShipCells.Remove(vec);
                    if (ship != null)
                    {
                        foreach (Building b in buildings) //remove other buildings that are no longer supported by ship parts
                        {
                            bool allOffShip = false;
                            foreach (IntVec3 v in GenAdj.CellsOccupiedBy(b))
                            {
                                if (!mapComp.ShipCells.ContainsKey(vec))
                                {
                                    allOffShip = true;
                                    break;
                                }
                            }
                            if (allOffShip)
                            {
                                ship.BuildingCount--;
                                ship.Mass -= b.def.Size.x * b.def.Size.z * 3;
                            }
                        }
                    }
                }
            }
            if (ship != null) //remove from cache
            {
                //if last bridge remove ship or end combat
                if (parent is Building_ShipBridge && ship.Bridges.Any(b => b != parent && !b.Destroyed))
                {
                    if (!mapComp.InCombat)
                    {
                        foreach (IntVec3 vec in ship.Area) //remove path
                            mapComp.ShipCells[vec] = new Tuple<int, int>(((Building_ShipBridge)parent).Index, -1);

                        mapComp.ShipsOnMapNew.Remove(parent.thingIDNumber);
                    }
                    else //if last ship end combat else move to grave
                    {
                        /*if (mapComp.ShipsOnMapNew.Count > 1)
                            mapComp.MoveShipToGraveyard(((Building_ShipBridge)parent).Index);
                        else
                            mapComp.EndBattle(parent.Map, false);*/
                    }
                    return;
                }
                ship.RemoveFromCache(parent as Building);

                if (!mapComp.InCombat) //perform check immediately
                    ship.CheckForDetach();
            }
        }
        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (myLight != null)
                myLight.DeSpawn();
            if (!(Props.isPlating || Props.isHardpoint || Props.isHull)) //hull temp to remove terrain under it
                return;
            foreach (IntVec3 pos in cellsUnder)
            {
                bool stillHasTile = false;
                foreach (Thing t in map.thingGrid.ThingsAt(pos))
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
        }
        public override void PostDraw()
        {
            base.PostDraw();

            if (!Props.roof)
                return;
            if ((Find.PlaySettings.showRoofOverlay || parent.Position.Fogged(parent.Map)) && parent.Position.Roofed(parent.Map))
            {
                foreach (Thing t in parent.Position.GetThingList(parent.Map).Where(t => t is Building))
                {
                    if (t.TryGetComp<CompShipHeat>() != null && t.def.altitudeLayer == AltitudeLayer.WorldClipper)
                    {
                        return;
                    }
                }
                if (isTile)
                {
                    Graphics.DrawMesh(material: roofedGraphicTile.MatSingleFor(parent), mesh: roofedGraphicTile.MeshAt(parent.Rotation), position: new Vector3(parent.DrawPos.x, 0, parent.DrawPos.z), rotation: Quaternion.identity, layer: 0);
                }
                else if (isMechTile || isArchoTile)
                {
                    Graphics.DrawMesh(material: roofedGraphicTileMech.MatSingleFor(parent), mesh: roofedGraphicTileMech.MeshAt(parent.Rotation), position: new Vector3(parent.DrawPos.x, 0, parent.DrawPos.z), rotation: Quaternion.identity, layer: 0);
                }
            }
        }
        public void SetShipTerrain(IntVec3 v)
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
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo giz in base.CompGetGizmosExtra())
                yield return giz;
            if (Props.canLight && ((parent.Faction==Faction.OfPlayer && ResearchProjectDefOf.ColoredLights.IsFinished) || DebugSettings.godMode))
            {
                Command_Action toggleLight = new Command_Action
                {
                    action = delegate
                    {
                        if(hasLight)
                        {
                            hasLight = false;
                            if (myLight != null)
                                myLight.DeSpawn();
                            else
                                Log.Error("Tried to disable ship lighting at position " + parent.Position + " when no light exists. Please report this bug to the SoS2 team.");
                        }
                        else
                        {
                            SpawnLight(lightColor, false);
                        }
                    },
                    icon = ShipWallLightIcon,
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipWallLight"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipWallLightDesc"),
                    disabled = !AnyAdjacentRoom(),
                    disabledReason = TranslatorFormattedStringExtensions.Translate("ShipWallLightAdjacency")
                };

                yield return toggleLight;

                if (hasLight)
                {
                    Command_Toggle toggleSun = new Command_Toggle
                    {
                        toggleAction = delegate
                        {
                            sunLight = !sunLight;
                            if (myLight != null)
                                myLight.DeSpawn();
                            else
                                Log.Error("Tried to enable sunlight mode at position " + parent.Position + " when no light exists. Please report this bug to the SoS2 team.");
                            SpawnLight(lightColor, sunLight);
                        },
                        isActive = delegate { return sunLight; },
                        icon = ShipSunLightIcon,
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipWallLightSun"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipWallLightSunDesc")
                    };

                    yield return toggleSun;

                    Command_Toggle toggleDisco = new Command_Toggle
                    {
                        toggleAction = delegate
                        {
                            discoMode = !discoMode;
                        },
                        isActive = delegate { return discoMode; },
                        icon = DiscoModeIcon,
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipWallLightDisco"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipWallLightDiscoDesc")
                    };

                    yield return toggleDisco;
                }

                if(hasLight)
                {
                    foreach (Gizmo giz in myLight.GetGizmos())
                        yield return giz;
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            if (Props.canLight)
            {
                Scribe_Values.Look<bool>(ref hasLight, "hasLight", false);
                Scribe_Values.Look<bool>(ref sunLight, "sunLight", false);
                Scribe_Values.Look<bool>(ref discoMode, "discoMode", false);
                if (hasLight)
                {
                    Scribe_Values.Look<ColorInt>(ref lightColor, "lightColor", new ColorInt(Color.white));
                    Scribe_References.Look<Building>(ref myLight, "myLight");
                }
            }
        }
        bool AnyAdjacentRoom()
        {
            IntVec3 pos = parent.Position + new IntVec3(0, 0, -1);
            if (pos.GetEdifice(map) == null || pos.GetEdifice(map).def.passability!=Traversability.Impassable)
                return true;
            pos += new IntVec3(1, 0, 1);
            if (pos.GetEdifice(map) == null || pos.GetEdifice(map).def.passability != Traversability.Impassable)
                return true;
            pos += new IntVec3(-2, 0, 0);
            if (pos.GetEdifice(map) == null || pos.GetEdifice(map).def.passability != Traversability.Impassable)
                return true;
            pos += new IntVec3(1, 0, 1);
            if (pos.GetEdifice(map) == null || pos.GetEdifice(map).def.passability != Traversability.Impassable)
                return true;
            return false;
        }
        public void SpawnLight(ColorInt? color = null, bool sun = false)
        {
            if (!Props.canLight)
            {
                Log.Error("Attempted to spawn light on non-lightable ship part " + parent);
                return;
            }
            hasLight = true;
            sunLight = sun;
            myLight = (Building)GenSpawn.Spawn(Props.light, parent.Position, parent.Map);
            CompPowerTrader trader = myLight.TryGetComp<CompPowerTrader>();
            if (trader != null)
            {
                trader.ConnectToTransmitter(parent.TryGetComp<CompPower>());
                if (sunLight)
                    trader.Props.basePowerConsumption = Props.sunLightPower;
                else
                    trader.Props.basePowerConsumption = Props.lightPower;
                trader.PowerOn = true;
            }
            CompShipLight lightComp = myLight.TryGetComp<CompShipLight>();
            if (lightComp != null)
            {
                if (color.HasValue)
                    lightColor = color.Value;
                else
                    lightColor = ColorIntUtility.AsColorInt(Color.white);
                lightComp.SetupLighting(this, sun);
            }
            else
                Log.Error("Failed to initialize ship lighting at position " + parent.Position + " - please report this bug to the SoS2 team.");
        }
    }
}