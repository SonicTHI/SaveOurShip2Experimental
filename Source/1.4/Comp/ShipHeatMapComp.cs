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
            RimworldMod.AccessExtensions.Utility.shipHeatMapCompCache.Add(this);
        }
        public override void MapRemoved()
        {
            RimworldMod.AccessExtensions.Utility.shipHeatMapCompCache.Remove(this);
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
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look<Map>(ref ShipCombatOriginMap, "ShipCombatOriginMap");
            Scribe_References.Look<Map>(ref ShipCombatMasterMap, "ShipCombatMasterMap");
            Scribe_References.Look<Map>(ref ShipCombatTargetMap, "ShipCombatTargetMap");
            Scribe_References.Look<Map>(ref ShipGraveyard, "ShipCombatGraveyard");
            Scribe_References.Look<Faction>(ref ShipFaction, "ShipFaction");
            Scribe_References.Look<Lord>(ref ShipLord, "ShipLord");
            Scribe_References.Look<Lord>(ref InvaderLord, "InvaderLord");
            Scribe_Values.Look<bool>(ref ShipCombatMaster, "ShipCombatMaster", false);
            Scribe_Values.Look<bool>(ref InCombat, "InCombat", false);
            Scribe_Values.Look<bool>(ref IsGraveyard, "IsGraveyard", false);
            Scribe_Values.Look<bool>(ref BurnUpSet, "BurnUpSet", false);
            Scribe_Values.Look<int>(ref EngineRot, "EngineRot");
            if (InCombat)
            {
                Scribe_Values.Look<int>(ref BurnTimer, "BurnTimer");
                Scribe_Values.Look<int>(ref Heading, "Heading");
                Scribe_Collections.Look<ShipCombatProjectile>(ref Projectiles, "ShipProjectiles");
                Scribe_Collections.Look<ShipCombatProjectile>(ref TorpsInRange, "ShipTorpsInRange");
                //SC cache
                Scribe_Collections.Look<Building_ShipBridge>(ref MapRootListAll, "MapRootListAll", LookMode.Reference);
                originMapComp = null;
                masterMapComp = null;
                //SCM only
                Scribe_Values.Look<bool>(ref callSlowTick, "callSlowTick");
                Scribe_Values.Look<float>(ref Range, "BattleRange");
                Scribe_Values.Look<float>(ref RangeToKeep, "RangeToKeep");
                Scribe_Values.Look<bool>(ref PlayerMaintain, "PlayerMaintain");
                Scribe_Values.Look<bool>(ref attackedTradeship, "attackedTradeship");
                Scribe_Values.Look<int>(ref BuildingCountAtStart, "BuildingCountAtStart");
                Scribe_Values.Look<bool>(ref enemyRetreating, "enemyRetreating");
                Scribe_Values.Look<bool>(ref warnedAboutRetreat, "warnedAboutRetreat");
                Scribe_Values.Look<int>(ref warnedAboutAdrift, "warnedAboutAdrift");
                Scribe_Values.Look<bool>(ref hasAnyPlayerPartDetached, "PlayerPartDetached");
                Scribe_Values.Look<bool>(ref startedBoarderLoad, "StartedBoarding");
                Scribe_Values.Look<bool>(ref launchedBoarders, "LaunchedBoarders");
                Scribe_Values.Look<int>(ref BattleStartTick, "BattleStartTick");
                Scribe_Values.Look<bool>(ref Scanned, "Scanned");
            }
        }
        //non SC caches
        public List<CompShipCombatShield> Shields = new List<CompShipCombatShield>(); //workjob, hit detect
        public List<Building_ShipCloakingDevice> Cloaks = new List<Building_ShipCloakingDevice>(); //td get this into shipcache?
        public List<Building_ShipTurretTorpedo> TorpedoTubes = new List<Building_ShipTurretTorpedo>(); //workjob
        public List<CompBuildingConsciousness> Spores = new List<CompBuildingConsciousness>(); //workjob
        //SC vars
        public Map ShipCombatOriginMap; //"player" map - initializes combat vars
        public Map ShipCombatMasterMap; //"AI" map - runs all non duplicate code
        public Map ShipCombatTargetMap; //target map - for proj, etc.
        public Map ShipGraveyard; //map to put destroyed ships to
        public Faction ShipFaction;
        public Lord ShipLord;
        public Lord InvaderLord;
        public bool ShipCombatMaster = false; //true only on ShipCombatMasterMap
        public bool InCombat = false;
        public bool IsGraveyard = false; //temp map, will be removed in a few days
        public bool BurnUpSet = false; //force terminate map+WO if no player pawns or pods present or in flight to
        public int EngineRot;

        public float MapEnginePower;
        public int BurnTimer = 0;
        public int Heading; //+closer, -apart
        public int BuildingsCount;
        public float totalThreat;
        public float[] threatPerSegment = { 1, 1, 1, 1 };
        //SC cache
        public List<ShipCombatProjectile> Projectiles;
        public List<ShipCombatProjectile> TorpsInRange;
        public List<Building_ShipBridge> MapRootListAll = new List<Building_ShipBridge>(); //all bridges on map
        List<Building> cores = new List<Building>();

        //SC cache new
        //after spawn init all, after moveship: assign same as from map to new map
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            RecacheMap();
        }
        public bool CacheOff = true;
        //cells occupied by shipParts, if cacheoff = null, item1 = index, item2 = path, if path is -1 = wreck
        private Dictionary<IntVec3, Tuple<int, int>> shipCells;
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
        //bridgeId, ship
        private Dictionary<int, SoShipCache> shipsOnMapNew;
        public Dictionary<int, SoShipCache> ShipsOnMapNew
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
        public void ResetCache()
        {
            ShipsOnMapNew.Clear();
            foreach (IntVec3 vec in MapShipCells.Keys.ToList())
            {
                MapShipCells[vec] = new Tuple<int, int>(-1, -1);
            }
        }
        public void RecacheMap() //rebuild all ships, wrecks on map init or after ship gen
        {
            foreach (Building_ShipBridge b in MapRootListAll)
            {
                b.ShipIndex = -1;
            }
            for (int i = 0; i < MapRootListAll.Count; i++) //for each bridge make a ship, assign index
            {
                if (MapRootListAll[i].ShipIndex == -1) //skip any with valid index
                {
                    ShipsOnMapNew.Add(MapRootListAll[i].thingIDNumber, new SoShipCache());
                    ShipsOnMapNew[MapRootListAll[i].thingIDNumber].RebuildCache(MapRootListAll[i]);
                }
            }
            foreach (IntVec3 vec in MapShipCells.Keys.ToList()) //ship wrecks from leftovers
            {
                if (MapShipCells[vec].Item1 == -1)
                {
                    Thing t = vec.GetThingList(map).FirstOrDefault(b => b.TryGetComp<CompSoShipPart>() != null);
                    int mergeToIndex = t.thingIDNumber;

                    ShipsOnMapNew.Add(mergeToIndex, new SoShipCache());
                    ShipsOnMapNew[mergeToIndex].RebuildCache(t as Building);
                }
            }
            CacheOff = false;
            Log.Message("SOS2 recached on map: " + map + " Found ships: " + ShipsOnMapNew.Count);
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
            IntVec3 mergeTo = IntVec3.Zero;
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
            if (mergeTo == IntVec3.Zero) //merging to wrecks only
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
            int path = MapShipCells[mergeTo].Item2 + 1;
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
                if (!ship.IsWreck)
                    path++;
                //Log.Message("parts at i: "+ current.Count + "/" + i);
            }
            Log.Message("Attached: " + cellsDone.Count + " to ship: " + mergeToIndex);
        }
        public int ShipIndexOnVec(IntVec3 vec) //return index if ship on cell, else return -1
        {
            if (MapShipCells.ContainsKey(vec))
            {
                return MapShipCells[vec].Item1;
            }
            return -1;
        }
        public bool VecHasLS(IntVec3 vec)
        {
            int shipIndex = ShipIndexOnVec(vec);
            if (shipIndex != -1 && ShipsOnMapNew[shipIndex].LifeSupports.Any(s => s.active))
                return true;
            return false;
        }
        //SC cache new end

        public ShipHeatMapComp originMapComp;
        public ShipHeatMapComp OriginMapComp
        {
            get
            {
                if (ShipCombatOriginMap == null)
                    return null;
                if (this.originMapComp == null)
                {
                    this.originMapComp = ShipCombatOriginMap.GetComponent<ShipHeatMapComp>();
                }
                return this.originMapComp;
            }
        }
        public ShipHeatMapComp masterMapComp;
        public ShipHeatMapComp MasterMapComp
        {
            get
            {
                if (ShipCombatMasterMap == null)
                    return null;
                if (this.masterMapComp == null)
                {
                    this.masterMapComp = ShipCombatMasterMap.GetComponent<ShipHeatMapComp>();
                }
                return this.masterMapComp;
            }
        }
        //SCM only
        public bool callSlowTick = false;
        public float Range; //400 is furthest away, 0 is up close and personal
        public float RangeToKeep;
        public bool PlayerMaintain;
        public bool attackedTradeship;
        public int BuildingCountAtStart = 0;
        public bool enemyRetreating = false;
        public bool warnedAboutRetreat = false;
        public int warnedAboutAdrift = 0;
        public bool hasAnyPlayerPartDetached = false;
        public bool startedBoarderLoad = false;
        public bool launchedBoarders = false;
        public bool Scanned = false;
        public int BattleStartTick = 0;
        public int lastPDTick = 0;
        readonly float[] minRange = new[] { 0f, 60f, 110f, 160f };
        readonly float[] maxRange = new[] { 40f, 90f, 140f, 190f };

        public void SpawnEnemyShip(PassingShip passingShip, Faction faction, bool fleet, bool bounty, out List<Building> cores)
        {
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
                CR = MapThreat() * 0.9f;
                if (!Prefs.DevMode && CR > 30)
                {
                    int daysPassed = GenDate.DaysPassedSinceSettle;
                    if (daysPassed < 30) //reduce difficulty early on
                        CR *= 0.5f;
                    else if (daysPassed < 60)
                        CR *= 0.75f;
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
                if (passingShip is TradeShip)
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

                    Find.World.GetComponent<PastWorldUWO2>().PlayerFactionBounty += 5;
                    attackedTradeship = true;
                }
                else //find a random attacking ship to spawn
                {
                    if (bounty)
                        CR *= (float)Math.Pow(Find.World.GetComponent<PastWorldUWO2>().PlayerFactionBounty, 0.2);
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
                map.passingShipManager.RemoveShip(passingShip);
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
            ShipCombatMasterMap = GetOrGenerateMapUtility.GetOrGenerateMap(ShipInteriorMod2.FindWorldTile(), new IntVec3(250, 1, 250), ResourceBank.WorldObjectDefOf.ShipEnemy);
            ((WorldObjectOrbitingShip)ShipCombatMasterMap.Parent).radius = radius;
            ((WorldObjectOrbitingShip)ShipCombatMasterMap.Parent).theta = theta;
            ((WorldObjectOrbitingShip)ShipCombatMasterMap.Parent).phi = phi;
            if (passingShip is DerelictShip d)
            {
                shieldsActive = false;
                MasterMapComp.IsGraveyard = true;
                //int time = Rand.RangeInclusive(600000, 120000);
                ShipCombatMasterMap.Parent.GetComponent<TimedForcedExitShip>().StartForceExitAndRemoveMapCountdown(d.ticksUntilDeparture);
                Find.LetterStack.ReceiveLetter("ShipEncounterStart".Translate(), "ShipEncounterStartDesc".Translate(ShipCombatMasterMap.Parent.GetComponent<TimedForcedExitShip>().ForceExitAndRemoveMapCountdownTimeLeftString), LetterDefOf.NeutralEvent);
            }
            MasterMapComp.ShipFaction = faction;
            if (wreckLevel != 3)
                MasterMapComp.ShipLord = LordMaker.MakeNewLord(faction, new LordJob_DefendShip(faction, map.Center), map);
            if (fleet) //spawn fleet - not for passingShips other than trade yet
            {
                ShipInteriorMod2.GenerateFleet(CR, ShipCombatMasterMap, passingShip, faction, MasterMapComp.ShipLord, out cores, shieldsActive, false, wreckLevel, navyDef: navyDef);
                return;
            }
            else //spawn ship
            {
                //keep this for troubleshooting
                Log.Message("SOS2: spawning shipdef: " + shipDef + ", of faction: " + faction + ", of navy: " + navyDef + ", wrecklvl: " + wreckLevel);
                ShipInteriorMod2.GenerateShip(shipDef, ShipCombatMasterMap, passingShip, faction, MasterMapComp.ShipLord, out cores, shieldsActive, false, wreckLevel, navyDef: navyDef);
            }
            //post ship spawn
            //if (cores != null)
            //    Log.Message("Spawned enemy cores: " + cores.Count);
        }
        public int MapThreat()
        {
            int threat = 0;
            foreach (int index in ShipsOnMapNew.Keys)
            {
                threat += ShipsOnMapNew[index].Threat;
            }
            return threat;
        }

        public void StartShipEncounter(Building playerShipRoot, PassingShip passingShip = null, Map enemyMap = null, Faction fac = null, int range = 0, bool fleet = false, bool bounty = false)
        {
            //startup on origin
            if (playerShipRoot == null || InCombat || BurnUpSet)
            {
                Log.Message("SOS2 Error: Unable to start ship encounter.");
                return;
            }
            //origin vars
            originMapComp = null;
            masterMapComp = null;
            ShipCombatOriginMap = this.map;
            ShipFaction = this.map.Parent.Faction;
            attackedTradeship = false;
            //target or create master + spawn ship
            if (enemyMap == null)
                SpawnEnemyShip(passingShip, fac, fleet, bounty, out cores);
            else
                ShipCombatMasterMap = enemyMap;
            //master vars
            ShipCombatTargetMap = ShipCombatMasterMap;
            MasterMapComp.ShipCombatTargetMap = ShipCombatOriginMap;
            MasterMapComp.ShipCombatOriginMap = this.map;
            MasterMapComp.ShipCombatMasterMap = ShipCombatMasterMap;

            //if ship is derelict switch to "encounter"
            if (MasterMapComp.IsGraveyard)
            {
                return;
            }
            //Log.Message("Ships: " + MasterMapComp.MapRootListAll.Count + " on map: " + MasterMapComp.map);
            MasterMapComp.ShipCombatMaster = true;
            //start caches
            StartBattleCache();
            MasterMapComp.StartBattleCache();
            //set range DL:1-9
            byte detectionLevel = 7;
            var worldComp = Find.World.GetComponent<PastWorldUWO2>();
            List<Building_ShipAdvSensor> Sensors = worldComp.Sensors.Where(s => s.Map == this.map).ToList();
            List<Building_ShipAdvSensor> SensorsEnemy = worldComp.Sensors.Where(s => s.Map == MasterMapComp.map).ToList();
            if (Sensors.Where(sensor => sensor.def == ResourceBank.ThingDefOf.Ship_SensorClusterAdv && sensor.TryGetComp<CompPowerTrader>().PowerOn).Any())
            {
                //ShipCombatMasterMap.fogGrid.ClearAllFog();
                detectionLevel += 2;
            }
            else if (Sensors.Where(sensor => sensor.TryGetComp<CompPowerTrader>().PowerOn).Any())
                detectionLevel += 1;
            if (range == 0)
            {
                if (Cloaks.Where(cloak => cloak.TryGetComp<CompPowerTrader>().PowerOn).Any())
                    detectionLevel -= 2;
                if (SensorsEnemy.Where(sensor => sensor.def == ResourceBank.ThingDefOf.Ship_SensorClusterAdv && sensor.TryGetComp<CompPowerTrader>().PowerOn).Any())
                    detectionLevel -= 2;
                else if (SensorsEnemy.Any())
                    detectionLevel -= 1;
                if (MasterMapComp.Cloaks.Where(cloak => cloak.TryGetComp<CompPowerTrader>().PowerOn).Any())
                    detectionLevel -= 2;
                MasterMapComp.Range = 180 + detectionLevel * 20 + Rand.Range(0, 40);
                Log.Message("Enemy range at start: " + MasterMapComp.Range);
            }

            MasterMapComp.callSlowTick = true;
        }
        public void StartBattleCache()
        {
            //reset vars
            ShipGraveyard = null;
            InCombat = true;
            BurnUpSet = false;
            Heading = 0;
            MapEnginePower = 0;
            Projectiles = new List<ShipCombatProjectile>();
            TorpsInRange = new List<ShipCombatProjectile>();
            //SCM only
            if (ShipCombatMaster)
            {
                RangeToKeep = Range;
                PlayerMaintain = false;
                enemyRetreating = false;
                warnedAboutRetreat = false;
                warnedAboutAdrift = 0;
                hasAnyPlayerPartDetached = false;
                startedBoarderLoad = false;
                launchedBoarders = false;
                BattleStartTick = Find.TickManager.TicksGame;
            }
            foreach (int index in shipsOnMapNew.Keys) //combat start calcs per ship
            {
                var ship = shipsOnMapNew[index];
                //if (!ship.IsWreck)
                ship.BuildingCountAtCombatStart = ship.BuildingCount;
                BuildingCountAtStart += ship.BuildingCountAtCombatStart;
            }
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            if (InCombat && (this.map == ShipCombatOriginMap || this.map == ShipCombatMasterMap))
            {
                if (ShipCombatMaster)
                {
                    if (OriginMapComp.Heading == 1)
                        Range -= OriginMapComp.MapEnginePower;
                    else if (OriginMapComp.Heading == -1)
                        Range += OriginMapComp.MapEnginePower;
                    if (Heading == 1)
                        Range -= MapEnginePower;
                    else if (Heading == -1)
                        Range += MapEnginePower;
                    if (Range > 400)
                        Range = 400;
                    else if (Range < 0)
                        Range = 0;
                    //no end if player has pawns on enemy ship
                    if (Range >= 395 && enemyRetreating && !ShipCombatMasterMap.mapPawns.AnyColonistSpawned)
                    {
                        EndBattle(this.map, true);
                        return;
                    }
                }
                //deregister projectiles in own cache, spawn them on targetMap
                List<ShipCombatProjectile> toRemove = new List<ShipCombatProjectile>();
                foreach (ShipCombatProjectile proj in Projectiles)
                {
                    if (proj.range >= MasterMapComp.Range)
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
                    else if ((proj.spawnProjectile.thingClass == typeof(Projectile_TorpedoShipCombat) || proj.spawnProjectile.thingClass == typeof(Projectile_ExplosiveShipCombatAntigrain)) && !TorpsInRange.Contains(proj) && MasterMapComp.Range - proj.range < 65)
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

                //ship destruction code
                if (Find.TickManager.TicksGame % 20 == 0)
                {
                    /*foreach (int i in ShipsOnMapNew.Keys)
                    {
                        if (ShipsOnMapNew[i].CheckForDetach())
                            callSlowTick = true;
                    }*/
                    if (MapRootListAll.NullOrEmpty()) //if all ships gone, end combat
                    {
                        //Log.Message("Map defeated: " + this.map);
                        EndBattle(this.map, false);
                        return;
                    }
                }
                //SCM only: call both slow ticks
                if (ShipCombatMaster && (callSlowTick || Find.TickManager.TicksGame % 60 == 0))
                {
                    OriginMapComp.SlowTick();
                    SlowTick();
                    callSlowTick = false;
                }
            }
            else if ((Find.TickManager.TicksGame % 60 == 0) && map.gameConditionManager.ConditionIsActive(ResourceBank.GameConditionDefOf.SpaceDebris))
            {
                //reduce durration per engine vs mass
                HashSet<CompEngineTrail> engines = new HashSet<CompEngineTrail>();
                foreach (int index in ShipsOnMapNew.Keys)
                {
                    var ship = shipsOnMapNew[index];
                    if (!ship.IsWreck && ship.Engines.Any())
                    {
                        engines.Concat(ship.Engines);
                    }
                }
                if (!engines.Any())
                    return;
                MapEnginePower = 0;
                EngineRot = engines.FirstOrDefault().parent.Rotation.AsByte;
                foreach (CompEngineTrail engineComp in engines)
                {
                    if (engineComp != null && engineComp.CanFire(EngineRot) && engineComp.active)
                    {
                        MapEnginePower += engineComp.Props.thrust;
                    }
                }
                if (MapEnginePower > 0)
                {
                    MapEnginePower *= 40000f / Mathf.Pow(map.listerBuildings.allBuildingsColonist.Count * 2.5f, 1.1f);
                    Log.Message("thrust " + MapEnginePower);
                    var cond = map.gameConditionManager.ActiveConditions.FirstOrDefault(c => c is GameCondition_SpaceDebris);
                    if (BurnTimer > cond.TicksLeft)
                    {
                        cond.End();
                        BurnTimer = 0;
                        foreach (CompEngineTrail engine in engines)
                        {
                            engine.Off();
                        }
                    }
                    else
                    {
                        BurnTimer += (int)MapEnginePower;
                        //Log.Message("ticks remain " + map.gameConditionManager.ActiveConditions.FirstOrDefault(c => c is GameCondition_SpaceDebris).TicksLeft);
                    }
                }
            }
        }
        public void SlowTick()
        {
            //td ship AI
            //map AI evals ships
            //
            totalThreat = 1;
            threatPerSegment = new[] { 1f, 1f, 1f, 1f };
            int TurretNum = 0;
            MapEnginePower = 0;
            bool anyMapEngineCanActivate = false;
            BuildingsCount = 0;
            //SCM vars
            float powerCapacity = 0;
            float powerRemaining = 0;
            foreach (int index in ShipsOnMapNew.Keys) //first engine rot on proper ship
            {
                var ship = shipsOnMapNew[index];
                if (!ship.IsWreck && ship.Engines.Any())
                {
                    EngineRot = ship.Engines.FirstOrDefault().parent.Rotation.AsByte;
                    break;
                }
            }
            //threat and engine power calcs
            foreach (int index in ShipsOnMapNew.Keys)
            {
                var ship = shipsOnMapNew[index];
                if (ShipCombatMaster && !ship.IsWreck)
                {
                    foreach (var battery in ship.Core.PowerComp.PowerNet.batteryComps)
                    {
                        powerCapacity += battery.Props.storedEnergyMax;
                        powerRemaining += battery.StoredEnergy;
                    }
                    ship.PurgeCheck();
                }
                threatPerSegment.Zip(ship.ThreatPerSegment, (x, y) => x + y);
                foreach (Building_ShipTurret turret in ship.Turrets)
                {
                    int threat = turret.heatComp.Props.threat;
                    var torp = turret.TryGetComp<CompChangeableProjectilePlural>();
                    var fuel = turret.TryGetComp<CompRefuelable>();
                    if ((torp != null && !torp.Loaded) || (fuel != null && fuel.Fuel == 0f))
                    {
                        if (turret.heatComp.Props.maxRange > 150) //long
                        {
                            threatPerSegment[0] -= threat / 6f;
                            threatPerSegment[1] -= threat / 4f;
                            threatPerSegment[2] -= threat / 2f;
                            threatPerSegment[3] -= threat;
                        }
                        else if (turret.heatComp.Props.maxRange > 100) //med
                        {
                            threatPerSegment[0] -= threat / 4f;
                            threatPerSegment[1] -= threat / 2f;
                            threatPerSegment[2] -= threat;
                        }
                        else if (turret.heatComp.Props.maxRange > 50) //short
                        {
                            threatPerSegment[0] -= threat / 2f;
                            threatPerSegment[1] -= threat;
                        }
                        else //cqc
                            threatPerSegment[0] -= threat;
                    }
                    else
                    {
                        totalThreat += threat;
                        TurretNum++;
                    }
                }
                if (ship.Engines.FirstOrDefault() != null)
                    EngineRot = ship.Engines.FirstOrDefault().parent.Rotation.AsByte;
                float enginePower = ship.EnginePower(EngineRot, Heading);
                if (ship.CanMove)
                {
                    MapEnginePower += enginePower;
                    anyMapEngineCanActivate = true;
                }
                BuildingsCount += ship.Buildings.Count;
            }
            //Log.Message("Engine power: " + MapEnginePower + ", ship size: " + BuildingsCount);
            if (anyMapEngineCanActivate)
                MapEnginePower *= 40f / Mathf.Pow(BuildingsCount, 1.1f);
            else
                MapEnginePower = 0;
            //Log.Message("Engine power: " + MapEnginePower + ", ship size: " + BuildingsCount);

            //SCM only: ship AI and player distance maintain
            if (ShipCombatMaster)
            {
                if (anyMapEngineCanActivate) //set AI heading
                {
                    //True, totalThreat:1, OriginMapComp.totalThreat:1, TurretNum:0
                    //retreat
                    if (enemyRetreating || totalThreat / OriginMapComp.totalThreat < 0.4f || powerRemaining / powerCapacity < 0.2f || TurretNum == 0 || BuildingsCount / (float)BuildingCountAtStart < 0.7f || Find.TickManager.TicksGame > BattleStartTick + 90000)
                    {
                        Heading = -1;
                        enemyRetreating = true;
                        if (!warnedAboutRetreat)
                        {
                            Log.Message(enemyRetreating + ", totalThreat:" + totalThreat + ", OriginMapComp.totalThreat:" + OriginMapComp.totalThreat + ", powerRemaining:" + powerRemaining + ", powerCapacity:" + powerCapacity + ", TurretNum:" + TurretNum + ", BuildingsCount:" + BuildingsCount + ", BuildingCountAtStart:" + BuildingCountAtStart);
                            Messages.Message("EnemyShipRetreating".Translate(), MessageTypeDefOf.ThreatBig);
                            warnedAboutRetreat = true;
                        }
                    }
                    else //move to range
                    {
                        //calc ratios
                        float[] threatRatio = new[] { threatPerSegment[0] / OriginMapComp.threatPerSegment[0],
                            threatPerSegment[1] / OriginMapComp.threatPerSegment[1],
                            threatPerSegment[2] / OriginMapComp.threatPerSegment[2],
                            threatPerSegment[3] / OriginMapComp.threatPerSegment[3] };
                        float max = 0;
                        int best = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            if (threatRatio[i] == 1) //threat is 0 for both
                                threatRatio[i] = 0;
                            if (threatRatio[i] > max)
                            {
                                max = threatRatio[i];
                                best = i;
                            }
                        }
                        if (Range > maxRange[best]) //forward
                        {
                            if (Heading != 1)
                                //Log.Message("enemy ship now moving forward Threat ratios (LMSC): " + threatRatio[3].ToString("F2") + " " + threatRatio[2].ToString("F2") + " " + threatRatio[1].ToString("F2") + " " + threatRatio[0].ToString("F2"));
                            Heading = 1;
                        }
                        else if (Range <= minRange[best]) //back
                        {
                            if (Heading != -1)
                                //Log.Message("enemy ship now moving backward Threat ratios (LMSC): " + threatRatio[3].ToString("F2") + " " + threatRatio[2].ToString("F2") + " " + threatRatio[1].ToString("F2") + " " + threatRatio[0].ToString("F2"));
                            Heading = -1;
                        }
                        else //chill
                        {
                            if (Heading != 0)
                                //Log.Message("enemy ship now stopped Threat ratios (LMSC): " + threatRatio[3].ToString("F2") + " " + threatRatio[2].ToString("F2") + " " + threatRatio[1].ToString("F2") + " " + threatRatio[0].ToString("F2"));
                            Heading = 0;
                        }
                    }
                }
                else //engines dead or disabled
                {
                    if ((threatPerSegment[0] == 1 && threatPerSegment[1] == 1 && threatPerSegment[2] == 1 && threatPerSegment[3] == 1) || Find.TickManager.TicksGame > BattleStartTick + 120000)
                    {
                        //no turrets to fight with - exit
                        EndBattle(this.map, false);
                        return;
                    }
                    if (warnedAboutAdrift == 0)
                    {
                        Messages.Message(TranslatorFormattedStringExtensions.Translate("EnemyShipAdrift"), this.map.Parent, MessageTypeDefOf.NegativeEvent);
                        warnedAboutAdrift = Find.TickManager.TicksGame + Rand.RangeInclusive(60000, 100000);
                    }
                    else if (Find.TickManager.TicksGame > warnedAboutAdrift)
                    {
                        EndBattle(this.map, false, warnedAboutAdrift - Find.TickManager.TicksGame);
                        return;
                    }
                    Heading = 0;
                    enemyRetreating = false;
                }
                if (PlayerMaintain)
                {
                    if (Heading == 1) //enemy moving to player
                    {
                        if (RangeToKeep > Range)
                            OriginMapComp.Heading = -1;
                        else
                            OriginMapComp.Heading = 0;
                    }
                    else if (Heading == -1) //enemy moving from player
                    {
                        if (RangeToKeep < Range)
                            OriginMapComp.Heading = 1;
                        else
                            OriginMapComp.Heading = 0;
                    }
                    else if (Heading == 0)
                    {
                        OriginMapComp.Heading = 0;
                    }
                }
                //AI boarding code
                if ((hasAnyPlayerPartDetached || Find.TickManager.TicksGame > BattleStartTick + 5000) && !startedBoarderLoad && !enemyRetreating)
                {
                    List <CompTransporter> transporters = new List<CompTransporter>();
                    float transporterMass = 0;
                    foreach (Thing t in this.map.listerThings.ThingsInGroup(ThingRequestGroup.Transporter))
                    {
                        if (t.Faction == Faction.OfPlayer) continue;

                        var transporter = t.TryGetComp<CompTransporter>();
                        if (transporter == null) continue;

                        if (t.def.defName.Equals("PersonalShuttle"))
                        {
                            transporters.Add(transporter);
                            transporterMass += transporter.Props.massCapacity;
                        }
                    }
                    foreach (Pawn p in this.map.mapPawns.AllPawnsSpawned)
                    {
                        if (p.Faction != Faction.OfPlayer && transporterMass >= p.RaceProps.baseBodySize * 70 && p.Faction != Faction.OfPlayer && p.mindState.duty != null && p.kindDef.combatPower > 40)
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
                    startedBoarderLoad = true;
                }
                if (startedBoarderLoad && !launchedBoarders && !enemyRetreating)
                {
                    //abort and reset if player on ship
                    if (this.map.mapPawns.AllPawnsSpawned.Any(o => o.Faction == Faction.OfPlayer))
                    {
                        foreach (Thing t in this.map.listerThings.ThingsInGroup(ThingRequestGroup.Transporter).Where(tr => tr.Faction != Faction.OfPlayer))
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
                        foreach (Pawn p in this.map.mapPawns.AllPawnsSpawned.Where(o => o.Faction != Faction.OfPlayer))
                        {
                            if (p.mindState?.duty?.transportersGroup == 0 && p.MannedThing() == null)
                                allOnPods = false;
                        }
                        if (allOnPods) //launch
                        {
                            List<CompShuttleLaunchable> transporters = new List<CompShuttleLaunchable>();
                            foreach (Thing t in this.map.listerThings.ThingsInGroup(ThingRequestGroup.Transporter).Where(tr => tr.Faction != Faction.OfPlayer))
                            {
                                var transporter = t.TryGetComp<CompTransporter>();
                                if (!(transporter?.innerContainer.Any ?? false)) continue;

                                var launchable = t.TryGetComp<CompShuttleLaunchable>();
                                if (launchable == null) continue;

                                transporters.Add(launchable);
                            }
                            if (transporters.Count > 0)
                            {
                                transporters[0].TryLaunch(ShipCombatOriginMap.Parent, new TransportPodsArrivalAction_ShipAssault(ShipCombatOriginMap.Parent));
                                OriginMapComp.ShipLord = LordMaker.MakeNewLord(ShipFaction, new LordJob_AssaultShip(ShipFaction, false, false, false, true, false), ShipCombatOriginMap, new List<Pawn>());
                            }
                            launchedBoarders = true;
                        }
                    }
                }
            }
        }
        public void RemoveShipFromBattle(int shipIndex, Building b = null, Faction fac = null)
        {
            if (ShipsOnMapNew.Count > 1) //move to graveyard if not last ship
            {
                if (b == null)
                {
                    b = ShipsOnMapNew[shipIndex].Parts.FirstOrDefault();
                }
                if (b != null)
                {
                    if (ShipGraveyard == null)
                        SpawnGraveyard();
                    ShipInteriorMod2.MoveShip(b, ShipGraveyard, new IntVec3(0, 0, 0), fac);
                }
            }
            else if (fac != null) //last ship hacked
            {
                ShipsOnMapNew[shipIndex].Capture(fac);
            }
            Log.Message("Ships remaining: " + ShipsOnMapNew.Count);
        }
        public void SpawnGraveyard() //if not present, create a graveyard
        {
            float adj;
            if (ShipCombatMaster)
                adj = Rand.Range(-0.075f, -0.125f);
            else
                adj = Rand.Range(0.025f, 0.075f);
            ShipGraveyard = GetOrGenerateMapUtility.GetOrGenerateMap(ShipInteriorMod2.FindWorldTile(), this.map.Size, ResourceBank.WorldObjectDefOf.WreckSpace);
            ShipGraveyard.fogGrid.ClearAllFog();
            ((WorldObjectOrbitingShip)ShipGraveyard.Parent).radius = 150;
            ((WorldObjectOrbitingShip)ShipGraveyard.Parent).theta = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).theta + adj;
            ((WorldObjectOrbitingShip)ShipGraveyard.Parent).phi = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).phi - 0.01f + 0.001f * Rand.Range(0, 20);
            var graveMapComp = ShipGraveyard.GetComponent<ShipHeatMapComp>();
            graveMapComp.IsGraveyard = true;
            if (ShipCombatMaster)
            {
                graveMapComp.ShipFaction = MasterMapComp.ShipFaction;
                graveMapComp.ShipCombatMasterMap = this.map;
                graveMapComp.ShipCombatOriginMap = graveMapComp.MasterMapComp.ShipCombatOriginMap;
                //graveMapComp.ShipLord = LordMaker.MakeNewLord(ShipFaction, new LordJob_DefendShip(ShipFaction, map.Center), map);
            }
            else
            {
                graveMapComp.ShipFaction = OriginMapComp.ShipFaction;
                graveMapComp.ShipCombatOriginMap = this.map;
                graveMapComp.ShipCombatMasterMap = graveMapComp.OriginMapComp.ShipCombatMasterMap;
            }
        }
        public void ShipBuildingsOff()
        {
            foreach (Building b in ShipCombatOriginMap.listerBuildings.allBuildingsColonist)
            {
                if (b.TryGetComp<CompEngineTrail>() != null)
                    b.TryGetComp<CompEngineTrail>().Off();
            }
            foreach (Building b in ShipCombatMasterMap.listerBuildings.allBuildingsColonist) //hacked last ship off
            {
                if (b.TryGetComp<CompEngineTrail>() != null)
                    b.TryGetComp<CompEngineTrail>().Off();
            }
            foreach (Building b in ShipCombatMasterMap.listerBuildings.allBuildingsNonColonist)
            {
                if (b.TryGetComp<CompEngineTrail>() != null)
                    b.TryGetComp<CompEngineTrail>().Off();
                else if (b.TryGetComp<CompShipCombatShield>() != null)
                    b.TryGetComp<CompFlickable>().SwitchIsOn = false;
            }
            foreach (Thing t in this.map.listerThings.ThingsInGroup(ThingRequestGroup.Transporter).Where(tr => tr.Faction != Faction.OfPlayer))
            {
                var transporter = t.TryGetComp<CompTransporter>();
                if (transporter.innerContainer.Any || transporter.AnythingLeftToLoad)
                    transporter.CancelLoad();
            }
        }
        public void EndBattle(Map loser, bool fled, int burnTimeElapsed = 0)
        {
			//td destroy all proj
            OriginMapComp.InCombat = false;
            MasterMapComp.InCombat = false;
            OriginMapComp.ShipBuildingsOff();
            MasterMapComp.ShipCombatMaster = false;
            if (OriginMapComp.ShipGraveyard != null)
                OriginMapComp.ShipGraveyard.Parent.GetComponent<TimedForcedExitShip>().StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(120000, 240000) - burnTimeElapsed);
            if (MasterMapComp.ShipGraveyard != null)
                MasterMapComp.ShipGraveyard.Parent.GetComponent<TimedForcedExitShip>().StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(120000, 240000) - burnTimeElapsed);
            if (loser == ShipCombatMasterMap)
            {
                if (!fled) //master lost
                {
                    MasterMapComp.IsGraveyard = true;
                    if (OriginMapComp.attackedTradeship)
                        Find.World.GetComponent<PastWorldUWO2>().PlayerFactionBounty += 15;
                    ShipCombatMasterMap.Parent.GetComponent<TimedForcedExitShip>().StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(120000, 240000) - burnTimeElapsed);
                    Find.LetterStack.ReceiveLetter("WinShipBattle".Translate(), "WinShipBattleDesc".Translate(ShipCombatMasterMap.Parent.GetComponent<TimedForcedExitShip>().ForceExitAndRemoveMapCountdownTimeLeftString), LetterDefOf.PositiveEvent);
                }
                else //master fled
                {
                    MasterMapComp.BurnUpSet = true;
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("EnemyShipRetreated"), MessageTypeDefOf.ThreatBig);
                }
            }
            else
            {
                if (!fled) //origin lost
                {
                    ShipCombatOriginMap.Parent.GetComponent<TimedForcedExitShip>()?.StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(60000, 120000));
                    //Find.GameEnder.CheckOrUpdateGameOver();
                }
                //origin fled or lost: if origin has grave with a ship, start combat on it
                if (OriginMapComp.ShipGraveyard != null && !OriginMapComp.attackedTradeship)
                {
                    Building bridge = OriginMapComp.ShipGraveyard.listerBuildings.allBuildingsColonist.Where(x => x is Building_ShipBridge).FirstOrDefault();
                    if (bridge != null)
                    {
                        //OriginMapComp.IsGraveyard = true;
                        OriginMapComp.ShipGraveyard.GetComponent<ShipHeatMapComp>().StartShipEncounter(bridge, null, ShipCombatMasterMap);
                    }
                }
                else //origin fled or lost with no graveyard
                {
                    MasterMapComp.BurnUpSet = true;
                }
            }
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
