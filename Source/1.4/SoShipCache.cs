using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using SaveOurShip2;
using RimWorld.Planet;
using UnityEngine;

namespace RimWorld
{
    public class SoShipCache
    {
        //td no function on ship parts if no bridge (tur,eng,sen)
        //functionality:
        //post load or after ship spawn: rebuild cache
        //shipmove: if moved vec - offset/rot area, if diff map rem from curr, add to target

        //on shipPart added
        //possible merge: ooc - add all parts to this ship / ic - same but shrink destroyedarea

        //on shipPart removed
        //possible split: ooc - check for bridge, ic - detach, ooc - RebuildCache for each dcon bridge, -1 to rest

        //other buildings: +-for count, mass if on shipPart

        public HashSet<IntVec3> Area = new HashSet<IntVec3>(); //shipParts add to area, removed in despawn, detach
        public HashSet<IntVec3> AreaDestroyed = new HashSet<IntVec3>(); //add to when destroyed in combat
        public HashSet<Building> Parts = new HashSet<Building>(); //shipParts only
        public HashSet<Building> Buildings = new HashSet<Building>(); //all on ship parts, even partially
        //for rebuild after battle, reset before combat, rebuild gizmo
        public HashSet<Tuple<BuildableDef,IntVec3,Rot4>> BuildingsDestroyed = new HashSet<Tuple<BuildableDef, IntVec3, Rot4>>();
        public HashSet<Building> BuildingsNonRot = new HashSet<Building>();
        public List<CompEngineTrail> Engines = new List<CompEngineTrail>();
        public List<CompRCSThruster> RCSs = new List<CompRCSThruster>();
        public List<CompShipHeatPurge> HeatPurges = new List<CompShipHeatPurge>();
        public List<CompShipCombatShield> Shields = new List<CompShipCombatShield>();
        public List<CompCryptoLaunchable> Pods = new List<CompCryptoLaunchable>();
        public List<Building_ShipBridge> Bridges = new List<Building_ShipBridge>();
        public List<Building_ShipBridge> AICores = new List<Building_ShipBridge>();
        public HashSet<Building_ShipTurret> Turrets = new HashSet<Building_ShipTurret>();
        public List<Building_ShipAdvSensor> Sensors = new List<Building_ShipAdvSensor>();
        public List<CompHullFoamDistributor> FoamDistributors = new List<CompHullFoamDistributor>();
        public List<CompShipLifeSupport> LifeSupports = new List<CompShipLifeSupport>();
        public int Mass = 0;
        public float MaxTakeoff = 0;
        public int BuildingCount = 0;
        public int BuildingCountAtCombatStart = 0;
        public int ThreatRaw = 0;
        public bool DetachCheck = false;
        private Map map;
        public Map Map
        {
            get { return map; }
            set
            {
                map = value;
                if (map == null)
                    mapComp = null;
                else
                    mapComp = map.GetComponent<ShipHeatMapComp>();
            }
        }
        public ShipHeatMapComp mapComp;
        public bool PathDirty = true;
        public float[] ThreatPerSegment = new[] { 1f, 1f, 1f, 1f };
        public int Threat => ThreatRaw + Mass / 100;
        public bool Rotatable => !BuildingsNonRot.Any();
        public float ThrustRaw = 0;
        public float ThrustRatio => ThrustRaw * 500f / Mass;
        private string name;
        public string Name
        {
            set
            {
                foreach (Building_ShipBridge b in Bridges)
                    b.ShipName = value;
                name = value;
            }
            get
            {
                return name;
            }
        }
        public int Index = -1;
        public Building_ShipBridge Core; //main bridge
        public Faction Faction => Buildings.First().Faction;
        public bool IsWreck => Core == null;
        public bool IsStuck => Core == null || Bridges.All(b => b.TacCon); //ship cant move with taccon
        public Rot4 Rot => Engines.FirstOrDefault().parent.Rotation;
        public bool EnginesCanActivate()
        {
            if (Engines.Any(e => e.CanFire(Rot)))
                return true;
            return false;
        }
        public bool CanMove => !IsStuck && EnginesCanActivate();
        public float EnginesOn()
        {
            if (IsStuck)
                return 0;
            float enginePower = 0;
            foreach (var engine in Engines)
            {
                if (engine.CanFire(Rot))
                {
                    if (engine.On())
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
        public int EnginePower()
        {
            if (IsStuck || Engines.NullOrEmpty())
                return 0;
            int enginePower = 0;
            foreach (var engine in Engines)
            {
                if (engine.CanFire(Rot))
                {
                    enginePower += engine.Props.thrust;
                }
            }
            return enginePower;
        }
        public void MoveAtThrust(int thrust)
        {
            List<CompEngineTrail> list = new List<CompEngineTrail>();
            list = Engines.Where(e => e.CanFire(Rot)).OrderByDescending(e => e.Props.energy).ThenBy(e => e.Thrust).ToList();
            int i = 0;
            foreach (CompEngineTrail engine in list)
            {
                i += engine.Thrust;
                engine.On();
                if (i > thrust)
                    return;
            }
        }
        public void EnginesOff()
        {
            foreach (var engine in Engines)
            {
                engine.Off();
            }
        }
        public void Capture(Faction fac)
        {
            foreach (Building building in Buildings)
            {
                if (building.def.CanHaveFaction)
                    building.SetFaction(fac);
            }
        }
        //shipmove
        public bool CanShipMove(float needFuel)
        {
            if (!IsStuck && RCSs.Any())
            {
                float fuel = 0;
                foreach (var engine in Engines.Where(e => !e.Props.reactionless))
                {
                    fuel += engine.refuelComp.Fuel;
                }
                if (fuel > needFuel)
                    return true;
            }
            return false;
        }
        public bool HasPilotRCSAndFuel(float fuelPercentNeeded, bool RCS = false)
        {
            float fuelNeeded = Mass;
            Log.Message("Mass: " + Mass + " fuel req: " + fuelNeeded * fuelPercentNeeded + " RCS: " + RCSs.Count);
            if (RCS && RCSs.Count * 2000 < fuelNeeded) //2k weight/RCS to move
            {
                Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipInsideMoveFailRCS", 1 + (fuelNeeded / 2000)), Core, MessageTypeDefOf.NeutralEvent);
                return false;
            }
            fuelNeeded *= fuelPercentNeeded;
            if (TakeoffCurrent() < fuelNeeded)
            {
                Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipInsideMoveFailFuel", fuelNeeded), Core, MessageTypeDefOf.NeutralEvent);
                return false;
            }
            if (!HasMannedBridge())
            {
                Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipInsideMoveFailPilot"), Core, MessageTypeDefOf.NeutralEvent);
                return false;
            }
            return true;
        }
        public float TakeoffCurrent()
        {
            float fuelHad = 0f;
            foreach (CompEngineTrail engine in Engines.Where(e => e.Props.takeOff))
            {
                fuelHad += engine.refuelComp.Fuel;
                if (engine.refuelComp.Props.fuelFilter.AllowedThingDefs.Contains(ResourceBank.ThingDefOf.ShuttleFuelPods))
                    fuelHad += engine.refuelComp.Fuel;
            }
            return fuelHad;
        }
        public bool HasMannedBridge()
        {
            bool hasPilot = false;
            foreach (Building_ShipBridge bridge in Bridges) //first bridge with pilot/AI
            {
                if (!hasPilot && bridge.powerComp.PowerOn)
                {
                    if (bridge.mannableComp == null || (bridge.mannableComp != null && bridge.mannableComp.MannedNow))
                    {
                        hasPilot = true;
                        return true;
                    }
                }
            }
            return false;
        }
        public IntVec3 LowestCorner(byte rotb, Map map)
        {
            IntVec3 lowestCorner = new IntVec3(int.MaxValue, 0, int.MaxValue);
            foreach (IntVec3 v in Area)
            {
                if (v.x < lowestCorner.x)
                    lowestCorner.x = v.x;
                if (v.z < lowestCorner.z)
                    lowestCorner.z = v.z;
            }
            if (rotb == 1)
            {
                int temp = lowestCorner.x;
                lowestCorner.x = map.Size.z - lowestCorner.z;
                lowestCorner.z = temp;
            }
            else if (rotb == 2)
            {
                lowestCorner.x = map.Size.x - lowestCorner.x;
                lowestCorner.z = map.Size.z - lowestCorner.z;
            }
            return lowestCorner;
        }
        public IEnumerable<Building> OuterNonShipWalls()
        {
            foreach (Building b in Buildings.Where(b => !Parts.Contains(b) && b.def.passability == Traversability.Impassable && (b.def.Size.x == 1 || b.def.Size.z == 1)))
            {
                bool air = false;
                bool vac = false;
                foreach (IntVec3 v in GenAdj.CellsAdjacentCardinal(b.Position, Rot4.North, new IntVec2(1, 1)))
                {
                    Room room = v.GetRoom(Map);
                    if (room == null)
                        continue;
                    if (room.TouchesMapEdge)
                        vac = true;
                    else if (room.ProperRoom && room.OpenRoofCount == 0)
                        air = true;

                    if (air && vac)
                    {
                        yield return b;
                        break;
                    }
                }
            }
        }
        public HashSet<IntVec3> OuterCells()
        {
            HashSet<IntVec3> cells = new HashSet<IntVec3>();
            foreach (IntVec3 vec in Area)
            {
                foreach (IntVec3 v in GenAdj.CellsAdjacentCardinal(vec, Rot4.North, new IntVec2(1, 1)).Where(v => !Area.Contains(v)))
                {
                    Room room = v.GetRoom(Map);
                    if (room != null && room.TouchesMapEdge)
                        cells.Add(vec);
                }
            }
            return cells;
        }
        public Sketch GenerateShipSketch(Map targetMap, IntVec3 lowestCorner, byte rotb = 0)
        {
            Sketch sketch = new Sketch();
            IntVec3 rot = new IntVec3(0, 0, 0);
            foreach (IntVec3 pos in Area)
            {
                if (rotb == 1)
                {
                    rot.x = targetMap.Size.x - pos.z;
                    rot.z = pos.x;
                    sketch.AddThing(ThingDef.Named("Ship_FakeBeam"), rot - lowestCorner, Rot4.North);
                }
                else if (rotb == 2)
                {
                    rot.x = targetMap.Size.x - pos.x;
                    rot.z = targetMap.Size.z - pos.z;
                    sketch.AddThing(ThingDef.Named("Ship_FakeBeam"), rot - lowestCorner, Rot4.North);
                }
                else
                    sketch.AddThing(ThingDef.Named("Ship_FakeBeam"), pos - lowestCorner, Rot4.North);
            }
            return sketch;
        }
        public void MoveShipSketch(Map targetMap, byte rotb = 0, bool salvage = false, int bMax = 0, bool includeRock = false)
        {
            if (salvage && !IsStuck)
            {
                Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipSalvageBridge"), MessageTypeDefOf.NeutralEvent);
                return;
            }

            float bCountF = BuildingCount * 2.5f;
            if (salvage && bCountF > bMax)
            {
                Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipSalvageCount", (int)bCountF, bMax), MessageTypeDefOf.NeutralEvent);
            }
            //td add rock terrain and walls
            IntVec3 lowestCorner = LowestCorner(rotb, Map);
            Sketch shipSketch = GenerateShipSketch(targetMap, lowestCorner, rotb);
            MinifiedThingShipMove fakeMover = (MinifiedThingShipMove)new ShipMoveBlueprint(shipSketch).TryMakeMinified();
            if (IsWreck)
                fakeMover.shipRoot = Buildings.First();
            else
                fakeMover.shipRoot = Core;
            fakeMover.includeRock = includeRock;
            fakeMover.shipRotNum = rotb;
            fakeMover.bottomLeftPos = lowestCorner;
            ShipInteriorMod2.shipOriginMap = Map;
            fakeMover.targetMap = targetMap;
            fakeMover.Position = fakeMover.shipRoot.Position;
            fakeMover.SpawnSetup(targetMap, false);
            List<object> selected = new List<object>();
            foreach (object ob in Find.Selector.SelectedObjects)
                selected.Add(ob);
            foreach (object ob in selected)
                Find.Selector.Deselect(ob);
            Current.Game.CurrentMap = targetMap;
            Find.Selector.Select(fakeMover);
            if (Find.TickManager.Paused)
                Find.TickManager.TogglePaused();
            InstallationDesignatorDatabase.DesignatorFor(ThingDef.Named("ShipMoveBlueprint")).ProcessInput(null);
        }
        //dep
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
        public void AllOff()
        {
            EnginesOff();
            var heatnNet = Core.TryGetComp<CompShipHeat>().myNet;
            heatnNet.ShieldsOff();
            heatnNet.TurretsOff();
        }
        public float[] ActualThreatPerSegment() //dep turrets that cant actually fire due to ammo
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
        //AI
        public void PurgeCheck()
        {
            if (!HeatPurges.Any(purge => purge.purging)) //heatpurge - only toggle when not purging
            {
                var myNet = Core.heatComp.myNet;
                if (HeatPurges.Any(purge => purge.fuelComp.FuelPercentOfMax > 0.2f) && Core != null && myNet != null && myNet.RatioInNetwork > 0.7f) //start purge
                {
                    foreach (CompShipHeatPurge purge in HeatPurges)
                    {
                        purge.StartPurge();
                    }
                }
                else
                {
                    if (Shields.Any(shield => shield.shutDown)) //repower shields
                    {
                        foreach (var shield in Shields)
                        {
                            if (shield.flickComp == null)
                                continue;
                            shield.flickComp.SwitchIsOn = true;
                        }
                    }
                    if (myNet.RatioInNetwork > 0.85f && !myNet.venting)
                        myNet.StartVent();
                }
            }
        }
        //cache
        public void RebuildCache(Building origin, HashSet<IntVec3> exclude = null) //full rebuild, on load, merge
        {
            if (origin == null || origin.Destroyed)
            {
                Log.Error("SOS2 ship recache: tried merging to null or destroyed origin");
                return;
            }
            Map = origin.Map;
            Index = origin.thingIDNumber;
            int path = -1;
            if (origin is Building_ShipBridge core)
            {
                Core = core;
                name = core.ShipName;
                Core.ShipIndex = Index;
                path = 0;
            }

            HashSet<IntVec3> cellsTodo = new HashSet<IntVec3>();
            HashSet<IntVec3> cellsDone = new HashSet<IntVec3>();
            if (exclude != null)
                cellsDone.AddRange(exclude);
            cellsTodo.AddRange(origin.OccupiedRect());

            //find cells cardinal to all prev.pos, exclude prev.pos, if found part, set corePath to i, shipIndex to core.shipIndex, set corePath
            while (cellsTodo.Count > 0)
            {
                List<IntVec3> current = cellsTodo.ToList();
                foreach (IntVec3 vec in current) //do all of the current corePath
                {
                    mapComp.MapShipCells[vec] = new Tuple<int, int>(Index, path); //add new vec, index, corepath
                    foreach (Thing t in vec.GetThingList(Map))
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
                    cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(vec, Rot4.North, new IntVec2(1, 1)).Where(v => !cellsDone.Contains(v) && mapComp.MapShipCells.ContainsKey(v)));
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
            Log.Message("Core: " + Core);
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
                                if (refuelable.Props.fuelFilter.AllowedThingDefs.Contains(ResourceBank.ThingDefOf.ShuttleFuelPods))
                                    MaxTakeoff += refuelable.Props.fuelCapacity;
                            }
                            Engines.Add(b.TryGetComp<CompEngineTrail>());
                        }
                        else if (b.TryGetComp<CompRCSThruster>() != null)
                            RCSs.Add(b.GetComp<CompRCSThruster>());
                    }
                    else
                    {
                        if (b.TryGetComp<CompCryptoLaunchable>() != null)
                            Pods.Add(b.GetComp<CompCryptoLaunchable>());
                        else if (b is Building_ShipBridge bridge)
                        {
                            Bridges.Add(bridge);
                            if (b.TryGetComp<CompBuildingConsciousness>() != null)
                                AICores.Add(bridge);
                            if (IsWreck) //bridge placed on wreck, repath
                            {
                                Core = bridge;
                                if (name == null)
                                    name = bridge.ShipName;
                                RebuildCorePath();
                            }
                            bridge.ShipIndex = Index;
                            bridge.ShipName = Name;
                        }
                        else if (b is Building_ShipAdvSensor sensor)
                            Sensors.Add(sensor);
                        else if (b.TryGetComp<CompHullFoamDistributor>() != null)
                            FoamDistributors.Add(b.GetComp<CompHullFoamDistributor>());
                        else if (b.TryGetComp<CompShipLifeSupport>() != null)
                            LifeSupports.Add(b.GetComp<CompShipLifeSupport>());
                    }
                }
                var heatComp = b.TryGetComp<CompShipHeat>();
                if (heatComp != null)
                {
                    ThreatRaw += heatComp.Props.threat;
                    if (b is Building_ShipTurret turret)
                    {
                        Turrets.Add(turret);
                        int threat = heatComp.Props.threat;
                        if (heatComp.Props.maxRange > 150) //long
                        {
                            ThreatPerSegment[0] += threat / 6f;
                            ThreatPerSegment[1] += threat / 4f;
                            ThreatPerSegment[2] += threat / 2f;
                            ThreatPerSegment[3] += threat;
                        }
                        else if (heatComp.Props.maxRange > 100) //med
                        {
                            ThreatPerSegment[0] += threat / 4f;
                            ThreatPerSegment[1] += threat / 2f;
                            ThreatPerSegment[2] += threat;
                        }
                        else if (heatComp.Props.maxRange > 50) //short
                        {
                            ThreatPerSegment[0] += threat / 2f;
                            ThreatPerSegment[1] += threat;
                        }
                        else //cqc
                            ThreatPerSegment[0] += threat;
                    }
                    else if (heatComp is CompShipHeatPurge purge)
                    {
                        HeatPurges.Add(purge);
                    }
                    else if (heatComp is CompShipCombatShield shield)
                        Shields.Add(shield);
                }
                else if (b.def == ThingDef.Named("ShipSpinalAmplifier"))
                    ThreatRaw += 5;
                Mass += b.def.Size.x * b.def.Size.z * 3;
            }
        }
        public void RemoveFromCache(Building b, DestroyMode mode)
        {
            if (Buildings.Remove(b))
            {
                BuildingCount--;
                if (mapComp.InCombat && !IsWreck && b.def.blueprintDef != null && (mode == DestroyMode.KillFinalize || mode == DestroyMode.KillFinalizeLeavingsOnly))
                {
                    BuildingsDestroyed.Add(new Tuple<BuildableDef, IntVec3, Rot4>(b.def, b.Position, b.Rotation));
                }
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
                                if (refuelable.Props.fuelFilter.AllowedThingDefs.Contains(ResourceBank.ThingDefOf.ShuttleFuelPods))
                                    MaxTakeoff -= refuelable.Props.fuelCapacity;
                            }
                            Engines.Remove(b.TryGetComp<CompEngineTrail>());
                        }
                        else if (b.TryGetComp<CompRCSThruster>() != null)
                            RCSs.Remove(b.GetComp<CompRCSThruster>());
                    }
                    else
                    {
                        if (b.TryGetComp<CompCryptoLaunchable>() != null)
                            Pods.Remove(b.GetComp<CompCryptoLaunchable>());
                        else if (b is Building_ShipBridge bridge)
                        {
                            Bridges.Remove(bridge);
                            if (b.TryGetComp<CompBuildingConsciousness>() != null)
                                AICores.Remove(bridge);
                            if (bridge == Core)
                                ReplaceCoreOrWreck();
                            //bridge.ShipIndex = -1;
                            //bridge.ShipName = "destroyed ship";
                        }
                        else if (b is Building_ShipAdvSensor sensor)
                            Sensors.Remove(sensor);
                        else if (b.TryGetComp<CompHullFoamDistributor>() != null)
                            FoamDistributors.Remove(b.GetComp<CompHullFoamDistributor>());
                        else if (b.TryGetComp<CompShipLifeSupport>() != null)
                            LifeSupports.Remove(b.GetComp<CompShipLifeSupport>());
                    }
                }
                var heatComp = b.TryGetComp<CompShipHeat>();
                if (heatComp != null)
                {
                    ThreatRaw -= heatComp.Props.threat;
                    if (b is Building_ShipTurret turret)
                    {
                        Turrets.Remove(turret);
                        int threat = heatComp.Props.threat;
                        if (heatComp.Props.maxRange > 150) //long
                        {
                            ThreatPerSegment[0] -= threat / 6f;
                            ThreatPerSegment[1] -= threat / 4f;
                            ThreatPerSegment[2] -= threat / 2f;
                            ThreatPerSegment[3] -= threat;
                        }
                        else if (heatComp.Props.maxRange > 100) //med
                        {
                            ThreatPerSegment[0] -= threat / 4f;
                            ThreatPerSegment[1] -= threat / 2f;
                            ThreatPerSegment[2] -= threat;
                        }
                        else if (heatComp.Props.maxRange > 50) //short
                        {
                            ThreatPerSegment[0] -= threat / 2f;
                            ThreatPerSegment[1] -= threat;
                        }
                        else //cqc
                            ThreatPerSegment[0] -= threat;
                    }
                    else if (heatComp is CompShipHeatPurge purge)
                    {
                        HeatPurges.Remove(purge);
                    }
                    else if (heatComp is CompShipCombatShield shield)
                        Shields.Remove(shield);
                }
                else if (b.def == ThingDef.Named("ShipSpinalAmplifier"))
                    ThreatRaw -= 5;
                Mass -= b.def.Size.x * b.def.Size.z * 3;
            }
        }
        public bool ReplaceCoreOrWreck() //before despawn try find replacer for core
        {
            if (Bridges.NullOrEmpty()) //core died, no replacements
            {
                Log.Message("SOS2c: ship wrecked: " + Index);
                Core = null;
                ResetCorePath();
                if (mapComp.InCombat) //if last ship end combat else move to grave
                {
                    if (mapComp.ShipsOnMapNew.Count > 1)
                        mapComp.DestroyedIncombat.Add(Index, null);
                    else
                        mapComp.EndBattle(Map, false);
                }
                return true;
            }
            var replacer = Bridges.FirstOrDefault(b => b != Core && !b.Destroyed);
            if (replacer == null) //hull under core died, no replacer exists
            {
                return false;
            }
            //core died but replacer exists
            Log.Message("SOS2c: replaced primary bridge on ship: " + Index);
            Core = replacer;
            RebuildCorePath();
            return true;
        }
        public void ResetCorePath()
        {
            foreach (IntVec3 vec in Area)
            {
                mapComp.MapShipCells[vec] = new Tuple<int, int>(Index, -1);
            }
        }
        public void RebuildCorePath() //run before combat if PathDirty and in combat after bridge replaced
        {
            var cellsTodo = new HashSet<IntVec3>();
            var cellsDone = new HashSet<IntVec3>();
            cellsTodo.AddRange(Core.OccupiedRect());
            int mergeToIndex = mapComp.MapShipCells[Core.Position].Item1;

            //find parts cardinal to all prev.pos, exclude prev.pos
            int path = 0;
            while (cellsTodo.Count > 0)
            {
                List<IntVec3> current = cellsTodo.ToList();
                foreach (IntVec3 vec in current) //do all of the current corePath
                {
                    mapComp.MapShipCells[vec] = new Tuple<int, int>(mergeToIndex, path);
                    cellsTodo.Remove(vec);
                    cellsDone.Add(vec);
                }
                foreach (IntVec3 vec in current) //find next set cardinal to all cellsDone, exclude cellsDone
                {
                    cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(vec, Rot4.North, new IntVec2(1, 1)).Where(v => !cellsDone.Contains(v) && mapComp.MapShipCells.ContainsKey(v)));
                }
                path++;
            }
            PathDirty = false;
            Log.Message("SOS2c: RebuildCorePath Rebuilt corePath for ship: " + Index +" at " + Core.Position);
        }
        //detach
        public void Tick()
        {
            if (DetachCheck)
            {
                CheckForDetach();
                DetachCheck = false;
            }
        }
        public bool CheckForDetach() //finds all shipcells around detached and for each tries to path back to first with lower index, if not possible, detaches all in set
        {
            if (AreaDestroyed.Any())
            {
                HashSet<IntVec3> initialCells = new HashSet<IntVec3>(); //cells to start checks from
                HashSet<IntVec3> cellsDone = new HashSet<IntVec3>(); //cells that were checked
                HashSet<IntVec3> cellsAttached = new HashSet<IntVec3>(); //cells that were checked and are attached
                foreach (IntVec3 vec in AreaDestroyed)
                {
                    foreach (IntVec3 v in GenAdj.CellsAdjacentCardinal(vec, Rot4.North, new IntVec2(1, 1)).Where(v => !AreaDestroyed.Contains(v) && Area.Contains(v) &&  mapComp.MapShipCells[v].Item2 != 0))
                    {
                        initialCells.Add(v);
                    }
                }
                /*if (AreaDestroyed.Count > 1)
                    Log.Warning("SOS2c: CheckForDetach for " + AreaDestroyed.Count + " destroyed cells. Cells to check for detach: " + initialCells.Count);
                else
                    Log.Message("SOS2c: CheckForDetach for " + AreaDestroyed.FirstOrDefault() + " Cells to check for detach: " + initialCells.Count);¸*/
                foreach (IntVec3 setStartCell in initialCells)
                {
                    if (cellsDone.Contains(setStartCell)) //skip already checked cells
                    {
                        continue;
                    }
                    bool detach = true;
                    HashSet<IntVec3> cellsToDetach = new HashSet<IntVec3>();
                    List<IntVec3> cellsTodo = new List<IntVec3>{setStartCell};
                    while (cellsTodo.Any())
                    {
                        IntVec3 current = cellsTodo.First();
                        cellsTodo.Remove(current);
                        cellsDone.Add(current);
                        if (mapComp.MapShipCells[current].Item2 < mapComp.MapShipCells[setStartCell].Item2)
                        {
                            cellsAttached.AddRange(cellsDone);
                            detach = false;
                            //foreach (IntVec3 vec in cellsToDetach)
                            //    Log.Warning("" + vec);
                            //Log.Message("SOS2c: CheckForDetach still attached at " + current);
                            break; //if part with lower corePath found this set is attached
                        }
                        if (cellsToDetach.Add(current)) //add current tile && extend search range, skip non ship, destroyed tiles
                        {
                            HashSet<IntVec3> temp = GenAdj.CellsAdjacentCardinal(current, Rot4.North, new IntVec2(1, 1)).ToHashSet();
                            foreach (IntVec3 v in temp)
                            {
                                if (cellsAttached.Contains(v)) //if next to an already attached and checked
                                {
                                    detach = false;
                                    //foreach (IntVec3 vec in cellsToDetach)
                                    //    Log.Warning("" + vec);
                                    //Log.Message("SOS2c: CheckForDetach still attached to already checked " + current);
                                    break; //if part with lower corePath found this set is attached
                                }
                                if (!cellsDone.Contains(v) && Area.Contains(v) && !AreaDestroyed.Contains(v))
                                {
                                    cellsTodo.Add(v);
                                }

                            }
                            if (!detach)
                                break;
                            //cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(current, Rot4.North, new IntVec2(1, 1)).Where(v => !cellsDone.Contains(v) && Area.Contains(v) && !AreaDestroyed.Contains(v)));
                        }
                    }
                    if (detach)
                    {
                        string str = "SOS2c: CheckForDetach on ship " + Index + ", Detach " + cellsToDetach.Count + " cells: ";
                        foreach (IntVec3 vec in cellsToDetach)
                            str += vec;
                        Log.Warning(str);
                        
                        Detach(cellsToDetach);
                    }
                }
                AreaDestroyed.Clear();

                /*foreach (int i in mapComp.ShipsOnMapNew.Keys)
                {
                    Log.Message("SOS2c: area on ship: " + i + " is " + mapComp.ShipsOnMapNew[i].AreaDestroyed.Count + "/" + mapComp.ShipsOnMapNew[i].Area.Count);
                }*/
                return true;
            }
            return false;
        }
        public void Detach(HashSet<IntVec3> detachArea) //if bridge found detach as new ship else as wreck
        {
            Building newCore = null;
            DestroyMode mode = DestroyMode.Vanish;
            if (mapComp.InCombat && newCore == null)
                mode = DestroyMode.KillFinalize;
            foreach (IntVec3 vec in detachArea) //clean buildings, try to find bridge on detached
            {
                foreach (Thing t in vec.GetThingList(Map))
                {
                    if (t is Building b)
                    {
                        if (b is Building_ShipBridge bridge)
                            newCore = bridge;
                        RemoveFromCache(b, mode);
                    }
                }
            }
            foreach (IntVec3 vec in detachArea) //clean area indexes
            {
                Area.Remove(vec);
                mapComp.MapShipCells[vec] = new Tuple<int, int>(-1, -1);
            }
            if (newCore == null) //wreck
            {
                if (mapComp.InCombat) //float wreck in battle and destroy it
                {
                    FloatAndDestroy(detachArea);
                    return;
                }
                newCore = (Building)detachArea.First().GetThingList(Map).First(t => t.TryGetComp<CompSoShipPart>() != null);
            }
            Log.Message("SOS2c: Detach new ship/wreck with: " + newCore);
            if (mapComp.ShipsOnMapNew.ContainsKey(newCore.thingIDNumber))
            {
                Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
                int newKey = mapComp.ShipsOnMapNew.Keys.Max() + 1;
                Log.Error("SOS2c: Detach error, shipID " + newCore.thingIDNumber + " already exits! Using fallback index: " + newKey + ", pausing game. Recheck ship area with infestation overlay. If not correct - save and reload!");
                newCore.thingIDNumber = newKey;
            }
            mapComp.ShipsOnMapNew.Add(newCore.thingIDNumber, new SoShipCache());
            mapComp.ShipsOnMapNew[newCore.thingIDNumber].RebuildCache(newCore, AreaDestroyed);
            if (mapComp.InCombat)
            {
                mapComp.DestroyedIncombat.Add(newCore.thingIDNumber, null);
            }
            if (Map == mapComp.ShipCombatOriginMap)
                mapComp.hasAnyPlayerPartDetached = true;
            return;
        }
        private void FloatAndDestroy(HashSet<IntVec3> detachArea)
        {
            //float wreck and destroy
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
                foreach (Thing t in vec.GetThingList(Map).Where(t => t.def.destroyable && !t.Destroyed))
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
                part.SpawnSetup(Map, false);
            }
            foreach (Pawn p in toKill)
            {
                p.Kill(new DamageInfo(DamageDefOf.Bomb, 100f));
            }
            foreach (Thing t in toDestroy)
            {
                if (t.def.destroyable && !t.Destroyed)
                {
                    try
                    {
                        t.Destroy(DestroyMode.Vanish);
                    }
                    catch (Exception e)
                    {
                        var sb = new StringBuilder();
                        sb.AppendFormat("Wrecking Error on {0}: {1}\n", t.def.label, e.Message);
                        sb.AppendLine(e.StackTrace);
                        Log.Warning(sb.ToString());
                    }
                }
            }
            foreach (IntVec3 c in detachArea)
            {
                Map.terrainGrid.RemoveTopLayer(c, false);
                Map.roofGrid.SetRoof(c, null);
            }
            ShipInteriorMod2.AirlockBugFlag = false;
        }
    }
}