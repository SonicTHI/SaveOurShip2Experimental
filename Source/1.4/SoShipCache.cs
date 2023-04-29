using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using SaveOurShip2;
using RimWorld.Planet;
using UnityEngine;
using Verse.AI.Group;

namespace RimWorld
{
    public class SoShipCache
    {
		/*
        //td no function on ship parts if no bridge (tur,eng,sen)
        //functionality:
        //post load: after all spawned, RebuildCache //td currently only on clicking bridge
        //battle start: player - RebuildCorePath if corePathDirty, enemy - RebuildCache //td currently RebuildCache both
        //shipmove: if moved vec - offset/rot area, if diff map rem from curr, add to target
        //possible merge: //td either prevent placenear other ship or merge code

        //on shipPart added
        //possible merge: ooc - add all parts to this ship / ic - should not happen //td prevent construction PlaceWorker_ShipPart

        //on shipPart removed
        //possible split: ooc - check for bridge, ic - detach, ooc - RebuildCache for each dcon bridge, -1 to rest

        //other buildings: +-for count, mass if on shipPart

        public string Name = "Unnamed Ship";
        public int Index = -1;
        public bool IsWreck = true;
        public bool CanMove = false;
        public Building_ShipBridge Core; //if null, this is a wreck
        public int Mass = 0;
        public float MaxTakeoff = 0;
        public int BuildingCount = 0;
        public int BuildingCountAtCombatStart = 0;
        public int ThreatRaw = 0;
        public Map map;
        public ShipHeatMapComp mapComp;
        public bool PathDirty = true;
        public int Threat
        {
            get
            {
                return ThreatRaw + Mass / 100;
            }
        }
        private float[] ThreatPerSegment = new[] { 1f, 1f, 1f, 1f };
        public bool Rotatable
        {
            get { return !BuildingsNonRot.Any(); }
        }
        public float ThrustRaw = 0;
        public float ThrustRatio
        {
            get
            {
                return ThrustRaw * 500f / Mathf.Pow(BuildingCount, 1.1f);
            }
        }
        public IntVec3 LowestCorner
        {
            get
            {
                IntVec3 lowestCorner = new IntVec3(int.MaxValue, 0, int.MaxValue);
                foreach (IntVec3 v in Area)
                {
                    if (v.x < lowestCorner.x)
                        lowestCorner.x = v.x;
                    if (v.z < lowestCorner.z)
                        lowestCorner.z = v.z;
                }
                return lowestCorner;
            }
        }
        public List<Thing> ThingsOnShip() //dep
        {
            List<Thing> things = new List<Thing>();
            foreach (IntVec3 v in Area)
            {
                foreach (Thing t in v.GetThingList(Core.Map))
                {
                    if (!things.Contains(t))
                        things.Add(t);
                }
            }
            return things;
        }
        public List<Building> BuildingsOnShip() //dep
        {
            if (Buildings.Any())
                return Buildings.ToList();

            List<Building> buildings = new List<Building>();
            foreach (IntVec3 v in Area)
            {
                foreach (Thing t in v.GetThingList(Core.Map))
                {
                    if (t is Building b && !buildings.Contains(b))
                        buildings.Add(b);
                }
            }
            return buildings;
        }

        public HashSet<IntVec3> Area = new HashSet<IntVec3>(); //shipParts add to area
        public HashSet<IntVec3> AreaDestroyed = new HashSet<IntVec3>(); //add to when destroyed in combat
        public HashSet<Building> Parts = new HashSet<Building>(); //shipParts only - limited use atm
        public HashSet<Building> Buildings = new HashSet<Building>(); //all on ship parts, even partially
        public HashSet<Building> BuildingsNonRot = new HashSet<Building>();
        public List<CompEngineTrail> Engines = new List<CompEngineTrail>();
        public List<CompPowerBattery> Batteries = new List<CompPowerBattery>();
        public List<CompShipHeatPurge> HeatPurges = new List<CompShipHeatPurge>();
        public List<CompShipCombatShield> Shields = new List<CompShipCombatShield>();
        public List<CompCryptoLaunchable> Pods = new List<CompCryptoLaunchable>();
        public List<Building_ShipBridge> Bridges = new List<Building_ShipBridge>();
        public HashSet<Building_ShipTurret> Turrets = new HashSet<Building_ShipTurret>();
        public List<Building_ShipAdvSensor> Sensors = new List<Building_ShipAdvSensor>();
        public List<CompHullFoamDistributor> FoamDistributors = new List<CompHullFoamDistributor>();
        public List<CompShipLifeSupport> LifeSupports = new List<CompShipLifeSupport>();

        public bool TryReplaceCore()
        {
            if (Bridges.NullOrEmpty())
            {
                IsWreck = true;
                return false;
            }
            Core = Bridges.FirstOrDefault(b => !b.Destroyed);
            mapComp.MapRootList.Add(Core);
            return true;
        }
        public void Capture(Faction fac)
        {
            foreach (Building building in Buildings)
            {
                if (building.def.CanHaveFaction)
                    building.SetFaction(fac);
            }
        }
        public void AllOff()
        {
            EnginesOff();
            var heatnNet = Core.TryGetComp<CompShipHeat>().myNet;
            heatnNet.ShieldsOff();
            heatnNet.TurretsOff();
        }
        public void EnginesOff()
        {
            foreach (var engine in Engines)
            {
                engine.Off();
            }
        }


        public float[] ActualThreatPerSegment() //turrets that cant actually fire due to ammo
        {
            float[] actualThreatPerSegment = ThreatPerSegment;
            foreach (var turret in Turrets)
            {
                var torp = turret.TryGetComp<CompChangeableProjectilePlural>();
                var fuel = turret.TryGetComp<CompRefuelable>();
                if ((torp != null && !torp.Loaded) || (fuel != null && fuel.Fuel == 0f))
                {
                    int threat = turret.heatComp.Props.threat;
                    if (turret.heatComp.Props.maxRange > 150) //long
                    {
                        actualThreatPerSegment[0] -= threat / 6f;
                        actualThreatPerSegment[1] -= threat / 4f;
                        actualThreatPerSegment[2] -= threat / 2f;
                        actualThreatPerSegment[3] -= threat;
                    }
                    else if (turret.heatComp.Props.maxRange > 100) //med
                    {
                        actualThreatPerSegment[0] -= threat / 4f;
                        actualThreatPerSegment[1] -= threat / 2f;
                        actualThreatPerSegment[2] -= threat;
                    }
                    else if (turret.heatComp.Props.maxRange > 50) //short
                    {
                        actualThreatPerSegment[0] -= threat / 2f;
                        actualThreatPerSegment[1] -= threat;
                    }
                    else //cqc
                        actualThreatPerSegment[0] -= threat;
                }
            }
            return actualThreatPerSegment;
        }
        public float EnginePower(int engineRot, int heading)
        {
            CanMove = false;
            float enginePower = 0;
            foreach (var engine in Engines)
            {
                if (engine.CanFire(engineRot))
                {
                    CanMove = true;
                    if (heading != 0 && engine.On())
                    {
                        enginePower += engine.Props.thrust;
                    }
                    else
                        engine.Off();
                }
                else
                    engine.Off();
            }
            return enginePower;
        }
        public void PurgeCheck()
        {
            if (!HeatPurges.Any(purge => purge.purging)) //heatpurge - only toggle when not purging
            {
                if (HeatPurges.Any(purge => purge.fuelComp.FuelPercentOfMax > 0.2f) && Core != null && Core.heatComp.myNet != null && Core.heatComp.myNet.RatioInNetwork > 0.7f) //start purge
                {
                    foreach (CompShipHeatPurge purge in HeatPurges)
                    {
                        purge.StartPurge();
                    }
                }
                else if (Shields.Any(shield => shield.shutDown)) //repower shields
                {
                    foreach (var shield in Shields)
                    {
                        if (shield.flickComp == null)
                            continue;
                        shield.flickComp.SwitchIsOn = true;
                    }
                }
            }
        }

        public void RebuildCorePath(Building core) //run before combat if PathDirty and in combat after bridge replaced
        {
            if (core == null || !(core is Building_ShipBridge) || core.Destroyed)
                return;

            Core = core as Building_ShipBridge;
            var mapComp = map.GetComponent<ShipHeatMapComp>();
            var cellsTodo = new HashSet<IntVec3>();
            var cellsDone = new HashSet<IntVec3>();
            cellsTodo.Add(core.Position);
            int mergeToIndex = mapComp.ShipCells[core.Position].Item1;

            //find parts cardinal to all prev.pos, exclude prev.pos
            int path = 0;
            while (cellsTodo.Count > 0)
            {
                List<IntVec3> current = cellsTodo.ToList();
                foreach (IntVec3 vec in current) //do all of the current corePath
                {
                    mapComp.ShipCells[vec] = new Tuple<int, int>(mergeToIndex, path);
                    cellsTodo.Remove(vec);
                    cellsDone.Add(vec);
                }
                foreach (IntVec3 vec in current) //find parts cardinal to all prev.pos, exclude prev.pos
                {
                    cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(vec, Rot4.North, new IntVec2(1, 1)).Where(v => !cellsDone.Contains(v) && mapComp.ShipCells.ContainsKey(v)));
                }
                path++;
            }
            PathDirty = false;
            Log.Message("New bridge: " + Core);
            Log.Message("Rebuilt corePath for ship: " + core.Position);
        }
        public void RebuildCache(Building origin, HashSet<IntVec3> exclude = null) //full rebuild, on load, merge
        {
            if (origin == null || origin.Destroyed)
            {
                Log.Error("SOS2 ship recache: tried merging to null or destroyed origin");
                return;
            }
            map = origin.Map;
            mapComp = map.GetComponent<ShipHeatMapComp>();
            Index = origin.thingIDNumber;
            int path = -1;
            if (origin is Building_ShipBridge core)
            {
                Core = core;
                mapComp.MapRootList.Add(core);
                Name = core.ShipName;
                IsWreck = false;
                Core.Index = Index;
                Core.Ship = mapComp.ShipsOnMapNew[Index];
                path = 0;
            }

            HashSet<IntVec3> cellsTodo = new HashSet<IntVec3>();
            HashSet<IntVec3> cellsDone = new HashSet<IntVec3>();
            if (exclude != null)
                cellsDone.AddRange(exclude);
            cellsTodo.Add(origin.Position);

            //find cells cardinal to all prev.pos, exclude prev.pos, if found part, set corePath to i, shipIndex to core.shipIndex, set corePath
            while (cellsTodo.Count > 0)
            {
                List<IntVec3> current = cellsTodo.ToList();
                foreach (IntVec3 vec in current) //do all of the current corePath
                {
                    mapComp.ShipCells[vec] = new Tuple<int, int>(Index, path); //add new vec, index, corepath
                    foreach (Thing t in vec.GetThingList(map))
                    {
                        if (t is Building b)
                        {
                            AddToCache(b);
                        }
                    }
                    cellsTodo.Remove(vec);
                    cellsDone.Add(vec);
                }
                foreach (IntVec3 vec in current) //find next set cardinal to all cellsDone, exclude cellsDone
                {
                    cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(vec, Rot4.North, new IntVec2(1, 1)).Where(v => mapComp.ShipCells.ContainsKey(v) && !cellsDone.Contains(v)));
                }
                if (path > -1)
                    path++;
                //Log.Message("parts at i: "+ current.Count + "/" + i);
            }
            PathDirty = false;
            Log.Message("Rebuilt cache for ship: " + Index);
            Log.Message("Parts: " + Parts.Count);
            Log.Message("Buildings: " + Buildings.Count);
            Log.Message("Bridges: " + Bridges.Count);
            Log.Message("Area: " + Area.Count);
        }
        public void AddToCache(Building b)
        {
            if (Buildings.Add(b))
            {
                BuildingCount++;
                if (b.def.rotatable == false && b.def.size.x != b.def.size.z)
                {
                    BuildingsNonRot.Add(b);
                }
                var part = b.TryGetComp<CompSoShipPart>();
                if (part != null)
                {
                    if (b.def.building.shipPart)
                    {
                        Parts.Add(b);
                        foreach (IntVec3 v in GenAdj.CellsOccupiedBy(b))
                        {
                            Area.Add(v);
                        }
                        if (part.Props.isPlating)
                        {
                            Mass += 1;
                            return;
                        }
                        else if (b.TryGetComp<CompEngineTrail>() != null)
                        {
                            var refuelable = b.TryGetComp<CompRefuelable>();
                            ThrustRaw += b.TryGetComp<CompEngineTrail>().Props.thrust;
                            if (refuelable != null)
                            {
                                MaxTakeoff += refuelable.Props.fuelCapacity;
                                if (refuelable.Props.fuelFilter.AllowedThingDefs.Contains(ThingDef.Named("ShuttleFuelPods")))
                                    MaxTakeoff += refuelable.Props.fuelCapacity;
                            }
                            Engines.Add(b.TryGetComp<CompEngineTrail>());
                        }
                    }
                    else
                    {
                        if (b.TryGetComp<CompCryptoLaunchable>() != null)
                            Pods.Add(b.GetComp<CompCryptoLaunchable>());
                        else if (b is Building_ShipBridge bridge)
                        {
                            IsWreck = false;
                            Bridges.Add(bridge);
                            bridge.ShipName = Name;
                            bridge.Index = Index;
                            bridge.Ship = mapComp.ShipsOnMapNew[Index];
                            if (Core.DestroyedOrNull())
                            {
                                mapComp.MapRootList.Add(Core);
                                Core = bridge;
                            }
                        }
                        else if (b is Building_ShipTurret turret)
                        {
                            Turrets.Add(turret);
                            int threat = turret.heatComp.Props.threat;
                            if (turret.heatComp.Props.maxRange > 150) //long
                            {
                                ThreatPerSegment[0] += threat / 6f;
                                ThreatPerSegment[1] += threat / 4f;
                                ThreatPerSegment[2] += threat / 2f;
                                ThreatPerSegment[3] += threat;
                            }
                            else if (turret.heatComp.Props.maxRange > 100) //med
                            {
                                ThreatPerSegment[0] += threat / 4f;
                                ThreatPerSegment[1] += threat / 2f;
                                ThreatPerSegment[2] += threat;
                            }
                            else if (turret.heatComp.Props.maxRange > 50) //short
                            {
                                ThreatPerSegment[0] += threat / 2f;
                                ThreatPerSegment[1] += threat;
                            }
                            else //cqc
                                ThreatPerSegment[0] += threat;
                        }
                        else if (b is Building_ShipAdvSensor sensor)
                            Sensors.Add(sensor);
                        else if (b.TryGetComp<CompHullFoamDistributor>() != null)
                            FoamDistributors.Add(b.GetComp<CompHullFoamDistributor>());
                        else if (b.TryGetComp<CompShipLifeSupport>() != null)
                            LifeSupports.Add(b.GetComp<CompShipLifeSupport>());
                    }
                }
                var comp = b.TryGetComp<CompShipHeat>();
                if (comp != null)
                {
                    ThreatRaw += comp.Props.threat;
                    if (comp is CompShipHeatPurge purge)
                    {
                        HeatPurges.Add(purge);
                    }
                    else if (comp is CompShipCombatShield shield)
                        Shields.Add(shield);
                }
                else if (b.def == ThingDef.Named("ShipSpinalAmplifier"))
                    ThreatRaw += 5;
                Mass += b.def.Size.x * b.def.Size.z * 3;
            }
        }
        public void RemoveFromCache(Building b)
        {
            if (Buildings.Contains(b))
            {
                BuildingCount--;
                Buildings.Remove(b);
                if (BuildingsNonRot.Contains(b))
                {
                    BuildingsNonRot.Remove(b);
                }
                var part = b.TryGetComp<CompSoShipPart>();
                if (part != null)
                {
                    if (b.def.building.shipPart)
                    {
                        Parts.Remove(b);
                        if (part.Props.isPlating)
                        {
                            Mass -= 1;
                            return;
                        }
                        else if (b.TryGetComp<CompEngineTrail>() != null)
                        {
                            var refuelable = b.TryGetComp<CompRefuelable>();
                            ThrustRaw -= b.TryGetComp<CompEngineTrail>().Props.thrust;
                            if (refuelable != null)
                            {
                                MaxTakeoff -= refuelable.Props.fuelCapacity;
                                if (refuelable.Props.fuelFilter.AllowedThingDefs.Contains(ThingDef.Named("ShuttleFuelPods")))
                                    MaxTakeoff -= refuelable.Props.fuelCapacity;
                            }
                            Engines.Remove(b.TryGetComp<CompEngineTrail>());
                        }
                    }
                    else
                    {
                        if (b.TryGetComp<CompCryptoLaunchable>() != null)
                            Pods.Remove(b.GetComp<CompCryptoLaunchable>());
                        else if (b is Building_ShipBridge bridge)
                        {
                            Bridges.Remove(bridge);
                            if (bridge == Core)
                            {
                                mapComp.MapRootList.Remove(bridge);
                                TryReplaceCore();
                            }
                            bridge.Index = -1;
                            bridge.Ship = null;
                            bridge.ShipName = "Unnamed Ship";
                        }
                        else if (b is Building_ShipTurret turret)
                        {
                            Turrets.Remove(turret);
                            int threat = turret.heatComp.Props.threat;
                            if (turret.heatComp.Props.maxRange > 150) //long
                            {
                                ThreatPerSegment[0] -= threat / 6f;
                                ThreatPerSegment[1] -= threat / 4f;
                                ThreatPerSegment[2] -= threat / 2f;
                                ThreatPerSegment[3] -= threat;
                            }
                            else if (turret.heatComp.Props.maxRange > 100) //med
                            {
                                ThreatPerSegment[0] -= threat / 4f;
                                ThreatPerSegment[1] -= threat / 2f;
                                ThreatPerSegment[2] -= threat;
                            }
                            else if (turret.heatComp.Props.maxRange > 50) //short
                            {
                                ThreatPerSegment[0] -= threat / 2f;
                                ThreatPerSegment[1] -= threat;
                            }
                            else //cqc
                                ThreatPerSegment[0] -= threat;
                        }
                        else if (b is Building_ShipAdvSensor sensor)
                            Sensors.Remove(sensor);
                        else if (b.TryGetComp<CompHullFoamDistributor>() != null)
                            FoamDistributors.Remove(b.GetComp<CompHullFoamDistributor>());
                        else if (b.TryGetComp<CompShipLifeSupport>() != null)
                            LifeSupports.Remove(b.GetComp<CompShipLifeSupport>());
                    }
                }
                var comp = b.TryGetComp<CompShipHeat>();
                if (comp != null)
                {
                    ThreatRaw -= comp.Props.threat;
                    if (comp is CompShipHeatPurge purge)
                    {
                        HeatPurges.Remove(purge);
                    }
                    else if (comp is CompShipCombatShield shield)
                        Shields.Remove(shield);
                }
                else if (b.def == ThingDef.Named("ShipSpinalAmplifier"))
                    ThreatRaw -= 5;
                Mass -= b.def.Size.x * b.def.Size.z * 3;
            }
        }
        public bool CheckForDetach()
        {
            if (AreaDestroyed.Any()) //path all back to bridge, if not possible, detach
            {
                Log.Message("Destroyed cells: " + AreaDestroyed.Count);
                //find adj to all detached that are not in detach and are in ship area
                HashSet<IntVec3> initialCells = new HashSet<IntVec3>(); //cells to start checks from
                HashSet<IntVec3> cellsDone = new HashSet<IntVec3>(); //all cells that were checked
                foreach (IntVec3 vec in AreaDestroyed)
                {
                    foreach (IntVec3 v in GenAdj.CellsAdjacentCardinal(vec, Rot4.North, new IntVec2(1, 1)).Where(v => !AreaDestroyed.Contains(v) && Area.Contains(v)))
                    {
                        initialCells.Add(v);
                    }
                }
                Log.Message("Cells to check for detach: " + initialCells.Count);
                //for each cell try and path back to cell with lower core path, if true found area is safe else detach
                foreach (IntVec3 setStartCell in initialCells)
                {
                    if (cellsDone.Contains(setStartCell)) //skip already checked cells
                    {
                        continue;
                    }
                    bool detach = true;
                    HashSet<IntVec3> cellsToDetach = new HashSet<IntVec3>();
                    HashSet<IntVec3> cellsTodo = new HashSet<IntVec3>{setStartCell};
                    while (cellsTodo.Count > 0)
                    {
                        IntVec3 current = cellsTodo.First();
                        cellsTodo.Remove(current);
                        cellsDone.Add(current);
                        if (mapComp.ShipCells[current].Item2 < mapComp.ShipCells[setStartCell].Item2)
                        {
                            detach = false;
                            break; //if part with lower corePath found this set is attached
                        }
                        if (cellsToDetach.Add(current)) //add current tile && extend search range, skip non ship, destroyed tiles
                        {
                            cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(current, Rot4.North, new IntVec2(1, 1)).Where(v => !cellsDone.Contains(v) && Area.Contains(v) && !AreaDestroyed.Contains(v)));
                        }
                    }
                    if (detach)
                    {
                        Log.Message("Detaching cells: " + cellsToDetach.Count);
                        Detach(cellsToDetach);
                    }
                }
                AreaDestroyed.Clear();
                return true;
            }
            return false;
        }
        public void Detach(HashSet<IntVec3> detachArea)
        {
            //detach as new ship if bridge found or wreck
            Building newCore = null;
            foreach (IntVec3 vec in detachArea) //clean area indexes and find bridge
            {
                if (newCore == null)
                {
                    foreach (Building bridge in mapComp.MapRootListAll)
                    {
                        if (bridge.Position == vec)
                        {
                            newCore = bridge;
                            break;
                        }
                    }
                }
                mapComp.ShipCells[vec] = new Tuple<int, int>(-1, -1);
            }
            if (!mapComp.InCombat)
            {
                if (newCore == null) //make wreck
                {
                    newCore = (Building)detachArea.First().GetThingList(map).First(t => t is Building);
                    Log.Message("making new wreck with: " + newCore);
                    mapComp.ShipsOnMapNew.Add(newCore.thingIDNumber, new SoShipCache());
                    mapComp.ShipsOnMapNew[newCore.thingIDNumber].RebuildCache(newCore, AreaDestroyed);
                    return;
                }
                else
                {
                    Log.Message("making new ship with: " + newCore);
                    mapComp.ShipsOnMapNew.Add(newCore.thingIDNumber, new SoShipCache());
                    mapComp.ShipsOnMapNew[newCore.thingIDNumber].RebuildCache(newCore, AreaDestroyed);
                    if (!mapComp.InCombat)
                        return;
                }
            }
            return;
            //td clean this up
            //combat: make floating wreck, if bridge found send to grave

            //Log.Message("Detaching " + detached.Count + " tiles");
            ShipInteriorMod2.AirlockBugFlag = true;
            HashSet<Thing> toDestroy = new HashSet<Thing>();
            HashSet<Thing> toReplace = new HashSet<Thing>();
            HashSet<Pawn> toKill = new HashSet<Pawn>();
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minZ = int.MaxValue;
            int maxZ = int.MinValue;
            foreach (IntVec3 vec in detachArea)
            {
                //Log.Message("Detaching location " + at);
                foreach (Thing t in vec.GetThingList(map).Where(t => t.def.destroyable && !t.Destroyed))
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
                    if (t is Building b && b.def.building.shipPart)
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
            if (newCore != null)
            {
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
                mapComp.RemoveShipFromBattle(newCore.thingIDNumber, newCore);
            }
            ShipInteriorMod2.AirlockBugFlag = false;
            if (map == mapComp.ShipCombatOriginMap)
                mapComp.hasAnyPlayerPartDetached = true;
        }*/
    }
}