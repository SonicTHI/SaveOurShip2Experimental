﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse.AI.Group;
using Vehicles;
using SaveOurShip2.Vehicles;
using Verse.AI;

namespace SaveOurShip2
{
	//ship map state, only use on space maps
	public enum ShipMapState : byte
	{
		nominal, //stable, maintained orbit - player home ship only
		inCombat, //fighting another ship - player or enemy ship
		isGraveyard, //will fall to the ground
		inTransit, //moving from/to planet surface
		inEvent, //events that have movement - meteors
		burnUpSet //force terminate map+WO if no player pawns or pods present or in flight to
	}
	public enum ShipAI : byte
	{
		none,
		normal, //aggressive, consider weapons vs enemy
		carrier, //try to stay just out of torp range
		avoidant //flee is possible, else fight - traders, etc.
	}

	public class ShipMapComp : MapComponent, IThingHolder //It's an IThingHolder because it holds shuttles while they're on missions
	{
		public List<ShipHeatNet> cachedNets = new List<ShipHeatNet>();
		public List<CompShipHeat> cachedPipes = new List<CompShipHeat>();

		public int[] grid;
		public bool heatGridDirty;
		public bool loaded = false;

		public ShipMapComp(Map map) : base(map)
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
			//Log.Message("Recaching all heatnets");
			//temp save heat to sinks
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
				HashSet<CompShipHeat> batch = new HashSet<CompShipHeat>() { comp };
				AccumulateToNetNew(batch, net);
				list.Add(net);
			}
			cachedNets = list;

			base.map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Buildings);
			base.map.mapDrawer.WholeMapChanged(MapMeshFlagDefOf.Things);
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
		readonly float[] minRange = new[] { 0f, 60f, 110f, 160f, 310f };
		readonly float[] maxRange = new[] { 40f, 90f, 140f, 190f, 330f };
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look<Faction>(ref ShipFaction, "ShipFaction");
			Scribe_References.Look<Lord>(ref ShipLord, "ShipLord");
			Scribe_References.Look<Lord>(ref InvaderLord, "InvaderLord");
			Scribe_References.Look<Map>(ref NextTargetMap, "NextTargetMap");
			Scribe_References.Look<Map>(ref ShipGraveyard, "ShipCombatGraveyard");
			Scribe_References.Look<Map>(ref GraveOrigin, "GraveOrigin");

			Scribe_Values.Look<IntVec3>(ref MoveToVec, "MoveToVec");
			Scribe_References.Look<Map>(ref MoveToMap, "MoveToMap");
			Scribe_Values.Look<int>(ref MoveToTile, "MoveToTile");
			Scribe_References.Look<Map>(ref PrevMap, "PrevMap");
			Scribe_Values.Look<int>(ref PrevTile, "PrevTile");
			Scribe_Values.Look<bool>(ref Takeoff, "Takeoff");

			Scribe_Values.Look<ShipMapState>(ref ShipMapState, "ShipMapState", 0);
			Scribe_Values.Look<bool>(ref EnginesOn, "ToggleEngines", false);
			Scribe_Values.Look<float>(ref Altitude, "Altitude", 1000);
			Scribe_Values.Look<int>(ref Heading, "Heading");
			Scribe_Values.Look<int>(ref BurnTimer, "BurnTimer");
			Scribe_Values.Look<int>(ref LastAttackTick, "LastShipBattleTick", 0);
			Scribe_Values.Look<int>(ref LastBountyRaidTick, "LastBountyRaidTicks", 0);
			Scribe_Collections.Look<Building_ShipAirlock>(ref Docked, "Docked", LookMode.Reference);
			if (ShipMapState == ShipMapState.inCombat)
			{
				//SC only - both maps
				targetMapComp = null;
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
				Scribe_Values.Look<float>(ref Difficulty, "Difficulty");
				Scribe_Values.Look<float>(ref Range, "Range");
				Scribe_Values.Look<bool>(ref attackedTradeship, "attackedTradeship");
				Scribe_Values.Look<bool>(ref callSlowTick, "callSlowTick");

				//SC only - target only
				Scribe_Values.Look<ShipAI>(ref ShipMapAI, "ShipMapAI", 0);
				Scribe_Values.Look<int>(ref BattleStartTick, "BattleStartTick");
				Scribe_Values.Look<bool>(ref Retreating, "Retreating");
				Scribe_Values.Look<bool>(ref warnedAboutRetreat, "warnedAboutRetreat");
				Scribe_Values.Look<int>(ref warnedAboutAdrift, "warnedAboutAdrift");
				Scribe_Values.Look<bool>(ref hasAnyPartDetached, "hasAnyPartDetached");
				Scribe_Values.Look<bool>(ref startedBoarderLoad, "StartedBoarding");
				Scribe_Values.Look<bool>(ref launchedBoarders, "LaunchedBoarders");
				Scribe_Values.Look<bool>(ref startedPilotLoad, "StartedShuttleMissions");
				Scribe_Collections.Look<VehiclePawn>(ref shuttlesYetToLaunch, "ShuttlesYetToLaunch", LookMode.Reference);
				Scribe_Collections.Look<VehiclePawn>(ref shuttlesWantingBoarders, "ShuttlesWantingBoarders", LookMode.Reference);

				Scribe_Collections.Look<Building_ShipBridge>(ref MapRootListAll, "MapRootListAll", LookMode.Reference); //td rem?
				Scribe_Deep.Look(ref ShuttlesOnMissions, "ShuttlesOnMissions", this);
				Scribe_Collections.Look<ShuttleMissionData>(ref ShuttleMissions, "ShuttleMissions", LookMode.Deep);
			}
		}
		//SC only - both maps
		public Faction ShipFaction;
		public bool Scanned = false; //target map has been fully scanned - prevent further scanner checks
		public int lastPDTick = 0; //mapwide PD tick delay
		public int BuildingCountAtStart = 0; //AI retreat param
		public int BuildingsCount;
		public float MapEnginePower;
		public bool Maintain = false; //map will try to maintain RangeToKeep
		public float RangeToKeep;
		public float totalThreat = 1;
		public float[] threatPerSegment = { 1, 1, 1, 1 };
		public HashSet<int> ShipsToMove = new HashSet<int>(); //move to grave or end combat
		public List<int> ShipsToRemove = new List<int>(); //remove from cache on tick
		public List<ShipCombatProjectile> Projectiles;
		public List<ShipCombatProjectile> TorpsInRange;
		public List<VehiclePawn> ShuttlesInRange;
		public Map ShipCombatOriginMap; //"player" map - initializes combat vars, runs all non duplicate code, AI
		private ShipMapComp originMapComp;
		public ThingOwner<VehiclePawn> ShuttlesOnMissions = new ThingOwner<VehiclePawn>();
		public List<ShuttleMissionData> ShuttleMissions = new List<ShuttleMissionData>();
		public ShipMapComp OriginMapComp
		{
			get
			{
				if (ShipCombatOriginMap == null)
					return null;
				if (originMapComp == null)
				{
					originMapComp = ShipCombatOriginMap.GetComponent<ShipMapComp>();
				}
				return originMapComp;
			}
		}
		public Map ShipCombatTargetMap; //target map - if there is one, we are in combat, for proj, etc.
		private ShipMapComp targetMapComp;
		public ShipMapComp TargetMapComp
		{
			get
			{
				if (ShipCombatTargetMap == null)
					return null;
				if (targetMapComp == null)
				{
					targetMapComp = ShipCombatTargetMap.GetComponent<ShipMapComp>();
				}
				return targetMapComp;
			}
		}

		//SC only - origin only
		public float Difficulty; //current battle difficulty factor taken from settings
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
		public ShipAI ShipMapAI = 0; //target ship map AI
		public IntVec3 ShuttleTarget = IntVec3.Invalid;
		public bool HasShipMapAI => ShipMapAI != ShipAI.none; //target has ship map AI
		//public Thing TurretTarget; //AI target for turrets
		public int BattleStartTick = 0; //AI retreat param, stalemate eject
		public bool Retreating = false; //AI is retreating
		public bool warnedAboutRetreat = false; //AI warned player
		public int warnedAboutAdrift = 0; //tick player was warned AI is adrift
		public bool hasAnyPartDetached = false; //AI is loosing ship parts, will load shuttles //td rework
		public bool startedBoarderLoad = false; //AI started loading
		public bool launchedBoarders = false; //AI launched
		public bool startedPilotLoad = false; //AI started moving pawns to pilot shuttles
		public List<VehiclePawn> shuttlesYetToLaunch = new List<VehiclePawn>(); //Shuttles which haven't launched yet
		public List<VehiclePawn> shuttlesWantingBoarders = new List<VehiclePawn>(); //Shuttles which have been requested to launch boarding actions
		void ResetShipAI()
		{
			//TurretTarget = TargetMapComp.MapRootListAll.RandomElement();
			BattleStartTick = Find.TickManager.TicksGame;
			Retreating = false;
			warnedAboutRetreat = false;
			warnedAboutAdrift = 0;
			hasAnyPartDetached = false;
			startedBoarderLoad = false;
			launchedBoarders = false;
			startedPilotLoad = false;
			shuttlesYetToLaunch = new List<VehiclePawn>();
			shuttlesWantingBoarders = new List<VehiclePawn>();
		}

		//all maps
		public Lord ShipLord; //AI ship lord - defends or attacks
		public Lord InvaderLord; //second AI ship lord for wreck second facton
		public Map NextTargetMap; //if any, will trigger battle after 10s
		public Map ShipGraveyard; //map to put destroyed ships to
		public Map GraveOrigin; //set on grave creation, check if parent is in combat

		//atmospheric move
		public IntVec3 MoveToVec; //vec to move to after altitude reached
		public Map MoveToMap; //ship move after altitude reached
		public int MoveToTile; //if ground target map is closed, find new valid LZ near this
		public Map PrevMap; //on takeoff, fallback to MoveToMap
		public int PrevTile; //on takeoff, fallback to MoveToTile
		public bool Takeoff; //started from planet
		public float Altitude = ShipInteriorMod2.altitudeNominal;
		public int Heading; //in combat: +closer, -apart, OOC: +up, 0down, -forcedown
		public bool IsGraveOriginInCombat
		{
			get
			{
				if (GraveOrigin == null)
					return false;
				if (GraveOrigin.GetComponent<ShipMapComp>().ShipMapState == ShipMapState.inCombat)
					return true;
				return false;
			}
		}
		public ShipMapState ShipMapState; //new state system
		public bool EnginesOn = false; //OOC for events
		public int BurnTimer = 0; //OOC for events
		public WorldObjectOrbitingShip mapParent => map.Parent as WorldObjectOrbitingShip;
		public bool IsPlayerShipMap => map.Parent.def == ResourceBank.WorldObjectDefOf.ShipOrbiting;
		public ShipMapComp GraveComp => ShipGraveyard.GetComponent<ShipMapComp>();
		public int engineRot = -1;
		public int EngineRot //reset after any engine despawns and there are other engines present
		{
			get
			{
				if (engineRot == -1)
				{
					//engine that can fire on proper ship, any engine on non wreck, default left
					List<SpaceShipCache> shipsEng = ShipsOnMap.Values.Where(s => s.CanFire()).ToList();
					if (shipsEng.Any())
						engineRot = shipsEng.First().Rot;
					else if (ShipsOnMap.Values.Any(s => !s.IsWreck && s.Engines.Any()))
						engineRot = ShipsOnMap.Values.First(s => !s.IsWreck && s.Engines.Any()).Rot;
					else if (ShipsOnMap.Values.Any(s => s.Engines.Any()))
						engineRot = ShipsOnMap.Values.First(s => s.Engines.Any()).Rot;
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
			foreach (Building_ShipAirlock b in Docked.ToList()) //Copy to avoid changing list
			{
				if (ShipIndexOnVec(b.Position) == index)
					b.DeSpawnDock();
			}
		}

		//map caches
		public List<Building_ShipBridge> MapRootListAll = new List<Building_ShipBridge>(); //all bridges on map
		public List<CompShipHeatShield> Shields = new List<CompShipHeatShield>(); //workjob, hit detect
		public List<Building_ShipCloakingDevice> Cloaks = new List<Building_ShipCloakingDevice>(); //td get this into shipcache?
		public List<Building_ShipTurretTorpedo> TorpedoTubes = new List<Building_ShipTurretTorpedo>(); //workjob
		public List<CompBuildingConsciousness> Spores = new List<CompBuildingConsciousness>(); //workjob
		public List<CompShipBay> Bays = new List<CompShipBay>(); //landing checks
		public HashSet<IntVec3> MapExtenderCells = new HashSet<IntVec3>(); //extender EVA checks
		public List<CompEngineTrail> MaxSalvageWeightOnMap(out int maxMass, out float fuel) //for moving/stabilizing wrecks
		{
			List<CompEngineTrail> engines = new List<CompEngineTrail>();
			maxMass = 0;
			fuel = 0;
			foreach (SpaceShipCache ship in ShipsOnMap.Values)
			{
				if (ship.CanFire() && ship.HasMannedBridge() && ship.HasRCS())
				{
					foreach (CompEngineTrail engine in ship.Engines.Where(e => e.FuelUse > 0))
					{
						fuel += engine.refuelComp.Fuel;
						if (engine.PodFueled)
							fuel += engine.refuelComp.Fuel;
						engines.Add(engine);
					}
					foreach (CompShipBay bay in ship.Bays.Where(t => t is CompShipBaySalvage))
					{
						maxMass += ((CompShipBaySalvage)bay).SalvageWeight;
					}
				}
			}
			return engines;
		}

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
		private Dictionary<int, SpaceShipCache> shipsOnMap;
		public Dictionary<int, SpaceShipCache> ShipsOnMap //cache of ships (bridgeId, ship)
		{
			get
			{
				if (shipsOnMap == null)
				{
					shipsOnMap = new Dictionary<int, SpaceShipCache>();
				}
				return shipsOnMap;
			}
		}

        public IThingHolder ParentHolder => map;

        public void ResetCache()
		{
			foreach (IntVec3 vec in MapShipCells.Keys.ToList())
			{
				MapShipCells[vec] = new Tuple<int, int>(-1, -1);
			}
		}
		public void RepathMap() //repath all ships, on start player and end all maps
		{
			foreach (SpaceShipCache ship in ShipsOnMap.Values.Where(s => !s.IsWreck))
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
			ShipsOnMap.Clear();
			for (int i = 0; i < MapRootListAll.Count; i++) //for each bridge make a ship, assign index
			{
				if (MapRootListAll[i].ShipIndex == -1) //skip any with valid index
				{
					ShipInteriorMod2.WorldComp.AddNewShip(ShipsOnMap, MapRootListAll[i]);
				}
			}
			List<IntVec3> invalidCells = new List<IntVec3>(); //might happen with wrecks - temp solution
			foreach (IntVec3 vec in MapShipCells.Keys.ToList()) //ship wrecks from leftovers
			{
				if (MapShipCells[vec].Item1 == -1)
				{
					Thing t = vec.GetThingList(map).FirstOrDefault(b => b.TryGetComp<CompShipCachePart>() != null);
					if (t == null)
					{
						invalidCells.Add(vec);
						continue;
					}
					ShipInteriorMod2.WorldComp.AddNewShip(ShipsOnMap, t as Building);
				}
			}
			if (invalidCells.Any())
			{
				Log.Warning("SOS2: ".Colorize(Color.cyan) + map + " Recaching found ".Colorize(Color.red) + invalidCells.Count + " invalid cells! FIXING.");
				foreach (IntVec3 vec in invalidCells)
				{
					MapShipCells.Remove(vec);
				}
			}
			CacheOff = false;
			Log.Message("SOS2: ".Colorize(Color.cyan) + map + " Recached,".Colorize(Color.green) + " Found ships: " + ShipsOnMap.Count);
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
				if (!ShipsOnMap[i].IsWreck && ShipsOnMap[i].Mass > mass)
				{
					mass = ShipsOnMap[i].Mass;
					mergeToIndex = ShipsOnMap[i].Index;
					origin = ShipsOnMap[i].Core;
				}
			}
			if (mergeToIndex == -1) //merging to wrecks only
			{
				foreach (int i in indexes)
				{
					if (ShipsOnMap[i].Mass > mass)
					{
						mass = ShipsOnMap[i].Mass;
						mergeToIndex = ShipsOnMap[i].Index;
						origin = ShipsOnMap[i].Buildings.First();
					}
				}
			}
			foreach (int i in ships) //delete all ships
			{
				ShipsOnMap.Remove(i);
			}
			//full rebuild
			ShipInteriorMod2.WorldComp.AddNewShip(ShipsOnMap, origin);
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
				if (shipIndex != -1 && ShipsOnMap[shipIndex].Mass > mass)
				{
					mergeTo = vec;
					mass = ShipsOnMap[shipIndex].Mass;
				}
			}
			if (mergeTo == IntVec3.Invalid) //merging to wrecks only
			{
				foreach (IntVec3 vec in cellsToMerge)
				{
					int shipIndex = ShipIndexOnVec(vec);
					if (ShipsOnMap.ContainsKey(shipIndex) && ShipsOnMap[shipIndex].Mass > mass)
					{
						mergeTo = vec;
						mass = ShipsOnMap[shipIndex].Mass;
					}
				}
			}
			mergeToIndex = MapShipCells[mergeTo].Item1;
			ships.Remove(mergeToIndex);
			foreach (int i in ships) //delete all ships except mergeto
			{
				ShipsOnMap.Remove(i);
			}
			AttachAll(mergeTo, mergeToIndex);
		}
		public void AttachAll(IntVec3 mergeTo, int mergeToIndex) //merge and build corePath if ship
		{
			SpaceShipCache ship = ShipsOnMap[mergeToIndex];
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
			if (ShipsOnMap.ContainsKey(index))
			{
				Log.Warning("SOS2: ".Colorize(Color.cyan) + map + " Ship ".Colorize(Color.green) + index + " Removed from cache.");
				ShipsOnMap.Remove(index);
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
		public bool VecHasLS(IntVec3 vec)
		{
			int shipIndex = ShipIndexOnVec(vec);
			if ((shipIndex > 0 && ShipsOnMap[shipIndex].LifeSupports.Any(s => s.active)) || MapExtenderCells.Contains(vec))
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
							if (shipIndex > 0 && ShipsOnMap[shipIndex].LifeSupports.Any(s => s.active) && room.ContainsCell(b.ventTo))
								return true;
						}
					}
				}
			}
			return false;
		}
		public bool CanClaimNow(Faction faction, bool countDormantPawnsAsHostile = false, bool canBeFogged = false)
		{
			foreach (IAttackTarget item in map.attackTargetsCache.TargetsHostileToFaction(faction))
			{
				if (GenHostility.IsActiveThreatTo(item, faction) && !(item.Thing is VehiclePawn))
				{
					return false;
				}

				Pawn pawn;
				if (countDormantPawnsAsHostile && item.Thing.HostileTo(faction) && (canBeFogged || !item.Thing.Fogged()) && !item.ThreatDisabled(null) && (pawn = item.Thing as Pawn) != null && !(pawn is VehiclePawn))
				{
					CompCanBeDormant comp = pawn.GetComp<CompCanBeDormant>();
					if (comp != null && !comp.Awake)
					{
						return false;
					}
				}
			}
			return true;
		}
		public void Claim()
		{
			Log.Message("SOS2 autofire claim");
			List<Building> buildings = new List<Building>();
			List<Pawn> animals = new List<Pawn>();
			List<Thing> things = new List<Thing>();
			List<VehiclePawn> shuttles = new List<VehiclePawn>();
			foreach (Thing t in map.listerThings.AllThings)
			{
				if (t is Building b && b.def.CanHaveFaction && b.Faction != Faction.OfPlayer)
					buildings.Add(b);
				else if (t is VehiclePawn p)
					shuttles.Add(p);
				else if (t is Pawn pawn && pawn.IsNonMutantAnimal)
					animals.Add(pawn);
				else if (t is DetachedShipPart)
					things.Add(t);
			}
			if (buildings.Any())
			{
				foreach (Building b in buildings)
				{
					if (!b.def.CanHaveFaction)
						continue;
					if (b is Building_Storage s)
						s.settings.filter.SetDisallowAll();
					b.SetFaction(Faction.OfPlayer);
				}
				Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.ClaimWrecksSuccess", buildings.Count), null, MessageTypeDefOf.PositiveEvent);
			}
			//remove floating tiles
			foreach (Thing t in things)
			{
				t.Destroy();
			}
			foreach (VehiclePawn shuttle in shuttles)
			{
				shuttle.DisembarkAll();
				shuttle.SetFaction(Faction.OfPlayer);
				shuttle.ignition.Drafted = false;
			}
			foreach (Pawn animal in animals)
            {
				if (animal.RaceProps.wildness <= 0.5f)
					animal.SetFaction(Faction.OfPlayer);
				else
					animal.SetFaction(null);
            }
			map.fogGrid.ClearAllFog();
		}
		//battle start
		public int MapThreat()
		{
			int threat = 0;
			foreach (SpaceShipCache ship in ShipsOnMap.Values)
			{
				threat += ship.Threat;
			}
			foreach (VehiclePawn veh in map.mapPawns.AllPawnsSpawned.Where(pawn => pawn is VehiclePawn veh && veh.CompVehicleLauncher != null && veh.CompVehicleLauncher.SpaceFlight))
			{
				int size = 20;
				if (veh.GetStatValue(VehicleStatDefOf.CargoCapacity) > 2000) //huge
					size = 80;
				else if (veh.GetStatValue(VehicleStatDefOf.CargoCapacity) > 1000) //large
					size = 40;
				threat += size;
			}
			return threat;
		}
		public void StartShipEncounter(PassingShip passingShip = null, Map targetMap = null, Faction fac = null, int range = 0, bool fleet = false, bool bounty = false)
		{
			//startup on origin
			if (ShipMapState != ShipMapState.nominal || MapRootListAll.NullOrEmpty())
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
				ShipCombatTargetMap = SpawnEnemyShipMap(passingShip, fac, fleet, bounty);
			else
				ShipCombatTargetMap = targetMap;
			//if ship is derelict switch to "encounter"
			if (TargetMapComp.ShipMapState == ShipMapState.isGraveyard)
			{
				ShipCombatTargetMap = null; //td no ship combat vs no ship maps, for now
				targetMapComp = null;
				return;
			}
			Log.Message("SOS2: ".Colorize(Color.cyan) + map + " Starting combat vs map: ".Colorize(Color.green) + ShipCombatTargetMap);
			TargetMapComp.ShipCombatTargetMap = ShipCombatOriginMap;
			TargetMapComp.ShipCombatOriginMap = ShipCombatOriginMap;
			//start caches
			RepathMap();
			ResetCombatVars();
			TargetMapComp.ResetCombatVars();

			if (range == 0) //set range DL:1-9
				DetermineInitialRange(passingShip != null);
			Log.Message("SOS2: ".Colorize(Color.cyan) + map + " Enemy range at start: " + Range);

			//callSlowTick = true;
		}
		public Map SpawnEnemyShipMap(PassingShip passingShip, Faction faction, bool fleet, bool bounty)
		{
			Map newMap = new Map();
			List<Building> cores = new List<Building>();
			ShipDef shipDef = null;
			NavyDef navyDef = null;
			int wreckLevel = 0;
			bool fakeWreck = false;
			bool shieldsActive = true;
			bool isDerelict = false;
			float CR = 0;
			float radius = 150f;
			float theta = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).Theta - 0.1f + 0.002f * Rand.Range(0, 20);
			float phi = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).Phi - 0.01f + 0.001f * Rand.Range(-20, 20);

			if (passingShip is AttackableShip attackableShip)
			{
				shipDef = attackableShip.attackableShip;
				navyDef = attackableShip.spaceNavyDef;
				faction = attackableShip.shipFaction;
			}
			else if (passingShip is DerelictShip derelictShip)
			{
				isDerelict = true;
				shipDef = derelictShip.derelictShip;
				navyDef = derelictShip.spaceNavyDef;
				faction = derelictShip.shipFaction;
				if (derelictShip.wreckLevel == 2 && !derelictShip.derelictShip.neverRandom && (derelictShip.derelictShip.neverAttacks && Rand.Chance(0.05f) || Rand.Chance(0.2f))) //fake wreck chance
				{
					fakeWreck = true;
					if (Rand.Chance(0.1f))
						wreckLevel = 0;
					else
						wreckLevel = 1;
				}
				else
				{
					wreckLevel = derelictShip.wreckLevel;
					theta = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).Theta + (0.05f + 0.002f * Rand.Range(0, 40)) * (Rand.Bool ? 1 : -1);
				}
			}
			else //using player ship combat rating
			{
				Difficulty = (float)ModSettings_SoS.difficultySoS;
				CR = MapThreat() * Difficulty;
				if (CR < 30) //minimum rating
					CR = 30;
				else //reduce difficulty early or at low rating
				{
					int daysPassed = GenDate.DaysPassedSinceSettle;
					if (Prefs.DevMode)
						CR *= 0.9f;
					else if (daysPassed < 30)
						CR *= 0.6f;
					else if (daysPassed < 60 || (CR < 500 && (!fleet || passingShip == null)))
						CR *= 0.8f;
					else
						CR *= 0.9f;
				}
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
						shipDef = ShipInteriorMod2.RandomValidShipFrom(navyDef.spaceShipDefs, CR, false, true);
				}
				else if (passingShip is TradeShip)
				{
					//find suitable navyDef
					faction = passingShip.Faction;
					if (faction != null && DefDatabase<NavyDef>.AllDefs.Any(n => n.factionDefs.Contains(faction.def) && n.spaceShipDefs.Any(s => s.tradeShip)))
					{
						navyDef = DefDatabase<NavyDef>.AllDefs.Where(n => n.factionDefs.Contains(faction.def)).RandomElement();
						if (!fleet)
							shipDef = ShipInteriorMod2.RandomValidShipFrom(navyDef.spaceShipDefs, CR, true, true);
					}
					else if (!fleet) //navy has no trade ships - use default ones
					{
						shipDef = ShipInteriorMod2.RandomValidShipFrom(DefDatabase<ShipDef>.AllDefs.ToList(), CR, true, false);
					}
					ShipInteriorMod2.WorldComp.PlayerFactionBounty += 5;
					attackedTradeship = true;
				}
				else //find a random attacking ship to spawn
				{
					if (bounty)
						CR *= (float)Math.Pow(ShipInteriorMod2.WorldComp.PlayerFactionBounty, 0.2);
					//spawned with faction override - try to find a valid navy
					if (faction != null && DefDatabase<NavyDef>.AllDefs.Any(n => n.factionDefs.Contains(faction.def)))
					{
						navyDef = DefDatabase<NavyDef>.AllDefs.Where(n => n.factionDefs.Contains(faction.def)).RandomElement();
						shipDef = ShipInteriorMod2.RandomValidShipFrom(navyDef.spaceShipDefs, CR, false, true);
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
							shipDef = ShipInteriorMod2.RandomValidShipFrom(navyDef.spaceShipDefs, CR, false, true);
						}
					}
					if (faction == null || shipDef == null) //no navy, faction or fallback
					{
						navyDef = null;
						if (!fleet)
							shipDef = ShipInteriorMod2.RandomValidShipFrom(DefDatabase<ShipDef>.AllDefs.ToList(), CR, false, false);
					}
					if (shipDef != null)
						Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.CombatStart"), TranslatorFormattedStringExtensions.Translate("SoS.CombatStartDesc", shipDef.label), LetterDefOf.ThreatBig);
					else
						Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.CombatStart"), TranslatorFormattedStringExtensions.Translate("SoS.CombatFleetDesc"), LetterDefOf.ThreatBig);
				}
			}
			if (passingShip != null)
			{
				ShipCombatOriginMap.passingShipManager.RemoveShip(passingShip);
				if (ModsConfig.IdeologyActive && !isDerelict)
					IdeoUtility.Notify_PlayerRaidedSomeone(map.mapPawns.FreeColonists);
			}
			if (faction == null)
			{
				if (navyDef != null)
					faction = Find.FactionManager.AllFactions.Where(f => navyDef.factionDefs.Contains(f.def)).RandomElement();
				else
					faction = Faction.OfAncientsHostile;
			}
			if (!isDerelict && faction.HasGoodwill && faction.AllyOrNeutralTo(Faction.OfPlayer))
				faction.TryAffectGoodwillWith(Faction.OfPlayer, -150);

			//spawn map
			IntVec3 mapSize = new IntVec3(250, 1, 250);
			if (fleet && CR > 2000 && ModSettings_SoS.enemyMapSize > 250)
			{
				int mapX = Math.Max(250, (ModSettings_SoS.enemyMapSize + 100) / 2);
				mapSize = new IntVec3(mapX, 1, ModSettings_SoS.enemyMapSize);
			}

			newMap = GetOrGenerateMapUtility.GetOrGenerateMap(ShipInteriorMod2.FindWorldTile(), mapSize, ResourceBank.WorldObjectDefOf.ShipEnemy);

			var mp = (WorldObjectOrbitingShip)newMap.Parent;
			mp.Radius = radius;
			mp.Theta = theta;
			mp.Phi = phi;
			var newMapComp = newMap.GetComponent<ShipMapComp>();
			newMapComp.ShipMapAI = ShipAI.normal;
			if (passingShip is DerelictShip d)
			{
				if (fakeWreck)
				{
					Find.LetterStack.ReceiveLetter("SoS.EncounterAmbush".Translate(), "SoS.EncounterAmbushDesc".Translate(d.derelictShip.label), LetterDefOf.ThreatBig);
				}
				else
				{
					newMapComp.ShipMapAI = ShipAI.none;
					shieldsActive = false;
					newMapComp.ShipMapState = ShipMapState.isGraveyard;
					newMap.Parent.GetComponent<TimedForcedExitShip>().StartForceExitAndRemoveMapCountdown(d.ticksUntilDeparture);
					Find.LetterStack.ReceiveLetter("SoS.EncounterStart".Translate(), "SoS.EncounterStartDesc".Translate(newMap.Parent.GetComponent<TimedForcedExitShip>().ForceExitAndRemoveMapCountdownTimeLeftString), LetterDefOf.NeutralEvent);
				}
			}
			else if (shipDef != null)
			{
				if (shipDef.carrier)
					newMapComp.ShipMapAI = ShipAI.carrier;
				else if (shipDef.tradeShip)
					newMapComp.ShipMapAI = ShipAI.avoidant;
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
				mp.Name = "SoS.ShipFleet".Translate() + " " + newMap.uniqueID;
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
			foreach (int index in shipsOnMap.Keys) //combat start calcs per ship
			{
				var ship = shipsOnMap[index];
				//if (!ship.IsWreck)
				ship.BuildingCountAtCombatStart = ship.BuildingCount;
				BuildingCountAtStart += ship.BuildingCountAtCombatStart;
				ship.BuildingsDestroyed.Clear();
				ship.AreaDestroyed.Clear();
			}
			BuildingsCount = BuildingCountAtStart;
			Log.Message("SOS2: ".Colorize(Color.cyan) + map + " BuildingCountAtStart: ".Colorize(Color.green) + BuildingCountAtStart);
			ShipGraveyard = null;
			ShipMapState = ShipMapState.inCombat;
			Heading = 0;
			MapEnginePower = 0;
			Maintain = false;
			Projectiles = new List<ShipCombatProjectile>();
			TorpsInRange = new List<ShipCombatProjectile>();
			ShuttlesInRange = new List<VehiclePawn>();
			ShuttlesOnMissions = new ThingOwner<VehiclePawn>();
			ShuttleMissions = new List<ShuttleMissionData>();
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
		private void DetermineInitialRange(bool ambush)
		{
			//advsensors = further, active cloak = closer
			//nominal should be 320-360
			//ambush 200-300
			int detectionLevel = 0;
			if (ambush)
				detectionLevel -= 3;

			List<Building_ShipSensor> Sensors = ShipInteriorMod2.WorldComp.Sensors.Where(s => s.Map == map && s.def == ResourceBank.ThingDefOf.Ship_SensorClusterAdv && s.TryGetComp<CompPowerTrader>().PowerOn).ToList();
			if (Sensors.Any())
				detectionLevel += 1;
			if (Cloaks.Where(cloak => cloak.TryGetComp<CompPowerTrader>().PowerOn).Any())
				detectionLevel -= 2;

			if (TargetMapComp.Cloaks.Where(cloak => cloak.TryGetComp<CompPowerTrader>().PowerOn).Any())
				detectionLevel -= 2;
			Range = 300 + (detectionLevel * 20) + Rand.Range(0, 40);
			if (ambush)
				Range += Rand.Range(20, 40);
		}

		//battle
		public override void MapComponentTick()
		{
			base.MapComponentTick();
			List<SpaceShipCache> shipToRemove = new List<SpaceShipCache>();
			foreach (SpaceShipCache ship in ShipsOnMap.Values)
			{
				ship.Tick();
				if (!ship.Area.Any())
					shipToRemove.Add(ship);
			}
			if (shipToRemove.Any())
			{
				foreach (SpaceShipCache cache in shipToRemove)
				{
					RemoveShipFromCache(cache.Index);
				}
				Log.Message("SOS2: ".Colorize(Color.cyan) + map + " Removed " + shipToRemove.Count + " ships. Remaining: " + ShipsOnMap.Count);
			}
			if (!map.IsSpace())
				return;

			int tick = Find.TickManager.TicksGame;
			if (ShipMapState == ShipMapState.inCombat)
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
					else if ((proj.spawnProjectile.thingClass == typeof(Projectile_ExplosiveShipTorpedo) || proj.spawnProjectile.thingClass == typeof(Projectile_ExplosiveShipAntigrain)) && !TorpsInRange.Contains(proj) && OriginMapComp.Range - proj.range < 65)
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
				//Shuttle missions
				List<ShuttleMissionData> missionsComplete = new List<ShuttleMissionData>();
				ShuttlesInRange = new List<VehiclePawn>();
				foreach(ShuttleMissionData mission in ShuttleMissions)
                {
					if (OriginMapComp.Range - mission.rangeTraveled < 65 && mission.liftedOffYet)
						ShuttlesInRange.Add(mission.shuttle);
                }
				float bestThrustRatio = MapEnginePower / 8f;

				if (bestThrustRatio >= 4f)
					bestThrustRatio = 4f;
				else if (bestThrustRatio == 0)
					bestThrustRatio = 1;
				else if (bestThrustRatio < 2f)
					bestThrustRatio = 2f;
				foreach (ShuttleMissionData mission in ShuttleMissions)
                {
					if (!mission.liftedOffYet)
						continue;
					mission.shuttle.Tick();
					float moveSpeed = Mathf.Max(mission.shuttle.GetStatValue(VehicleStatDefOf.FlightSpeed) / 100f, 0.05f);
					if((mission.mission==ShuttleMission.BOARD && this==OriginMapComp) || mission.mission==ShuttleMission.BOMB || mission.mission==ShuttleMission.STRAFE) //Opposed by enemy ship's engines
                    {
						moveSpeed /= bestThrustRatio;

						if (mission.shuttle.Faction != Faction.OfPlayer)
							moveSpeed /= 1.5f;
                    }

					if (mission.mission == ShuttleMission.BOARD)
					{
						mission.rangeTraveled += moveSpeed;
						if (mission.rangeTraveled >= OriginMapComp.Range)
							missionsComplete.Add(mission);
					}
					else if (mission.mission == ShuttleMission.RETURN)
					{
						mission.rangeTraveled -= moveSpeed;
						if (mission.rangeTraveled <= 0)
							missionsComplete.Add(mission);
					}
					else
                    {
						float targetRange = 0f;

						if (mission.mission == ShuttleMission.INTERCEPT)
							targetRange = Mathf.Max(0f, OriginMapComp.Range - 90f);
						else if (mission.mission == ShuttleMission.STRAFE)
							targetRange = Mathf.Max(0f, OriginMapComp.Range - 40f);
						else
							targetRange = OriginMapComp.Range;

						//Log.Message("Shuttle position is " + mission.rangeTraveled + ", move speed is " + moveSpeed + ", target range is " + targetRange);

						if (mission.rangeTraveled >= targetRange)
							mission.rangeTraveled = Mathf.Max(targetRange, mission.rangeTraveled - moveSpeed);
						else
							mission.rangeTraveled = Mathf.Min(targetRange, mission.rangeTraveled + moveSpeed);

						if (mission.mission == ShuttleMission.STRAFE)
						{
							mission.weaponCooldown -= 1;
							if (mission.weaponCooldown <= 0 && mission.rangeTraveled >= OriginMapComp.Range - 50f)
							{
								int? bestSkill = mission.shuttle.FindPawnWithBestStat(StatDefOf.ShootingAccuracyPawn)?.skills?.GetSkill(SkillDefOf.Shooting)?.Level;
								int skill = 0;
								if (bestSkill.HasValue)
									skill = bestSkill.Value;
								float missAngle = Mathf.Lerp(1.5f, 0.5f, skill / 20f);
								IntVec3 targetCell;
								if(mission.shuttle.Faction==Faction.OfPlayer)
                                {
									if (TargetMapComp.ShuttleTarget != IntVec3.Invalid)
										targetCell = TargetMapComp.ShuttleTarget;
									else
										targetCell = ShipCombatTargetMap.listerBuildings.allBuildingsNonColonist.RandomElement().Position;

								}
								else
									targetCell = ShipCombatTargetMap.listerBuildings.allBuildingsColonist.RandomElement().Position;
								IntVec3 spawnCell = FindClosestEdgeCell(ShipCombatTargetMap, targetCell);
								foreach (VehicleTurret turret in mission.shuttle.CompVehicleTurrets.turrets)
								{
									if (turret.turretDef == ResourceBank.VehicleTurretDefOf.SoS2ShuttleLaser)
									{
										Projectile projectile = (Projectile)GenSpawn.Spawn(ResourceBank.ThingDefOf.Shuttle_Laser_Space, spawnCell, ShipCombatTargetMap);
										IntVec3 a = targetCell - spawnCell;
										float angle = a.AngleFlat + Rand.Range(-missAngle/4f, missAngle/4f);
										IntVec3 c = spawnCell + new Vector3(1000 * Mathf.Sin(Mathf.Deg2Rad * angle), 0, 1000 * Mathf.Cos(Mathf.Deg2Rad * angle)).ToIntVec3();
										projectile.Launch(mission.shuttle, spawnCell.ToVector3Shifted(), c, targetCell, ProjectileHitFlags.All);
										mission.shuttle.compFuel.ConsumeFuel(2);
									}
									else if (turret.turretDef == ResourceBank.VehicleTurretDefOf.SoS2ShuttlePlasma)
									{
										Projectile projectile = (Projectile)GenSpawn.Spawn(ResourceBank.ThingDefOf.Shuttle_Plasma, spawnCell, ShipCombatTargetMap);
										IntVec3 a = targetCell - spawnCell;
										float angle = a.AngleFlat + Rand.Range(-missAngle, missAngle);
										IntVec3 c = spawnCell + new Vector3(1000 * Mathf.Sin(Mathf.Deg2Rad * angle), 0, 1000 * Mathf.Cos(Mathf.Deg2Rad * angle)).ToIntVec3();
										projectile.Launch(mission.shuttle, spawnCell.ToVector3Shifted(), c, targetCell, ProjectileHitFlags.All);
										mission.shuttle.compFuel.ConsumeFuel(2);
									}
								}
								mission.weaponCooldown = 120f;
							}
						}
						else if (mission.mission == ShuttleMission.BOMB)
						{
							mission.weaponCooldown -= 1;
							if (mission.weaponCooldown <= 0 && mission.rangeTraveled >= OriginMapComp.Range - 10f)
							{
								int? bestSkill = mission.shuttle.FindPawnWithBestStat(StatDefOf.ShootingAccuracyPawn)?.skills?.GetSkill(SkillDefOf.Shooting)?.Level;
								int skill = 0;
								if (bestSkill.HasValue)
									skill = bestSkill.Value;
								float missAngle = Mathf.Lerp(4f, 2f, skill / 20f);
								IntVec3 targetCell = (mission.shuttle.Faction == Faction.OfPlayer ? ShipCombatTargetMap.listerBuildings.allBuildingsNonColonist.RandomElement().Position : ShipCombatTargetMap.listerBuildings.allBuildingsColonist.RandomElement().Position);
								IntVec3 spawnCell = FindClosestEdgeCell(ShipCombatTargetMap, targetCell);
								foreach (VehicleTurret turret in mission.shuttle.CompVehicleTurrets.turrets)
								{
									if (turret.turretDef == ResourceBank.VehicleTurretDefOf.SoS2ShuttleTorpedo && turret.loadedAmmo != null)
									{
										Projectile projectile = (Projectile)GenSpawn.Spawn(turret.loadedAmmo.projectileWhenLoaded.interactionCellIcon, spawnCell, ShipCombatTargetMap);
										IntVec3 a = targetCell - spawnCell;
										float angle = a.AngleFlat + Rand.Range(-missAngle, missAngle);
										IntVec3 c = spawnCell + new Vector3(1000 * Mathf.Sin(Mathf.Deg2Rad * angle), 0, 1000 * Mathf.Cos(Mathf.Deg2Rad * angle)).ToIntVec3();
										projectile.Launch(mission.shuttle, spawnCell.ToVector3Shifted(), c, targetCell, ProjectileHitFlags.All);
										Thing torpToLoad = mission.shuttle.inventory.innerContainer.Where(thing => thing.HasThingCategory(ResourceBank.ThingCategoryDefOf.SpaceTorpedoes)).FirstOrDefault();
										if (torpToLoad==null)
                                        {
											if(mission.shuttle.Faction==Faction.OfPlayer)
												Messages.Message("SoS.ShuttleOutOfTorps".Translate(), MessageTypeDefOf.CautionInput);
											else
												Messages.Message("SoS.EnemyShuttleOutOfTorps".Translate(), MessageTypeDefOf.PositiveEvent);
											mission.mission = ShuttleMission.RETURN;
                                        }
										else
                                        {
											turret.shellCount=0;
											turret.AutoReloadCannon();
                                        }
									}
									else if (turret.turretDef == ResourceBank.VehicleTurretDefOf.SoS2ShuttleLaser)
									{
										for (int i = 0; i < 6; i++)
										{
											Projectile projectile = (Projectile)GenSpawn.Spawn(ResourceBank.ThingDefOf.Shuttle_Laser, spawnCell, ShipCombatTargetMap);
											IntVec3 a = targetCell - spawnCell;
											float angle = a.AngleFlat + Rand.Range(-2, 2);
											IntVec3 c = spawnCell + new Vector3(1000 * Mathf.Sin(Mathf.Deg2Rad * angle), 0, 1000 * Mathf.Cos(Mathf.Deg2Rad * angle)).ToIntVec3();
											projectile.Launch(mission.shuttle, spawnCell.ToVector3Shifted(), c, targetCell, ProjectileHitFlags.All);
										}
										mission.shuttle.compFuel.ConsumeFuel(2);
									}
									else if (turret.turretDef == ResourceBank.VehicleTurretDefOf.SoS2ShuttlePlasma)
									{
										Projectile projectile = (Projectile)GenSpawn.Spawn(ResourceBank.ThingDefOf.Shuttle_Plasma, spawnCell, ShipCombatTargetMap);
										IntVec3 a = targetCell - spawnCell;
										float angle = a.AngleFlat + Rand.Range(-4, 4);
										IntVec3 c = spawnCell + new Vector3(1000 * Mathf.Sin(Mathf.Deg2Rad * angle), 0, 1000 * Mathf.Cos(Mathf.Deg2Rad * angle)).ToIntVec3();
										projectile.Launch(mission.shuttle, spawnCell.ToVector3Shifted(), c, targetCell, ProjectileHitFlags.All);
										mission.shuttle.compFuel.ConsumeFuel(2);
									}
								}
								mission.weaponCooldown = 2400f;
							}
						}
						else //intercept
                        {
							mission.weaponCooldown -= 1;
							if (mission.weaponCooldown <= 0)
							{
								int? bestSkill = mission.shuttle.FindPawnWithBestStat(StatDefOf.ShootingAccuracyPawn)?.skills?.GetSkill(SkillDefOf.Shooting)?.Level;
								int pilotShootingSkill = 0;
								if (bestSkill.HasValue)
									pilotShootingSkill = bestSkill.Value;
								if (TargetMapComp.TorpsInRange.Any())
								{
									int numLasers = mission.shuttle.CompVehicleTurrets.turrets.Where(turret => turret.turretDef == ResourceBank.VehicleTurretDefOf.SoS2ShuttleLaser).Count();
									for (int i = 0; i < numLasers; i++)
									{
										if (Rand.Chance(Mathf.Lerp(0.75f, 1.25f, pilotShootingSkill / 20f)))
										{
											ShipCombatProjectile projtr = TargetMapComp.TorpsInRange.RandomElement();
											TargetMapComp.Projectiles.Remove(projtr);
											TargetMapComp.TorpsInRange.Remove(projtr);
										}
										mission.shuttle.compFuel.ConsumeFuel(2);
									}
									mission.weaponCooldown = 80f;
								}
								else if (TargetMapComp.ShuttlesInRange.Where(shuttle => shuttle.Faction != mission.shuttle.Faction).Any())
								{
									foreach (VehicleTurret laser in mission.shuttle.CompVehicleTurrets.turrets.Where(turret => turret.turretDef == ResourceBank.VehicleTurretDefOf.SoS2ShuttleLaser))
									{
										VehiclePawn shuttleHit = TargetMapComp.ShuttlesInRange.Where(shuttle => shuttle.Faction != mission.shuttle.Faction).RandomElement();
										int? bestIntellectual = shuttleHit.FindPawnWithBestStat(StatDefOf.ResearchSpeed)?.skills?.GetSkill(SkillDefOf.Intellectual)?.Level;
										int targetIntellectualSkill = 0;
										if (bestIntellectual.HasValue)
											targetIntellectualSkill = bestIntellectual.Value;
										if (Rand.Chance(Mathf.Lerp(0.85f, 1.15f, pilotShootingSkill / 20f) - (shuttleHit.GetStatValue(ResourceBank.VehicleStatDefOf.SoS2CombatDodgeChance) / Mathf.Lerp(120, 80, targetIntellectualSkill / 20f))))
										{
											CompVehicleHeatNet heatNet = shuttleHit.GetComp<CompVehicleHeatNet>();
											if (shuttleHit.GetComp<CompShipHeatShield>() != null && shuttleHit.statHandler.componentsByKeys["shieldGenerator"].health > 10 && heatNet != null && heatNet.myNet.StorageUsed < heatNet.myNet.StorageCapacity) //Shield takes the hit
											{
												Projectile dummyProjectile = (Projectile)ThingMaker.MakeThing(ResourceBank.ThingDefOf.Shuttle_Laser);
												shuttleHit.GetComp<CompShipHeatShield>().HitShield(dummyProjectile);
												dummyProjectile.Destroy();
											}
											else
											{
												shuttleHit.TakeDamage(new DamageInfo(ResourceBank.ThingDefOf.Shuttle_Laser.projectile.damageDef, ResourceBank.ThingDefOf.Shuttle_Laser.projectile.damageAmountBase), IntVec2.Zero);
												if (shuttleHit.statHandler.GetStatValue(VehicleStatDefOf.BodyIntegrity) <= 0)
												{
													if (shuttleHit.Faction == Faction.OfPlayer)
														Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CombatPodDestroyedPlayer"), null, MessageTypeDefOf.NegativeEvent);
													else
														Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CombatPodDestroyedEnemy"), null, MessageTypeDefOf.PositiveEvent);
													TargetMapComp.DeRegisterShuttleMission(TargetMapComp.ShuttleMissions.Where(otherMission => otherMission.shuttle == shuttleHit).First(), true);
													foreach (Pawn pawn in shuttleHit.AllPawnsAboard.ListFullCopy())
													{
														if (pawn.Faction == Faction.OfPlayer && (ModSettings_SoS.easyMode || Rand.Chance(0.5f)))
														{
															HealthUtility.DamageUntilDowned(pawn, false);
															shuttleHit.RemovePawn(pawn);
															DropPodUtility.DropThingsNear(DropCellFinder.RandomDropSpot(ShipCombatOriginMap), OriginMapComp.map, new List<Thing> { pawn });
														}
														else
														{
															shuttleHit.RemovePawn(pawn);
															pawn.Kill(new DamageInfo(DamageDefOf.Bomb, 100f));
															if (pawn.Faction == Faction.OfPlayer)
																DropPodUtility.DropThingsNear(DropCellFinder.RandomDropSpot(ShipCombatOriginMap), OriginMapComp.map, new List<Thing> { pawn.Corpse });
														}
													}
													foreach (Thing cargo in shuttleHit.GetDirectlyHeldThings())
														cargo.Kill();
												}
												else if (shuttleHit.statHandler.GetStatValue(VehicleStatDefOf.BodyIntegrity) <= ((CompShuttleLauncher)shuttleHit.CompVehicleLauncher).retreatAtHealth)
												{
													ShuttleMissionData missionData = TargetMapComp.ShuttleMissions.Where(otherMission => otherMission.shuttle == shuttleHit).First();
													if (missionData.mission != ShuttleMission.RETURN)
													{
														if (shuttleHit.Faction == Faction.OfPlayer)
															Messages.Message("SoS.ShuttleRetreat".Translate(), MessageTypeDefOf.NegativeEvent);
														else
															Messages.Message("SoS.EnemyShuttleRetreat".Translate(), MessageTypeDefOf.PositiveEvent);
													}
													missionData.mission = ShipMapComp.ShuttleMission.RETURN;
												}
											}
										}
										mission.shuttle.compFuel.ConsumeFuel(2);
									}
								}
							}
						}
                    }
					mission.shuttle.compFuel.ConsumeFuel(mission.shuttle.compFuel.FuelEfficiency / 6000f);
					if(mission.mission != ShuttleMission.RETURN && mission.shuttle.compFuel.Fuel < 10)
                    {
						if (mission.shuttle.Faction == Faction.OfPlayer)
							Messages.Message("SoS.ShuttleRetreatFuel".Translate(), MessageTypeDefOf.NegativeEvent);
						else
							Messages.Message("SoS.EnemyShuttleRetreatFuel".Translate(), MessageTypeDefOf.PositiveEvent);
						mission.mission = ShuttleMission.RETURN;
					}
				}
				foreach(ShuttleMissionData completed in missionsComplete)
                	DeRegisterShuttleMission(completed);
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
			else if (ShipMapState == ShipMapState.inTransit) //altitude - 0 at max or min only
			{
				if (MapEnginePower > 0)
				{
					if (Heading > 0) //ascend
					{
						Altitude += 0.1f * MapEnginePower;
					}
					else if (Heading < 0) //descend
					{
						Altitude -= 0.1f * MapEnginePower;
					}
				}
				else if (Altitude > ShipInteriorMod2.altitudeLand) //descend unless in stable or startup altitude
				{
					Altitude -= 0.2f;
				}
				if (tick % 2 == 0 && ShipInteriorMod2.WorldComp.renderedThatAlready == true)
					ShipInteriorMod2.WorldComp.renderedThatAlready = false;
				//move WO
				//max 1000 = 150, min 130 = 100
				float ratio = (Altitude - ShipInteriorMod2.altitudeLand) / (ShipInteriorMod2.altitudeNominal - ShipInteriorMod2.altitudeLand);
				if (!Takeoff) //reverse scaling - altitude always points up
				{
					ratio = 1 - ratio;
				}
				//vec to target - vec to origin, scale by altitude
				//td get a math wizard to make this a curve and point it at equator orbit or around planet to ground
				Vector3 d = mapParent.targetDrawPos - mapParent.originDrawPos;
				mapParent.drawPos = mapParent.originDrawPos + new Vector3(d.x * ratio, d.y * ratio, d.z * ratio);
			}
			if (callSlowTick) //origin only: call both slow ticks
			{
				SlowTick(tick);
				TargetMapComp.SlowTick(tick);
				callSlowTick = false;
			}
			else if (tick % 60 == 0)
			{
				SlowTick(tick);
			}
		}
		public void SlowTick(int tick)
		{
			foreach (SpaceShipCache ship in ShipsOnMap.Values)
			{
				ship.SlowTick();
			}
			if (ShipMapState == ShipMapState.inCombat)
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
					else if (Heading == 0 || TargetMapComp.Heading == 0)
					{
						Heading = 0;
					}
				}
				//engine power calcs
				bool anyShipCanMove = AnyShipCanMove();
				if (AnyShipCanMove() && Heading != 0) //can we move and should we move
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
				foreach (SpaceShipCache ship in ShipsOnMap.Values)
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
					BuildingsCount += ship.Buildings.Count;
					totalThreat += ship.ThreatCurrent;
				}
				//Log.Message("SOS2: ".Colorize(Color.cyan) + map + " threat CSML: " + threatPerSegment[0] + " " + threatPerSegment[1] + " " + threatPerSegment[2] + " " + threatPerSegment[3] + " ");

				//shipAI distance, boarding
				if (HasShipMapAI && tick > BattleStartTick + 60)
				{
					if (ShipsOnMap.Count > 1) //fleet AI evals ships in fleet and rem bad ships
					{
						if (anyShipCanMove)
						{
							foreach (int index in ShipsOnMap.Keys)
							{
								var ship = ShipsOnMap[index];
								//ship cant move and fleet fleeing or ship less than x of fleet threat
								if (!ship.CanMove() && (Retreating || totalThreat * 0.3f > ship.ThreatCurrent))
								{
									Log.Message("SOS2: ".Colorize(Color.cyan) + map + " Ship ".Colorize(Color.green) + index + " abandoned by AI.");
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
						if (Retreating || (totalThreat / (TargetMapComp.totalThreat * 0.9f * Difficulty) < 0.4f) || powerRemaining / powerCapacity < 0.2f || totalThreat == 1 || BuildingsCount / (float)BuildingCountAtStart < 0.7f || tick > BattleStartTick + 90000 || (ShipMapAI == ShipAI.avoidant && MapEnginePower > targetMapComp.MapEnginePower) || (ShipMapAI == ShipAI.carrier && tick > BattleStartTick + 9000 && !ShuttleMissions.Any()))
						{
							Heading = -1;
							Retreating = true;
							if (!warnedAboutRetreat)
							{
								Log.Message("SOS2: ".Colorize(Color.cyan) + map + " AI retreating:".Colorize(Color.red) + ", totalThreat:" + totalThreat + ", TargetMapComp.totalThreat:" + TargetMapComp.totalThreat + ", powerRemaining:" + powerRemaining + ", powerCapacity:" + powerCapacity + ", BuildingsCount:" + BuildingsCount + ", BuildingCountAtStart:" + BuildingCountAtStart);
								Messages.Message("SoS.EnemyShipRetreating".Translate(), MessageTypeDefOf.ThreatBig);
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
							int best = 0;
							if (ShipMapAI == ShipAI.carrier)
								best = 4;
							else
							{
								float max = threatRatio[0];
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
							}
							int prevHeading = Heading;
							//Log.Message(str);
							if (OriginMapComp.Range > maxRange[best]) //forward
								Heading = 1;
							else if (OriginMapComp.Range <= minRange[best]) //back
								Heading = -1;
							else //chill
								Heading = 0;

							if (Prefs.DevMode && prevHeading != Heading)
								Log.Message("SOS2: ".Colorize(Color.cyan) + map + " AI dir change: ".Colorize(Color.green) + Heading + " Threat ratios (CSML): " + threatRatio[0].ToString("F2") + " " + threatRatio[1].ToString("F2") + " " + threatRatio[2].ToString("F2") + " " + threatRatio[3].ToString("F2"));
						}
					}
					else //all engines dead or disabled
					{
						Heading = 0;
						Retreating = false;
						if ((threatPerSegment[0] == 1 && threatPerSegment[1] == 1 && threatPerSegment[2] == 1 && threatPerSegment[3] == 1) || tick > BattleStartTick + 120000)
						{
							//no turrets to fight with - exit
							EndBattle(map, false);
							return;
						}
						if (warnedAboutAdrift == 0)
						{
							Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.EnemyShipAdrift"), map.Parent, MessageTypeDefOf.NegativeEvent);
							warnedAboutAdrift = tick + Rand.RangeInclusive(60000, 180000);
						}
						else if (tick > warnedAboutAdrift)
						{
							EndBattle(map, false, false, warnedAboutAdrift - tick);
							return;
						}
					}
					//old AI boarding code
					/*if ((hasAnyPartDetached || tick > BattleStartTick + 5000) && !startedBoarderLoad && !Retreating)
					{
						foreach (SpaceShipCache ship in ShipsOnMap.Values)
						{
							List<CompTransporter> transporters = new List<CompTransporter>();
							float transporterMass = 0;
							foreach (Thing t in map.listerThings.ThingsInGroup(ThingRequestGroup.Transporter).Where(t => t.Faction == ship.Faction && ShipIndexOnVec(t.Position) == ship.Index))
							{
								var transporter = t.TryGetComp<CompTransporter>();
								if (transporter != null && t.def == ResourceBank.ThingDefOf.SoS2_Shuttle_Personal)
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
						startedBoarderLoad = true;
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

								if (foundShuttle)
									OriginMapComp.ShipLord = LordMaker.MakeNewLord(ShipFaction, new LordJob_AssaultShip(ShipFaction, false, false, false, true, false), ShipCombatTargetMap, new List<Pawn>());
								launchedBoarders = true;
							}
						}
					}*/

					if (!startedPilotLoad && !Retreating && tick > BattleStartTick + 500) //Shuttles for missions
					{
						foreach (SpaceShipCache ship in ShipsOnMap.Values)
						{
							List<VehiclePawn> shuttles = new List<VehiclePawn>();
							foreach (VehiclePawn vehicle in ship.ShuttlesOnShip(ship.Faction))
							{
								if (ShipInteriorMod2.IsShuttle(vehicle) && IsShuttleCombatReady(vehicle) && vehicle.CompUpgradeTree != null && ShipInteriorMod2.ShuttleIsArmed(vehicle) && vehicle.NextAvailableHandler() != null)
									shuttles.Add(vehicle);
							}
							List<VehiclePawn> shuttlesToBeFilled = new List<VehiclePawn>(shuttles);
							IEnumerable<Pawn> pawnsToBoard = ship.PawnsOnShip(ship.Faction).Where(pawn => (pawn.CurJob == null || pawn.CurJob.def != ResourceBank.JobDefOf.ManShipBridge));
							Log.Message("[SoS2] Planning shuttle missions. Found " + shuttlesToBeFilled.Count + " combat-ready shuttles and " + pawnsToBoard.Count() + " potential pilots.");
							foreach (Pawn p in pawnsToBoard)
							{
								if (shuttlesToBeFilled.Count > 0 && p.mindState.duty != null)
								{
									p.mindState.duty.transportersGroup = 0;
									VehiclePawn myShuttle = shuttlesToBeFilled.Where(shuttle=>p.CanReserveAndReach(shuttle, PathEndMode.Touch, Danger.Deadly)).RandomElement();
									if (myShuttle != null)
									{
										Job job = new Job(JobDefOf_Vehicles.Board, myShuttle);
										p.jobs.StopAll();
										p.jobs.StartJob(job);
										map.GetComponent<VehicleReservationManager>().Reserve<VehicleHandler, VehicleHandlerReservation>(myShuttle, p, job, myShuttle.NextAvailableHandler());
										shuttlesToBeFilled.Remove(myShuttle);
										shuttlesYetToLaunch.Add(myShuttle);
										Log.Message("[SoS2] Assigning " + p + " to shuttle (laser: " + ShipInteriorMod2.ShuttleHasLaser(myShuttle) + ") (plasma: " + ShipInteriorMod2.ShuttleHasPlasma(myShuttle) + ") (torpedo: " + ShipInteriorMod2.ShuttleHasTorp(myShuttle) + ")");
									}
								}
							}
							Log.Message("[SoS2] " + shuttlesYetToLaunch.Count + " shuttles assigned pilots, " + shuttlesToBeFilled.Count + " shuttles unfilled.");
						}
						startedPilotLoad = true;
					}
					if ((hasAnyPartDetached || tick > BattleStartTick + 5000) && !startedBoarderLoad && !Retreating) //Shuttles for boarders
					{
						foreach (SpaceShipCache ship in ShipsOnMap.Values)
						{
							List<VehiclePawn> shuttles = new List<VehiclePawn>();
							List<CompShipBay> bays = new List<CompShipBay>();
							foreach (VehiclePawn vehicle in ship.ShuttlesOnShip(ship.Faction))
							{
								if (ShipInteriorMod2.IsShuttle(vehicle) && (vehicle.CompUpgradeTree == null || !ShipInteriorMod2.ShuttleIsArmed(vehicle)) && vehicle.NextAvailableHandler() != null)
								{
									if (ShipInteriorMod2.IsPod(vehicle) || !ModSettings_SoS.shipMapPhysics)
										shuttles.Add(vehicle);
									else //for non pods, check if there is room to land
									{
										foreach (CompShipBay bay in targetMapComp.Bays)
										{
											if (!bays.Contains(bay) && bay.CanFitShuttleSize(vehicle) != IntVec3.Zero)
											{
												shuttles.Add(vehicle);
												bays.Add(bay);
											}
										}
									}
								}
							}
							List<VehiclePawn> shuttlesToBeFilled = new List<VehiclePawn>(shuttles);
							IEnumerable<Pawn> pawnsToBoard = ship.PawnsOnShip(ship.Faction).Where(pawn => (pawn.CurJob == null || pawn.CurJob.def != ResourceBank.JobDefOf.ManShipBridge) && pawn.kindDef.combatPower > 40);
							Log.Message("[SoS2] Planning boarding missions. Found " + shuttlesToBeFilled.Count + " boarding-ready shuttles and " + pawnsToBoard.Count() + " potential boarders.");
							foreach (Pawn p in pawnsToBoard)
							{
								if (shuttlesToBeFilled.Count > 0 && p.mindState.duty != null)
								{
									p.mindState.duty.transportersGroup = 1;
									VehiclePawn myShuttle = shuttlesToBeFilled.Where(shuttle=>p.CanReserveAndReach(shuttle, PathEndMode.Touch, Danger.Deadly)).RandomElement();
									if (myShuttle != null && myShuttle.NextAvailableHandler()!=null)
									{
										myShuttle.PromptToBoardVehicle(p, myShuttle.NextAvailableHandler());
										if (myShuttle.NextAvailableHandler() == null)
											shuttlesToBeFilled.Remove(myShuttle);
										if (!shuttlesWantingBoarders.Contains(myShuttle))
											shuttlesWantingBoarders.Add(myShuttle);
										Log.Message("[SoS2] Assigning " + p + " to boarding shuttle");
									}
								}
								Log.Message("[SoS2] " + shuttlesWantingBoarders.Count + " shuttles assigned boarders, " + shuttlesToBeFilled.Count + " shuttles unfilled.");
							}
						}
						startedBoarderLoad = true;
					}
					if (startedPilotLoad && shuttlesYetToLaunch.Count > 0 && !Retreating)
					{
						//abort and reset if player on ship
						if (map.mapPawns.AllPawnsSpawned.Any(o => o.Faction == Faction.OfPlayer))
						{
							foreach (Pawn pawn in map.mapPawns.AllHumanlike)
							{
								if (pawn.Faction != Faction.OfPlayer)
								{
									if (pawn.mindState.duty.transportersGroup == 0)
									{
										pawn.jobs.StopAll();
										pawn.mindState.duty.transportersGroup = -1;
									}
									if (pawn.GetVehicle() != null)
										pawn.GetVehicle().DisembarkPawn(pawn);
								}
							}
							startedPilotLoad = false;
						}
						else //continue launching
						{
							foreach (VehiclePawn shuttle in shuttlesYetToLaunch.ToList())
							{
								if (shuttle.AllPawnsAboard.Count > 0)
								{
									shuttle.CompVehicleLauncher.inFlight = true;
									shuttle.CompVehicleLauncher.launchProtocol.OrderProtocol(LaunchProtocol.LaunchType.Takeoff);
									VehicleSkyfaller_Leaving vehicleSkyfaller_Leaving = (VehicleSkyfaller_Leaving)VehicleSkyfallerMaker.MakeSkyfaller(shuttle.CompVehicleLauncher.Props.skyfallerLeaving, shuttle);
									vehicleSkyfaller_Leaving.vehicle = shuttle;
									vehicleSkyfaller_Leaving.createWorldObject = false;
									GenSpawn.Spawn(vehicleSkyfaller_Leaving, shuttle.Position, shuttle.Map, shuttle.CompVehicleLauncher.launchProtocol.CurAnimationProperties.forcedRotation ?? shuttle.Rotation);
									var shuttleMapComp = shuttle.Map.GetComponent<ShipMapComp>(); //td wouldnt it always be this.?
									if (ShipInteriorMod2.ShuttleHasLaser(shuttle))
									{
										if(Rand.Chance(InterceptMissionChance()))
											((ShuttleTakeoff)shuttle.CompVehicleLauncher.launchProtocol).TempMissionRef = shuttleMapComp.RegisterShuttleMission(shuttle, ShuttleMission.INTERCEPT);
										else
											((ShuttleTakeoff)shuttle.CompVehicleLauncher.launchProtocol).TempMissionRef = shuttleMapComp.RegisterShuttleMission(shuttle, ShuttleMission.STRAFE);
									}
									else if (ShipInteriorMod2.ShuttleHasTorp(shuttle))
										((ShuttleTakeoff)shuttle.CompVehicleLauncher.launchProtocol).TempMissionRef = shuttleMapComp.RegisterShuttleMission(shuttle, ShuttleMission.BOMB);
									else
										((ShuttleTakeoff)shuttle.CompVehicleLauncher.launchProtocol).TempMissionRef = shuttleMapComp.RegisterShuttleMission(shuttle, ShuttleMission.STRAFE);
									CameraJumper.TryHideWorld();
									shuttle.EventRegistry[VehicleEventDefOf.AerialVehicleLaunch].ExecuteEvents();
									shuttlesYetToLaunch.Remove(shuttle);
								}
							}
							if(shuttlesYetToLaunch.Count==0)
								startedPilotLoad = false; //Reset shuttles so that carriers can refuel their fighters
						}
					}
					if (startedBoarderLoad && !launchedBoarders && !Retreating)
					{
						//abort and reset if player on ship
						if (map.mapPawns.AllPawnsSpawned.Any(o => o.Faction == Faction.OfPlayer))
						{
							foreach (Pawn pawn in map.mapPawns.AllHumanlike)
							{
								if (pawn.Faction != Faction.OfPlayer)
								{
									if (pawn.mindState.duty.transportersGroup == 1)
									{
										pawn.jobs.StopAll();
										pawn.mindState.duty.transportersGroup = -1;
									}
									if (pawn.GetVehicle() != null)
										pawn.GetVehicle().DisembarkPawn(pawn);
								}
							}
							startedBoarderLoad = false;
						}
						else //continue boarding action
						{
							bool allOnPods = true;
							foreach (Pawn p in map.mapPawns.AllPawnsSpawned.Where(o => o.Faction != Faction.OfPlayer))
							{
								if (p.mindState?.duty?.transportersGroup == 1 && p.GetVehicle() == null)
									allOnPods = false;
							}
							if (allOnPods)
							{
								foreach (VehiclePawn shuttle in shuttlesWantingBoarders)
								{
									if (shuttle.AllPawnsAboard.Count > 0)
									{
										shuttle.CompVehicleLauncher.inFlight = true;
										shuttle.CompVehicleLauncher.launchProtocol.OrderProtocol(LaunchProtocol.LaunchType.Takeoff);
										VehicleSkyfaller_Leaving vehicleSkyfaller_Leaving = (VehicleSkyfaller_Leaving)VehicleSkyfallerMaker.MakeSkyfaller(shuttle.CompVehicleLauncher.Props.skyfallerLeaving, shuttle);
										vehicleSkyfaller_Leaving.vehicle = shuttle;
										vehicleSkyfaller_Leaving.createWorldObject = false;
										GenSpawn.Spawn(vehicleSkyfaller_Leaving, shuttle.Position, shuttle.Map, shuttle.CompVehicleLauncher.launchProtocol.CurAnimationProperties.forcedRotation ?? shuttle.Rotation);
										((ShuttleTakeoff)shuttle.CompVehicleLauncher.launchProtocol).TempMissionRef = shuttle.Map.GetComponent<ShipMapComp>().RegisterShuttleMission(shuttle, ShuttleMission.BOARD);
										CameraJumper.TryHideWorld();
										shuttle.EventRegistry[VehicleEventDefOf.AerialVehicleLaunch].ExecuteEvents();
									}
								}
								launchedBoarders = true;
							}
						}
					}
				}
				if (tick % 360 == 0 && ModSettings_SoS.shipMapPhysics && MapEnginePower > 0.02f)
				{
					MoveAllOffShip();
				}
			}
			else if (ShipMapState == ShipMapState.inTransit)
			{
				/*
				 * transit system:
				 * on ground, no spacehome: move to new spacehome and transit to orbit (ShipInteriorMod2)
				 * on ground, spacehome exists: placeworker on spacehome via (MinifiedThingShipMove), spawn and move to transit map, at destination attempt auto move to placeworker, if fail warn
				 * in orbit on spacehome, only ship: transit spacehome to ground via (MinifiedThingShipMove), at destination attempt auto move to placeworker, if fail make new map and land on it
				 * in orbit on spacehome with other ships: move to transit map via (MinifiedThingShipMove) and transit to ground, at destination attempt auto move to placeworker, if fail make new map and land on it
				 all vars are stored in this except WO drawPos (current, target, origin)
				*/
				if (Altitude >= ShipInteriorMod2.altitudeNominal) //orbit reached
				{
					Altitude = ShipInteriorMod2.altitudeNominal;
					MapFullStop();
					BurnTimer = 0;
					Map spacehome = ShipInteriorMod2.FindPlayerShipMap();
					if (spacehome == null) //spacehome is gone, make new
					{
						spacehome = ShipInteriorMod2.GeneratePlayerShipMap(map.Size);
					}
					if (map != spacehome) //arriving from temp map
					{
						if (ShipInteriorMod2.CanShipLandOnMap(map, MoveToMap)) //landing area clear
						{
							ShipInteriorMod2.MoveShip(ShipsOnMap.Values.First().Core, MoveToMap, MoveToVec);
							if (MapShipCells.NullOrEmpty() && !map.PlayerPawnsForStoryteller.Any())
							{
								ShipMapState = ShipMapState.burnUpSet; //remove transit map if clear
								return;
							}
						}
						else //blocked
						{
							//td message ready to move
							Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.OrbitAchieved"), TranslatorFormattedStringExtensions.Translate("SoS.OrbitAchievedDesc"), LetterDefOf.PositiveEvent);
						}
						ShipMapState = ShipMapState.isGraveyard;
						map.Parent.GetComponent<TimedForcedExitShip>().StartForceExitAndRemoveMapCountdown(10000);
					}
					else //arriving on spacehome
					{
						((WorldObjectOrbitingShip)map.Parent).SetNominalPos();
						ShipMapState = ShipMapState.nominal;
						Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.OrbitAchieved"), TranslatorFormattedStringExtensions.Translate("SoS.OrbitAchievedDesc"), LetterDefOf.PositiveEvent);
					}
				}
				else if (Altitude <= ShipInteriorMod2.altitudeLand && (Heading < 1 || !EnginesOn && BurnTimer > tick + 300)) //ground reached or fail to start engines in time - land/crash
				{
					Altitude = ShipInteriorMod2.altitudeLand;
					MapFullStop();
					BurnTimer = 0;
					int targetTile = -1;

					if (Takeoff) //fell from space
					{
						if (PrevMap == null) //takeoff map was closed
							MoveToTile = PrevTile;
						else
							MoveToMap = PrevMap;
					}

					if (MoveToMap != null && ShipInteriorMod2.CanShipLandOnMap(map, MoveToMap)) //ground map exists and has room
					{
						ShipInteriorMod2.MoveShip(ShipsOnMap.Values.First().Core, MoveToMap, MoveToVec);
					}
					else //moveto map was closed or no room
					{
						targetTile = MoveToTile;
						int tile = -1;
						List<int> tiles = ShipInteriorMod2.PossibleShipLandingTiles(targetTile, 2, 5);
						if (!tiles.NullOrEmpty())
							tile = tiles.RandomElement();
						else
						{
							tiles = ShipInteriorMod2.PossibleShipLandingTiles(targetTile, 5, 20);
							if (!tiles.NullOrEmpty())
								tile = tiles.RandomElement();
						}
						if (tile != -1)
						{
							SettleUtility.AddNewHome(tile, Faction.OfPlayer); //td change this to landed ship
							var newMapPar = GetOrGenerateMapUtility.GetOrGenerateMap(tile, map.Size, null).Parent;
							((Settlement)newMapPar).Name = "Landed ship";
							ShipInteriorMod2.MoveShip(ShipsOnMap.Values.First().Core, newMapPar.Map, IntVec3.Zero, clearArea: true);
						}
						else //td ship gone, pawns spawn like vanilla on random nearby map via pods
						{
							ShipMapState = ShipMapState.burnUpSet;
						}
					}
				}
				else if (EnginesOn && Heading < 0 && Altitude < ShipInteriorMod2.altitudeNominal - 50) //end first burn down
				{
					Log.Message("first burn done");
					MapFullStop();
				}

				if (Heading > 0) //consume fuel, if not enough engine power, lose altitude
				{
					//reduce durration per engine vs mass
					if (AnyShipCanMove() && EnginesOn) //can we move and should we move
					{
						MapEnginesOn();
						MapEnginePower *= 400f;
					}
					else
					{
						MapFullStop();
					}
				}
				KillAllOffShip();
			}
			else if (ShipMapState == ShipMapState.inEvent)
			{
				var cond = map.gameConditionManager.ActiveConditions.FirstOrDefault(c => c is GameCondition_SpaceDebris);
				//reduce durration per engine vs mass
				if (AnyShipCanMove() && EnginesOn) //can we move and should we move
				{
					MapEnginesOn();
					MapEnginePower *= 40000f;
					if (BurnTimer > cond.TicksLeft)
					{
						cond.End();
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
			else if (ShipMapState == ShipMapState.nominal)
			{
				if (tick % 300 == 0)
				{
					if (tick % 6000 == 0) //bounty hunters
					{
						if (IsPlayerShipMap)
						{
							if (ShipInteriorMod2.WorldComp.PlayerFactionBounty > 20 && tick - LastBountyRaidTick > Mathf.Max(600000f / Mathf.Sqrt(ShipInteriorMod2.WorldComp.PlayerFactionBounty), 60000f))
							{
								LastBountyRaidTick = tick;
								Building_ShipBridge bridge = MapRootListAll.FirstOrDefault();
								if (bridge == null)
									return;
								StartShipEncounter(bounty: true);
							}
						}
					}
					//auto claim
					if (ShipsOnMap.Values.Any(s => s.Faction != Faction.OfPlayer) && CanClaimNow(Faction.OfPlayer))
					{
						Claim();
					}
				}
			}
			if (tick % 300 == 0)
			{
				if (tick % 6000 == 0) //decomp
				{
					List<Building> buildings = new List<Building>();
					foreach (SpaceShipCache ship in ShipsOnMap.Values) //decompresson
					{
						foreach (Building b in ship.OuterNonShipWalls())
						{
							if (Rand.Chance(0.5f))
								buildings.Add(b);
						}
					}
					foreach (Building b in buildings)
					{
						b.Destroy(DestroyMode.KillFinalize);
					}
				}

				//trigger combat with next target
				if (NextTargetMap != null && tick > LastAttackTick + 600)
				{
					StartShipEncounter(null, NextTargetMap);
					NextTargetMap = null;
					return;
				}
				//RCS fx
				if (MapEnginePower != 0)
				{
					foreach (SpaceShipCache ship in ShipsOnMap.Values)
					{
						foreach (CompRCSThruster rcs in ship.RCSs)
						{
							if (rcs.active && Rand.Chance(0.3f)) //td need better fx, not affected by wind
								FleckMaker.ThrowHeatGlow(rcs.ventTo, rcs.parent.Map, 1f);
						}
					}
				}
			}
		}

		bool IsShuttleCombatReady(VehiclePawn shuttle)
        {
			if (shuttle.statHandler.GetStatValue(VehicleStatDefOf.BodyIntegrity) < 0.5f)
				return false; //Too damaged
			if (shuttle.CompFueledTravel.FuelPercent < 0.5f)
				return false; //Not enough fuel
			if (shuttle.CompFueledTravel.FuelPercent < 1f) //Check to see if still actively refueling from bay
            {
				ThingWithComps shuttleBay = shuttle.Position.GetFirstThingWithComp<CompShipBay>(map);
				if(shuttleBay!=null)
                {
					CompRefuelable fuel = shuttleBay.GetComp<CompRefuelable>();
					if (fuel != null && fuel.fuel > 0)
						return false;
                }
            }
			return true;
        }

		float InterceptMissionChance()
        {
			float chance = 0f;
			foreach(ShuttleMissionData mission in OriginMapComp.ShuttleMissions)
            {
				if (mission.mission != ShuttleMission.RETURN && mission.mission != ShuttleMission.INTERCEPT)
					chance += 0.15f;
            }
			foreach(VehiclePawn shuttle in ShipCombatOriginMap.listerThings.GetThingsOfType<VehiclePawn>())
            {
				if (ShipInteriorMod2.IsShuttle(shuttle))
					chance += 0.10f;
            }
			chance += 0.15f * OriginMapComp.TorpedoTubes.Count();
			chance -= ShuttleMissions.Where(mission => mission.mission == ShuttleMission.INTERCEPT).Count() * 0.4f;
			return chance;
        }

		public void MoveAllOffShip()
		{
			//fling pawns to grave if they stumble off ship
			List<Pawn> pawns = new List<Pawn>();
			foreach (Pawn p in map.mapPawns.AllPawnsSpawned.ToList())
			{
				if (ShipIndexOnVec(p.Position) == -1)
				{
					pawns.Add(p);
				}
			}
			if (pawns.Any())
			{
				if (ShipGraveyard == null)
					SpawnGraveyard();
				foreach (Pawn p in pawns)
				{
					p.DeSpawn();
				}
				foreach (Pawn p in pawns)
				{
					p.SpawnSetup(ShipGraveyard, false);
				}
			}
			//move things
			if (ShipsOnMap.Values.Any(s => s.AreaDestroyed.Any()))
			{
				List<Thing> things = new List<Thing>();
				if (ShipGraveyard == null)
					SpawnGraveyard();
				foreach (SpaceShipCache ship in ShipsOnMap.Values)
				{
					foreach (IntVec3 v in ship.AreaDestroyed)
					{
						foreach (Thing thing in v.GetThingList(map))
						{
							things.Add(thing);
						}
					}
				}
				foreach (Thing p in things)
				{
					p.DeSpawn();
				}
				foreach (Thing p in things)
				{
					p.SpawnSetup(ShipGraveyard, false);
				}
			}
		}
		public void KillAllOffShip()
		{
			List<Pawn> pawns = new List<Pawn>();
			HashSet<Thing> things = new HashSet<Thing>();
			foreach (IntVec3 v in map.AllCells.Except(MapShipCells.Keys)) //kill anything off ship
			{
				foreach (Thing t in v.GetThingList(map))
				{
					if (t is Pawn p)
						pawns.Add(p);
					things.Add(t);
				}
			}
			foreach (Thing t in things)
			{
				t.Destroy();
			}
			foreach (Pawn p in pawns)
			{
				p.Kill(new DamageInfo(DamageDefOf.Crush, 10000));
			}
		}
		public bool AnyShipCanMove() //any non stuck ship has a working and fueled engine and is aligned
		{
			foreach (SpaceShipCache ship in ShipsOnMap.Values)
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
			foreach (SpaceShipCache ship in ShipsOnMap.Values.Where(s => s.Engines.Any()))
			{
				ship.MoveAtThrustToWeight(MapEnginePower);
			}
		}
		public float SlowestThrustToWeight() //find worst t/w ship
		{
			if (ShipsOnMap.NullOrEmpty())
				return 0f;

			float enginePower = float.MaxValue;
			foreach (SpaceShipCache ship in ShipsOnMap.Values)
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
			EnginesOn = false;
			MapEnginePower = 0;
			Heading = 0;
			foreach (SpaceShipCache ship in ShipsOnMap.Values)
			{
				ship.EnginesOff();
			}
		}
		public void RemoveShipFromBattle(int shipIndex) //only call this on mapcomp tick!
		{
			Log.Warning("SOS2: ".Colorize(Color.cyan) + map + " Ship ".Colorize(Color.green) + shipIndex + " RemoveShipFromBattle");
			if (!ShipsOnMap.ContainsKey(shipIndex))
			{
				Log.Warning("SOS2: ".Colorize(Color.cyan) + map + " Tried removing ship ".Colorize(Color.green) + shipIndex + " but it is not present in cache!");
				return;
			}
			SpaceShipCache ship = ShipsOnMap[shipIndex];
			foreach (Building_ArchotechPillar pillar in ship.Buildings.Where(building => building is Building_ArchotechPillar)) //Handling for archotech pillar A
				ShipInteriorMod2.WorldComp.Unlocks.Add(((CompProps_ResearchUnlock)pillar.GetComp<CompResearchUnlock>().props).unlock);
			if (ShipsOnMap.Values.Count(s => !s.IsWreck) == 0 || (ShipsOnMap.Values.Count(s => !s.IsWreck) == 1 && !ship.IsWreck && ship.Faction != ShipFaction)) //end battle if last ship or last ship captured
			{
				EndBattle(map, false);
				return;
			}
			Building core = ship.Core;
			//ship.AreaDestroyed.Clear();
			if (core == null)
			{
				core = ShipsOnMap[shipIndex].Parts.FirstOrDefault();
			}
			if (core != null)
			{
				Log.Warning("SOS2: ".Colorize(Color.cyan) + map + " Ship ".Colorize(Color.green) + shipIndex + " Removing with: " + core);
				if (ShipGraveyard == null)
					SpawnGraveyard();
				foreach(Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
					if (pawn.Faction != Faction.OfPlayer && ship.Area.Contains(pawn.Position))
					{
						if (pawn is VehiclePawn vehicle)
						{
							vehicle.ignition.Drafted = false;
							vehicle.DisembarkAll();
						}
						else
						{
							pawn.jobs?.StopAll();
							if (pawn.TryGetLord(out Lord lord))
								lord.RemovePawn(pawn);
						}
					}
                }
				ShipInteriorMod2.MoveShip(core, ShipGraveyard, IntVec3.Zero);
			}
			Log.Warning("SOS2: ".Colorize(Color.cyan) + map + " Ships remaining: " + ShipsOnMap.Count);
			foreach (SpaceShipCache s in ShipsOnMap.Values)
			{
				Log.Warning("SOS2: ".Colorize(Color.cyan) + map + " Ship ".Colorize(Color.green) + s.Index + ", area: " + s.Area.Count + ", bldgs: " + s.BuildingCount + ", cores: " + s.Bridges.Count);
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
			mp.Radius = 150;
			mp.Theta = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).Theta + adj;
			mp.Phi = ((WorldObjectOrbitingShip)ShipCombatOriginMap.Parent).Phi - 0.01f + 0.001f * Rand.Range(0, 20);
			mp.Name += "Wreckage nr." + ShipGraveyard.uniqueID;
			var graveMapComp = ShipGraveyard.GetComponent<ShipMapComp>();
			graveMapComp.ShipMapState = ShipMapState.isGraveyard;
			graveMapComp.GraveOrigin = map;
			graveMapComp.ShipFaction = ShipFaction;
		}
		public void ShipBuildingsOff() //td should no longer be needed for engines
		{
			//td destroy all proj?
			OriginMapComp.MapFullStop();
			TargetMapComp.MapFullStop();
			foreach (SpaceShipCache ship in OriginMapComp.TargetMapComp.ShipsOnMap.Values)
			{
				foreach (CompShipHeatShield s in ship.Shields)
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
		public void EndBattle(Map loser, bool fled, bool hack = false, int burnTimeElapsed = 0)
		{
			if (loser.GetComponent<ShipMapComp>().ShipMapState != ShipMapState.inCombat)
				return;
			Log.Message("SOS2: ".Colorize(Color.cyan) + loser + " Lost ship battle!".Colorize(Color.red));
			//tgtMap is opponent of origin
			Map tgtMap = OriginMapComp.ShipCombatTargetMap;
			var tgtMapComp = OriginMapComp.TargetMapComp;
			tgtMapComp.ShipMapAI = ShipAI.none;
			tgtMapComp.ShuttleTarget = IntVec3.Invalid;
			tgtMapComp.ShipMapState = ShipMapState.isGraveyard;
			if (OriginMapComp.map == ShipInteriorMod2.FindPlayerShipMap())
				OriginMapComp.ShipMapState = ShipMapState.nominal;
			else
				OriginMapComp.ShipMapState = ShipMapState.isGraveyard;
			OriginMapComp.ShipBuildingsOff();
			OriginMapComp.ShipGraveyard?.Parent.GetComponent<TimedForcedExitShip>()?.StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(60000, 180000) - burnTimeElapsed);
			tgtMapComp.ShipGraveyard?.Parent.GetComponent<TimedForcedExitShip>()?.StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(60000, 180000) - burnTimeElapsed);
			if (loser != ShipCombatOriginMap)
			{
				if (fled) //target fled, remove target
				{
					tgtMapComp.ShipMapState = ShipMapState.burnUpSet;
					Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.EnemyShipRetreated"), MessageTypeDefOf.ThreatBig);
				}
				else //target lost
				{
					if (OriginMapComp.attackedTradeship)
						ShipInteriorMod2.WorldComp.PlayerFactionBounty += 15;
					tgtMap.Parent.GetComponent<TimedForcedExitShip>().StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(60000, 180000) - burnTimeElapsed);
					if (hack)
						Find.LetterStack.ReceiveLetter("SoS.WinShipBattle".Translate(), "SoS.WinShipBattleHackDesc".Translate(tgtMap.Parent.GetComponent<TimedForcedExitShip>().ForceExitAndRemoveMapCountdownTimeLeftString), LetterDefOf.PositiveEvent);
					else
						Find.LetterStack.ReceiveLetter("SoS.WinShipBattle".Translate(), "SoS.WinShipBattleDesc".Translate(tgtMap.Parent.GetComponent<TimedForcedExitShip>().ForceExitAndRemoveMapCountdownTimeLeftString), LetterDefOf.PositiveEvent);
				}
			}
			else //origin fled or lost
			{
				if (OriginMapComp.ShipMapState == ShipMapState.isGraveyard) //origingrave battle
				{
					if (!fled) //origingrave lost
					{
						OriginMapComp.ShipMapState = ShipMapState.burnUpSet;
					}
					tgtMapComp.ShipMapState = ShipMapState.burnUpSet;
				}
				else //normal battle
				{
					if (!fled) //origin lost
					{
						ShipCombatOriginMap.Parent.GetComponent<TimedForcedExitShip>()?.StartForceExitAndRemoveMapCountdown(Rand.RangeInclusive(60000, 180000));
						//Find.GameEnder.CheckOrUpdateGameOver();
					}
					if (OriginMapComp.ShipGraveyard != null)
					{
						//if origin has grave with a ship, grave starts combat with enemy
						if (OriginMapComp.GraveComp.MapRootListAll.Any() && !OriginMapComp.attackedTradeship)
						{
							OriginMapComp.GraveComp.LastAttackTick = Find.TickManager.TicksGame;
							OriginMapComp.GraveComp.NextTargetMap = OriginMapComp.ShipCombatTargetMap;
						}
						else //no ships in grave, enemy leaves, clean grave
						{
							tgtMapComp.ShipMapState = ShipMapState.burnUpSet;
							//remove all wrecks from map, leave pawns
							foreach (int shipIndex in OriginMapComp.GraveComp.ShipsOnMap.Keys)
							{
								ShipInteriorMod2.RemoveShipOrArea(OriginMapComp.ShipGraveyard, shipIndex, null, false);
							}
						}
					}
					else //no grave, enemy leaves
					{
						tgtMapComp.ShipMapState = ShipMapState.burnUpSet;
					}
				}
			}
			foreach(ShuttleMissionData mission in OriginMapComp.ShuttleMissions.ListFullCopy())
            {
				DeRegisterShuttleMission(mission);
			}
			foreach (ShuttleMissionData mission in OriginMapComp.TargetMapComp.ShuttleMissions.ListFullCopy())
			{
				DeRegisterShuttleMission(mission);
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
			var mapComp = map.GetComponent<ShipMapComp>();
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

		//shuttles
        public void GetChildHolders(List<IThingHolder> outChildren)
        {
			foreach (VehiclePawn shuttle in ShuttlesOnMissions)
				outChildren.Add(shuttle);
        }
        public ThingOwner GetDirectlyHeldThings()
        {
			return ShuttlesOnMissions;
        }
		public ShuttleMissionData RegisterShuttleMission(VehiclePawn shuttle, ShuttleMission mission)
        {
			if(shuttle.Spawned)
				shuttle.DeSpawn();
			ShuttlesOnMissions.TryAddOrTransfer(shuttle);
			ShuttleMissionData data = new ShuttleMissionData();
			data.shuttle = shuttle;
			data.mission = mission;
			data.retreatAtHealth = shuttle.GetComp<CompShuttleLauncher>().retreatAtHealth;
			data.uniqueID = ShipInteriorMod2.WorldComp.nextUniqueMissionID++;
			ShuttleMissions.Add(data);
			return data;
		}
		public void DeRegisterShuttleMission(ShuttleMissionData mission, bool destroyed=false)
        {
			Log.Message("De-registering shuttle mission " + mission.mission);
			ShuttlesOnMissions.Remove(mission.shuttle);
			ShuttleMissions.Remove(mission);
			if (!destroyed)
            {
				Map mapToSpawnIn;
				if (OriginMapComp.ShipMapState == ShipMapState.nominal)
					mapToSpawnIn = OriginMapComp.map;
				else
				{
					if (mission.mission == ShuttleMission.BOARD)
						mapToSpawnIn = ShipCombatTargetMap;
					else //Return mission
						mapToSpawnIn = map;
				}
				var mapToSpawnInComp = mapToSpawnIn.GetComponent<ShipMapComp>();
				if (mission.shuttle.Faction == Faction.OfPlayer)
				{
					if (!mapToSpawnInComp.IsPlayerShipMap && mapToSpawnInComp.Bays.Any(b => b.CanFitShuttleSize(mission.shuttle) != IntVec3.Zero))
					{
						foreach (var bay in mapToSpawnInComp.Bays) //unfog bays
						{
							FloodFillerFog.FloodUnfog(bay.parent.Position, mapToSpawnIn);
						}
					}
					Messages.Message("SoS.BoardingShuttleArrived".Translate(), MessageTypeDefOf.TaskCompletion);
					CameraJumper.TryJump(mapToSpawnIn.Center, mapToSpawnIn);
					ReturnBoardingParty.missionData = mission;
					LandingTargeter.Instance.BeginTargeting(mission.shuttle, mapToSpawnIn, delegate (LocalTargetInfo target, Rot4 rot)
					{
						IntVec3 vec = IntVec3.Zero;
						var bay = target.Cell.GetThingList(mapToSpawnIn).Where(t => t.TryGetComp<CompShipBay>() != null)?.FirstOrDefault();
						if (bay != null)
						{
							vec = bay.TryGetComp<CompShipBay>().CanFitShuttleSize(mission.shuttle);
							if (vec != IntVec3.Zero)
							{
								bay.TryGetComp<CompShipBay>().ReserveArea(vec, mission.shuttle);
							}
						}
						VehicleSkyfaller_Arriving vehicleSkyfaller_Arriving = (VehicleSkyfaller_Arriving)VehicleSkyfallerMaker.MakeSkyfaller(mission.shuttle.CompVehicleLauncher.Props.skyfallerIncoming, mission.shuttle);
						GenSpawn.Spawn(vehicleSkyfaller_Arriving, target.Cell, mapToSpawnIn, Rot4.East);
					},
					(LocalTargetInfo info) => { var bay = info.Cell.GetThingList(mapToSpawnIn).Where(t => t.TryGetComp<CompShipBay>() != null)?.FirstOrDefault(); return bay==null || bay.TryGetComp<CompShipBay>().CanFitShuttleSize(mission.shuttle)!=IntVec3.Zero; }, null, null, mission.shuttle.VehicleDef.rotatable && !(mission.shuttle.CompVehicleLauncher.launchProtocol.LandingProperties?.forcedRotation).HasValue, forcedTargeting: true);
				}
				else //enemy shuttles - never returns?
				{
					IntVec3 vec = IntVec3.Zero;
					foreach (var bay in mapToSpawnInComp.Bays)
					{
						vec = bay.CanFitShuttleSize(mission.shuttle);
						if (vec != IntVec3.Zero)
						{
							bay.ReserveArea(vec, mission.shuttle);
							break;
						}
					}

					if (ModSettings_SoS.shipMapPhysics && !ShipInteriorMod2.IsPod(mission.shuttle) && mapToSpawnInComp.MapEnginePower > 0.02f) //Restricted boarding, return if shuttle
					{
						Messages.Message("SoS.EnemyBoardingShuttleAborted".Translate(), MessageTypeDefOf.NeutralEvent);
						ShuttleMissionData newMission = mapToSpawnInComp.targetMapComp.RegisterShuttleMission(mission.shuttle, ShuttleMission.RETURN);
						newMission.rangeTraveled = OriginMapComp.Range;
						newMission.liftedOffYet = true;
						return;
					}

					if (vec == IntVec3.Zero) //fallbacks
					{
						var ship = mapToSpawnInComp.ShipsOnMap.Values.Where(s => !s.IsWreck)?.RandomElement();
						if (ship == null)
							ship = mapToSpawnInComp.ShipsOnMap.Values.Where(s => s.IsWreck)?.RandomElement();
						if (ship != null)
						{
							int i = 0;
							while (i < 10)
							{
								IntVec3 v = ship.OuterCells().RandomElement();
								vec = FindTargetForPod(mission, v);
								if (vec != null)
									break;
								i++;
							}
						}
					}

					Messages.Message("SoS.EnemyBoardingShuttleArrived".Translate(), MessageTypeDefOf.NegativeEvent);
					VehicleSkyfaller_Arriving vehicleSkyfaller_Arriving = (VehicleSkyfaller_Arriving)VehicleSkyfallerMaker.MakeSkyfaller(mission.shuttle.CompVehicleLauncher.Props.skyfallerIncoming, mission.shuttle);
					GenSpawn.Spawn(vehicleSkyfaller_Arriving, vec, mapToSpawnIn, Rot4.East);
				}
			}
        }
		public enum ShuttleMission
        {
			BOARD, //go to ShipCombatTargetMap, attempt boarding action
			RETURN, //return to map you started the previous mission from, attempt landing action
			INTERCEPT, //fly to close range, intercept enemy shuttles and projectiles
			STRAFE, //fly to far range, shoot at enemy ship
			BOMB //fly to furthest range, shoot at enemy ship, rturn when out of torpedoes
		}
		public class ShuttleMissionData : IExposable, ILoadReferenceable
        {
			public VehiclePawn shuttle;
			public ShuttleMission mission;
			public float rangeTraveled;
			public float retreatAtHealth;
			public float weaponCooldown;
			public bool liftedOffYet;
			public int uniqueID;

            public void ExposeData()
            {
				Scribe_References.Look<VehiclePawn>(ref shuttle, "shuttle");
				Scribe_Values.Look<ShuttleMission>(ref mission, "mission");
				Scribe_Values.Look<float>(ref rangeTraveled, "rangeTraveled");
				Scribe_Values.Look<float>(ref retreatAtHealth, "retreatAtHealth");
				Scribe_Values.Look<float>(ref weaponCooldown, "weaponCooldown");
				Scribe_Values.Look<bool>(ref liftedOffYet, "liftedOffYet");
				Scribe_Values.Look<int>(ref uniqueID, "uniqueID");
			}

			public static string MissionGerund(ShuttleMission mission)
            {
				switch (mission)
				{
					case ShuttleMission.BOARD:
						return "Boarding";
					case ShuttleMission.INTERCEPT:
						return "Intercepting";
					case ShuttleMission.STRAFE:
						return "Strafing";
					case ShuttleMission.BOMB:
						return "Bombing";
					default:
						return "Retreating";
				}
            }

            public string GetUniqueLoadID()
            {
				return "Mission_" + uniqueID;
            }
		}
		public IntVec3 FindTargetForPod(ShuttleMissionData mission, IntVec3 v)
		{
			if (!ModSettings_SoS.shipMapPhysics) //Default fallback, find landing spot next to ship's outer rooms
			{
				var result = IntVec3.Invalid;
				CellFinder.TryFindRandomCellNear(v, map, 9, (IntVec3 cell) => { return cell.Standable(map) && !map.roofGrid.Roofed(cell); }, out result);
				if (result == IntVec3.Invalid)
					return v; //Fallback, hope this won't have to be used
				return result;
			}
			else //Restricted boarding fallback, pods punch holes in player ship
			{
				foreach (IntVec3 vec in GenAdj.CellsAdjacent8Way(v, Rot4.North, new IntVec2(7, 7)))
				{
					Room room = vec.GetRoom(map);
					if (vec.InBounds(map) && vec.Standable(map) && room != null && !room.TouchesMapEdge && !room.IsDoorway && !AnyBridgeIn(room))
					{
						if (!vec.GetThingList(map).Any(t => t.def.preventSkyfallersLandingOn))
						{
							return vec;
						}
					}
				}
			}
			return IntVec3.Zero;
		}
		public bool AnyBridgeIn(Room room)
		{
			List<Region> regions = room.Regions;
			for (int i = 0; i < regions.Count; i++)
			{
				if (regions[i].ListerThings.AllThings.Any(t => t is Building_ShipBridge))
				{
					return true;
				}
			}
			return false;
		}
	}
}
