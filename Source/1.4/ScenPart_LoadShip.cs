using RimworldMod;
using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorld
{
    class ScenPart_LoadShip : ScenPart
    {
        static readonly string FILENAME_NONE = "Select ship to load";

        string filename = "Select ship to load";
        bool discardLog;
        bool discardTales;
        string playerFactionName;
        FactionDef playerFactionDef;
        List<GameComponent> components;
        public Ideo playerFactionIdeo;
        List<Ideo> ideosAboardShip;
        HashSet<CustomXenotype> xenosAboardShip;
        TickManager tickManager;
        CustomXenogermDatabase customXenogermDatabase;
        List<Thing> toLoad;
        List<Zone> zonesToLoad;
        List<IntVec3> terrainPos;
        List<TerrainDef> terrainDefs;
        List<IntVec3> roofPos;
        List<RoofDef> roofDefs;
        PlaySettings playSettings;
        StoryWatcher storyWatcher;
        ResearchManager researchManager;
        TaleManager taleManager;
        PlayLog playLog;
        OutfitDatabase outfitDatabase;
        DrugPolicyDatabase drugPolicyDatabase;
        FoodRestrictionDatabase foodRestrictionDatabase;

        public override bool CanCoexistWith(ScenPart other)
        {
            return !(other is ScenPart_StartInSpace || other is ScenPart_AfterlifeVault);
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref filename, "filename");
        }
        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 3f);
            Rect rect1 = new Rect(scenPartRect.x, scenPartRect.y, scenPartRect.width, scenPartRect.height / 3f);
            Rect rect2 = new Rect(scenPartRect.x, scenPartRect.y + scenPartRect.height / 3f, scenPartRect.width, scenPartRect.height / 3f);
            Rect rect3 = new Rect(scenPartRect.x, scenPartRect.y + 2 * scenPartRect.height / 3f, scenPartRect.width, scenPartRect.height / 3f);
            if (!HasValidFilename())
                LoadLatest();
            if (Widgets.ButtonText(rect1, filename))
            {
                FloatMenuUtility.MakeMenu(Directory.GetFiles(Path.Combine(GenFilePaths.SaveDataFolderPath, "SoS2")), (string path) => Path.GetFileNameWithoutExtension(path), (string path) => () => { filename = Path.GetFileNameWithoutExtension(path); });
            }
            if (Widgets.ButtonText(rect2, "Discard log: " + discardLog.ToString()))
            {
                List<FloatMenuOption> toggleLog = new List<FloatMenuOption>();
                toggleLog.Add(new FloatMenuOption("Discard log: True", delegate ()
                {
                    discardLog = true;
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
                toggleLog.Add(new FloatMenuOption("Discard log: False", delegate ()
                {
                    discardLog = false;
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
                Find.WindowStack.Add(new FloatMenu(toggleLog));
            }
            if (Widgets.ButtonText(rect3, "Discard tales: " + discardTales.ToString()))
            {
                List<FloatMenuOption> toggleTales = new List<FloatMenuOption>();
                toggleTales.Add(new FloatMenuOption("Discard tales: True", delegate ()
                {
                    discardTales = true;
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
                toggleTales.Add(new FloatMenuOption("Discard tales: False", delegate ()
                {
                    discardTales = false;
                }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
                Find.WindowStack.Add(new FloatMenu(toggleTales));
            }
        }
        public override string Summary(Scenario scen)
        {
            if (HasValidFilename())
                return "Load ship " + filename + "\nThis will disable many other types of scenario part, such as starting pawns.";
            return "";
        }

        public bool HasValidFilename()
        {
            return filename != FILENAME_NONE;
        }

        public override void PreConfigure()
        {
            base.PreConfigure();
            LoadLatest();
        }
        private void LoadLatest() //load latest save as default
        {
            var directory = new DirectoryInfo(Path.Combine(GenFilePaths.SaveDataFolderPath, "SoS2"));
            var mostRecentFile = directory.GetFiles().OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
            if (mostRecentFile != null)
                filename = Path.GetFileNameWithoutExtension(mostRecentFile.FullName);
        }

        public void DoEarlyInit() //Scenario.GetFirstConfigPage call via patch
        {
            WorldSwitchUtility.LoadShipFlag = true;
            LoadShip();
            //remove incompatible scenario parts - not sure if this works at all
            List<ScenPart> toRemove = new List<ScenPart>();
            foreach (ScenPart part in Find.Scenario.AllParts)
            {
                if (!ShipInteriorMod2.CompatibleWithShipLoad(part) && !(part is ScenPart_PlayerFaction))
                    toRemove.Add(part);
            }
            foreach (ScenPart part in toRemove)
                Find.Scenario.RemovePart(part);
        }
        void LoadShip()
        {
            Scribe.mode = LoadSaveMode.Inactive;

            Scribe.loader.InitLoading(Path.Combine(Path.Combine(GenFilePaths.SaveDataFolderPath, "SoS2"), filename + ".sos2"));

            Scribe_Defs.Look<FactionDef>(ref playerFactionDef, "playerFactionDef");
            Scribe_Values.Look(ref playerFactionName, "playerFactionName");
            //typeof(GameDataSaveLoader).GetField("isSavingOrLoadingExternalIdeo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, true);
            Scribe_Deep.Look(ref playerFactionIdeo, "playerFactionIdeo");
            //typeof(GameDataSaveLoader).GetField("isSavingOrLoadingExternalIdeo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, false);

            Scribe_Deep.Look<TickManager>(ref tickManager, false, "tickManager");
            Scribe_Deep.Look<PlaySettings>(ref playSettings, false, "playSettings");
            Scribe_Deep.Look<StoryWatcher>(ref storyWatcher, false, "storyWatcher");
            Scribe_Deep.Look<ResearchManager>(ref researchManager, false, "researchManager");
            Scribe_Deep.Look<TaleManager>(ref taleManager, false, "taleManager");
            Scribe_Deep.Look<OutfitDatabase>(ref outfitDatabase, false, "outfitDatabase");
            Scribe_Deep.Look<DrugPolicyDatabase>(ref drugPolicyDatabase, false, "drugPolicyDatabase");
            Scribe_Deep.Look<FoodRestrictionDatabase>(ref foodRestrictionDatabase, false, "foodRestrictionDatabase");
            Scribe_Deep.Look<UniqueIDsManager>(ref Current.Game.uniqueIDsManager, true, "uniqueIDsManager");

            //typeof(GameDataSaveLoader).GetField("isSavingOrLoadingExternalIdeo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, true);
            Scribe_Collections.Look<Ideo>(ref ideosAboardShip, "ideos", LookMode.Deep);
            //typeof(GameDataSaveLoader).GetField("isSavingOrLoadingExternalIdeo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, false);
            Scribe_Collections.Look<CustomXenotype>(ref xenosAboardShip, "xenotypes", LookMode.Deep);
            Scribe_Deep.Look<CustomXenogermDatabase>(ref customXenogermDatabase, false, "customXenogermDatabase");

            Scribe_Collections.Look<Thing>(ref toLoad, "shipThings", LookMode.Deep);
            Scribe_Collections.Look<Zone>(ref zonesToLoad, "shipZones", LookMode.Deep);
            Scribe_Collections.Look<IntVec3>(ref terrainPos, "terrainPos");
            Scribe_Collections.Look<TerrainDef>(ref terrainDefs, "terrainDefs");
            Scribe_Collections.Look<IntVec3>(ref roofPos, "roofPos");
            Scribe_Collections.Look<RoofDef>(ref roofDefs, "roofDefs");

            Scribe.mode = LoadSaveMode.Inactive;
        }
        public override void PostWorldGenerate()
        {
            if (HasValidFilename())
            {
                Faction.OfPlayer.def = playerFactionDef;
                Faction.OfPlayer.Name = playerFactionName;
                foreach (Ideo ideo in ideosAboardShip)
                    AddIdeo(ideo);
                foreach (CustomXenotype xeno in xenosAboardShip)
                    Current.Game.customXenotypeDatabase.customXenotypes.Add(xeno);
                Current.Game.tickManager = tickManager;
                Current.Game.tickManager.DebugSetTicksGame(Current.Game.tickManager.TicksAbs + 3600000 * Rand.RangeInclusive(ModSettings_SoS.minTravelTime, ModSettings_SoS.maxTravelTime));

                Current.Game.playSettings = playSettings;
                Current.Game.storyWatcher = storyWatcher;
                Current.Game.researchManager = researchManager;
                if (!discardTales)
                    Current.Game.taleManager = taleManager;
                if (Current.Game.taleManager == null)
                    Current.Game.taleManager = new TaleManager();
                if (!discardLog)
                {
                    Scribe_Deep.Look<PlayLog>(ref playLog, false, "playLog");
                    Current.Game.playLog = playLog; //playerlog calls gameStartAbsTick before set up, use GenTicks.TicksAbs
                    if (Current.Game.playLog == null)
                        Current.Game.playLog = new PlayLog();
                }
                Current.Game.outfitDatabase = outfitDatabase;
                Current.Game.drugPolicyDatabase = drugPolicyDatabase;
                Current.Game.foodRestrictionDatabase = foodRestrictionDatabase;

                foreach (CustomXenogerm xeno in customXenogermDatabase.CustomXenogermsForReading)
                    Current.Game.customXenogermDatabase.Add(xeno);

                Scribe_Collections.Look<GameComponent>(ref components, "components", LookMode.Deep); //init issue
                if (components.NullOrEmpty())
                {
                    Log.Message("comps are null");
                    return;
                }
                List<GameComponent> toClobber = new List<GameComponent>();
                foreach (GameComponent oldComp in Current.Game.components)
                {
                    foreach (GameComponent comp in components)
                    {
                        if (oldComp.GetType() == comp.GetType())
                            toClobber.Add(oldComp);
                    }
                }
                foreach (GameComponent clobber in toClobber)
                    Current.Game.components.Remove(clobber);

                foreach (GameComponent comp in components)
                    Current.Game.components.Add(comp);
                /*foreach (GameComponent component in components)
                {
                    GameComponent compToClobber = null;
                    foreach (GameComponent existingComp in Current.Game.components)
                    {
                        if (component == null || existingComp == null) //Apparently a null can sometimes sneak into this list
                            continue;
                        if (existingComp.GetType() == component.GetType())
                        {
                            compToClobber = existingComp;
                            break;
                        }
                    }
                    if (compToClobber != null)
                        Current.Game.components.Remove(compToClobber);
                    Current.Game.components.Add(component);
                }*/
            }
        }
        public static void AddIdeo(Ideo ideo)
        {
            int oldID = ideo.id;
            Find.IdeoManager.Add(ideo);
            if (ideo.id != oldID)
            {
                Ideo wrongIdeo = Find.IdeoManager.IdeosListForReading.Where(otherIdeo => otherIdeo.id == oldID).FirstOrDefault();
                if (wrongIdeo != null)
                    wrongIdeo.id = ideo.id;
                ideo.id = oldID;
            }
        }
        static void ReCacheIdeo(Ideo ideo)
        {
            foreach (Precept precept in ideo.PreceptsListForReading)
            {
                if (precept is Precept_Ritual ritual)
                {
                    foreach (RitualObligationTrigger trigger in ritual.obligationTriggers)
                    {
                        trigger.ritual = ritual;
                    }
                    if (ritual.attachableOutcomeEffect == null && !ritual.generatedAttachedReward && ritual.SupportsAttachableOutcomeEffect)
                    {
                        ritual.attachableOutcomeEffect = DefDatabase<RitualAttachableOutcomeEffectDef>.AllDefs.Where((RitualAttachableOutcomeEffectDef d) => d.CanAttachToRitual(ritual)).RandomElementWithFallback();
                        ritual.generatedAttachedReward = true;
                    }
                    if (ritual.obligationTargetFilter != null)
                    {
                        ritual.obligationTargetFilter.parent = ritual;
                    }
                }
                if (precept is Precept_Building building)
                {
                    building.presenceDemand.parent = building;
                }
            }
        }

        public static Map GenerateShipSpaceMap()
        {
            ScenPart_LoadShip scen = (ScenPart_LoadShip)Current.Game.Scenario.parts.FirstOrDefault(s => s is ScenPart_LoadShip);
            if (scen.filename != FILENAME_NONE)
            {
                int newTile = ShipInteriorMod2.FindWorldTile();
                Map spaceMap = GetOrGenerateMapUtility.GetOrGenerateMap(newTile, DefDatabase<WorldObjectDef>.GetNamed("ShipOrbiting"));
                ((WorldObjectOrbitingShip)spaceMap.Parent).radius = 150;
                ((WorldObjectOrbitingShip)spaceMap.Parent).theta = 2.75f;
                Current.ProgramState = ProgramState.MapInitializing;

                ShipInteriorMod2.AirlockBugFlag = true;

                Scribe.loader.crossRefs.ResolveAllCrossReferences();

                foreach (Thing thing in scen.toLoad)
                {
                    try
                    {
                        if (!thing.Destroyed)
                        {
                            thing.SpawnSetup(spaceMap, thing is Building_ShipBridge);
                            if(thing.def.CanHaveFaction)
                                thing.SetFaction(Faction.OfPlayer);
                            if(thing is IThingHolder holder && holder.GetDirectlyHeldThings()!=null)
                            {
                                foreach (Thing heldThing in holder.GetDirectlyHeldThings())
                                {
                                    if (heldThing.def.CanHaveFaction)
                                        heldThing.SetFaction(Faction.OfPlayer);
                                    if (heldThing is Pawn pawn)
                                        pawn.jobs.StartJob(new Verse.AI.Job(JobDefOf.Carried));
                                }
                            }
                            if (thing is Building_ShipTurret turret)
                                turret.GunCompEq.verbTracker.InitVerbsFromZero();
                            if (thing is Building_TurretGun turret2)
                                turret2.GunCompEq.verbTracker.InitVerbsFromZero();
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warning(e.Message + "\n" + e.StackTrace);
                    }
                }

                foreach (Zone zone in scen.zonesToLoad)
                {
                    zone.zoneManager = spaceMap.zoneManager;
                    spaceMap.zoneManager.RegisterZone(zone);
                }

                for (int i = 0; i < scen.terrainPos.Count; i++)
                {
                    spaceMap.terrainGrid.SetTerrain(scen.terrainPos[i], scen.terrainDefs[i]);
                }

                for (int i = 0; i < scen.roofPos.Count; i++)
                {
                    spaceMap.roofGrid.SetRoof(scen.roofPos[i], scen.roofDefs[i]);
                }

                ShipInteriorMod2.AirlockBugFlag = false;

                Current.ProgramState = ProgramState.Playing;
                IntVec2 secs = spaceMap.mapDrawer.SectionCount;
                Section[,] secArray = new Section[secs.x, secs.z];
                spaceMap.mapDrawer.sections = secArray;
                for (int i = 0; i < secs.x; i++)
                {
                    for (int j = 0; j < secs.z; j++)
                    {
                        if (secArray[i, j] == null)
                        {
                            secArray[i, j] = new Section(new IntVec3(i, 0, j), spaceMap);
                        }
                    }
                }

                spaceMap.fogGrid.ClearAllFog();
                CameraJumper.TryJump(spaceMap.Center, spaceMap);
                spaceMap.weatherManager.curWeather = ResourceBank.WeatherDefOf.OuterSpaceWeather;
                spaceMap.weatherManager.lastWeather = ResourceBank.WeatherDefOf.OuterSpaceWeather;
                spaceMap.Parent.SetFaction(Faction.OfPlayer);
                Find.MapUI.Notify_SwitchedMap();
                spaceMap.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
                foreach (Room r in spaceMap.regionGrid.allRooms)
                    r.Temperature = 21;
                AccessExtensions.Utility.RecacheSpaceMaps();
                foreach (Ideo ideo in Find.IdeoManager.IdeosInViewOrder)
                    ReCacheIdeo(ideo);

                return spaceMap;
            }
            return null;
        }
        public override void PostGameStart() //post load cleaup, open player crypto, sickness
        {
            foreach (Building b in Find.CurrentMap.listerBuildings.allBuildingsColonist.Where(b => b.TryGetComp<CompCryptoLaunchable>() != null))
            {
                Building_CryptosleepCasket c = b as Building_CryptosleepCasket;
                if (c.ContainedThing is Pawn p)
                {
                    p.needs.mood.thoughts.memories.Memories.Clear(); //clear memories as they might relate to old things
                    p.royalty = new Pawn_RoyaltyTracker(p); //reset royal everything
                    p.health.AddHediff(HediffDefOf.CryptosleepSickness, null, null, null);
                }
                c.Open();
            }
        }
    }
}
/* unfinished map for this mess of a system of scenPart, hpatches
Player view vanilla: start -> scen edit(opt) -> storyteller -> world gen -> pick spot -> pick ideo -> pick pawns -> map gen -> game


public static void SetupForQuickTestPlay()
{
	Current.ProgramState = ProgramState.Entry;
	Current.Game = new Game();
	Current.Game.InitData = new GameInitData();
	Current.Game.Scenario = ScenarioDefOf.Crashlanded.scenario;
	Find.Scenario.PreConfigure();
	Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);
	Current.Game.World = WorldGenerator.GenerateWorld(0.05f, GenText.RandomSeedString(), OverallRainfall.Normal, OverallTemperature.Normal, OverallPopulation.Normal, null, 0f);
	Find.GameInitData.ChooseRandomStartingTile();
	Find.GameInitData.mapSize = 150;
	Find.Scenario.PostIdeoChosen();
	Find.GameInitData.PrepForMapGen();
	Find.Scenario.PreMapGenerate();
}

todo exact call:
hpatch RemoveUnwantedScenPartText
hpatch FixThatBugInParticular
hpatch DoNotRemoveMyIdeo
hpatch NoNeedForMorePawns


Scenario.GetFirstConfigPage
	Page_SelectScenario.BeginScenarioConfiguration
		Current.Game.Scenario.PreConfigure();
			scenPart.PreConfigure();
		Current.Game.Scenario.GetFirstConfigPage(); - hpatch LoadTheUniqueIDs (scenPart.DoEarlyInit();)
		if above null PageUtility.InitGameStart();
	
WorldGenerator.GenerateWorld
	Current.CreatingWorld.FinalizeInit();
	Find.Scenario.PostWorldGenerate();
	scenPart.PostWorldGenerate();
	Find.Scenario.PostIdeoChosen();
	PageUtility.InitGameStart()
		Find.GameInitData.PrepForMapGen(); - hpatch FixPawnGen(return !WorldSwitchUtility.LoadShipFlag;)
		Find.Scenario.PreMapGenerate(); -> scenPart.PreMapGenerate (unused)

Game.InitNewGame()
	MapGenerator.GenerateMap - hpatch GenerateSpaceMapInstead(ScenPart_LoadShip.GenerateShipSpaceMap())
	scenPart.PostGameStart
*/
