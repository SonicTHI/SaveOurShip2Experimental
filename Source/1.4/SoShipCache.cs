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
        public Building_ShipBridge Core; //if null, this is a wreck
        //public bool corePathDirty = true; //false after recache, repath, true after merge
        public int Mass = 0;
        public float MaxTakeoff = 0;
        public int BuildingCount = 0;
        public int ThreatRaw = 0;
        public bool Rotatable = true; //td better check on move
        public bool PathDirty = true; //td
        public int Threat
        {
            get
            {
                return ThreatRaw + Mass / 100;
            }
        }

        public float ThrustRaw = 0;
        public float Thrust
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
        public HashSet<Building> Parts = new HashSet<Building>(); //shipParts only
        public HashSet<Building> Buildings = new HashSet<Building>(); //all
        public List<CompEngineTrail> Engines = new List<CompEngineTrail>();
        public List<CompCryptoLaunchable> Pods = new List<CompCryptoLaunchable>();
        public List<Building_ShipBridge> Bridges = new List<Building_ShipBridge>();
        public List<Building_ShipAdvSensor> Sensors = new List<Building_ShipAdvSensor>();
        public List<CompHullFoamDistributor> FoamDistributors = new List<CompHullFoamDistributor>();
        public List<CompShipLifeSupport> LifeSupports = new List<CompShipLifeSupport>();
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
        public void RebuildCorePath(Building core) //run before combat if corePathDirty and in combat after bridge destructio
        {
            if (core == null || !(core is Building_ShipBridge) || core.Destroyed)
                return;

            Core = core as Building_ShipBridge;
            var map = core.Map;
            var mapComp = map.GetComponent<ShipHeatMapComp>();
            var coreShipPart = core.TryGetComp<CompSoShipPart>();
            coreShipPart.corePath = 0;
            var cellsTodo = new HashSet<IntVec3>();
            var cellsDone = new HashSet<IntVec3>();
            cellsTodo.Add(core.Position);

            //find parts cardinal to all prev.pos, exclude prev.pos, if found part, set corePath to i, shipIndex to core.shipIndex
            int i = 0;
            while (cellsTodo.Count > 0)
            {
                List<IntVec3> current = cellsTodo.ToList();
                foreach (IntVec3 vec in current) //do all of the current corePath
                {
                    foreach (Thing t in vec.GetThingList(map))
                    {
                        if (t is Building b)
                        {
                            var shipPart = b.TryGetComp<CompSoShipPart>();
                            if (shipPart != null && b.def.building.shipPart)
                            {
                                shipPart.corePath = i;
                            }
                        }
                    }
                    cellsTodo.Remove(vec);
                    cellsDone.Add(vec);
                }
                foreach (IntVec3 vec in current) //use known shiparea, exclude prev.pos
                {
                    cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(vec, Rot4.North, new IntVec2(1, 1)).Where(v => !cellsDone.Contains(v) && Area.Contains(v)));
                }
                i++;
            }
            PathDirty = false;
            Log.Message("New bridge: " + Core);
            Log.Message("Rebuilt corePath for ship: " + core);
        }
        public void RebuildCache(Building core, Building exclude = null) //full rebuild, only call on load, shipspawn, ooc detach with a bridge
        {
            if (core == null || !(core is Building_ShipBridge) || core.Destroyed)
                return;

            Core = core as Building_ShipBridge;
            Name = ((Building_ShipBridge)core).ShipName;
            var map = core.Map;
            var mapComp = map.GetComponent<ShipHeatMapComp>();
            var coreShipPart = core.TryGetComp<CompSoShipPart>();
            coreShipPart.corePath = 0;
            coreShipPart.shipIndex = core.thingIDNumber;
            var cellsTodo = new HashSet<IntVec3>();
            var cellsDone = new HashSet<IntVec3>();
            if (exclude != null)
                cellsDone.AddRange(GenAdj.CellsOccupiedBy(exclude).ToList());
            cellsTodo.Add(core.Position);

            //find parts cardinal to all prev.pos, exclude prev.pos, if found part, set corePath to i, shipIndex to core.shipIndex, set corePath
            int i = 0;
            while (cellsTodo.Count > 0)
            {
                List<IntVec3> current = cellsTodo.ToList();
                foreach (IntVec3 vec in current) //do all of the current corePath
                {
                    HashSet<Building> otherBuildings = new HashSet<Building>();
                    bool partFound = false;
                    foreach (Thing t in vec.GetThingList(map))
                    {
                        if (t is Building b)
                        {
                            var shipPart = b.TryGetComp<CompSoShipPart>();
                            if (shipPart != null)
                            {
                                shipPart.shipIndex = coreShipPart.shipIndex; //shipIndex to core.shipIndex
                                AddToCache(b);
                                if (b.def.building.shipPart) //set corePath to i
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
                            if (Buildings.Add(b))
                            {
                                BuildingCount++;
                                Mass += b.def.Size.x * b.def.Size.z * 3;
                            }
                        }
                    }
                    cellsTodo.Remove(vec);
                    cellsDone.Add(vec);
                }
                foreach (IntVec3 vec in current) //find parts cardinal to all prev.pos, exclude prev.pos
                {
                    cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(vec, Rot4.North, new IntVec2(1,1)).Where(v => !cellsDone.Contains(v) && v.GetThingList(map).Any(t => t is Building b && b.def.building.shipPart && b.TryGetComp<CompSoShipPart>() != null)));
                }
                i++;
                //Log.Message("parts at i: "+ current.Count + "/" + i);
            }
            PathDirty = false;
            Log.Message("Rebuilt cache for ship: " + Core);
            Log.Message("Parts: " + Parts.Count);
            Log.Message("Bridges: " + Bridges.Count);
            Log.Message("Area: " + Area.Count);
        }
        public void AddToCache(Building b)
        {
            if (Parts.Add(b))
            {
                Buildings.Add(b);
                BuildingCount++;
                var shipComp = b.TryGetComp<CompSoShipPart>();
                if (b.def.building.shipPart)
                {
                    foreach (IntVec3 v in GenAdj.CellsOccupiedBy(b))
                    {
                        Area.Add(v);
                    }
                    if (shipComp.Props.isPlating)
                    {
                        Mass += 1;
                        return;
                    }
                    if (b.TryGetComp<CompEngineTrail>() != null)
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
                        Bridges.Add(bridge);
                        bridge.ShipName = Name;
                        if (Core.DestroyedOrNull())
                        {
                            Core = bridge;
                        }
                    }
                    else if (b is Building_ShipAdvSensor sensor)
                        Sensors.Add(sensor);
                    else if (b.TryGetComp<CompHullFoamDistributor>() != null)
                        FoamDistributors.Add(b.GetComp<CompHullFoamDistributor>());
                    else if (b.TryGetComp<CompShipLifeSupport>() != null)
                        LifeSupports.Add(b.GetComp<CompShipLifeSupport>());
                }
                if (b.TryGetComp<CompShipHeat>() != null)
                    ThreatRaw += b.TryGetComp<CompShipHeat>().Props.threat;
                else if (b.def == ThingDef.Named("ShipSpinalAmplifier"))
                    ThreatRaw += 5;
                Mass += b.def.Size.x * b.def.Size.z * 3;
            }
        }
        public void RemoveFromCache(Building b)
        {
            if (Parts.Contains(b))
            {
                BuildingCount--;
                Buildings.Remove(b);
                Parts.Remove(b);
                var shipComp = b.TryGetComp<CompSoShipPart>();
                shipComp.shipIndex = -1;
                if (b.def.building.shipPart)
                {
                    shipComp.corePath = -1;
                    foreach (IntVec3 v in GenAdj.CellsOccupiedBy(b))
                    {
                        if (Area.Contains(v) && v.GetThingList(b.Map).Any(u => u != b && u.TryGetComp<CompSoShipPart>() != null))
                            Area.Remove(v);
                    }
                    if (shipComp.Props.isPlating)
                    {
                        Mass -= 1;
                        return;
                    }
                    if (b.TryGetComp<CompEngineTrail>() != null)
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
                        bridge.ShipName = "Unnamed Ship";
                    }
                    else if (b is Building_ShipAdvSensor sensor)
                        Sensors.Remove(sensor);
                    else if (b.TryGetComp<CompHullFoamDistributor>() != null)
                        FoamDistributors.Remove(b.GetComp<CompHullFoamDistributor>());
                    else if (b.TryGetComp<CompShipLifeSupport>() != null)
                        LifeSupports.Remove(b.GetComp<CompShipLifeSupport>());
                }
                if (b.TryGetComp<CompShipHeat>() != null)
                    ThreatRaw -= b.TryGetComp<CompShipHeat>().Props.threat;
                else if (b.def == ThingDef.Named("ShipSpinalAmplifier"))
                    ThreatRaw -= 5;
                Mass -= b.def.Size.x * b.def.Size.z * 3;
            }
        }
    }
}