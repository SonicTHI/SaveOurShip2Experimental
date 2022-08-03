using System;
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
        //shipPart (xml tagged building shipPart): anything attachable: walls, plating, engines, corners, hardpoints, spinal barrels
        //shipPart hull (props isHull): walls and wall likes, corners, hullfoam replaces these
        //shipPart plating (props isPlating): can not be placed under buildings
        //part: parts that require to be placed on plating - not attached, no corePath

        //not shipPart hull airlock extenders - //td

        public int shipIndex = -1; //main bridge thingid
        public int corePath = -1; //how far away the main bridge is
        public ShipHeatMapComp mapComp;
        public CompProperties_SoShipPart Props
        {
            get
            {
                return (CompProperties_SoShipPart)props;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            //if cache=null add to precache, else add/merge
            mapComp = this.parent.Map.GetComponent<ShipHeatMapComp>();
            if (mapComp.ShipsOnMap.NullOrEmpty())// || ShipInteriorMod2.SoShipCacheOff)
            {
                return;
            }
            //shipPart or part placed on plating
            //shipPart: if any plating under has valid shipIndex, set to this, set to all plating under
            //part: if any plating under has valid shipIndex, set to this, return
            int index = -1;
            if (!this.Props.isPlating)
            {
                List<CompSoShipPart> shipPartsOnPlating = new List<CompSoShipPart>();
                foreach (IntVec3 vec in GenAdj.CellsOccupiedBy(this.parent))
                {
                    foreach (Thing t in vec.GetThingList(this.parent.Map))
                    {
                        if (t is Building b)
                        {
                            var shipPart = b.TryGetComp<CompSoShipPart>();
                            if (shipPart != null && shipPart.Props.isPlating)
                            {
                                shipPartsOnPlating.Add(shipPart); //add all found shipPart to list
                                if (index < 0 && shipPart.shipIndex > -1) //set shipIndex to first valid
                                {
                                    index = shipPart.shipIndex;
                                    corePath = shipPart.corePath;
                                    if (!b.def.building.shipPart) //part - set and return
                                    {
                                        mapComp.ShipsOnMapNew[index].AddToCache(this.parent as Building);
                                        return;
                                    }
                                }
                                else if (index == shipPart.shipIndex && corePath > shipPart.corePath)
                                {
                                    corePath = shipPart.corePath;
                                }
                            }
                        }
                    }
                }
                if (index > -1) //set same shipIndex under shipPart
                {
                    foreach (CompSoShipPart p in shipPartsOnPlating)
                    {
                        p.shipIndex = index;
                        p.corePath = corePath;
                    }
                }
            }
            List<int> shipsToMerge = new List<int>();
            bool foundWreck = false;
            //plating or shipPart: chk all cardinal, if any plating or shipPart has valid shipIndex, set to this
            //plating or shipPart with different or no shipIndex: merge connected to this ship
            //td better as something that also assigns corePath but would have to crawl through an entire merge
            if (this.parent.def.building.shipPart)
            {
                foreach (IntVec3 vec in GenAdj.CellsAdjacentCardinal(this.parent))
                {
                    foreach (Thing t in vec.GetThingList(this.parent.Map))
                    {
                        if (t is Building b && b.def.building.shipPart)
                        {
                            var shipPart = b.TryGetComp<CompSoShipPart>();
                            if (shipPart != null && shipPart.shipIndex > -1)
                            {
                                if (index > -1) //this and found have shipIndex
                                {
                                    if (index != shipPart.shipIndex) //found not same shipIndex
                                        shipsToMerge.Add(shipPart.shipIndex);
                                    else if (corePath > -1 && corePath + 1 > shipPart.corePath) //same ship, get lower corePath
                                    {
                                        corePath = shipPart.corePath + 1;
                                    }
                                }
                                else //this has no shipIndex
                                {
                                    index = shipPart.shipIndex;
                                    if (corePath < 0)
                                        corePath = shipPart.corePath + 1;
                                }
                            }
                            else if (!foundWreck) //found has no shipIndex
                            {
                                foundWreck = true;
                            }
                        }
                    }
                }
            }
            //if we have an index, merge all
            if (index > -1)
            {
                shipIndex = index;
                mapComp.ShipsOnMapNew[shipIndex].AddToCache(this.parent as Building);
                if (shipsToMerge.Any() || foundWreck)
                {
                    foreach (int i in shipsToMerge) //remove all ships being merged
                    {
                        mapComp.ShipsOnMapNew.Remove(i);
                    }
                    AttachAllTo(this.parent as Building);
                }
            }
        }
        public static void AttachAllTo(Building start) //merge and build corePath
        {
            var map = start.Map;
            var mapComp = map.GetComponent<ShipHeatMapComp>();
            var startShipPart = start.TryGetComp<CompSoShipPart>();
            var ship = mapComp.ShipsOnMapNew[startShipPart.shipIndex];
            var cellsTodo = new HashSet<IntVec3>();
            var cellsDone = new HashSet<IntVec3>();
            cellsTodo.Add(start.Position);

            //do
            //find parts cardinal to all prev.pos, exclude prev.pos, if found part, if corePath -1 set corePath to i, shipIndex to core.shipIndex, set corePath
            //till no more parts
            int i = startShipPart.corePath;
            while (cellsTodo.Count > 0)
            {
                List<IntVec3> current = cellsTodo.ToList();
                foreach (IntVec3 vec in current) //do all of the current corePath
                {
                    List<Building> otherBuildings = new List<Building>();
                    bool partFound = false;
                    foreach (Thing t in vec.GetThingList(map))
                    {
                        if (t is Building b)
                        {
                            var shipPart = b.TryGetComp<CompSoShipPart>();
                            if (shipPart != null)
                            {
                                shipPart.shipIndex = startShipPart.shipIndex; //shipIndex to core.shipIndex
                                ship.AddToCache(b);
                                if (b.def.building.shipPart) //if corePath -1 set corePath to i
                                {
                                    shipPart.corePath = i;
                                }
                                partFound = true;
                            }
                            else
                                otherBuildings.Add(b);
                        }
                    }
                    //add other buildings to weight
                    if (partFound)
                    {
                        foreach (Building b in otherBuildings)
                        {
                            ship.BuildingCount++;
                            ship.Mass += (b.def.Size.x * b.def.Size.z) * 3;
                        }
                    }

                    cellsTodo.Remove(vec);
                    cellsDone.Add(vec);
                }
                foreach (IntVec3 vec in current) //find parts cardinal to all prev.pos, exclude prev.pos
                {
                    cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(vec, Rot4.North, new IntVec2(1, 1)).Where(v => !cellsDone.Contains(v) && v.GetThingList(map).Any(t => t is Building b && b.def.building.shipPart && b.GetComp<CompSoShipPart>().shipIndex != startShipPart.shipIndex)));
                }
                i++;
                //Log.Message("parts at i: "+ current.Count + "/" + i);
            }
        }
        public void PreDeSpawn(Map map)
        {
            if (shipIndex > -1)
            {
                if (this.parent.def.building.shipPart) //remove and detach
                {
                    List<Building> toCheck = new List<Building>();
                    List<Building> toDeindex = new List<Building>();
                    foreach (IntVec3 vec in GenAdj.CellsAdjacentCardinal(this.parent)) //find all attached ship parts
                    {
                        foreach (Thing t in vec.GetThingList(map))
                        {
                            if (t is Building b)
                            {
                                if (b.def.building.shipPart && b.TryGetComp<CompSoShipPart>().corePath > corePath)
                                {
                                    toCheck.Add(b);
                                }
                            }
                        }
                    }
                    if (toCheck.Any())
                    {
                        Building_ShipBridge foundBridge = null;
                        foreach (Building b in toCheck) //check all surounding
                        {
                            List<IntVec3> area = FindAreaToDetach(b, this.parent as Building);
                            if (area.Any())
                            {
                                if (mapComp.InCombat)
                                {
                                    Detach(area, map, shipIndex);
                                    break;
                                }
                                foreach (IntVec3 v in area)
                                {
                                    foreach (Thing t in v.GetThingList(map))
                                    {
                                        if (t is Building u)
                                        {
                                            var shipPart = u.TryGetComp<CompSoShipPart>();
                                            if (shipPart != null)
                                            {
                                                if (u is Building_ShipBridge bridge)
                                                {
                                                    foundBridge = bridge;
                                                }
                                                toDeindex.Add(u);
                                                mapComp.ShipsOnMapNew[shipPart.shipIndex].RemoveFromCache(u);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (foundBridge != null) //make new ship
                        {
                            //foundBridge.CacheShip();
                        }
                        else //release as wreck
                        {
                            foreach (Building b in toDeindex)
                            {
                                b.TryGetComp<CompSoShipPart>().shipIndex = -1;
                            }
                        }
                    }
                }
                mapComp.ShipsOnMapNew[shipIndex].RemoveFromCache(this.parent as Building);
                shipIndex = -1;
                corePath = -1;
            }
        }
        public static List<IntVec3> FindAreaToDetach(Building root, Building exclude) //td touple to know if remove
        {
            var map = root.Map;
            var rootShipPart = root.TryGetComp<CompSoShipPart>();
            var cellsToDetach = new HashSet<IntVec3>();
            var cellsTodo = new HashSet<IntVec3>();
            var cellsDone = GenAdj.CellsOccupiedBy(exclude).ToList();

            //add only adjacent cells that have a shipPart part and a higher corePath
            cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(root).Where(v => v.GetThingList(map).Any(t => t is Building b && b.def.building.shipPart && b != exclude)));
            Log.Message("todo cell count: " + cellsTodo.Count);
            //check cells till you find lower than root core, if not return all found cells
            while (cellsTodo.Count > 0)
            {
                var current = cellsTodo.First();
                //Log.Message("cell: " + current.x +","+ current.z);
                cellsTodo.Remove(current);
                cellsDone.Add(current);
                var containedThings = current.GetThingList(map);

                if (!containedThings.Any(t => t is Building b && b.def.building.shipPart))
                {
                    //if no ship shipPart part on current tile skip
                    continue;
                }

                foreach (Thing t in containedThings)
                {
                    if (t is Building b)
                    {
                        var shipPart = b.TryGetComp<CompSoShipPart>();
                        if (b.def.building.shipPart && shipPart != null)
                        {
                            if (shipPart.corePath < rootShipPart.corePath) //if part with lower corePath found exit
                            {
                                Log.Message("lower corePath found: " + shipPart.corePath);
                                return new List<IntVec3>();
                            }

                            if (cellsToDetach.Add(b.Position)) //add current tile
                            {
                                //extend search range
                                cellsTodo.AddRange(GenAdj.CellsOccupiedBy(b).Concat(GenAdj.CellsAdjacentCardinal(b)).Where(cell => !cellsDone.Contains(cell)));
                            }
                        }
                    }
                }
            }
            Log.Message("detach cell count: " + cellsToDetach.Count);
            return cellsToDetach.ToList();
        }
        public static void Detach(List<IntVec3> detachArea, Map map, int shipIndex)
        {
            var mapComp = map.GetComponent<ShipHeatMapComp>();
            //Log.Message("Detaching " + detached.Count + " tiles");
            ShipInteriorMod2.AirlockBugFlag = true;
            List<Thing> toDestroy = new List<Thing>();
            List<Thing> toReplace = new List<Thing>();
            bool detachThing = false;
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minZ = int.MaxValue;
            int maxZ = int.MinValue;
            foreach (IntVec3 at in detachArea)
            {
                //Log.Message("Detaching location " + at);
                foreach (Thing t in at.GetThingList(map))
                {
                    if (t is Building_ShipBridge) //stopgap for invalid state bug
                    {
                        if (mapComp.ShipsOnMapNew.Keys.Contains(t.thingIDNumber))
                        {
                            Log.Message("Tried removing primary bridge from ship, aborting detach.");
                            ShipInteriorMod2.AirlockBugFlag = false;
                            mapComp.RemoveShipFromBattle(shipIndex);
                            return;
                        }
                    }
                    if (t is Pawn)
                    {
                        if (t.Faction != Faction.OfPlayer && Rand.Chance(0.75f))
                            toDestroy.Add(t);
                    }
                    else if (!(t is Blueprint))
                        toDestroy.Add(t);
                    if (t is Building b)
                    {
                        var shipPart = b.TryGetComp<CompSoShipPart>();
                        if (shipPart != null && b.def.building.shipPart)
                        {
                            shipPart.shipIndex = -1;
                            mapComp.ShipsOnMapNew[shipIndex].RemoveFromCache(b);
                        }
                        detachThing = true;
                        if (b.TryGetComp<CompRoofMe>() != null)
                        {
                            toReplace.Add(b);
                        }
                        else if (b.def.IsEdifice())
                        {
                            toReplace.Add(b);
                        }
                    }
                    if (t.Position.x < minX)
                        minX = t.Position.x;
                    if (t.Position.x > maxX)
                        maxX = t.Position.x;
                    if (t.Position.z < minZ)
                        minZ = t.Position.z;
                    if (t.Position.z > maxZ)
                        maxZ = t.Position.z;
                }
                map.terrainGrid.RemoveTopLayer(at, false);
            }
            foreach (Thing t in toReplace)
            {
                if (t.def.IsEdifice())
                {
                    Thing replacement = ThingMaker.MakeThing(ShipInteriorMod2.wreckedBeamDef);
                    replacement.Position = t.Position;
                    if (t.def.destroyable && !t.Destroyed)
                        t.Destroy();
                    replacement.SpawnSetup(map, false);
                    toDestroy.Add(replacement);
                }
                else
                {
                    Thing replacement = ThingMaker.MakeThing(ShipInteriorMod2.wreckedHullPlateDef);
                    replacement.Position = t.Position;
                    if (t.def.destroyable && !t.Destroyed)
                        t.Destroy();
                    replacement.SpawnSetup(map, false);
                    toDestroy.Add(replacement);
                }
            }
            if (detachThing)
            {
                DetachedShipPart part = (DetachedShipPart)ThingMaker.MakeThing(ThingDef.Named("DetachedShipPart"));
                part.Position = new IntVec3(minX, 0, minZ);
                part.xSize = maxX - minX + 1;
                part.zSize = maxZ - minZ + 1;
                part.wreckage = new byte[part.xSize, part.zSize];
                foreach (Thing t in toDestroy)
                {
                    if (t is Pawn)
                        t.Kill(new DamageInfo(DamageDefOf.Bomb, 100f));
                    else if (t.def == ShipInteriorMod2.wreckedBeamDef)
                        part.wreckage[t.Position.x - minX, t.Position.z - minZ] = 1;
                    else if (t.def == ShipInteriorMod2.wreckedHullPlateDef)
                        part.wreckage[t.Position.x - minX, t.Position.z - minZ] = 2;
                }
                part.SpawnSetup(map, false);
            }
            foreach (Thing t in toDestroy)
            {
                if (t is Building && map.IsPlayerHome && t.def.blueprintDef != null)
                {
                    GenConstruct.PlaceBlueprintForBuild(t.def, t.Position, map, t.Rotation, Faction.OfPlayer, t.Stuff);
                }
                map.terrainGrid.RemoveTopLayer(t.Position, false);
                if (t.def.destroyable && !t.Destroyed)
                    t.Destroy(DestroyMode.Vanish);
            }
            foreach (IntVec3 c in detachArea)
            {
                map.roofGrid.SetRoof(c, null);
            }
            ShipInteriorMod2.AirlockBugFlag = false;
            if (map == mapComp.ShipCombatOriginMap)
                mapComp.hasAnyPlayerPartDetached = true;
        }

        public override string CompInspectStringExtra() //debug only //td rem
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (this.parent.def.building.shipPart)
                stringBuilder.Append("shipIndex/corePath: " + shipIndex + "/" + corePath);
            else
                stringBuilder.Append("shipIndex: " + shipIndex);
            return stringBuilder.ToString();
        }
    }
}