using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using SaveOurShip2;
using RimWorld.Planet;
using UnityEngine;
using Verse.AI.Group;
using RimworldMod;
using System.Reflection;
using Verse.Noise;

namespace RimWorld
{
    public class ShipHeatMapComp : MapComponent
    {
        public List<ShipHeatNet> cachedNets = new List<ShipHeatNet>();
        public List<CompShipHeat> cachedPipes = new List<CompShipHeat>();

        public int[] grid;
        public bool heatGridDirty;
        public bool loaded = false;

        public ShipHeatMapComp(Map map) : base(map)
        {
            grid = new int[map.cellIndices.NumGridCells];
            heatGridDirty = true;
            AccessExtensions.Utility.shipHeatMapCompCache.Add(this);
        }
        public override void MapRemoved()
        {
            AccessExtensions.Utility.shipHeatMapCompCache.Remove(this);
            base.MapRemoved();
        }
        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            if (!heatGridDirty || (Find.TickManager.TicksGame % 60 != 0 && loaded))
            {
                return;
            }
            //temp save heat to sinks
            //Log.Message("Recaching all heatnets");
            foreach (ShipHeatNet net in cachedNets)
            {
                foreach (CompShipHeatSink sink in net.Sinks)
                {
                    sink.heatStored = sink.Props.heatCapacity * sink.myNet.RatioInNetworkRaw;
                    sink.depletion = sink.Props.heatCapacity * sink.myNet.DepletionRatio;
                }
            }
            //rebuild all nets on map
            List<ShipHeatNet> list = new List<ShipHeatNet>();
            for (int i = 0; i < grid.Length; i++)
                grid[i] = -1;
            int gridID = 0;
            foreach (CompShipHeat comp in cachedPipes)
            {
                if (comp.parent.Map == null || grid[comp.parent.Map.cellIndices.CellToIndex(comp.parent.Position)] > -1)
                    continue;
                ShipHeatNet net = new ShipHeatNet();
                net.GridID = gridID;
                gridID++;
                HashSet<CompShipHeat> batch = new HashSet<CompShipHeat>();
                batch.Add(comp);
                AccumulateToNetNew(batch, net);
                list.Add(net);
            }
            cachedNets = list;

            base.map.mapDrawer.WholeMapChanged(MapMeshFlag.Buildings);
            base.map.mapDrawer.WholeMapChanged(MapMeshFlag.Things);
            heatGridDirty = false;
            loaded = true;
        }
        void AccumulateToNetNew(HashSet<CompShipHeat> compBatch, ShipHeatNet net)
        {
            HashSet<CompShipHeat> newBatch = new HashSet<CompShipHeat>();
            foreach (CompShipHeat comp in compBatch)
            {
                if (comp.parent == null || !comp.parent.Spawned)
                    continue;
                comp.myNet = net;
                net.Register(comp);
                foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(comp.parent))
                {
                    grid[comp.parent.Map.cellIndices.CellToIndex(cell)] = net.GridID;
                }
                foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(comp.parent))
                {
                    if (grid[comp.parent.Map.cellIndices.CellToIndex(cell)] == -1)
                    {
                        foreach (Thing t in cell.GetThingList(comp.parent.Map))
                        {
                            if (t is Building b)
                            {
                                CompShipHeat heat = b.TryGetComp<CompShipHeat>();
                                if (heat != null)
                                    newBatch.Add(heat);
                            }
                        }
                    }
                }
            }
            if (newBatch.Any())
                AccumulateToNetNew(newBatch, net);
        }

        /*void AccumulateToNet(CompShipHeat comp, ShipHeatNet net, ref List<CompShipHeat> used)
        {
            used.Add(comp);
            comp.myNet = net;
            net.Register(comp);
            if (comp.parent == null || !comp.parent.Spawned)
                return;
            foreach (IntVec3 cell in GenAdj.CellsOccupiedBy(comp.parent))
            {
                grid[comp.parent.Map.cellIndices.CellToIndex(cell)] = net.GridID;
            }
            foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(comp.parent))
            {
                foreach(Thing t in cell.GetThingList(comp.parent.Map))
                {
                    if(t is ThingWithComps && ((ThingWithComps)t).TryGetComp<CompShipHeat>()!=null && !used.Contains(((ThingWithComps)t).TryGetComp<CompShipHeat>()))
                    {
                        AccumulateToNet(((ThingWithComps)t).TryGetComp<CompShipHeat>(), net, ref used);
                    }
                }
            }
        }*/
        //SC
        readonly float[] minRange = new[] { 0f, 60f, 110f, 160f };
        readonly float[] maxRange = new[] { 40f, 90f, 140f, 190f };
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Faction>(ref ShipFaction, "ShipFaction");
            Scribe_References.Look<Lord>(ref ShipLord, "ShipLord");
            Scribe_References.Look<Lord>(ref InvaderLord, "InvaderLord");
            Scribe_References.Look<Map>(ref NextTargetMap, "NextTargetMap");
            Scribe_References.Look<Map>(ref ShipGraveyard, "ShipCombatGraveyard");
            Scribe_References.Look<Map>(ref GraveOrigin, "GraveOrigin");
            Scribe_Values.Look<bool>(ref IsGraveyard, "IsGraveyard", false);
            Scribe_Values.Look<bool>(ref InCombat, "InCombat", false);
            Scribe_Values.Look<bool>(ref BurnUpSet, "BurnUpSet", false);
            Scribe_Values.Look<bool>(ref ToggleEngines, "ToggleEngines", false);
            Scribe_Values.Look<int>(ref BurnTimer, "BurnTimer");
            Scribe_Values.Look<int>(ref LastAttackTick, "LastShipBattleTick", 0);
            Scribe_Values.Look<int>(ref LastBountyRaidTick, "LastBountyRaidTicks", 0);
            Scribe_Collections.Look<Building_ShipAirlock>(ref Docked, "Docked", LookMode.Reference);
            if (InCombat)
            {
                //SC only - both maps
                targetMapComp = null;
                Scribe_Values.Look<int>(ref Heading, "Heading");
                Scribe_Values.Look<bool>(ref Scanned, "Scanned");
                Scribe_Values.Look<int>(ref BuildingCountAtStart, "BuildingCountAtStart");
                Scribe_Values.Look<bool>(ref Maintain, "Maintain");
                Scribe_Values.Look<float>(ref RangeToKeep, "RangeToKeep");
                Scribe_Collections.Look<ShipCombatProjectile>(ref Projectiles, "ShipProjectiles");
                Scribe_Collections.Look<ShipCombatProjectile>(ref TorpsInRange, "ShipTorpsInRange");
                Scribe_References.Look<Map>(ref ShipCombatOriginMap, "ShipCombatOriginMap");
                Scribe_References.Look<Map>(ref ShipCombatTargetMap, "ShipCombatTargetMap");

                //SC only - origin only
                originMapComp = null;
                Scribe_Values.Look<float>(ref Range, "Range");
                Scribe_Values.Look<bool>(ref attackedTradeship, "attackedTradeship");
                Scribe_Values.Look<bool>(ref callSlowTick, "callSlowTick");

                //SC only - target only
                Scribe_Values.Look<bool>(ref HasShipMapAI, "HasShipMapAI");
                Scribe_Values.Look<int>(ref BattleStartTick, "BattleStartTick");
                Scribe_Values.Look<bool>(ref Retreating, "Retreating");
                Scribe_Values.Look<bool>(ref warnedAboutRetreat, "warnedAboutRetreat");
                Scribe_Values.Look<int>(ref warnedAboutAdrift, "warnedAboutAdrift");
                Scribe_Values.Look<bool>(ref hasAnyPartDetached, "hasAnyPartDetached");
                Scribe_Values.Look<bool>(ref startedBoarderLoad, "StartedBoarding");
                Scribe_Values.Look<bool>(ref launchedBoarders, "LaunchedBoarders");

                Scribe_Collections.Look<Building_ShipBridge>(ref MapRootListAll, "MapRootListAll", LookMode.Reference); //td rem?
            }
        }
        //SC only - both maps
        public Faction ShipFaction;
        public bool Scanned = false; //target map has been fully scanned - prevent further scanner checks
        public int lastPDTick = 0; //mapwide PD tick delay
        public int BuildingCountAtStart = 0; //AI retreat param
        public int BuildingsCount;
        public int Heading; //+closer, -apart
        public float MapEnginePower;
        public bool Maintain = false; //map will try to maintain RangeToKeep
        public float RangeToKeep;
        public float totalThreat = 1;
        public float[] threatPerSegment = { 1, 1, 1, 1 };
        public HashSet<int> ShipsToMove = new HashSet<int>(); //move to grave or end combat
        public List<int> ShipsToRemove = new List<int>(); //remove from cache on tick
        public List<ShipCombatProjectile> Projectiles;
        public List<ShipCombatProjectile> TorpsInRange;
        public Map ShipCombatOriginMap; //"player" map - initializes combat vars, runs all non duplicate code, AI
        private ShipHeatMapComp originMapComp;
        public ShipHeatMapComp OriginMapComp
        {
            get
            {
                if (ShipCombatOriginMap == null)
                    return null;
                if (originMapComp == null)
                {
                    originMapComp = ShipCombatOriginMap.GetComponent<ShipHeatMapComp>();
                }
                return originMapComp;
            }
        }
        public Map ShipCombatTargetMap; //target map - if there is one, we are in combat, for proj, etc.
        private ShipHeatMapComp targetMapComp;
        public ShipHeatMapComp TargetMapComp
        {
            get
            {
                if (ShipCombatTargetMap == null)
                    return null;
                if (targetMapComp == null)
                {
                    targetMapComp = ShipCombatTargetMap.GetComponent<ShipHeatMapComp>();
                }
                return targetMapComp;
            }
        }

        //SC only - origin only
        public float Range; //400 is furthest away, 0 is up close and personal
        public bool attackedTradeship; //target was AI tradeship - notoriety gain
        public bool callSlowTick = false; //call both slow ticks
        public int LastAttackTick;
        public int LastBountyRaidTick;
        private bool shipCombatOrigin = false;
        public bool ShipCombatOrigin //reset after battle
        {
            get
            {
                shipCombatOrigin = map == ShipCombatOriginMap;
                return shipCombatOrigin;
            }
            set { shipCombatOrigin = value; }
        }

        //SC only - target only
        public bool HasShipMapAI = false; //target has ship map AI
        //public Thing TurretTarget; //AI target for turrets
        public int BattleStartTick = 0; //AI retreat param, stalemate eject
        public bool Retreating = false; //AI is retreating
        public bool warnedAboutRetreat = false; //AI warned player
        public int warnedAboutAdrift = 0; //tick player was warned AI is adrift
        public bool hasAnyPartDetached = false; //AI is loosing ship parts, will load shuttles //td rework
        public bool startedBoarderLoad = false; //AI started loading
        public bool launchedBoarders = false; //AI launched
        void ResetShipAI()
        {
            HasShipMapAI = true;
            //TurretTarget = TargetMapComp.MapRootListAll.RandomElement();
            BattleStartTick = Find.TickManager.TicksGame;
            Retreating = false;
            warnedAboutRetreat = false;
            warnedAboutAdrift = 0;
            hasAnyPartDetached = false;
            startedBoarderLoad = false;
            launchedBoarders = false;
        }

        //all maps
        public Lord ShipLord; //AI ship lord - defends or attacks
        public Lord InvaderLord; //second AI ship lord for wreck second facton 
        public Map NextTargetMap; //if any, will trigger battle after 10s
        public Map ShipGraveyard; //map to put destroyed ships to
        public Map GraveOrigin; //check if parent is in combat
        public bool IsGraveOriginInCombat
        {
            get
            {
                if (GraveOrigin == null)
                    return false;
                if (GraveOrigin.GetComponent<ShipHeatMapComp>().InCombat)
                    return true;
                return false;
            }
        }
        public bool IsGraveyard = false; //temp map, will be removed in a few days
        public bool InCombat = false;
        public bool BurnUpSet = false; //force terminate map+WO if no player pawns or pods present or in flight to
        public bool ToggleEngines = false; //OOC for events
        public int BurnTimer = 0; //OOC for events
        public List<Building_ShipBridge> MapRootListAll = new List<Building_ShipBridge>(); //all bridges on map
        List<Building> cores = new List<Building>(); //td recheck use
        public bool IsPlayerShipMap => map.Parent.def == ResourceBank.WorldObjectDefOf.ShipOrbiting;
        public ShipHeatMapComp GraveComp => ShipGraveyard.GetComponent<ShipHeatMapComp>();
        public int engineRot = -1;
        public int EngineRot //reset after any engine despawns and there are other engines present
        {
            get
            {
                if (engineRot == -1)
                {
                    //engine that can fire on proper ship, any engine on non wreck, default left
                    List<SoShipCache> shipsEng = ShipsOnMapNew.Values.Where(s => s.CanFire()).ToList();
                    if (shipsEng.Any())
                        engineRot = shipsEng.First().Rot;
                    else if (ShipsOnMapNew.Values.Any(s => !s.IsWreck && s.Engines.Any()))
                        engineRot = ShipsOnMapNew.Values.First(s => !s.IsWreck && s.Engines.Any()).Rot;
                    else if (ShipsOnMapNew.Values.Any(s => s.Engines.Any()))
                        engineRot = ShipsOnMapNew.Values.First(s => s.Engines.Any()).Rot;
                    else
                        engineRot = 3;
                    //Log.Message("SOS2: ".Colorize(Color.cyan) + map + " rot was -1, new rot: " + engineRot);
                }
                return engineRot;
            }
            set
            {
                engineRot = value;
            }
        }
        public List<Building_ShipAirlock> Docked = new List<Building_ShipAirlock>();
        public void UndockAllFrom (int index)
        {
            foreach (Building_ShipAirlock b in Docked)
            {
                if (ShipIndexOnVec(b.Position) == index)
                    b.DeSpawnDock();
            }
        }

        //non SC caches
        public List<CompShipCombatShield> Shields = new List<CompShipCombatShield>(); //workjob, hit detect
        public List<Building_ShipCloakingDevice> Cloaks = new List<Building_ShipCloakingDevice>(); //td get this into shipcache?
        public List<Building_ShipTurretTorpedo> TorpedoTubes = new List<Building_ShipTurretTorpedo>(); //workjob
        public List<CompBuildingConsciousness> Spores = new List<CompBuildingConsciousness>(); //workjob
        public HashSet<IntVec3> MapExtenderCells = new HashSet<IntVec3>(); //extender EVA checks
        public int SalvBayCount => map.listerBuildings.allBuildingsColonist.Where(t => t.TryGetComp<CompShipSalvageBay>() != null).Count();

        //ship cache functions
        public bool CacheOff = true; //set on map load to not cause massive joining calcs, proper parts assign to MapShipCells
        public override void FinalizeInit() //after spawn cache all ships
        {
            base.FinalizeInit();
            RecacheMap();
        }
        private Dictionary<IntVec3, Tuple<int, int>> shipCells; //cells occupied by shipParts, (index, path), if path is -1 = wreck
        public Dictionary<IntVec3, Tuple<int, int>> MapShipCells //td add bool if floor
        {
            get
            {
                if (shipCells == null)
                {
                    shipCells = new Dictionary<IntVec3, Tuple<int, int>>();
                }
                return shipCells;
            }
        }
        private Dictionary<int, SoShipCache> shipsOnMapNew;
        public Dictionary<int, SoShipCache> ShipsOnMapNew //cache of ships (bridgeId, ship)
        {
            get
            {
                if (shipsOnMapNew == null)
                {
                    shipsOnMapNew = new Dictionary<int, SoShipCache>();
                }
                return shipsOnMapNew;
            }
        }
        public List<SoShipCache> ShipsOnMap(bool allowWrecks = true)
        {
            List<SoShipCache> ships = new List<SoShipCache>();
            foreach (SoShipCache ship in ShipsOnMapNew.Values)
            {
                if (!allowWrecks && ship.IsWreck)
                    continue;
                ships.Add(ship);
            }
            return ships;
        }
        public void ResetCache()
        {
            foreach (IntVec3 vec in MapShipCells.Keys.ToList())
            {
                MapShipCells[vec] = new Tuple<int, int>(-1, -1);
            }
        }
        public void RepathMap() //repath all ships, on start player and end all maps
        {
            foreach (SoShipCache ship in ShipsOnMap().Where(s => !s.IsWreck))
            {
                ship.RebuildCorePath();
            }
        }
        public void RecacheMap() //rebuild all ships, wrecks on map init or after ship gen
        {
            foreach (Building_ShipBridge b in MapRootListAll)
            {
                b.ShipIndex = -1;
            }
            ShipsOnMapNew.Clear();
            for (int i = 0; i < MapRootListAll.Count; i++) //for each bridge make a ship, assign index
            {
                if (MapRootListAll[i].ShipIndex == -1) //skip any with valid index
                {
                    ShipsOnMapNew.Add(MapRootListAll[i].thingIDNumber, new SoShipCache());
                    ShipsOnMapNew[MapRootListAll[i].thingIDNumber].RebuildCache(MapRootListAll[i]);
                }
            }
            List<IntVec3> invalidCells = new List<IntVec3>(); //might happen with wrecks - temp solution
            foreach (IntVec3 vec in MapShipCells.Keys.ToList()) //ship wrecks from leftovers
            {
                if (MapShipCells[vec].Item1 == -1)
                {
                    Thing t = vec.GetThingList(map).FirstOrDefault(b => b.TryGetComp<CompSoShipPart>() != null);
                    if (t == null)
                    {
                        invalidCells.Add(vec);
                        continue;
                    }
                    int mergeToIndex = t.thingIDNumber;

                    ShipsOnMapNew.Add(mergeToIndex, new SoShipCache());
                    ShipsOnMapNew[mergeToIndex].RebuildCache(t as Building);
                }
            }
            if (invalidCells.Any())
            {
                Log.Message("SOS2: ".Colorize(Color.cyan) + map + " Recaching found ".Colorize(Color.red) + invalidCells.Count + " invalid cells! FIXING.");
                foreach (IntVec3 vec in invalidCells)
                {
                    MapShipCells.Remove(vec);
                }
            }
            CacheOff = false;
            Log.Message("SOS2: ".Colorize(Color.cyan) + map + " Recached,".Colorize(Color.green) + " Found ships: " + ShipsOnMapNew.Count);
        }
        public void CheckAndMerge(HashSet<int> indexes) //slower, finds best ship to merge to, removes all other ships
        {
            int mergeToIndex = -1;
            int mass = 0;
            Building origin = null;
            HashSet<int> ships = new HashSet<int>();
            foreach (int i in indexes) //find largest ship
            {
                ships.Add(i);
                if (!ShipsOnMapNew[i].IsWreck && ShipsOnMapNew[i].Mass > mass)
                {
                    mass = ShipsOnMapNew[i].Mass;
                    mergeToIndex = ShipsOnMapNew[i].Index;
                    origin = ShipsOnMapNew[i].Core;
                }
            }
            if (mergeToIndex == -1) //merging to wrecks only
            {
                foreach (int i in indexes)
                {
                    if (ShipsOnMapNew[i].Mass > mass)
                    {
                        mass = ShipsOnMapNew[i].Mass;
                        mergeToIndex = ShipsOnMapNew[i].Index;
                        origin = ShipsOnMapNew[i].Buildings.First();
                    }
                }
            }
            foreach (int i in ships) //delete all ships
            {
                ShipsOnMapNew.Remove(i);
            }
            //full rebuild
            ShipsOnMapNew.Add(mergeToIndex, new SoShipCache());
            ShipsOnMapNew[mergeToIndex].RebuildCache(origin);
        }
        public void CheckAndMerge(HashSet<IntVec3> cellsToMerge) //faster, attaches as a tumor
        {
            int mergeToIndex;
            IntVec3 mergeTo = IntVec3.Invalid;
            int mass = 0;
            HashSet<int> ships = new HashSet<int>();
            foreach (IntVec3 vec in cellsToMerge) //find largest ship
            {
                int shipIndex = ShipIndexOnVec(vec);
                ships.Add(shipIndex);
                if (shipIndex != -1 && ShipsOnMapNew[shipIndex].Mass > mass)
                {
                    mergeTo = vec;
                    mass = ShipsOnMapNew[shipIndex].Mass;
                }
            }
            if (mergeTo == IntVec3.Invalid) //merging to wrecks only
            {
                foreach (IntVec3 vec in cellsToMerge)
                {
                    int shipIndex = ShipIndexOnVec(vec);
                    if (ShipsOnMapNew[shipIndex].Mass > mass)
                    {
                        mergeTo = vec;
                        mass = ShipsOnMapNew[shipIndex].Mass;
                    }
                }
            }
            mergeToIndex = MapShipCells[mergeTo].Item1;
            ships.Remove(mergeToIndex);
            foreach (int i in ships) //delete all ships except mergeto
            {
                ShipsOnMapNew.Remove(i);
            }
            AttachAll(mergeTo, mergeToIndex);
        }
        public void AttachAll(IntVec3 mergeTo, int mergeToIndex) //merge and build corePath if ship
        {
            SoShipCache ship = ShipsOnMapNew[mergeToIndex];
            int path = ship.IsWreck ? -1 : (MapShipCells[mergeTo].Item2 + 1);
            HashSet<IntVec3> cellsTodo = new HashSet<IntVec3>();
            HashSet<IntVec3> cellsDone = new HashSet<IntVec3>();
            cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(mergeTo, Rot4.North, new IntVec2(1, 1)).Where(v => MapShipCells.ContainsKey(v) && MapShipCells[v]?.Item1 != mergeToIndex));

            //find cells cardinal that are in shiparea index and dont have same index, assign mergeTo corePath/index
            while (cellsTodo.Any())
            {
                List<IntVec3> current = cellsTodo.ToList();
                foreach (IntVec3 vec in current) //do all of the current corePath
                {
                    MapShipCells[vec] = new Tuple<int, int>(mergeToIndex, path); //assign new index, corepath
                    foreach (Thing t in vec.GetThingList(map))
                    {
                        if (t is Building b)
                        {
                            ship.AddToCache(b);
                        }
                    }
                    cellsTodo.Remove(vec);
                    cellsDone.Add(vec);
                }
                foreach (IntVec3 vec in current) //find parts cardinal to all prev.pos, exclude prev.pos, mergeto ship
                {
                    cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(vec, Rot4.North, new IntVec2(1, 1)).Where(v => MapShipCells.ContainsKey(v) && !cellsDone.Contains(v) && MapShipCells[v]?.Item1 != mergeToIndex));
                }
                if (path > -1)
                    path++;
                //Log.Message("parts at i: "+ current.Count + "/" + i);
            }
            Log.Message("SOS2: ".Colorize(Color.cyan) + map + " Ship ".Colorize(Color.green) + mergeToIndex + " Attached cells: " + cellsDone.Count);
        }
        public void RemoveShipFromCache(int index)
        {
            if (ShipsOnMapNew.ContainsKey(index))
            {
                Log.Warning("SOS2: ".Colorize(Color.cyan) + map + " Ship ".Colorize(Color.green) + index + " Removed from cache.");
                ShipsOnMapNew.Remove(index);
            }
        }
        public int ShipIndexOnVec(IntVec3 vec) //return index if ship on cell, else return -1
        {
            if (MapShipCells.ContainsKey(vec))
            {
                return MapShipCells[vec].Item1;
            }
            return -1;
        }
        public List<Pawn> PawnsOnShip(SoShipCache ship, Faction fac = null)
        {
            if (fac == null)
            {
                return map.mapPawns.AllPawns.Where(p => ShipIndexOnVec(p.Position) == ship.Index).ToList();
            }
            return map.mapPawns.AllPawns.Where(p => p.Faction == fac && ShipIndexOnVec(p.Position) == ship.Index).ToList();
            //return ShipLord.ownedPawns.Where(p => ShipIndexOnVec(p.Position) == ship.Index).ToList();
        }
        public bool VecHasLS(IntVec3 vec)
        {
            int shipIndex = ShipIndexOnVec(vec);
            if ((shipIndex > 0 && ShipsOnMapNew[shipIndex].LifeSupports.Any(s => s.active)) || MapExtenderCells.Contains(vec))
                return true;
            //LS if roofed room with thick rock roof and facing in vent that is attached to LS
            if (vec.Roofed(map) && vec.GetRoof(map) == RoofDefOf.RoofRockThick)
            {
                Room room = vec.GetRoom(map);
                if (!ShipInteriorMod2.ExposedToOutside(room))
                    return false;
                foreach (IntVec3 v in room.BorderCells)
                {
                    foreach (Thing t in v.GetThingList(map))
                    {
                        if (t is Building_ShipVent b)
                        {
                            shipIndex = ShipIndexOnVec(b.Position);
                            if (shipIndex > 0 && ShipsOnMapNew[shipIndex].LifeSupports.Any(s => s.active) && room.ContainsCell(b.ventTo))
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        //battle start
        public int MapThreat()
        {
            int threat = 0;
            foreach (SoShipCache ship in ShipsOnMapNew.Values)
            {
                threat += ship.Threat;
            }
            return threat;
        }
        public void StartShipEncounter(PassingShip passingShip = null, Map targetMap = null, Faction fac = null, int range = 0, bool fleet = false, bool bounty = false)
        {
            //startup on origin
            if (MapRootListAll.NullOrEmpty() || InCombat || BurnUpSet)
            {
                Log.Warning("SOS2: ".Colorize(Color.cyan) + map + " Error: Unable to start ship encounter.");
                return;
            }
            //origin vars
            ShipFaction = map.Parent.Faction;
            attackedTradeship = false;
            //target or create map + spawn ships
            ShipCombatOriginMap = map;
            if (targetMap == null)
                ShipCombatTargetMap = SpawnEnemyShipMap(passingShip, fac, fleet, bounty, out cores);
            else
                ShipCombatTargetMap = targetMap;
            //if ship is derelict switch to "encounter"
            if (TargetMapComp.IsGraveyard)
            {
                ShipCombatTargetMap = null; //td no ship combat vs no ship maps, for now
                targetMapComp = null;
                return;
            }
            Log.Message("SOS2: ".Colorize(Color.cyan) + map + " Starting combat vs map: ".Colorize(Color.green) + ShipCombatTargetMap);
            TargetMapComp.ShipCombatTargetMap = ShipCombatOriginMap;
            TargetMapComp.ShipCombatOriginMap = ShipCombatOriginMap;
            TargetMapComp.HasShipMapAI = true; //td for now set manually here
            //start caches
            RepathMap();
            ResetCombatVars();
            TargetMapComp.ResetCombatVars();

            if (range == 0) //set range DL:1-9
                DetermineInitialRange();
            Log.Message("SOS2: ".Colorize(Color.cyan) + map + " Enemy range at start: " + Range);

            callSlowTick = true;
        }
        public Map SpawnEnemyShipMap(PassingShip passingShip, Faction faction, bool fleet, bool bounty, out List<Building> cores)
        {
            Map newMap = new Map();
            cores = new List<Building>();
            EnemyShipDef shipDef = null;
            SpaceNavyDef navyDef = null;
            int wreckLevel = 0;
            bool shieldsActive = true;
            float CR = 0;
            float radius = 150f;
            float theta = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).theta - 0.1f + 0.002f * Rand.Range(0, 20);
            float phi = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).phi - 0.01f + 0.001f * Rand.Range(-20, 20);

            if (passingShip is AttackableShip attackableShip)
            {
                shipDef = attackableShip.attackableShip;
                navyDef = attackableShip.spaceNavyDef;
                faction = attackableShip.shipFaction;
            }
            else if (passingShip is DerelictShip derelictShip)
            {
                shipDef = derelictShip.derelictShip;
                navyDef = derelictShip.spaceNavyDef;
                faction = derelictShip.shipFaction;
                wreckLevel = derelictShip.wreckLevel;
                theta = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).theta + (0.05f + 0.002f * Rand.Range(0, 40)) * (Rand.Bool ? 1 : -1);
            }
            else //using player ship combat rating
            {
                CR = MapThreat();
                if (!fleet || passingShip == null)
                    CR *= 0.9f;
                if (!Prefs.DevMode && CR > 30)
                {
                    int daysPassed = GenDate.DaysPassedSinceSettle;
                    if (daysPassed < 30) //reduce difficulty early on
                        CR *= 0.6f;
                    else if (daysPassed < 60)
                        CR *= 0.8f;
                }
                CR *= Mathf.Clamp((float)ModSettings_SoS.difficultySoS, 0.1f, 5f);
                if (CR < 30) //minimum rating
                    CR = 30;
                if (CR > 100 && !fleet)
                {
                    if (CR > 2500 && (float)ModSettings_SoS.fleetChance < 0.8f) //past this more fleets due to high CR
                        fleet = Rand.Chance(0.8f);
                    else if (CR > 2000 && (float)ModSettings_SoS.fleetChance < 0.6f)
                        fleet = Rand.Chance(0.6f);
                    else
                        fleet = Rand.Chance((float)ModSettings_SoS.fleetChance);
                }
                if (passingShip is PirateShip pirateShip)
                {
                    faction = pirateShip.shipFaction;
                    navyDef = pirateShip.spaceNavyDef;
                    if (!fleet)
                        shipDef = ShipInteriorMod2.RandomValidShipFrom(navyDef.enemyShipDefs, CR, false, true);
                }
                else if (passingShip is TradeShip)
                {
                    //find suitable navyDef
                    faction = passingShip.Faction;
                    if (faction != null && DefDatabase<SpaceNavyDef>.AllDefs.Any(n => n.factionDefs.Contains(faction.def) && n.enemyShipDefs.Any(s => s.tradeShip)))
                    {
                        navyDef = DefDatabase<SpaceNavyDef>.AllDefs.Where(n => n.factionDefs.Contains(faction.def)).RandomElement();
                        if (!fleet)
                            shipDef = ShipInteriorMod2.RandomValidShipFrom(navyDef.enemyShipDefs, CR, true, true);
                    }
                    else if (!fleet) //navy has no trade ships - use default ones
                    {
                        shipDef = ShipInteriorMod2.RandomValidShipFrom(DefDatabase<EnemyShipDef>.AllDefs.ToList(), CR, true, false);
                    }
                    ShipInteriorMod2.WorldComp.PlayerFactionBounty += 5;
                    attackedTradeship = true;
                }
                else //find a random attacking ship to spawn
                {
                    if (bounty)
                        CR *= (float)Math.Pow(ShipInteriorMod2.WorldComp.PlayerFactionBounty, 0.2);
                    //spawned with faction override - try to find a valid navy
                    if (faction != null && DefDatabase<SpaceNavyDef>.AllDefs.Any(n => n.factionDefs.Contains(faction.def)))
                    {
                        navyDef = DefDatabase<SpaceNavyDef>.AllDefs.Where(n => n.factionDefs.Contains(faction.def)).RandomElement();
                        shipDef = ShipInteriorMod2.RandomValidShipFrom(navyDef.enemyShipDefs, CR, false, true);
                    }
                    else if (Rand.Chance((float)ModSettings_SoS.navyShipChance)) //try to spawn a random navy ship
                    {
                        //must have ships, hostile to player, able to operate
                        if (bounty)
                            navyDef = ShipInteriorMod2.ValidRandomNavyBountyHunts();
                        else
                            navyDef = ShipInteriorMod2.ValidRandomNavy(Faction.OfPlayer, true);

                        if (navyDef != null && !fleet)
                        {
                            shipDef = ShipInteriorMod2.RandomValidShipFrom(navyDef.enemyShipDefs, CR, false, true);
                        }
                    }
                    if (faction == null || shipDef == null) //no navy, faction or fallback
                    {
                        navyDef = null;
                        if (!fleet)
                            shipDef = ShipInteriorMod2.RandomValidShipFrom(DefDatabase<EnemyShipDef>.AllDefs.ToList(), CR, false, false);
                    }
                    if (shipDef != null)
                        Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("ShipCombatStart"), TranslatorFormattedStringExtensions.Translate("ShipCombatStartDesc", shipDef.label), LetterDefOf.ThreatBig);
                    else
                        Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("ShipCombatStart"), TranslatorFormattedStringExtensions.Translate("ShipCombatFleetDesc"), LetterDefOf.ThreatBig);
                }
            }
            if (passingShip != null)
            {
                ShipCombatOriginMap.passingShipManager.RemoveShip(passingShip);
                if (ModsConfig.IdeologyActive && !(passingShip is DerelictShip))
                    IdeoUtility.Notify_PlayerRaidedSomeone(map.mapPawns.FreeColonists);
            }
            if (faction == null)
            {
                if (navyDef != null)
                    faction = Find.FactionManager.AllFactions.Where(f => navyDef.factionDefs.Contains(f.def)).RandomElement();
                else
                    faction = Faction.OfAncientsHostile;
            }
            if (faction.HasGoodwill && faction.AllyOrNeutralTo(Faction.OfPlayer))
                faction.TryAffectGoodwillWith(Faction.OfPlayer, -150);

            //spawn map
            newMap = GetOrGenerateMapUtility.GetOrGenerateMap(ShipInteriorMod2.FindWorldTile(), new IntVec3(250, 1, 250), ResourceBank.WorldObjectDefOf.ShipEnemy);
            var mp = (WorldObjectOrbitingShip)newMap.Parent;
            mp.radius = radius;
            mp.theta = theta;
            mp.phi = phi;
            var newMapComp = newMap.GetComponent<ShipHeatMapComp>();
            if (passingShip is DerelictShip d)
            {
                shieldsActive = false;
                newMapComp.IsGraveyard = true;
                newMap.Parent.GetComponent<TimedForcedExitShip>().StartForceExitAndRemoveMapCountdown(d.ticksUntilDeparture);
                Find.LetterStack.ReceiveLetter("ShipEncounterStart".Translate(), "ShipEncounterStartDesc".Translate(newMap.Parent.GetComponent<TimedForcedExitShip>().ForceExitAndRemoveMapCountdownTimeLeftString), LetterDefOf.NeutralEvent);
            }
            newMapComp.ShipFaction = faction;
            if (wreckLevel != 3)
                newMapComp.ShipLord = LordMaker.MakeNewLord(faction, new LordJob_DefendShip(faction, newMap.Center), newMap);
            if (fleet) //spawn fleet - not for passingShips other than trade yet
            {
                ShipInteriorMod2.GenerateFleet(CR, newMap, passingShip, faction, newMapComp.ShipLord, out cores, shieldsActive, false, wreckLevel, navyDef: navyDef);
            }
            else //spawn ship
            {
                //keep this for troubleshooting
                Log.Message("SOS2: ".Colorize(Color.cyan) + map + " Spawning shipdef: " + shipDef + ", of faction: " + faction + ", of navy: " + navyDef + ", wrecklvl: " + wreckLevel);
                ShipInteriorMod2.GenerateShip(shipDef, newMap, passingShip, faction, newMapComp.ShipLord, out cores, shieldsActive, false, wreckLevel, navyDef: navyDef);
            }
            //post ship spawn - map name
            if (fleet)
            {
                mp.Name = "Ship fleet " + newMap.uniqueID;
            }
            else
            {
                mp.Name = shipDef.label + " " + newMap.uniqueID;
            }
            return newMap;
        }
        private void ResetCombatVars()
        {
            BuildingCountAtStart = 0;
            foreach (int index in shipsOnMapNew.Keys) //combat start calcs per ship
            {
                var ship = shipsOnMapNew[index];
                //if (!ship.IsWreck)
                ship.BuildingCountAtCombatStart = ship.BuildingCount;
                BuildingCountAtStart += ship.BuildingCountAtCombatStart;
                ship.BuildingsDestroyed.Clear();
            }
            BuildingsCount = BuildingCountAtStart;
            Log.Message("SOS2: ".Colorize(Color.cyan) + map + " BuildingCountAtStart: ".Colorize(Color.green) + BuildingCountAtStart);
            ShipGraveyard = null;
            InCombat = true;
            BurnUpSet = false;
            Heading = 0;
            MapEnginePower = 0;
            Maintain = false;
            Projectiles = new List<ShipCombatProjectile>();
            TorpsInRange = new List<ShipCombatProjectile>();
            //ship AI
            if (HasShipMapAI)
            {
                ResetShipAI();
            }
            else
            {
                RangeToKeep = Range;
            }
        }
        private void DetermineInitialRange()
        {
            byte detectionLevel = 7;
            List<Building_ShipAdvSensor> Sensors = ShipInteriorMod2.WorldComp.Sensors.Where(s => s.Map == map).ToList();
            List<Building_ShipAdvSensor> SensorsEnemy = ShipInteriorMod2.WorldComp.Sensors.Where(s => s.Map == ShipCombatTargetMap).ToList();
            if (Sensors.Where(sensor => sensor.def == ResourceBank.ThingDefOf.Ship_SensorClusterAdv && sensor.TryGetComp<CompPowerTrader>().PowerOn).Any())
            {
                detectionLevel += 2;
            }
            else if (Sensors.Where(sensor => sensor.TryGetComp<CompPowerTrader>().PowerOn).Any())
                detectionLevel += 1;

            if (Cloaks.Where(cloak => cloak.TryGetComp<CompPowerTrader>().PowerOn).Any())
                detectionLevel -= 2;
            if (SensorsEnemy.Where(sensor => sensor.def == ResourceBank.ThingDefOf.Ship_SensorClusterAdv && sensor.TryGetComp<CompPowerTrader>().PowerOn).Any())
                detectionLevel -= 2;
            else if (SensorsEnemy.Any())
                detectionLevel -= 1;
            if (TargetMapComp.Cloaks.Where(cloak => cloak.TryGetComp<CompPowerTrader>().PowerOn).Any())
                detectionLevel -= 2;
            Range = 180 + detectionLevel * 20 + Rand.Range(0, 40);
        }

        //battle
        public override void MapComponentTick()
        {
            base.MapComponentTick();
            List<SoShipCache> shipToRemove = new List<SoShipCache>();
            foreach (SoShipCache ship in ShipsOnMapNew.Values)
            {
                ship.Tick();
                /*if (ship.Index % 100 == Find.TickManager.TicksGame)
                {
                    ship.RareTick();
                }*/
                if (!ship.Area.Any())
                    shipToRemove.Add(ship);
            }
            if (shipToRemove.Any())
            {
                foreach (SoShipCache cache in shipToRemove)
                {
                    RemoveShipFromCache(cache.Index);
                }
                Log.Message("SOS2: ".Colorize(Color.cyan) + map + " Removed " + shipToRemove.Count + " ships. Remaining: " + ShipsOnMapNew.Count);
            }
            if (!map.IsSpace())
                return;

            if (InCombat)
            {
                foreach (int index in ShipsToMove)
                {
                    RemoveShipFromBattle(index);
                }
                ShipsToMove.Clear();
                if (Heading == 1)
                    OriginMapComp.Range -= MapEnginePower; //reduce distance
                else if (Heading == -1)
                    OriginMapComp.Range += MapEnginePower; //inrease distance

                OriginMapComp.Range = Mathf.Clamp(OriginMapComp.Range, 0f, 400f);

                //deregister projectiles in own cache, spawn them on targetMap
                List<ShipCombatProjectile> toRemove = new List<ShipCombatProjectile>();
                foreach (ShipCombatProjectile proj in Projectiles)
                {
                    if (proj.range >= OriginMapComp.Range)
                    {
                        //td determine miss, remove proj
                        //factors for miss: range+,pilot console+,mass-,thrusters+
                        //factors when fired/registered:weapacc-,tac console-
                        Projectile projectile;
                        IntVec3 spawnCell;
                        if (proj.burstLoc == IntVec3.Invalid)
                            spawnCell = FindClosestEdgeCell(ShipCombatTargetMap, proj.target.Cell);
                        else
                            spawnCell = proj.burstLoc;
                        //Log.Message("Spawning " + proj.turret + " projectile on player ship at " + proj.target);
                        projectile = (Projectile)GenSpawn.Spawn(proj.spawnProjectile, spawnCell, ShipCombatTargetMap);

                        //get angle
                        IntVec3 a = proj.target.Cell - spawnCell;
                        float angle = a.AngleFlat;
                        //get miss
                        float missAngle = Rand.Range(-proj.missRadius, proj.missRadius); //base miss from xml
                        float rng = proj.range - proj.turret.heatComp.Props.optRange;
                        if (rng > 0)
                        {
                            //add miss to angle
                            missAngle *= (float)Math.Sqrt(rng); //-20 - 20
                            //Log.Message("angle: " + angle + ", missangle: " + missAngle);
                        }
                        //shooter adj 0-50%
                        missAngle *= (100 - proj.accBoost * 2.5f) / 100;
                        angle += missAngle;
                        //new vec from origin + angle
                        IntVec3 c = spawnCell + new Vector3(1000 * Mathf.Sin(Mathf.Deg2Rad * angle), 0, 1000 * Mathf.Cos(Mathf.Deg2Rad * angle)).ToIntVec3();
                        //Log.Message("Target cell was " + proj.target.Cell + ", adjusted to " + c);
                        projectile.Launch(proj.turret, spawnCell.ToVector3Shifted(), c, proj.target.Cell, ProjectileHitFlags.All, equipment: proj.turret);
                        toRemove.Add(proj);
                    }
                    else if ((proj.spawnProjectile.thingClass == typeof(Projectile_TorpedoShipCombat) || proj.spawnProjectile.thingClass == typeof(Projectile_ExplosiveShipCombatAntigrain)) && !TorpsInRange.Contains(proj) && OriginMapComp.Range - proj.range < 65)
                    {
                        TorpsInRange.Add(proj);
                    }
                    else
                    {
                        proj.range += proj.speed / 4;
                    }
                }
                foreach (ShipCombatProjectile proj in toRemove)
                {
                    Projectiles.Remove(proj);
                    if (TorpsInRange.Contains(proj))
                        TorpsInRange.Remove(proj);
                }
                //shipAI cant retreat if player has pawns on its map
                if (HasShipMapAI)
                {
                    if (OriginMapComp.Range >= 395 && Retreating && !map.mapPawns.AnyColonistSpawned)
                    {
                        EndBattle(map, true);
                        return;
                    }
                }
                if (MapRootListAll.NullOrEmpty()) //if all ships on map gone, lose combat
                {
                    EndBattle(map, false);
                    return;
                }
            }
            if (callSlowTick) //origin only: call both slow ticks
            {
                SlowTick();
                TargetMapComp.SlowTick();
                callSlowTick = false;
            }
            else if (Find.TickManager.TicksGame % 60 == 0)
            {
                SlowTick();
            }
        }
        public void SlowTick()
        {
            if (InCombat)
            {
                if (Maintain) //distance maintain
                {
                    if (TargetMapComp.Heading == 1) //target moving to origin
                    {
                        if (RangeToKeep > OriginMapComp.Range)
                            Heading = -1;
                        else
                            Heading = 0;
                    }
                    else if (TargetMapComp.Heading == -1) //target moving from origin
                    {
                        if (RangeToKeep < OriginMapComp.Range)
                            Heading = 1;
                        else
                            Heading = 0;
                    }
                    else if (Heading == 0)
                    {
                        Heading = 0;
                    }
                }
                //engine power calcs
                bool anyShipCanMove = AnyShipCanMove();
                if (anyShipCanMove && Heading != 0) //can we move and should we move
                {
                    MapEnginesOn();
                    MapEnginePower *= 40f;
                }
                else
                {
                    MapFullStop();
                }

                //threat calcs
                totalThreat = 1;
                threatPerSegment = new[] { 1f, 1f, 1f, 1f };
                BuildingsCount = 0;
                float powerCapacity = 0;
                float powerRemaining = 0;
                foreach (SoShipCache ship in ShipsOnMapNew.Values)
                {
                    if (HasShipMapAI && !ship.IsWreck && ship.Core.PowerComp.PowerNet != null) //shipAI purge
                    {
                        foreach (var battery in ship.Core.PowerComp.PowerNet.batteryComps)
                        {
                            powerCapacity += battery.Props.storedEnergyMax;
                            powerRemaining += battery.StoredEnergy;
                        }
                        ship.PurgeCheck();
                    }
                    float[] actualThreatPerSegment = ship.ActualThreatPerSegment();
                    threatPerSegment[0] += actualThreatPerSegment[0];
                    threatPerSegment[1] += actualThreatPerSegment[1];
                    threatPerSegment[2] += actualThreatPerSegment[2];
                    threatPerSegment[3] += actualThreatPerSegment[3];
                    //threatPerSegment = threatPerSegment.Zip(ship.ActualThreatPerSegment, (x, y) => x + y).ToArray();
                    //Log.Message("map threat: " + threatPerSegment[0] + " " + threatPerSegment[1] + " " + threatPerSegment[2] + " " + threatPerSegment[3] + " ");

                    BuildingsCount += ship.Buildings.Count;
                    totalThreat += ship.ThreatCurrent;
                }

                //shipAI distance, boarding
                if (HasShipMapAI)
                {
                    if (ShipsOnMapNew.Count > 1) //fleet AI evals ships in fleet and rem bad ships
                    {
                        if (anyShipCanMove)
                        {
                            foreach (int index in ShipsOnMapNew.Keys)
                            {
                                var ship = ShipsOnMapNew[index];
                                //ship cant move and fleet fleeing or ship less than x of fleet threat
                                if (!ship.CanMove() && (Retreating || totalThreat * 0.2f > ship.ThreatCurrent))
                                {
                                    ShipsToMove.Add(index);
                                }
                                //ship is much slower than rest of fleet
                            }
                        }
                    }


                    //AI abandon ship
                    /*if (MapRootListAll.NullOrEmpty()) //get all crypto, board, launch
                    {
                        foreach (SoShipCache ship in ShipsOnMapNew.Values)
                        {
                            List<CompCryptoLaunchable> pods = new List<CompCryptoLaunchable>(ship.Pods);
                            List<Pawn> pawns = new List<Pawn>(PawnsOnShip(ship, ship.Faction));

                            int i = 0;
                            while (pawns.Count < 1 || pods.Count < 1)
                            {
                                pawns.First().jobs.curJob = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("BoardPods"), pods.First().parent);
                            }
                        }
                    }*/

                    /*if (TurretTarget == null || TurretTarget.Destroyed) //find new target
                    {
                        //try and obliterate player map, if player has grave, swap to it if it is a better target 
                        if (TargetMapComp.MapRootListAll.Any(b => !b.Destroyed))
                            TurretTarget = TargetMapComp.MapRootListAll.RandomElement();
                        else if (TargetMapComp.ShipGraveyard != null && TargetMapComp.ShipGraveyard.mapPawns.ColonistCount > ShipCombatTargetMap.mapPawns.ColonistCount)
                        {
                            //prefer going after the graveyard
                            EndBattle(ShipCombatTargetMap, false);
                        }
                        else
                        {
                            if (ShipCombatTargetMap.mapPawns.ColonistCount > 0)
                                TurretTarget = ShipCombatTargetMap.mapPawns.AllPawns.Where(p => p.IsColonist).RandomElement();
                            else if (ShipCombatTargetMap.listerBuildings.allBuildingsColonist.Any())
                                TurretTarget = ShipCombatTargetMap.listerBuildings.allBuildingsColonist.RandomElement();
                            else //no valid targets found, AI won
                                EndBattle(ShipCombatTargetMap, false);
                        }
                    }*/

                    if (anyShipCanMove) //set AI heading
                    {
                        //True, totalThreat:1, TargetMapComp.totalThreat:1, TurretNum:0
                        //retreat
                        if (Retreating || totalThreat / TargetMapComp.totalThreat < 0.4f || powerRemaining / powerCapacity < 0.2f || totalThreat == 1 || BuildingsCount / (float)BuildingCountAtStart < 0.7f || Find.TickManager.TicksGame > BattleStartTick + 90000)
                        {
                            Heading = -1;
                            Retreating = true;
                            if (!warnedAboutRetreat)
                            {
                                Log.Message("SOS2: ".Colorize(Color.cyan) + map + " AI retreating:".Colorize(Color.red) + ", totalThreat:" + totalThreat + ", TargetMapComp.totalThreat:" + TargetMapComp.totalThreat + ", powerRemaining:" + powerRemaining + ", powerCapacity:" + powerCapacity + ", BuildingsCount:" + BuildingsCount + ", BuildingCountAtStart:" + BuildingCountAtStart);
                                Messages.Message("EnemyShipRetreating".Translate(), MessageTypeDefOf.ThreatBig);
                                warnedAboutRetreat = true;
                            }
                        }
                        else //move to range
                        {
                            //calc ratios, higher = better
                            float[] threatRatio = new[] { threatPerSegment[0] / TargetMapComp.threatPerSegment[0],
                            threatPerSegment[1] / TargetMapComp.threatPerSegment[1],
                            threatPerSegment[2] / TargetMapComp.threatPerSegment[2],
                            threatPerSegment[3] / TargetMapComp.threatPerSegment[3] };
                            float max = threatRatio[0];
                            int best = 0;
                            //string str = "threat LMSC: ";
                            for (int i = 1; i < 4; i++)
                            {
                                if (threatRatio[i] == 1) //threat is 0 for both
                                    threatRatio[i] = 0;
                                //str += threatRatio[i] + " ";
                                if (threatRatio[i] > max)
                                {
                                    max = threatRatio[i];
                                    best = i;
                                }
                            }
                            //Log.Message(str);
                            if (OriginMapComp.Range > maxRange[best]) //forward
                            {
                                if (Heading != 1)
                                    Log.Message("SOS2: ".Colorize(Color.cyan) + map + " AI now moving forward".Colorize(Color.green) + " Threat ratios (CSML): " + threatRatio[0].ToString("F2") + " " + threatRatio[1].ToString("F2") + " " + threatRatio[2].ToString("F2") + " " + threatRatio[3].ToString("F2"));
                                Heading = 1;
                            }
                            else if (OriginMapComp.Range <= minRange[best]) //back
                            {
                                if (Heading != -1)
                                    Log.Message("SOS2: ".Colorize(Color.cyan) + map + " AI now moving backward".Colorize(Color.green) + " Threat ratios (CSML): " + threatRatio[0].ToString("F2") + " " + threatRatio[1].ToString("F2") + " " + threatRatio[2].ToString("F2") + " " + threatRatio[3].ToString("F2"));
                                Heading = -1;
                            }
                            else //chill
                            {
                                if (Heading != 0)
                                    Log.Message("SOS2: ".Colorize(Color.cyan) + map + " AI now stopped".Colorize(Color.green) + " Threat ratios (CSML): " + threatRatio[0].ToString("F2") + " " + threatRatio[1].ToString("F2") + " " + threatRatio[2].ToString("F2") + " " + threatRatio[3].ToString("F2"));
                                Heading = 0;
                            }
                        }
                    }
                    else //all engines dead or disabled
                    {
                        Heading = 0;
                        Retreating = false;
                        if ((threatPerSegment[0] == 1 && threatPerSegment[1] == 1 && threatPerSegment[2] == 1 && threatPerSegment[3] == 1) || Find.TickManager.TicksGame > BattleStartTick + 120000)
                        {
                            //no turrets to fight with - exit
                            EndBattle(map, false);
                            return;
                        }
                        if (warnedAboutAdrift == 0)
                        {
                            Messages.Message(TranslatorFormattedStringExtensions.Translate("EnemyShipAdrift"), map.Parent, MessageTypeDefOf.NegativeEvent);
                            warnedAboutAdrift = Find.TickManager.TicksGame + Rand.RangeInclusive(60000, 180000);
                        }
                        else if (Find.TickManager.TicksGame > warnedAboutAdrift)
                        {
                            EndBattle(map, false, warnedAboutAdrift - Find.TickManager.TicksGame);
                            return;
                        }
                    }
                    //AI boarding code
                    if ((hasAnyPartDetached || Find.TickManager.TicksGame > BattleStartTick + 5000) && !startedBoarderLoad && !Retreating)
                    {
                        foreach (SoShipCache ship in ShipsOnMapNew.Values)
                        {
                            List<CompTransporter> transporters = new List<CompTransporter>();
                            float transporterMass = 0;
                            foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.Transporter).Where(t => t.Faction == ship.Faction && ShipIndexOnVec(t.Position) == ship.Index))
                            {
                                var transporter = t.TryGetComp<CompTransporter>();
                                if (transporter != null && t.def == ResourceBank.ThingDefOf.PersonalShuttle)
                                {
                                    transporters.Add(transporter);
                                    transporterMass += transporter.Props.massCapacity;
                                }
                            }
                            foreach (Pawn p in PawnsOnShip(ship, ship.Faction))
                            {
                                if (transporterMass >= p.RaceProps.baseBodySize * 70 && p.mindState.duty != null && p.kindDef.combatPower > 40)
                                {
                                    TransferableOneWay tr = new TransferableOneWay();
                                    tr.things.Add(p);
                                    CompTransporter porter = transporters.RandomElement();
                                    porter.groupID = 0;
                                    porter.AddToTheToLoadList(tr, 1);
                                    p.mindState.duty.transportersGroup = 0;
                                    transporterMass -= p.RaceProps.baseBodySize * 70;
                                }
                            }
                        }
                    }
                    if (startedBoarderLoad && !launchedBoarders && !Retreating) //td change per ship?
                    {
                        //abort and reset if player on ship
                        if (map.mapPawns.AllPawnsSpawned.Any(o => o.Faction == Faction.OfPlayer))
                        {
                            foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.Transporter).Where(tr => tr.Faction != Faction.OfPlayer))
                            {
                                var transporter = t.TryGetComp<CompTransporter>();
                                if (transporter.innerContainer.Any || transporter.AnythingLeftToLoad)
                                    transporter.CancelLoad();
                            }
                            startedBoarderLoad = false;
                        }
                        else //board
                        {
                            bool allOnPods = true;
                            foreach (Pawn p in map.mapPawns.AllPawnsSpawned.Where(o => o.Faction != Faction.OfPlayer))
                            {
                                if (p.mindState?.duty?.transportersGroup == 0 && p.MannedThing() == null)
                                    allOnPods = false;
                            }
                            if (allOnPods) //launch
                            {
                                List<CompShuttleLaunchable> transporters = new List<CompShuttleLaunchable>();
                                foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.Transporter).Where(tr => tr.Faction != Faction.OfPlayer))
                                {
                                    var transporter = t.TryGetComp<CompTransporter>();
                                    if (!(transporter?.innerContainer.Any ?? false)) continue;

                                    var launchable = t.TryGetComp<CompShuttleLaunchable>();
                                    if (launchable == null) continue;

                                    transporters.Add(launchable);
                                }
                                if (transporters.Count > 0)
                                {
                                    transporters[0].TryLaunch(ShipCombatTargetMap.Parent, new TransportPodsArrivalAction_ShipAssault(ShipCombatTargetMap.Parent));
                                    OriginMapComp.ShipLord = LordMaker.MakeNewLord(ShipFaction, new LordJob_AssaultShip(ShipFaction, false, false, false, true, false), ShipCombatTargetMap, new List<Pawn>());
                                }
                                launchedBoarders = true;
                            }
                        }
                    }
                }
            }
            else
            {
                if (Find.TickManager.TicksGame % 6000 == 0) //very slow checks - decomp, bounty
                {
                    foreach (SoShipCache ship in ShipsOnMapNew.Values) //decompresson
                    {
                        List<Building> buildings = new List<Building>();
                        foreach (Building b in ship.OuterNonShipWalls())
                        {
                            if (Rand.Chance(0.5f))
                                buildings.Add(b);
                        }
                        foreach (Building b in buildings)
                        {
                            b.Destroy(DestroyMode.KillFinalize);
                        }
                    }
                    if (IsPlayerShipMap) //bounty event
                    {
                        if (ShipInteriorMod2.WorldComp.PlayerFactionBounty > 20 && Find.TickManager.TicksGame - LastBountyRaidTick > Mathf.Max(600000f / Mathf.Sqrt(ShipInteriorMod2.WorldComp.PlayerFactionBounty), 60000f))
                        {
                            LastBountyRaidTick = Find.TickManager.TicksGame;
                            Building_ShipBridge bridge = MapRootListAll.FirstOrDefault();
                            if (bridge == null)
                                return;
                            StartShipEncounter(bounty: true);
                        }
                    }
                }
                //trigger combat with graveyard
                if (IsGraveyard && NextTargetMap != null && Find.TickManager.TicksGame > LastAttackTick + 600)
                {
                    StartShipEncounter(null, NextTargetMap);
                    NextTargetMap = null;
                    return;
                }
                var cond = map.gameConditionManager.ActiveConditions.FirstOrDefault(c => c is GameCondition_SpaceDebris);
                if (cond != null)//map.gameConditionManager.ConditionIsActive(ResourceBank.GameConditionDefOf.SpaceDebris))
                {
                    //reduce durration per engine vs mass
                    bool anyShipCanMove = AnyShipCanMove();
                    if (anyShipCanMove && ToggleEngines) //can we move and should we move
                    {
                        MapEnginesOn();
                        MapEnginePower *= 40000f;
                        if (BurnTimer > cond.TicksLeft)
                        {
                            cond.End();
                            BurnTimer = 0;
                            MapFullStop();
                        }
                        else
                        {
                            BurnTimer += (int)MapEnginePower;
                            //Log.Message("ticks remain " + map.gameConditionManager.ActiveConditions.FirstOrDefault(c => c is GameCondition_SpaceDebris).TicksLeft);
                        }
                    }
                    else
                    {
                        MapFullStop();
                    }
                }
            }
        }
        public bool AnyShipCanMove() //any non stuck ship has a working and fueled engine and is aligned
        {
            foreach (SoShipCache ship in ShipsOnMapNew.Values)
            {
                if (ship.CanMove())
                {
                    return true;
                }
            }
            return false;
        }
        public void MapEnginesOn()
        {
            MapEnginePower = SlowestThrustToWeight();
            //Log.Message("thrust " + MapEnginePower);
            //Log.Message("SOS2: ".Colorize(Color.cyan) + map + " SlowestThrustOnMap: " + MapEnginePower);
            foreach (SoShipCache ship in ShipsOnMapNew.Values.Where(s => s.Engines.Any()))
            {
                ship.MoveAtThrustToWeight(MapEnginePower);
            }
        }
        public float SlowestThrustToWeight() //find worst t/w ship
        {
            float enginePower = float.MaxValue;
            foreach (SoShipCache ship in ShipsOnMapNew.Values)
            {
                if (!ship.CanMove())
                    return 0;
                float currPower = ship.ThrustToWeight();
                if (currPower == 0)
                    return 0;
                if (currPower < enginePower)
                    enginePower = currPower;
            }
            return enginePower * 14;
        }
        public void MapFullStop()
        {
            MapEnginePower = 0;
            Heading = 0;
            foreach (SoShipCache ship in ShipsOnMapNew.Values)
            {
                ship.EnginesOff();
            }
        }
        public void RemoveShipFromBattle(int shipIndex) //only call this on mapcomp tick!
        {
            if (ShipsOnMapNew.Values.Count() > 1) //move to graveyard if not last ship
            {
                Log.Warning("SOS2: ".Colorize(Color.cyan) + map + " Ship ".Colorize(Color.green) + shipIndex + " RemoveShipFromBattle");
                SoShipCache ship = ShipsOnMapNew[shipIndex];
                Building core = ship.Core;
                ship.LastBridgeDied = false;
                //ship.AreaDestroyed.Clear();
                if (core == null)
                {
                    core = ShipsOnMapNew[shipIndex].Parts.FirstOrDefault();
                }
                if (core != null)
                {
                    Log.Warning("SOS2: ".Colorize(Color.cyan) + map + " Ship ".Colorize(Color.green) + shipIndex + " Removing with: " + core);
                    if (ShipGraveyard == null)
                        SpawnGraveyard();
                    ShipInteriorMod2.MoveShip(core, ShipGraveyard, new IntVec3(0, 0, 0));
                }
            }
            Log.Warning("SOS2: ".Colorize(Color.cyan) + map + " Ships remaining: " + ShipsOnMapNew.Count);
            foreach (SoShipCache ship in ShipsOnMapNew.Values)
            {
                Log.Warning("SOS2: ".Colorize(Color.cyan) + map + " Ship ".Colorize(Color.green) + ship.Index + ", area: " + ship.Area.Count + ", bldgs: " + ship.BuildingCount + ", cores: " + ship.Bridges.Count);
            }
        }
        public void SpawnGraveyard() //if not present, create a graveyard
        {
            //Log.Message("SOS2: ".Colorize(Color.cyan) + map + " SpawnGraveyard");
            float adj;
            if (ShipCombatOrigin)
                adj = Rand.Range(0.025f, 0.075f);
            else
                adj = Rand.Range(-0.075f, -0.125f);
            ShipGraveyard = GetOrGenerateMapUtility.GetOrGenerateMap(ShipInteriorMod2.FindWorldTile(), map.Size, ResourceBank.WorldObjectDefOf.WreckSpace);
            ShipGraveyard.fogGrid.ClearAllFog();
            var mp = (WorldObjectOrbitingShip)ShipGraveyard.Parent;
            mp.radius = 150;
            mp.theta = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).theta + adj;
            mp.phi = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).phi - 0.01f + 0.001f * Rand.Range(0, 20);
            mp.Name += "Wreckage nr." + ShipGraveyard.uniqueID;
            var graveMapComp = ShipGraveyard.GetComponent<ShipHeatMapComp>();
            graveMapComp.IsGraveyard = true;
            graveMapComp.GraveOrigin = map;
            graveMapComp.ShipFaction = ShipFaction;
        }
        public void ShipBuildingsOff() //td should no longer be needed for engines
        {
            //td destroy all proj?
            foreach (SoShipCache ship in OriginMapComp.ShipsOnMapNew.Values)
            {
                ship.EnginesOff();
            }
            foreach (SoShipCache ship in OriginMapComp.TargetMapComp.ShipsOnMapNew.Values)
            {
                ship.EnginesOff();
                foreach (CompShipCombatShield s in ship.Shields)
                {
                    s.flickComp.SwitchIsOn = false;
                }
            }
            foreach (Thing t in ShipCombatTargetMap.listerThings.ThingsInGroup(ThingRequestGroup.Transporter).Where(tr => tr.Faction != Faction.OfPlayer))
            {
                var transporter = t.TryGetComp<CompTransporter>();
                if (transporter.innerContainer.Any || transporter.AnythingLeftToLoad)
                    transporter.CancelLoad();
            }
        }
        public void EndBattle(Map loser, bool fled, int burnTimeElapsed = 0)
        {
            Log.Message("SOS2: ".Colorize(Color.cyan) + loser + " Lost ship battle!".Colorize(Color.red));
            //tgtMap is opponent of origin
            Map tgtMap = OriginMapComp.ShipCombatTargetMap;
            var tgtMapComp = OriginMapComp.TargetMapComp;
            tgtMapComp.HasShipMapAI = false;
            tgtMapComp.InCombat = false;
            OriginMapComp.InCombat = false;
            OriginMapComp.ShipBuildingsOff();
            OriginMapComp.ShipGraveyard?.Parent.GetComponent<TimedForcedExitShip>()?.StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(60000, 180000) - burnTimeElapsed);
            tgtMapComp.ShipGraveyard?.Parent.GetComponent<TimedForcedExitShip>()?.StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(60000, 180000) - burnTimeElapsed);
            if (loser != ShipCombatOriginMap)
            {
                if (fled) //target fled, remove target
                {
                    tgtMapComp.BurnUpSet = true;
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("EnemyShipRetreated"), MessageTypeDefOf.ThreatBig);
                }
                else //target lost
                {
                    tgtMapComp.IsGraveyard = true;
                    if (OriginMapComp.attackedTradeship)
                        ShipInteriorMod2.WorldComp.PlayerFactionBounty += 15;
                    tgtMap.Parent.GetComponent<TimedForcedExitShip>().StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(60000, 180000) - burnTimeElapsed);
                    Find.LetterStack.ReceiveLetter("WinShipBattle".Translate(), "WinShipBattleDesc".Translate(tgtMap.Parent.GetComponent<TimedForcedExitShip>().ForceExitAndRemoveMapCountdownTimeLeftString), LetterDefOf.PositiveEvent);
                }
            }
            else
            {
                if (!fled) //origin lost
                {
                    ShipCombatOriginMap.Parent.GetComponent<TimedForcedExitShip>()?.StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(60000, 180000));
                    //Find.GameEnder.CheckOrUpdateGameOver();
                }
                //origin fled or lost: if origin has grave with a ship, grave starts combat with target
                if (OriginMapComp.ShipGraveyard != null && OriginMapComp.GraveComp.MapRootListAll.Any() && !OriginMapComp.attackedTradeship)
                {
                    OriginMapComp.GraveComp.LastAttackTick = Find.TickManager.TicksGame;
                    OriginMapComp.GraveComp.NextTargetMap = OriginMapComp.ShipCombatTargetMap;
                }
                else //origin fled or lost with no graveyard, remove target
                {
                    //td instead launch boarders to origin
                    tgtMapComp.BurnUpSet = true;
                }
            }

            //td temp
            tgtMapComp.ShipCombatTargetMap = null;
            tgtMapComp.originMapComp = null;
            tgtMapComp.targetMapComp = null;
            OriginMapComp.ShipCombatOrigin = false;
            OriginMapComp.ShipCombatTargetMap = null;
            OriginMapComp.originMapComp = null;
            OriginMapComp.targetMapComp = null;
        }
        //proj
        public IntVec3 FindClosestEdgeCell(Map map, IntVec3 targetCell)
        {
            Rot4 dir;
            var mapComp = map.GetComponent<ShipHeatMapComp>();
            if (mapComp.Heading == 1) //target advancing - shots from front
            {
                dir = new Rot4(mapComp.EngineRot);
            }
            else if (mapComp.Heading == -1) //target retreating - shots from back
            {
                dir = new Rot4(mapComp.EngineRot + 2);
            }
            else //shots from closest edge
            {
                if (targetCell.x < map.Size.x / 2 && targetCell.x < targetCell.z && targetCell.x < (map.Size.z) - targetCell.z)
                    dir = Rot4.West;
                else if (targetCell.x > map.Size.x / 2 && map.Size.x - targetCell.x < targetCell.z && map.Size.x - targetCell.x < (map.Size.z) - targetCell.z)
                    dir = Rot4.East;
                else if (targetCell.z > map.Size.z / 2)
                    dir = Rot4.North;
                else
                    dir = Rot4.South;
            }
            return CellFinder.RandomEdgeCell(dir, map);
        }
    }
}
