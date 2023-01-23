using System;
using Verse;
using RimWorld;
using RimWorld.Planet;
using HugsLib.Utils;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using Verse.AI.Group;

namespace SaveOurShip2
{
	public static class WorldSwitchUtility
	{
		public static bool SelectiveWorldGenFlag = false;
        public static bool FactionRelationFlag = false;
        public static bool LoadWorldFlag = false;
		public static Faction SavedPlayerFaction=null;
		public static Map SavedMap=null;
		public static UniqueIDsManager Uniques=null;
		public static WorldPawns Pawns=null;
		public static FactionManager Factions = null;
        public static IdeoManager Ideos = null;
		private static PastWorldUWO2 PastWorldTrackerInternal = null;
        public static World SoonToBeObsoleteWorld = null;
        public static bool planetkiller = false;
        public static bool NoRecache = false;
        public static List<ScenPart> CachedScenario;

        static Faction DonatedToFaction=null;
        static float DonatedAmount=0;
        static List<UtilityWorldObject> tmpUWOs = new List<UtilityWorldObject>();
        static List<WorldComponent> tmpComponents = new List<WorldComponent>();

        public static PastWorldUWO2 PastWorldTracker
        {
            get
            {
                if (PastWorldTrackerInternal == null && Find.World != null)
                    PastWorldTrackerInternal = (PastWorldUWO2)Find.World.components.Where(x => x is PastWorldUWO2).First();
                return PastWorldTrackerInternal;
            }
        }

        public static void PurgePWT()
        {
            PastWorldTrackerInternal = null;
        }

        public static void ColonyAbandonWarning(Action action)
        {
            DiaNode theNode;
            if(Find.Scenario.AllParts.Any(p => p.def.defName.Equals("GameCondition_Planetkiller")))
            {
                theNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("ShipAbandonColoniesWarningPK"));
                planetkiller = true;
            }
            else
                theNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("ShipAbandonColoniesWarning"));

            DiaOption accept = new DiaOption("Accept");
            accept.resolveTree = true;
            List<Faction> alliedFactions = new List<Faction>();
            foreach(Faction f in Find.FactionManager.AllFactionsVisible)
            {
                if (f != Faction.OfPlayer && f.PlayerRelationKind == FactionRelationKind.Ally)
                    alliedFactions.Add(f);
            }
            if (alliedFactions.Any() && !planetkiller)
                accept.action = delegate { ChooseAllyToTakeColony(action, alliedFactions); };
            else
                accept.action = delegate { AbandonColony(action); };
            theNode.options.Add(accept);

            DiaOption cancel = new DiaOption("Cancel");
            cancel.resolveTree = true;
            theNode.options.Add(cancel);

            Dialog_NodeTree dialog_NodeTree = new Dialog_NodeTree(theNode, true, false, null);
            dialog_NodeTree.silenceAmbientSound = false;
            dialog_NodeTree.closeOnCancel = true;
            Find.WindowStack.Add(dialog_NodeTree);
        }

        private static void ChooseAllyToTakeColony(Action action, List<Faction> alliedFactions)
        {
            DiaNode theNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("ChooseAllyToTakeColony"));

            foreach (Faction f in alliedFactions)
            {
                DiaOption option = new DiaOption(f.Name);
                option.resolveTree = true;
                option.action = delegate { AllyTakeColony(f);  action.Invoke(); };
                theNode.options.Add(option);
            }

            DiaOption none = new DiaOption("None - leave the colonists to their fate");
            none.resolveTree = true;
            none.action = delegate { AbandonColony(action); };
            theNode.options.Add(none);

            DiaOption cancel = new DiaOption("Cancel");
            cancel.resolveTree = true;
            theNode.options.Add(cancel);

            Dialog_NodeTree dialog_NodeTree = new Dialog_NodeTree(theNode, true, false, null);
            dialog_NodeTree.silenceAmbientSound = false;
            dialog_NodeTree.closeOnCancel = true;
            Find.WindowStack.Add(dialog_NodeTree);
        }

        private static void AllyTakeColony(Faction f)
        {
            List<Map> colonies = new List<Map>();
            foreach(Map m in Find.Maps)
            {
                if(m.IsPlayerHome && !(m.Parent is WorldObjectOrbitingShip))
                {
                    colonies.Add(m);
                }
            }
            foreach(Map m in colonies)
            {
                m.wealthWatcher.ForceRecount();
                DonatedToFaction = f;
                DonatedAmount=m.wealthWatcher.WealthTotal;
                List<Pawn> pawnsToDonate = new List<Pawn>();
                foreach (Pawn p in m.mapPawns.FreeColonistsAndPrisonersSpawned)
                {
                    pawnsToDonate.Add(p);
                }
                foreach(Pawn p in pawnsToDonate)
                {
                    p.SetFaction(f);
                    p.DeSpawn();
                    Find.WorldPawns.PassToWorld(p);
                }
                int tile = m.Parent.Tile;
                Find.WorldObjects.Remove(m.Parent);
                WorldObject newSettlement = WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                newSettlement.SetFaction(f);
                newSettlement.Tile = tile;
                if (m.Parent is Settlement)
                    ((Settlement)newSettlement).Name = ((Settlement)m.Parent).Name;
                Find.WorldObjects.Add(newSettlement);
            }

        }

        private static void AbandonColony(Action action)
        {
            List<Map> colonies = new List<Map>();
            foreach (Map m in Find.Maps)
            {
                if (m.IsPlayerHome && !(m.Parent is WorldObjectOrbitingShip))
                {
                    colonies.Add(m);
                }
            }
            foreach(Map m in colonies)
            {
                int tile = m.Parent.Tile;
                Find.WorldObjects.Remove(m.Parent);
                WorldObject newSettlement = WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.AbandonedSettlement);
                newSettlement.SetFaction(Faction.OfPlayer);
                newSettlement.Tile = tile;
                Find.WorldObjects.Add(newSettlement);
            }
            action.Invoke();
        }

        private static void KillAllColonistsNotInCrypto(Map shipMap, Building_ShipBridge bridge)
        {
            List<Pawn> toKill = new List<Pawn>();
            foreach (Pawn p in shipMap.mapPawns.AllPawns)
            {
                if (p.RaceProps != null && p.RaceProps.IsFlesh && (!p.InContainerEnclosed) && (!ShipInteriorMod2.IsHologram(p) || p.health.hediffSet.GetFirstHediff<HediffPawnIsHologram>().consciousnessSource.Map!=shipMap))
                    toKill.Add(p);
            }
            foreach (Pawn p in toKill)
                p.Kill(null);
            foreach(Thing t in shipMap.spawnedThings)
            {
                if (t is Corpse c)
                {
                    var compRot = c.GetComp<CompRottable>();
                    if (t.GetRoom() != null && t.GetRoom().Temperature > 0 && compRot != null)
                        compRot.RotProgress = compRot.PropsRot.TicksToDessicated;
                }
            }

            ShipInteriorMod2.renderedThatAlready = false;

            float EnergyCost = 100000;
            foreach(CompPowerBattery capacitor in bridge.PowerComp.PowerNet.batteryComps)
            {
                if(capacitor.StoredEnergy <= EnergyCost)
                {
                    capacitor.SetStoredEnergyPct(capacitor.StoredEnergyPct - (EnergyCost / capacitor.Props.storedEnergyMax));
                    EnergyCost = 0;
                    break;
                }
                else
                {
                    EnergyCost -= capacitor.StoredEnergy;
                    capacitor.SetStoredEnergyPct(0);
                }
            }

            //Oddly enough, interstellar flight takes a lot of time
            int years = Rand.RangeInclusive(ShipInteriorMod2.minTravelTime.Value, ShipInteriorMod2.maxTravelTime.Value);
            Current.Game.tickManager.DebugSetTicksGame(Current.Game.tickManager.TicksAbs + 3600000 * years);
            Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoSTimePassedLabel"), TranslatorFormattedStringExtensions.Translate("SoSTimePassed",years), LetterDefOf.NeutralEvent);
        }

        public static void SwitchToNewWorld(Map shipMap, Building_ShipBridge bridge)
		{
            KillAllColonistsNotInCrypto(shipMap, bridge);
			SaveUniqueIDsFactionsAndWorldPawns ();
			Find.World.components.Remove (PastWorldTracker);
            SavedMap = shipMap;
            WorldSwitchUtility.KillAllMapsExceptShip();
            Current.Game.InitData = new GameInitData();
			Current.Game.InitData.playerFaction = Faction.OfPlayer;
			SelectiveWorldGenFlag=true;
			SavedPlayerFaction = Faction.OfPlayer;
			Find.WorldInterface.selector.ClearSelection();
            SoonToBeObsoleteWorld = Find.World;
            CacheFactions(SoonToBeObsoleteWorld.info.name);
            List<ScenPart> scenarioPartsToRemove = new List<ScenPart>();
            CachedScenario = new List<ScenPart>();
            foreach(ScenPart part in Find.Scenario.AllParts)
            {
                if (part.def.category == ScenPartCategory.StartingItem || part.def.category == ScenPartCategory.WorldThing || part.def.category == ScenPartCategory.StartingImportant || part.def.category == ScenPartCategory.Fixed || part.def.category == ScenPartCategory.PlayerPawnFilter || part.def.defName.Equals("GameStartDialog"))
                    scenarioPartsToRemove.Add(part);
                else
                    CachedScenario.Add(part);
            }
            foreach (ScenPart part in scenarioPartsToRemove)
                Find.Scenario.RemovePart(part);
            Find.SoundRoot.sustainerManager.EndAllInMap(shipMap);
            Page thePage = new Page_ScenarioEditor(Find.Scenario);
            thePage.silenceAmbientSound = true;
            thePage.next= new Page_CreateWorldParams();
            thePage.next.next = new Page_ConfigureIdeo();
            IdeoUIUtility.selected = Faction.OfPlayer.ideos.PrimaryIdeo;
            Find.WindowStack.Add(thePage);
		}

		public static void ReturnToPreviousWorld(Map shipMap, Building_ShipBridge bridge)
        {
            SaveUniqueIDsFactionsAndWorldPawns ();
            DiaNode theNode = new DiaNode ("ShipPlanetReturnTo".Translate ());
			foreach (PreviousWorld w in PastWorldTracker.PastWorlds) {
				DiaOption worldOption = new DiaOption (w.info.name);
				worldOption.action = delegate {
                    KillAllColonistsNotInCrypto(shipMap, bridge);
                    List<ScenPart> scenarioPartsToRemove = new List<ScenPart>();
                    CachedScenario = new List<ScenPart>();
                    foreach (ScenPart part in Find.Scenario.AllParts)
                    {
                        if (part.def.category == ScenPartCategory.StartingItem || part.def.category == ScenPartCategory.WorldThing || part.def.category == ScenPartCategory.StartingImportant || part.def.category == ScenPartCategory.Fixed || part.def.category == ScenPartCategory.PlayerPawnFilter || part.def.defName.Equals("GameStartDialog"))
                            scenarioPartsToRemove.Add(part);
                        else
                            CachedScenario.Add(part);
                    }
                    foreach (ScenPart part in scenarioPartsToRemove)
                        Find.Scenario.RemovePart(part);
                    DoWorldSwitch(shipMap,w);
				};
				worldOption.resolveTree = true;
				theNode.options.Add (worldOption);
			}
			DiaOption cancel = new DiaOption ("Cancel");
			cancel.resolveTree = true;
			theNode.options.Add (cancel);

			Dialog_NodeTree dialog_NodeTree = new Dialog_NodeTree(theNode, true, false, null);
			dialog_NodeTree.silenceAmbientSound = false;
			dialog_NodeTree.closeOnCancel = true;
			Find.WindowStack.Add(dialog_NodeTree);
		}

		static void DoWorldSwitch(Map shipMap, PreviousWorld w)
        {
            Find.World.components.Remove (PastWorldTracker);
            foreach (WorldObject ob in Find.World.worldObjects.AllWorldObjects)
            {
                if (ob is UtilityWorldObject)
                    tmpUWOs.Add((UtilityWorldObject)ob);
            }
            foreach (UtilityWorldObject uwo in tmpUWOs)
            {
                //Compatibility issues with PostRemove stuff, so we do this manually via reflection
                ((List<WorldObject>)typeof(WorldObjectsHolder).GetField("worldObjects", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Find.World.worldObjects)).Remove(uwo);
                typeof(WorldObjectsHolder).GetMethod("RemoveFromCache", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Find.World.worldObjects, new object[] { uwo });
            }
            foreach (WorldComponent comp in Find.World.components)
            {
                if (!(comp is TileTemperaturesComp) && !(comp is WorldGenData))
                    tmpComponents.Add(comp);
            }
            foreach (WorldComponent comp in tmpComponents)
                Find.World.components.Remove(comp);

            PastWorldTracker.PastWorlds.Add (PreviousWorldFromWorld(Find.World));
            Current.Game.InitData = new GameInitData();
            Current.Game.InitData.playerFaction = Faction.OfPlayer;
            SelectiveWorldGenFlag =true;
			SavedPlayerFaction = Faction.OfPlayer;
			SavedMap = shipMap;
            Find.WorldInterface.selector.ClearSelection();
            PastWorldTracker.PastWorlds.Remove (w);
            KillAllMapsExceptShip();

            Current.Game.World = WorldFromPreviousWorld(w);
            LoadUniqueIDsFactionsAndWorldPawns ();
            WorldComponent obToRemove = null;
            foreach(WorldComponent ob in Current.Game.World.components)
            {
                if (ob is PastWorldUWO2)
                    obToRemove = ob;
            }
            if(obToRemove != null)
                Current.Game.World.components.Remove(obToRemove);
            Current.Game.World.components.Add (PastWorldTracker);
            Current.Game.World.features.UpdateFeatures();

            foreach (UtilityWorldObject uwo in tmpUWOs)
            {
                Find.WorldObjects.Add(uwo);
            }
            WorldComponent toReplace;
            foreach (WorldComponent comp in tmpComponents)
            {
                toReplace = null;
                foreach (WorldComponent otherComp in Find.World.components)
                {
                    if (otherComp.GetType() == comp.GetType())
                        toReplace = otherComp;
                }
                if (toReplace != null)
                    Find.World.components.Remove(toReplace);
                Find.World.components.Add(comp);
            }

            Find.World.renderer.RegenerateAllLayersNow();
            RespawnShip ();
            if(w.donatedFaction != null && w.donatedAmount >= 10000)
            {
                ThingSetMakerParams parms = default(ThingSetMakerParams);
                parms.totalMarketValueRange = new FloatRange(Mathf.Min(DonatedAmount / 8, 50000), Mathf.Min(DonatedAmount / 16,30000));
                List<Thing> FactionGifts = ThingSetMakerDefOf.Reward_ItemsStandard.root.Generate(parms);
                string loot = "";
                foreach (Thing t in FactionGifts)
                    loot += t.Label + "\n";
                Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("LetterLabelPlanetReturnGift"), TranslatorFormattedStringExtensions.Translate("LetterPlanetReturnGift",DonatedToFaction,loot), LetterDefOf.PositiveEvent);
                IntVec3 intVec = DropCellFinder.TradeDropSpot(SavedMap);
                DropPodUtility.DropThingsNear(intVec, SavedMap, FactionGifts, 110, false, false, false);
            }
        }

		public static void RespawnShip()
		{
            List<WorldObject> oldShips = new List<WorldObject>();
            foreach (WorldObject ob in Find.WorldObjects.AllWorldObjects)
            {
                if (ob.def.defName.Equals("ShipOrbiting"))
                    oldShips.Add(ob);
            }
			WorldObjectOrbitingShip orbiter = (WorldObjectOrbitingShip)WorldObjectMaker.MakeWorldObject (DefDatabase<WorldObjectDef>.GetNamed ("ShipOrbiting"));
			orbiter.radius = 150;
			orbiter.theta = -3;
			orbiter.SetFaction (Faction.OfPlayer);
			Find.WorldObjects.Add (orbiter);
			WorldSwitchUtility.SavedMap.info.parent = orbiter;
			WorldSwitchUtility.SelectiveWorldGenFlag = false;
            foreach (WorldObject ob in oldShips)
            {
                //Compatibility issues with PostRemove stuff, so we do this manually via reflection
                ((List<WorldObject>)typeof(WorldObjectsHolder).GetField("worldObjects", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Find.World.worldObjects)).Remove(ob);
                typeof(WorldObjectsHolder).GetMethod("RemoveFromCache", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Find.World.worldObjects, new object[] { ob });

            }
        }

		static void SaveUniqueIDsFactionsAndWorldPawns()
		{
			Uniques = Find.UniqueIDsManager;
			Pawns = Find.WorldPawns;
			Factions = Find.FactionManager;
            Ideos = Find.IdeoManager;
		}

		public static void LoadUniqueIDsFactionsAndWorldPawns()
		{
			Current.Game.uniqueIDsManager = Uniques;
            if (Find.World.worldPawns == null)
            {
                Find.World.worldPawns = new WorldPawns();
            }
            if (Find.World.factionManager == null)
            {
                Find.World.factionManager = new FactionManager();
            }
            NoRecache = true;
            foreach (Faction f in ((List<Faction>)typeof(FactionManager).GetField("allFactions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Factions)))
            {
                Find.FactionManager.Add(f);
                if (f.def.isPlayer)
                {
                    Find.GameInitData.playerFaction = f;
                    typeof(FactionManager).GetField("ofPlayer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(Find.FactionManager, f);
                }
            }
            foreach(Ideo i in Ideos.IdeosListForReading)
            {
                Find.IdeoManager.Add(i);
            }
            NoRecache = false;
            typeof(FactionManager).GetMethod("RecacheFactions", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Find.FactionManager, new object[] { });
            foreach (Pawn p in Pawns.AllPawnsAliveOrDead)
            {
                if (!Find.WorldPawns.Contains(p))
                {
                    Find.WorldPawns.PassToWorld(p);
                }
            }
		}

		public static void KillAllMapsExceptShip()
		{
            List<Map> SettlementsToKill = new List<Map>();
			foreach (Map m in Find.Maps) {
				if (m != WorldSwitchUtility.SavedMap) {
                    SettlementsToKill.Add(m);
				}
			}
            foreach(Map m in SettlementsToKill)
            {
                typeof(SettlementAbandonUtility).GetMethod("Abandon", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { ((MapParent)m.ParentHolder) });
            }
		}

        public static World WorldFromPreviousWorld(PreviousWorld prev)
        {
            World world = new World();
            world.ConstructComponents();
            world.pathGrid = new WorldPathGrid();
            world.info = prev.info;
            world.components = prev.components;
            world.worldObjects = prev.worldObjects;
            world.features = prev.features;
            world.grid = prev.grid;
            List<ScenPart> parts = new List<ScenPart>();
            foreach(ScenPart part in Find.Scenario.AllParts)
                parts.Add(part);
            foreach(ScenPart part in parts)
                Find.Scenario.RemovePart(part);
            foreach (ScenPart part in prev.scenario)
            {
                if (part.def.defName.Equals("CreateIncident"))
                {
                    Type createIncident = typeof(ScenPart).Assembly.GetType("RimWorld.ScenPart_CreateIncident");
                    createIncident.GetField("occurTick", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(part, (float)createIncident.GetProperty("IntervalTicks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(part, null) + Current.Game.tickManager.TicksAbs);
                }
                ((List<ScenPart>)typeof(Scenario).GetField("parts", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Find.Scenario)).Add(part);
            }
            Find.Scenario.name = prev.scenarioName;
            Find.Scenario.summary = prev.scenarioSummary;
            Find.Scenario.description = prev.scenarioDescription;
            world.FinalizeInit();
            DonatedAmount = prev.donatedAmount;
            DonatedToFaction = prev.donatedFaction;
            return world;
        }

        public static PreviousWorld PreviousWorldFromWorld(World world)
        {
            PreviousWorld prev = new PreviousWorld();
            prev.info = world.info;
            prev.components = world.components;
            prev.worldObjects = world.worldObjects;
            prev.features = world.features;
            prev.grid = world.grid;
            prev.scenario = CachedScenario;
            prev.scenarioName = Find.Scenario.name;
            prev.scenarioSummary = Find.Scenario.summary;
            prev.scenarioDescription = Find.Scenario.description;
            prev.donatedFaction = DonatedToFaction;
            prev.donatedAmount = DonatedAmount;
            return prev;
        }

        public static void CacheFactions(string worldName)
        {
            foreach (Faction fac in Find.FactionManager.AllFactions)
            {
                bool factionDoesNotExistYet = true;
                foreach (string otherWorldName in PastWorldTracker.WorldFactions.Keys)
                {
                    foreach(string otherFacName in PastWorldTracker.WorldFactions[otherWorldName].myFactions)
                    {
                        if(fac.GetUniqueLoadID().Equals(otherFacName))
                        {
                            factionDoesNotExistYet = false;
                            break;
                        }
                    }
                }
                if(factionDoesNotExistYet)
                {
                    if (!PastWorldTracker.WorldFactions.ContainsKey(worldName))
                    {
                        PastWorldTracker.WorldFactions.Add(worldName, new WorldFactionList());
                    }
                    if (!PastWorldTracker.WorldFactions[worldName].myFactions.Contains(fac.GetUniqueLoadID()))
                    {
                        PastWorldTracker.WorldFactions[worldName].myFactions.Add(fac.GetUniqueLoadID());
                    }
                }
            }
        }

        public static IEnumerable<Faction> FactionsOnCurrentWorld(IEnumerable<Faction> allFactions)
        {
            if (Scribe.mode != LoadSaveMode.Inactive || Current.Game == null || Current.Game.World == null)
            {
                return allFactions;
            }
            if(allFactions == null)
            {
                return allFactions;
            }
            if (WorldSwitchUtility.PastWorldTracker == null)
            {
                return allFactions;
            }
            if (WorldSwitchUtility.PastWorldTracker.WorldFactions == null)
            {
                return allFactions;
            }
            if (WorldSwitchUtility.PastWorldTracker.WorldFactions.Keys.Count == 0)
            {
                return allFactions;
            }
            List<Faction> facs = new List<Faction>();
            if (WorldSwitchUtility.PastWorldTracker.WorldFactions.Keys.Contains(Find.World.info.name))
            {
                List<string> thisWorldsFactions = WorldSwitchUtility.PastWorldTracker.WorldFactions[Find.World.info.name].myFactions;
                foreach (Faction fac in allFactions)
                {
                    if (thisWorldsFactions.Contains(fac.GetUniqueLoadID()) || fac.def.isPlayer || fac.def.hidden)
                        facs.Add(fac);
                }
            }
            else if (SelectiveWorldGenFlag && FactionRelationFlag) //Look up all factions *not* yet assigned to any world. Used to make initial relations.
            {
                foreach (Faction fac in allFactions)
                {
                    if (fac.def.isPlayer || fac.def.hidden)
                        facs.Add(fac);
                    else
                    {
                        bool FoundFaction = false;
                        foreach (string key in WorldSwitchUtility.PastWorldTracker.WorldFactions.Keys)
                        {
                            if (WorldSwitchUtility.PastWorldTracker.WorldFactions[key].myFactions.Contains(fac.GetUniqueLoadID()))
                            {
                                FoundFaction = true;
                                break;
                            }
                        }
                        if (!FoundFaction)
                        {
                            facs.Add(fac);
                        }
                    }
                }
            }
            return facs;
        }
	}

    //Despite what common sense might tell you, this object does more than store past worlds. It also holds SoS story progress and research unlocks, as well as ship combat data.
	public class PastWorldUWO2 : WorldComponent
    {
        private int ShipsHaveInsidesVersion;
        public int PlayerFactionBounty;
        public int LastBountyRaidTick;				   
		public List<PreviousWorld> PastWorlds=new List<PreviousWorld>();
        public Dictionary<string, WorldFactionList> WorldFactions = new Dictionary<string, WorldFactionList>();
        private List<string> UnlocksInt = new List<string>();
        public bool startedEndgame;
        public Dictionary<int, byte> PawnsInSpaceCache = new Dictionary<int, byte>();
        public List<Building_ShipAdvSensor> Sensors = new List<Building_ShipAdvSensor>();

        public PastWorldUWO2(World world) : base(world)
        {

        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (!Find.FactionManager.AllFactions.Any(f => f.def == FactionDefOf.Mechanoid))
                Log.Warning("SOS2: Mechanoid faction not found! Parts of SOS2 will likely fail to function properly!");
            if (!Find.FactionManager.AllFactions.Any(f => f.def == FactionDefOf.Insect))
                Log.Warning("SOS2: Insect faction not found! SOS2 gameplay experience will be affected.");
        }

        public List<string> Unlocks
        {
            get
            {
                if (UnlocksInt == null)
                    UnlocksInt = new List<string>();
                return UnlocksInt;
            }
        }

		public override void ExposeData() {
			base.ExposeData ();
            Scribe_Values.Look<int>(ref ShipsHaveInsidesVersion,"SoSVersion",0);
            Scribe_Collections.Look<string, WorldFactionList>(ref WorldFactions, "WorldFactions", LookMode.Value, LookMode.Deep);
            WorldSwitchUtility.LoadWorldFlag = true;
			Scribe_Collections.Look<PreviousWorld> (ref PastWorlds, "PastWorlds", LookMode.Deep, new object[0]);
            WorldSwitchUtility.LoadWorldFlag = false;
            Scribe_Collections.Look<string>(ref UnlocksInt, "Unlocks", LookMode.Value);
            Scribe_Values.Look<int>(ref PlayerFactionBounty, "PlayerFactionBounty", 0);
            Scribe_Values.Look<int>(ref LastBountyRaidTick, "LastBountyRaidTicks", 0);
            Scribe_Values.Look<bool>(ref startedEndgame, "StartedEndgame");
            Scribe_Values.Look<int>(ref IncidentWorker_ShipCombat.LastAttackTick, "LastShipBattleTick");


            /*if (Scribe.mode!=LoadSaveMode.Saving)
            {
                if(Unlocks.Contains("JTDrive")) //Back-compatibility: unlock JT drive research project if you got it before techprints were a thing
                {
                    Find.ResearchManager.FinishProject(ResearchProjectDef.Named("SoSJTDrive"));
                    Unlocks.Remove("JTDrive");
                }
                if (!Unlocks.Contains("JTDriveToo")) //Legacy compatibility for back when policies were different and a certain developer's head was still outside his own ass
                {
                    if (!Unlocks.Contains("JTDriveResearchChecked") && Find.ResearchManager.GetProgress(ResearchProjectDef.Named("SoSJTDrive")) >= 4000) //Hey, if you've already finished this research, you deserve special commemmoration!
                    {
                        Unlocks.Add("JTDriveToo");
                        GiveMeEntanglementManifold();
                    }
                    else //Let's check another way!
                    {
                        foreach (FieldInfo field in typeof(ResearchManager).GetFields()) //Let's randomly look at fields inside the research manager!
                        {
                            if (field.FieldType == typeof(Dictionary<ResearchProjectDef, int>)) //Hmm, any sort of dictionary of research projects and integers must be important!
                            {
                                if (((Dictionary<ResearchProjectDef, int>)field.GetValue(Find.ResearchManager)).ContainsKey(ResearchProjectDef.Named("SoSJTDrive"))) //Hey, if the JT drive gets mentioned in such an important place, maybe it means you already found one!
                                {
                                    Unlocks.Add("JTDriveToo");
                                    GiveMeEntanglementManifold();
                                    ((Dictionary<ResearchProjectDef, int>)field.GetValue(Find.ResearchManager)).Remove(ResearchProjectDef.Named("SoSJTDrive")); //Remove the NASTY EVIL FORBIDDEN DATA!
                                }
                            }
                        }
                    }
                    if(!Unlocks.Contains("JTDriveResearchChecked"))
                        Unlocks.Add("JTDriveResearchChecked");
                }
            }
            //recover from incorrect savestates
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!ShipCombatManager.InCombat && !ShipCombatManager.InEncounter)
                {
                    if (ShipCombatManager.EnemyShip == null
                        && (ShipCombatManager.CanSalvageEnemyShip || ShipCombatManager.ShouldSalvageEnemyShip))
                    {
                        Log.Error("Recovering from incorrect state regarding enemy ship in save file. If there was an enemy ship, it is now lost and cannot be salvaged.");
                        ShipCombatManager.CanSalvageEnemyShip = false;
                        ShipCombatManager.ShouldSalvageEnemyShip = false;
                        ShipCombatManager.ShouldSkipSalvagingEnemyShip = false;
                    }
                }
            }*/
        }
		public override void WorldComponentTick()
        {
            if (Find.TickManager.TicksGame % 600 == 0)
            {
                var check = ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def.defName.Equals("ShipOrbiting")).FirstOrDefault());
                if (check == null)
                    return;
                Map map = check.Map;
                if (map != null && PlayerFactionBounty > 20 && Find.TickManager.TicksGame-LastBountyRaidTick > Mathf.Max(600000f / Mathf.Sqrt(PlayerFactionBounty),60000f) && !map.GetComponent<ShipHeatMapComp>().InCombat)
                {
                    LastBountyRaidTick = Find.TickManager.TicksGame;
                    Building_ShipBridge bridge = map.listerBuildings.AllBuildingsColonistOfClass<Building_ShipBridge>().FirstOrDefault();
                    if (bridge == null)
                        return;
                    map.GetComponent<ShipHeatMapComp>().StartShipEncounter(bridge, bounty : true);
                }
            }
        }

        private void GiveMeEntanglementManifold()
        {
            IncidentParms parms = new IncidentParms();
            parms.target = Find.World;
            parms.forced = true;
            QueuedIncident qi = new QueuedIncident(new FiringIncident(IncidentDef.Named("SoSFreeEntanglement"), null, parms),Find.TickManager.TicksGame, Find.TickManager.TicksGame+99999999);
            Find.Storyteller.incidentQueue.Add(qi);
        }
	}

    //Included for legacy compatibility
    public class PastWorldUWO : UtilityWorldObject
    {
        public List<PreviousWorld> PastWorlds = new List<PreviousWorld>();
        public Dictionary<string, WorldFactionList> WorldFactions = new Dictionary<string, WorldFactionList>();
        private List<string> UnlocksInt = new List<string>();

        public PastWorldUWO()
        {

        }

        public List<string> Unlocks
        {
            get
            {
                if (UnlocksInt == null)
                    UnlocksInt = new List<string>();
                return UnlocksInt;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look<PreviousWorld>(ref PastWorlds, "PastWorlds", LookMode.Deep, new object[0]);
            Scribe_Collections.Look<string, WorldFactionList>(ref WorldFactions, "WorldFactions", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look<string>(ref UnlocksInt, "Unlocks", LookMode.Value);
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                PastWorldUWO2 newUWO = (PastWorldUWO2)Find.World.components.Where(x => x is PastWorldUWO2)?.FirstOrDefault();
                if(newUWO == null)
                {
                    newUWO = new PastWorldUWO2(Find.World);
                    Find.World.components.Add(newUWO);
                }
                newUWO.PastWorlds = PastWorlds;
                newUWO.WorldFactions = WorldFactions;
                typeof(PastWorldUWO2).GetField("UnlocksInt", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(newUWO, UnlocksInt);
                Find.World.worldObjects.Remove(this);
            }

        }
    }

    public class WorldFactionList : IExposable
    {
        public List<string> myFactions = new List<String>();

        public void ExposeData()
        {
            Scribe_Collections.Look<string>(ref myFactions, "MyFactions", LookMode.Value);
        }
    }
}

