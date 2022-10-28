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
        //xml tagged building shipPart: anything attachable - walls, hullfoam, plating, engines, corners, hardpoints, spinal barrels
        //SoShipPart isHull (props isHull): walls, corners, hullfoam fills these if destroyed, wrecks form from these
        //SoShipPart isPlating (props isPlating): can not be placed under buildings, wrecks form from these
        //SoShipPart hermetic: hold air in vacuum - walls, corners, engines, hullfoam, extenders, spinal barrels
        //SoShipPart: other parts that are cached - not attached, no corePath


        //old - commented code  here and in SoShipCache refers to this, change to above
        //xml tagged building shipPart only: never merge, only hold air - extenders
        //shipPart + xml tagged building shipPart: anything attachable - walls, plating, engines, corners, hardpoints, spinal barrels
        //shipPart hull (props isHull): walls, corners, hullfoam fills these if destroyed, wrecks form from these
        //shipPart plating (props isPlating): can not be placed under buildings, wrecks form from these
        //shipPart: other parts that are cached - not attached, no corePath

        //public int shipIndex = -1; //main bridge thingid
        //public int corePath = -1; //how far away the main bridge is
        public ShipHeatMapComp mapComp;
        public CompProperties_SoShipPart Props
        {
            get
            {
                return (CompProperties_SoShipPart)props;
            }
        }
        /*
        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (Prefs.DevMode)
            {
                if (this.parent.def.building.shipPart)
                    stringBuilder.Append("shipIndex: " + shipIndex + " / corePath: " + corePath);
                else
                    stringBuilder.Append("shipIndex: " + shipIndex);
            }
            return stringBuilder.ToString();
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            mapComp = this.parent.Map.GetComponent<ShipHeatMapComp>();
            if (mapComp.CacheOff)
            {
                return;
            }
            //shipPart or part placed on plating
            //shipPart: if any plating under has valid shipIndex, set to this, set to all other plating under
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
                                        mapComp.ShipsOnMap[index].AddToCache(this.parent as Building);
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
            //plating or shipPart: chk all cardinal, if any plating or shipPart has valid shipIndex, set to this
            //plating or shipPart with different or no shipIndex: merge connected to this ship
            if (this.parent.def.building.shipPart)
            {
                foreach (IntVec3 vec in GenAdj.CellsAdjacentCardinal(this.parent))
                {
                    foreach (Thing t in vec.GetThingList(this.parent.Map))
                    {
                        if (t is Building b && b.def.building.shipPart)
                        {
                            var shipPart = b.TryGetComp<CompSoShipPart>();
                            if (shipPart != null && shipPart.shipIndex > -1) //found ship has shipIndex
                            {
                                if (index < 0) //assign first shipIndex
                                {
                                    index = shipPart.shipIndex;
                                    if (corePath < 0)
                                        corePath = shipPart.corePath + 1;
                                }
                                else //this and found have shipIndex
                                {
                                    if (index != shipPart.shipIndex) //another ship found
                                    {
                                        //if larger, add prev to list + set this to new, else add new to merge
                                        if (mapComp.ShipsOnMap[shipPart.shipIndex].Mass > mapComp.ShipsOnMap[index].Mass)
                                        {
                                            shipsToMerge.Add(index);
                                            index = shipPart.shipIndex;
                                            if (corePath < 0)
                                                corePath = shipPart.corePath + 1;
                                        }
                                        else
                                            shipsToMerge.Add(shipPart.shipIndex);
                                    }
                                    else if (corePath > -1 && corePath + 1 > shipPart.corePath) //same ship, get lower corePath
                                    {
                                        corePath = shipPart.corePath + 1;
                                    }
                                }
                                break; //stop after first ship part on cell
                            }
                        }
                    }
                }
            }
            //if we have an index, merge all
            if (index > -1)
            {
                shipIndex = index;
                mapComp.ShipsOnMap[shipIndex].AddToCache(this.parent as Building);
                if (shipsToMerge.Any())
                {
                    foreach (int i in shipsToMerge) //remove all ships being merged
                    {
                        mapComp.ShipsOnMap.Remove(i);
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
            var ship = mapComp.ShipsOnMap[startShipPart.shipIndex];
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
                            ship.Mass += b.def.Size.x * b.def.Size.z * 3;
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
        public void PreDeSpawn(Map map, bool detach) //called in building.destroy, before comps get removed
        {
            if (shipIndex == -1)
            {
                return;
            }
            if (this.parent.def.building.shipPart && detach)
            {
                HashSet<Building> toDeindex = new HashSet<Building>();
                List<IntVec3> toCheck = FindAreaToDetach(this.parent as Building);
                if (toCheck.Any()) //path all back to bridge, if not possible, detach
                {
                    Log.Message("Detached cells: " + toCheck.Count);
                    Building_ShipBridge foundBridge = null;
                    if (mapComp.InCombat) //ic make wreck
                    {
                        mapComp.ShipsOnMap[shipIndex].RemoveFromCache(this.parent as Building);
                        Detach(toCheck, map, shipIndex);
                        return;
                    }
                    foreach (IntVec3 v in toCheck) //ooc deindex or make new ship if bridge found
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
                                }
                                //remove other buildings from weight
                                mapComp.ShipsOnMap[shipIndex].BuildingCount--;
                                mapComp.ShipsOnMap[shipIndex].Mass -= u.def.Size.x * u.def.Size.z * 3;
                            }
                        }
                    }
                    if (foundBridge != null) //make new ship
                    {
                        Log.Message("Ship separation, added new ship: " + foundBridge.thingIDNumber);
                        foundBridge.shipComp.shipIndex = -1;
                        foundBridge.CacheShip(this.parent as Building);
                    }
                    else //release as wreck
                    {
                        foreach (Building i in toDeindex)
                        {
                            mapComp.ShipsOnMap[shipIndex].RemoveFromCache(i);
                        }
                    }
                }
            }
            mapComp.ShipsOnMap[shipIndex].RemoveFromCache(this.parent as Building);
        }
        public List<IntVec3> FindAreaToDetach(Building root)
        {
            var map = root.Map;
            var rootShipPart = root.TryGetComp<CompSoShipPart>();
            var cellsToDetach = new HashSet<IntVec3>();
            var cellsTodo = new HashSet<IntVec3>();
            var cellsDone = GenAdj.CellsOccupiedBy(root).ToList();

            //add only adjacent cells that have a shipPart part and a higher corePath
            cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(root).Where(v => v.GetThingList(map).Any(t => t is Building b && !b.Destroyed && b.def.building.shipPart && b != root && b.TryGetComp<CompSoShipPart>() != null && b.GetComp<CompSoShipPart>().corePath > corePath)));
            //check cells till you find lower than root core, if not return all found cells
            while (cellsTodo.Count > 0)
            {
                //Log.Message("todo cell count: " + cellsTodo.Count);
                var current = cellsTodo.First();
                //Log.Message("cell: " + current.x +","+ current.z);
                cellsTodo.Remove(current);
                cellsDone.Add(current);
                var containedThings = current.GetThingList(map);

                if (!containedThings.Any(t => t is Building b && !b.Destroyed && b.def.building.shipPart))
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
                                //Log.Message("lower corePath found: " + shipPart.corePath);
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
            return cellsToDetach.ToList();
        }
        public static void Detach(List<IntVec3> detachArea, Map map, int shipIndex) //td clean this up
        {
            var mapComp = map.GetComponent<ShipHeatMapComp>();
            //Log.Message("Detaching " + detached.Count + " tiles");
            ShipInteriorMod2.AirlockBugFlag = true;
            HashSet<Thing> toDestroy = new HashSet<Thing>();
            HashSet<Thing> toReplace = new HashSet<Thing>();
            HashSet<Pawn> toKill = new HashSet<Pawn>();
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minZ = int.MaxValue;
            int maxZ = int.MinValue;
            foreach (IntVec3 at in detachArea)
            {
                //Log.Message("Detaching location " + at);
                foreach (Thing t in at.GetThingList(map).Where(t => t.def.destroyable && !t.Destroyed))
                {
                    if (t is Pawn p)
                    {
                        if (p.Faction != Faction.OfPlayer && Rand.Chance(0.75f))
                        {
                            toKill.Add(p);
                            toDestroy.Add(t);
                        }
                    }
                    else if (!(t is Blueprint))
                        toDestroy.Add(t);
                    if (t is Building b && b.TryGetComp<CompSoShipPart>() != null)
                    {
                        toReplace.Add(b);
                        if (t.Position.x < minX)
                            minX = t.Position.x;
                        if (t.Position.x > maxX)
                            maxX = t.Position.x;
                        if (t.Position.z < minZ)
                            minZ = t.Position.z;
                        if (t.Position.z > maxZ)
                            maxZ = t.Position.z;
                    }
                }
            }
            if (toReplace.Any()) //any shipPart, make a floating wreck
            {
                DetachedShipPart part = (DetachedShipPart)ThingMaker.MakeThing(ThingDef.Named("DetachedShipPart"));
                part.Position = new IntVec3(minX, 0, minZ);
                part.xSize = maxX - minX + 1;
                part.zSize = maxZ - minZ + 1;
                part.wreckage = new byte[part.xSize, part.zSize];
                foreach (Thing t in toReplace)
                {
                    var comp = t.TryGetComp<CompSoShipPart>();
                    if (comp.Props.isHull)
                        part.wreckage[t.Position.x - minX, t.Position.z - minZ] = 1;
                    else if (comp.Props.isPlating)
                        part.wreckage[t.Position.x - minX, t.Position.z - minZ] = 2;
                }
                part.SpawnSetup(map, false);
            }
            foreach (Pawn p in toKill)
            {
                p.Kill(new DamageInfo(DamageDefOf.Bomb, 100f));
            }
            foreach (Thing t in toDestroy)
            {
                if (t is Building && map.IsPlayerHome && t.def.blueprintDef != null)
                {
                    GenConstruct.PlaceBlueprintForBuild(t.def, t.Position, map, t.Rotation, Faction.OfPlayer, t.Stuff);
                }
                if (t.def.destroyable && !t.Destroyed)
                    t.Destroy(DestroyMode.Vanish);
            }
            foreach (IntVec3 c in detachArea)
            {
                map.terrainGrid.RemoveTopLayer(c, false);
                map.roofGrid.SetRoof(c, null);
            }
            ShipInteriorMod2.AirlockBugFlag = false;
            if (map == mapComp.ShipCombatOriginMap)
                mapComp.hasAnyPlayerPartDetached = true;
        }
        */
    }
}