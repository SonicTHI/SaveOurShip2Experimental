using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using Verse.Sound;
using HarmonyLib;
using System.Text;
using UnityEngine;
using Verse.AI.Group;
using RimWorld.QuestGen;
using RimworldMod;
using System.Net;
using System.IO;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq.Expressions;
using static SaveOurShip2.ModSettings_SoS;
using System.Net.NetworkInformation;
using System.Reflection;
using Verse.Noise;

namespace SaveOurShip2
{
	[StaticConstructorOnStartup]
	static class Setup
	{
		static Setup()
        {
			//manual stupidity prevention, good idea to eventually add all mod issues into it, clearly not fool proof enough yet
            if (VersionControl.CurrentMinor < ShipInteriorMod2.SOS2ReqCurrentMinor || VersionControl.CurrentBuild < ShipInteriorMod2.SOS2ReqCurrentBuild)
            {
				string error = "SOS2EXP " + ShipInteriorMod2.SOS2EXPversion + " requires Rimworld 1."+ ShipInteriorMod2.SOS2ReqCurrentMinor + "."+ ShipInteriorMod2.SOS2ReqCurrentBuild + " or greater!";
                Log.Error(error);
                string errorLong = error + "\n\nUpdate your game!";
                LongEventHandler.QueueLongEvent(() => Find.WindowStack.Add(new Dialog_MessageBox(errorLong, "Quit", delegate(){ Root.Shutdown(); }, null, null, "ERROR: ".Colorize(Color.red), false, null, null, WindowLayer.Super)), null, false, null);

                return;
            }
			if (!ModLister.HasActiveModWithName("Harmony"))
            {
                string error = "ERROR: SOS2EXP requires Harmony! Download and enable it!";
                Log.Error(error);
                string errorLong = error + "\n\nIt must be loaded at the top of the list, before Core!";
                LongEventHandler.QueueLongEvent(() => Find.WindowStack.Add(new Dialog_MessageBox(errorLong, null, null, null, null, "ERROR: ".Colorize(Color.red), false, null, null, WindowLayer.Super)), null, false, null);
                return;
            }
            Harmony pat = new Harmony("ShipInteriorMod2");
            ShipInteriorMod2.HasSoS2CK = ModLister.HasActiveModWithName("Save Our Ship Creation Kit");

            //Legacy methods. All 3 of these could technically be merged
            ShipInteriorMod2.DefsLoaded();
			ShipInteriorMod2.SceneLoaded();
			pat.PatchAll();
			//Needs an init delay
			if (useSplashScreen) LongEventHandler.QueueLongEvent(() => ShipInteriorMod2.UseCustomSplashScreen(), "ShipInteriorMod2", false, null);
		}
	}
	public class ModSettings_SoS : ModSettings
	{
		public override void ExposeData()
		{
			Scribe_Values.Look(ref difficultySoS, "difficultySoS", 1.0);
			Scribe_Values.Look(ref frequencySoS, "frequencySoS", 1.0);
			Scribe_Values.Look(ref navyShipChance, "navyShipChance", 0.4);
			Scribe_Values.Look(ref fleetChance, "fleetChance", 0.3);

			Scribe_Values.Look(ref easyMode, "easyMode", false);
			//Scribe_Values.Look(ref useVacuumPathfinding, "useVacuumPathfinding", true);
			Scribe_Values.Look(ref renderPlanet, "renderPlanet", false);
			Scribe_Values.Look(ref useSplashScreen, "useSplashScreen", true);
            Scribe_Values.Look(ref persistShipUI, "persistShipUI", false);

            Scribe_Values.Look(ref minTravelTime, "minTravelTime", 5);
			Scribe_Values.Look(ref maxTravelTime, "maxTravelTime", 100);
            Scribe_Values.Look(ref offsetUIx, "offsetUIx");
			Scribe_Values.Look(ref offsetUIy, "offsetUIy");
			base.ExposeData();
		}

		public static double
			difficultySoS = 1,
			frequencySoS = 1,
			navyShipChance = 0.4,
			fleetChance = 0.3;
		public static bool
			easyMode = false,
			//useVacuumPathfinding = true,
			renderPlanet = false,
			useSplashScreen = true,
			persistShipUI = false;
		public static int
			minTravelTime = 5,
			maxTravelTime = 100,
            offsetUIx = 0,
            offsetUIy = 0;
	}
	public class ShipInteriorMod2 : Mod
	{
		public ShipInteriorMod2(ModContentPack content) : base(content)
		{
			base.GetSettings<ModSettings_SoS>();
        }
        public static readonly string SOS2EXPversion = "V96f3";
        public static readonly int SOS2ReqCurrentMinor = 4;
        public static readonly int SOS2ReqCurrentBuild = 3704;

        public static readonly float crittersleepBodySize = 0.7f;
		public static bool loadedGraphics = false;
		public static bool AirlockBugFlag = false; //see CompSoShipPart
        public static Building shipOriginRoot = null; //used for patched original launch code
		public static Map shipOriginMap = null; //used to check for shipmove map size problem, reset after move
        public static bool SaveShipFlag = false; //used in patch to trigger ending scene
        public static bool LoadShipFlag = false; //set to true in ScenPart_LoadShip.PostWorldGenerate and false in the patch to MapGenerator.GenerateMap
        public static bool StartShipFlag = false; //as above but for ScenPart_StartInSpace
        public static bool HasSoS2CK = false;
        private static PastWorldUWO2 worldComp = null;
        public static PastWorldUWO2 WorldComp
        {
            get
            {
                if (worldComp == null && Find.World != null)
                    worldComp = (PastWorldUWO2)Find.World.components.First(x => x is PastWorldUWO2);
                return worldComp;
            }
        }
        public static void PurgeWorldComp()
        {
            worldComp = null;
        }

        public static RoofDef[] compatibleAirtightRoofs; // Additional array of compatible RoofDefs from other mods.
		public static TerrainDef[] rockTerrains; // Contains terrain types that are considered a "rock".
		public static string[] allowedQuests;
		public static List<ThingDef> randomPlants;
		public static Dictionary<ThingDef, ThingDef> wreckDictionary;

        public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard options = new Listing_Standard();
			options.Begin(inRect);

			options.Label("SoS.Settings.DifficultySoS".Translate("0.5", "10", "1", Math.Round(difficultySoS, 1).ToString()), -1f, "SoS.Settings.DifficultySoS.Desc".Translate());
			difficultySoS = options.Slider((float)difficultySoS, 0.5f, 10f);

			options.Label("SoS.Settings.FrequencySoS".Translate("0", "10", "1", Math.Round(frequencySoS, 1).ToString()), -1f, "SoS.Settings.FrequencySoS.Desc".Translate());
			frequencySoS = options.Slider((float)frequencySoS, 0f, 10f);

			options.Label("SoS.Settings.NavyShipChance".Translate("0", "1", "0.2", Math.Round(navyShipChance, 1).ToString()), -1f, "SoS.Settings.NavyShipChance.Desc".Translate());
			navyShipChance = options.Slider((float)navyShipChance, 0f, 1f);

			options.Label("SoS.Settings.FleetChance".Translate("0", "1", "0.3", Math.Round(fleetChance, 1).ToString()), -1f, "SoS.Settings.FleetChance.Desc".Translate());
			fleetChance = options.Slider((float)fleetChance, 0f, 1f);

			options.Gap();
			options.CheckboxLabeled("SoS.Settings.EasyMode".Translate(), ref easyMode, "SoS.Settings.EasyMode.Desc".Translate());
			//options.CheckboxLabeled("SoS.Settings.UseVacuumPathfinding".Translate(), ref useVacuumPathfinding, "SoS.Settings.UseVacuumPathfinding.Desc".Translate());
			options.Gap();

			options.Label("SoS.Settings.MinTravelTime".Translate("1", "50", "5", minTravelTime.ToString()), -1f, "SoS.Settings.MinTravelTime.Desc".Translate());
			minTravelTime = (int)options.Slider(minTravelTime, 1f, 50f);

			options.Label("SoS.Settings.MaxTravelTime".Translate("50", "1000", "100", maxTravelTime.ToString()), -1f, "SoS.Settings.MaxTravelTime.Desc".Translate());
			maxTravelTime = (int)options.Slider(maxTravelTime, 1f, 1000f);

			options.CheckboxLabeled("SoS.Settings.RenderPlanet".Translate(), ref renderPlanet, "SoS.Settings.RenderPlanet.Desc".Translate());
			options.CheckboxLabeled("SoS.Settings.UseSplashScreen".Translate(), ref useSplashScreen, "SoS.Settings.UseSplashScreens.Desc".Translate());
			options.Gap();
            options.CheckboxLabeled("SoS.Settings.PersistShipUI".Translate(), ref persistShipUI, "SoS.Settings.PersistShipUI.Desc".Translate());
            options.Label("SoS.Settings.OffsetUIx".Translate(), -1f, "SoS.Settings.OffsetUIx.Desc".Translate());
			string bufferX = offsetUIx.ToString();
			options.TextFieldNumeric<int>(ref offsetUIx, ref bufferX, int.MinValue, int.MaxValue);

			options.Label("SoS.Settings.OffsetUIy".Translate(), -1f, "SoS.Settings.OffsetUIy.Desc".Translate());
			string bufferY = offsetUIy.ToString();
			options.TextFieldNumeric<int>(ref offsetUIy, ref bufferY, int.MinValue, int.MaxValue);

			options.End();
			base.DoSettingsWindowContents(inRect);
		}
		public override string SettingsCategory()
		{
			return "Save Our Ship";
		}
		public override void WriteSettings()
		{
			base.WriteSettings();
		}
		public static void DefsLoaded()
        {
			Log.Message("SOS2EXP " + SOS2EXPversion + " active");
			randomPlants = DefDatabase<ThingDef>.AllDefs.Where(t => t.plant != null && !t.defName.Contains("Anima")).ToList();

			foreach (EnemyShipDef ship in DefDatabase<EnemyShipDef>.AllDefs.Where(d => d.saveSysVer < 2 && !d.neverRandom).ToList())
            {
				Log.Error("SOS2: mod \"" + ship.modContentPack.Name + "\" contains EnemyShipDef: \"" + ship + "\" that can spawn as a random ship but is saved with an old version of CK!");
			}

			wreckDictionary = new Dictionary<ThingDef, ThingDef>
			{
				{ResourceBank.ThingDefOf.ShipHullTile, ResourceBank.ThingDefOf.ShipHullTileWrecked},
				{ResourceBank.ThingDefOf.ShipHullTileMech, ResourceBank.ThingDefOf.ShipHullTileWrecked},
				{ResourceBank.ThingDefOf.ShipHullTileArchotech, ResourceBank.ThingDefOf.ShipHullTileWrecked},
				{ResourceBank.ThingDefOf.Ship_Beam, ResourceBank.ThingDefOf.Ship_Beam_Wrecked},
				{ResourceBank.ThingDefOf.Ship_BeamMech, ResourceBank.ThingDefOf.Ship_Beam_Wrecked},
				{ResourceBank.ThingDefOf.Ship_BeamArchotech, ResourceBank.ThingDefOf.Ship_Beam_Wrecked},
				{ThingDef.Named("Ship_Beam_Unpowered"), ResourceBank.ThingDefOf.Ship_Beam_Wrecked},
				{ThingDef.Named("Ship_BeamMech_Unpowered"), ResourceBank.ThingDefOf.Ship_Beam_Wrecked},
				{ThingDef.Named("Ship_BeamArchotech_Unpowered"), ResourceBank.ThingDefOf.Ship_Beam_Wrecked},
				{ThingDef.Named("ShipInside_SolarGenerator"), ResourceBank.ThingDefOf.Ship_Beam_Wrecked},
				{ThingDef.Named("ShipInside_SolarGeneratorMech"), ResourceBank.ThingDefOf.Ship_Beam_Wrecked},
				{ThingDef.Named("ShipInside_SolarGeneratorArchotech"), ResourceBank.ThingDefOf.Ship_Beam_Wrecked},
				{ThingDef.Named("ShipInside_PassiveVent"), ResourceBank.ThingDefOf.Ship_Beam_Wrecked},
				{ThingDef.Named("ShipInside_PassiveVentMechanoid"), ResourceBank.ThingDefOf.Ship_Beam_Wrecked},
				{ThingDef.Named("ShipInside_PassiveVentArchotech"), ResourceBank.ThingDefOf.Ship_Beam_Wrecked},
				{ResourceBank.ThingDefOf.ShipAirlock, ResourceBank.ThingDefOf.ShipAirlockWrecked},
				{ThingDef.Named("ShipAirlockMech"), ResourceBank.ThingDefOf.ShipAirlockWrecked},
				{ThingDef.Named("ShipAirlockArchotech"), ResourceBank.ThingDefOf.ShipAirlockWrecked},
				{ThingDef.Named("ShipAirlockBeam"), ResourceBank.ThingDefOf.Ship_Beam_Wrecked}
			};

			var compatibleRoofs = new List<RoofDef>();
			// Compatibility tricks for Roofs Extended.
			RoofDef roof = DefDatabase<RoofDef>.GetNamed("RoofTransparent", false);
			if (roof != null)
				compatibleRoofs.Add(roof);
			roof = DefDatabase<RoofDef>.GetNamed("RoofSolar", false);
			if (roof != null)
				compatibleRoofs.Add(roof);

			compatibleAirtightRoofs = new RoofDef[compatibleRoofs.Count];
			for (int i = 0; i < compatibleRoofs.Count; i++)
			{
				Log.Message(string.Format("SOS2: Registering compatible roof {0}", compatibleRoofs[i].defName));
				compatibleAirtightRoofs[i] = compatibleRoofs[i];
			}
			rockTerrains = new TerrainDef[]
			{
				TerrainDef.Named("Slate_Rough"),
				TerrainDef.Named("Slate_RoughHewn"),
				TerrainDef.Named("Marble_Rough"),
				TerrainDef.Named("Marble_RoughHewn"),
				TerrainDef.Named("Granite_Rough"),
				TerrainDef.Named("Granite_RoughHewn"),
			};
			allowedQuests = new string[]
			{
				//vanilla
				"OpportunitySite_BanditCamp",
				"OpportunitySite_PeaceTalks",
				"TradeRequest",
				"OpportunitySite_ItemStash",
				//roy
				"PawnLend",
				//"Hospitality_Prisoners", can cause raids
				//"Hospitality_Animals", can cause raids
				//ideo
				"OpportunitySite_WorkSite",
				"Hack_WorshippedTerminal",
				"RelicHunt",
				"AncientComplex_Standard",
				//bt
				"OpportunitySite_AncientComplex_Mechanitor",
				//mod
				"VFEA_OpportunitySite_SealedVault",
				"VFEM_OpportunitySite_LootedVault"
			};

			/*foreach (TraitDef AITrait in DefDatabase<TraitDef>.AllDefs.Where(t => t.exclusionTags.Contains("AITrait")))
            {
                typeof(TraitDef).GetField("commonality", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(AITrait, 0);
            }*/
			//foreach (EnemyShipDef def in DefDatabase<EnemyShipDef>.AllDefs)
			//{
			/*def.ConvertToSymbolTable();
			def.ConvertToBigString();
			string path = Path.Combine(GenFilePaths.SaveDataFolderPath, "RecycledShips");
			DirectoryInfo dir = new DirectoryInfo(path);
			if (!dir.Exists)
				dir.Create();
			string filename = Path.Combine(path, def.defName + ".xml");
			SafeSaver.Save(filename, "Defs", () =>
			{
				Scribe.EnterNode("EnemyShipDef");
				Scribe_Values.Look<string>(ref def.defName, "defName");
				Scribe_Values.Look<string>(ref def.label, "label");
				Scribe_Values.Look<int>(ref def.combatPoints, "combatPoints", 0);
				Scribe_Values.Look<int>(ref def.randomTurretPoints, "randomTurretPoints", 0);
				Scribe_Values.Look<int>(ref def.cargoValue, "cargoValue", 0);
				Scribe_Values.Look<bool>(ref def.neverRandom, "neverRandom");
				Scribe_Values.Look<bool>(ref def.neverAttacks, "neverAttacks");
				Scribe_Values.Look<bool>(ref def.spaceSite, "spaceSite");
				Scribe_Values.Look<bool>(ref def.imperialShip, "imperialShip");
				Scribe_Values.Look<bool>(ref def.pirateShip, "pirateShip");
				Scribe_Values.Look<bool>(ref def.bountyShip, "bountyShip");
				Scribe_Values.Look<bool>(ref def.mechanoidShip, "mechanoidShip");
				Scribe_Values.Look<bool>(ref def.fighterShip, "fighterShip");
				Scribe_Values.Look<bool>(ref def.carrierShip, "carrierShip");
				Scribe_Values.Look<bool>(ref def.tradeShip, "tradeShip");
				Scribe_Values.Look<bool>(ref def.startingShip, "startingShip");
				Scribe_Values.Look<bool>(ref def.startingDungeon, "startingDungeon");
				Scribe.EnterNode("core");
				Scribe_Values.Look<string>(ref def.core.shapeOrDef, "shapeOrDef");
				Scribe_Values.Look<int>(ref def.core.x, "x");
				Scribe_Values.Look<int>(ref def.core.z, "z");
				Scribe_Values.Look<Rot4>(ref def.core.rot, "rot");
				Scribe.ExitNode();
				Scribe.EnterNode("symbolTable");
				foreach(char key in def.symbolTable.Keys)
				{
					Scribe.EnterNode("li");
					char realKey = key;
					Scribe_Values.Look<char>(ref realKey, "key"); ;
					ShipShape realShape = def.symbolTable[key];
					Scribe_Deep.Look<ShipShape>(ref realShape, "value");
					Scribe.ExitNode();
				}
				Scribe.ExitNode();
				Scribe_Values.Look<string>(ref def.bigString, "bigString");
			});*/
			//def.ConvertFromBigString();
			//def.ConvertFromSymbolTable();
			//}
		}
		public static void SceneLoaded()
		{
			if (!loadedGraphics)
			{
				foreach (ThingDef thingToResolve in CompShuttleCosmetics.GraphicsToResolve.Keys)
				{
					Graphic_Single[] graphicsResolved = new Graphic_Single[CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphics.Count];
					Graphic_Multi[] graphicsHoverResolved = new Graphic_Multi[CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphicsHover.Count];

					for (int i = 0; i < CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphics.Count; i++)
					{
						Graphic_Single graphic = new Graphic_Single();
						GraphicRequest req = new GraphicRequest(typeof(Graphic_Single), CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphics[i].texPath, ShaderDatabase.Cutout, CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphics[i].drawSize, Color.white, Color.white, CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphics[i], 0, null, "");
						graphic.Init(req);
						graphicsResolved[i] = graphic;
					}
					for (int i = 0; i < CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphicsHover.Count; i++)
					{
						Graphic_Multi graphic = new Graphic_Multi();
						GraphicRequest req = new GraphicRequest(typeof(Graphic_Multi), CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphicsHover[i].texPath, ShaderDatabase.Cutout, CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphicsHover[i].drawSize, Color.white, Color.white, CompShuttleCosmetics.GraphicsToResolve[thingToResolve].graphicsHover[i], 0, null, "");
						graphic.Init(req);
						graphicsHoverResolved[i] = graphic;
					}

					CompShuttleCosmetics.graphics.Add(thingToResolve.defName, graphicsResolved);
					CompShuttleCosmetics.graphicsHover.Add(thingToResolve.defName, graphicsHoverResolved);
				}
				foreach (ThingDef thingToResolve in CompArcholifeCosmetics.GraphicsToResolve.Keys)
                {
					Graphic_Multi[] graphicsResolved = new Graphic_Multi[CompArcholifeCosmetics.GraphicsToResolve[thingToResolve].graphics.Count];
					for (int i = 0; i < CompArcholifeCosmetics.GraphicsToResolve[thingToResolve].graphics.Count; i++)
                    {
						Graphic_Multi graphic = new Graphic_Multi();
						GraphicRequest req = new GraphicRequest(typeof(Graphic_Multi), CompArcholifeCosmetics.GraphicsToResolve[thingToResolve].graphics[i].texPath, ShaderDatabase.Cutout, CompArcholifeCosmetics.GraphicsToResolve[thingToResolve].graphics[i].drawSize, Color.white, Color.white, CompArcholifeCosmetics.GraphicsToResolve[thingToResolve].graphics[i], 0, null, "");
						graphic.Init(req);
						graphicsResolved[i] = graphic;
                    }

					CompArcholifeCosmetics.graphics.Add(thingToResolve.defName, graphicsResolved);
                }
				loadedGraphics = true;
			}
		}
		public static void UseCustomSplashScreen()
		{
			((UI_BackgroundMain)UIMenuBackgroundManager.background).overrideBGImage = ResourceBank.Splash;
		}

		/// <summary>
		/// Checks if specified RoofDef is properly airtight.
		/// </summary>
		/// <param name="roof"></param>
		/// <returns></returns>
		public static bool IsRoofDefAirtight(RoofDef roof)
		{
			if (roof == null)
				return false;
            if (roof == ResourceBank.RoofDefOf.RoofShip || roof == RoofDefOf.RoofRockThick)
                return true;
            if (compatibleAirtightRoofs != null)
			{
				// I do not expect a lot of values here.
				foreach (var r in compatibleAirtightRoofs)
				{
					if (roof == r)
						return true;
				}
			}
			return false;
		}
		public static int FindWorldTile()
		{
			for (int i = 0; i < Find.World.grid.TilesCount; i++)
			{
				if (!Find.World.worldObjects.AnyWorldObjectAt(i) && TileFinder.IsValidTileForNewSettlement(i))
				{
					//Log.Message("Generating orbiting ship at tile " + i);
					return i;
				}
			}
			return -1;
		}
        public static int FindWorldTilePlayer() //slower, will find tile nearest to ship object pos
        {
            float bestAbsLatitude = float.MaxValue;
            int bestTile = -1;
            for (int i = 0; i < Find.World.grid.TilesCount; i+=10)
            {
                if (Find.World.worldObjects.AnyWorldObjectAt(i) || !TileFinder.IsValidTileForNewSettlement(i))
                    continue;
                float absLatitude = Math.Abs(Find.WorldGrid.LongLatOf(i).y);
                if (absLatitude < bestAbsLatitude)
                {
                    bestAbsLatitude = absLatitude;
                    bestTile = i;
                }
            }
			if (bestTile == -1) //fallback
			{
				bestTile = FindWorldTile();
            }
            return bestTile;
        }
        public static Map GeneratePlayerShipMap(IntVec3 size)
		{
			WorldObjectOrbitingShip orbiter = (WorldObjectOrbitingShip)WorldObjectMaker.MakeWorldObject(ResourceBank.WorldObjectDefOf.ShipOrbiting);
			orbiter.radius = 150;
			orbiter.theta = -3;
			orbiter.SetFaction(Faction.OfPlayer);
			orbiter.Tile = FindWorldTilePlayer();
			Find.WorldObjects.Add(orbiter);
			Map map = MapGenerator.GenerateMap(size, orbiter, orbiter.MapGeneratorDef);
			map.fogGrid.ClearAllFog();
			return map;
        }
        public static Map FindPlayerShipMap()
		{
			return ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def == ResourceBank.WorldObjectDefOf.ShipOrbiting).FirstOrDefault())?.Map;
        }
		public static WorldObject GenerateSite(string defName)
        {
            WorldObject site = WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed(defName));
            site.Tile = TileFinder.RandomStartingTile();
            Find.WorldObjects.Add(site);
            return site;
        }
		public static SpaceNavyDef ValidRandomNavy(Faction hostileTo = null, bool needsShips = true, bool bountyHunts = false)
		{
			return DefDatabase<SpaceNavyDef>.AllDefs.Where(navy =>
			{
				if (needsShips && navy.enemyShipDefs.NullOrEmpty())
					return false;
				if (bountyHunts && !navy.bountyHunts)
					return false;
				if (hostileTo != null) //any faction that has same def as navy, defeat check, hostile check
				{
					if (Find.FactionManager.AllFactions.Any(f => f.HostileTo(hostileTo) && navy.factionDefs.Contains(f.def) && (!f.defeated || (f.defeated && navy.canOperateAfterFactionDefeated))))
						return true;
				}
				//any faction that has same def as navy, defeat check
				else if (Find.FactionManager.AllFactions.Any(f => navy.factionDefs.Contains(f.def) && (!f.defeated || (f.defeated && navy.canOperateAfterFactionDefeated))))
					return true;
				return false;
			}).RandomElement();
		}
		public static SpaceNavyDef ValidRandomNavyBountyHunts()
		{
			return DefDatabase<SpaceNavyDef>.AllDefs.Where(navy =>
			{
				if (!navy.bountyHunts || navy.enemyShipDefs.NullOrEmpty())
					return false;
				if (Find.FactionManager.AllFactions.Any(f => navy.factionDefs.Contains(f.def) && !f.defeated || (f.defeated && navy.canOperateAfterFactionDefeated)))
					return true;
				return false;
			}).RandomElement();
		}
		public static EnemyShipDef RandomValidShipFrom(List<EnemyShipDef> ships, float CR, bool tradeShip, bool allowNavyExc, bool randomFleet = false, int minZ = 0, int maxZ = 0)
		{
			int rarity = Rand.RangeInclusive(1, 2);
			Log.Message("Spawning ship from CR: " + CR + " tradeShip: " + tradeShip + " allowNavyExc: " + allowNavyExc + " randomFleet: " + randomFleet + " rarityLevel: " + rarity + " minZ: " + minZ + " maxZ: " + maxZ);
			List<EnemyShipDef> check = new List<EnemyShipDef>();
			if (randomFleet)
			{
				check = ships.Where(def => ValidShipDef(def, 0.7f * CR, 1.1f * CR, tradeShip, allowNavyExc, randomFleet, rarity, minZ, maxZ)).ToList();
				if (check.Any())
					return check.RandomElement();
			}
			Log.Message("fallback 0");
			check = ships.Where(def => ValidShipDef(def, 0.5f * CR, 1.3f * CR, tradeShip, allowNavyExc, randomFleet, rarity, minZ, maxZ)).ToList();
			if (check.Any())
				return check.RandomElement();
			Log.Message("fallback 1");
			check = ships.Where(def => ValidShipDef(def, 0.25f * CR, 2f * CR, tradeShip, allowNavyExc, randomFleet, 0, minZ, maxZ)).ToList();
			if (check.Any())
				return check.RandomElement();
			//too high or too low adjCR - ignore difficulty
			Log.Warning("SOS2: difficulty set too low/high or no suitable ships found for your CR, using fallback");
			if (CR < 1000)
				check = ships.Where(def => ValidShipDef(def, 0f * CR, 1f * CR, tradeShip, allowNavyExc, randomFleet, 0, minZ, maxZ)).ToList();
			else
				check = ships.Where(def => ValidShipDef(def, 0.5f * CR, 100f * CR, tradeShip, allowNavyExc, randomFleet, 0, minZ, maxZ)).ToList();
			if (check.Any())
				return check.RandomElement();
			//last fallback, not for fleets or navy exclusive
			if (tradeShip)
			{
				Log.Message("trade ship fallback");
				check = ships.Where(def => ValidShipDef(def, 0, 100000f, tradeShip, allowNavyExc, randomFleet, 0, minZ, maxZ)).ToList();
				if (check.Any())
					return check.RandomElement();
				Log.Warning("SOS2: navy has no trade ships, choosing any random.");
				return DefDatabase<EnemyShipDef>.AllDefs.Where(def => ValidShipDef(def, 0f, 100000f, tradeShip, false, randomFleet, 0, minZ, maxZ)).RandomElement();
			}
			else if (!allowNavyExc && !randomFleet)
			{
				Log.Warning("SOS2: found no suitable enemy ship, choosing any random.");
				check = ships.Where(def => ValidShipDef(def, 0, 100000f, tradeShip, allowNavyExc, randomFleet, 0, minZ, maxZ)).ToList();
				if (check.Any())
					return check.RandomElement();
				ships.Where(def => !def.neverAttacks && !def.neverRandom && (allowNavyExc || !def.navyExclusive)).RandomElement();
			}
			return null;
		}
		public static bool ValidShipDef(EnemyShipDef def, float CRmin, float CRmax, bool tradeShip, bool allowNavyExc, bool randomFleet, int rarity = 0, int minZ = 0, int maxZ = 0)
		{
			if (rarity > 0 && def.rarityLevel > rarity)
            {
				return false;
            }
			if (tradeShip)
			{
				if (!def.tradeShip)
					return false;
			}
			else if (def.neverAttacks)
				return false;

			if (randomFleet && (def.neverFleet || !def.ships.NullOrEmpty()))
				return false;
			if (def.neverRandom || def.combatPoints < CRmin || def.combatPoints > CRmax || (minZ > 0 && def.sizeZ < minZ) || (maxZ > 0 && def.sizeZ > maxZ))
			{
				return false;
			}
			if (def.navyExclusive && !allowNavyExc)
				return false;
			return true;
		}
		public static void GenerateShip(EnemyShipDef shipDef, Map map, PassingShip passingShip, Faction fac, Lord lord, out List<Building> cores, bool shipActive = true, bool clearArea = false, int wreckLevel = 0, int offsetX = -1, int offsetZ = -1, SpaceNavyDef navyDef = null)
        {
			var mapComp = map.GetComponent<ShipHeatMapComp>();
            mapComp.CacheOff = true;
            List<IntVec3> area = new List<IntVec3>();
			List<Thing> planters = new List<Thing>();
			List<IntVec3> areaOut;
			List<Thing> plantersOut;
			cores = new List<Building>();
			List<Building> coresOut;
			if (shipDef.ships.NullOrEmpty())
			{
				GenerateShipDef(shipDef, map, passingShip, fac, lord, out coresOut, out areaOut, out plantersOut, shipActive, clearArea, wreckLevel, offsetX, offsetZ, navyDef);
				cores.AddRange(coresOut);
				area.AddRange(areaOut);
				planters.AddRange(plantersOut);
            }
            else //non procgen fleet
			{
				for (int i = 0; i < shipDef.ships.Count; i++)
				{
					Log.Message("SOS2: ".Colorize(Color.cyan) + map + " Spawning fleet ship nr." + i);
					var genShip = DefDatabase<EnemyShipDef>.GetNamedSilentFail(shipDef.ships[i].ship);
					if (genShip == null)
					{
						Log.Error("Fleet ship not found in database");
						return;
					}
					GenerateShipDef(DefDatabase<EnemyShipDef>.GetNamedSilentFail(shipDef.ships[i].ship), map, passingShip, fac, lord, out coresOut, out areaOut, out plantersOut, shipActive, clearArea, wreckLevel, shipDef.ships[i].offsetX, shipDef.ships[i].offsetZ, navyDef);
					cores.AddRange(coresOut);
					area.AddRange(areaOut);
					planters.AddRange(plantersOut);
				}
			}
			PostGenerateShipDef(map, clearArea, area, planters);
		}
		public static void GenerateFleet(float CR, Map map, PassingShip passingShip, Faction fac, Lord lord, out List<Building> cores, bool shipActive = true, bool clearArea = false, int wreckLevel = 0, SpaceNavyDef navyDef = null)
		{
            //use player points to spawn ships of the same navy, fit z, random x
            //main + twin, twin, twin + escort, squadron, tradeship + escorts, tradeship + large, tradeship + large + escort
            //60-20-20,50-50,40-40-10-10
            var mapComp = map.GetComponent<ShipHeatMapComp>();
            mapComp.CacheOff = true;
            List<EnemyShipDef> ships;
			bool allowNavyExc = true;
			if (navyDef != null)
				ships = navyDef.enemyShipDefs;
            else
			{
				allowNavyExc = false;
				ships = DefDatabase<EnemyShipDef>.AllDefs.Where(def => !def.navyExclusive).ToList();
			}
			bool tradeShip = passingShip is TradeShip;
			List<IntVec3> area = new List<IntVec3>();
			List<Thing> planters = new List<Thing>();
			List<IntVec3> areaOut;
			List<Thing> plantersOut;
			cores = new List<Building>();
			List<Building> coresOut;
			bool firstLarger = Rand.Bool;
			bool escorts = Rand.Bool;
			float CRfactor = CR;
			Log.Message("SOS2: ".Colorize(Color.cyan) + map + " Spawning random fleet from CR: " + CR + " navyDef: " + navyDef + " tradeShip: " + tradeShip + " firstLarger: " + firstLarger + " escorts: " + escorts);
			int marginZ;
			if (firstLarger)
			{
				CRfactor = 0.6f;
				marginZ = 15;
			}
			else if (escorts)
			{
				CRfactor = 0.3f;
				marginZ = 15;
			}
			else
			{
				CRfactor = 0.4f;
				marginZ = 25;
			}
			int offsetZ = (map.Size.z - marginZ) / 2;
			int offsetZup = (map.Size.z + marginZ) / 2;
			int maxSizeZ; //max z to find a random def for
			int minSizeZ = 20; //min z after margins to spawn a ship
			EnemyShipDef shipDef = null;
			int i = 1;
			while (i < 8 && CR > 50)
			{
				//pick def
				if (i % 2 == 0) //even up 2,4,6
				{
					maxSizeZ = map.Size.z - offsetZup - marginZ;
					if (maxSizeZ < minSizeZ)
						shipDef = null;
					else if (!(i == 2 && !tradeShip && !firstLarger && Rand.Chance(0.3f))) //second ship, chance for twins - retain def
						shipDef = RandomValidShipFrom(ships, CR * CRfactor, false, allowNavyExc, true, 0, maxSizeZ);
				}
				else //odd down - 1,3,5
				{
					maxSizeZ = offsetZ - marginZ;
					if (i == 1) //first ship, can be trade
					{
						if (firstLarger) //larger first
						{
							maxSizeZ = offsetZ * 4 / 3;
						}
						if (tradeShip)
						{
							shipDef = RandomValidShipFrom(ships, CR * CRfactor, true, allowNavyExc, true, 0, maxSizeZ);
						}
						else
						{
							shipDef = RandomValidShipFrom(ships, CR * CRfactor, false, allowNavyExc, true, 0, maxSizeZ);
						}
						if (firstLarger) //shift all to center
						{
							offsetZ += (marginZ + shipDef.sizeZ) / 2;
							offsetZup += (marginZ + shipDef.sizeZ) / 2;
						}
						else if (!escorts)
							CRfactor = 0.9f;
					}
					else if (maxSizeZ < minSizeZ)
						shipDef = null;
					else
						shipDef = RandomValidShipFrom(ships, CR, false, allowNavyExc, true, 0, maxSizeZ);
				}
				//stop if no escorts
				if (!escorts && ((i > 2 && !firstLarger) || (i > 3 && firstLarger)))
				{
					break;
				}
				if (shipDef != null) //skip up/down if ship is null
				{
					int offsetZAdj;
					if (i % 2 == 0) //after def is picked, adjust z for next offset
					{
						offsetZAdj = offsetZup;
						offsetZup += shipDef.sizeZ + marginZ;
					}
					else
					{
						offsetZ -= shipDef.sizeZ;
						offsetZAdj = offsetZ;
						offsetZ -= marginZ;
					}
					Log.Message("random ship: " + shipDef + " z: " + offsetZAdj);
					int sizeXadj = map.Size.x - shipDef.sizeX;
					int offsetX = Mathf.Clamp((int)Rand.Gaussian(sizeXadj / 2, sizeXadj / 8), 20, sizeXadj - 20);
					CR -= shipDef.combatPoints;
					Log.Message("random ship: " + shipDef + " CR remain: " + CR);
					if (shipDef != null)
					{
						GenerateShipDef(shipDef, map, passingShip, fac, lord, out coresOut, out areaOut, out plantersOut, !shipActive, false, wreckLevel, offsetX, offsetZAdj, navyDef);
						cores.AddRange(coresOut);
						area.AddRange(areaOut);
						planters.AddRange(plantersOut);
					}
				}
				i++;
			}
            PostGenerateShipDef(map, clearArea, area, planters);
		}
		public static void GenerateShipDef(EnemyShipDef shipDef, Map map, PassingShip passingShip, Faction fac, Lord lord, out List<Building> cores, out List<IntVec3> cellsToFog, out List<Thing> planters, bool shipActive = true, bool clearArea = false, int wreckLevel = 0, int offsetX = -1, int offsetZ = -1, SpaceNavyDef navyDef = null)
        {
            cellsToFog = new List<IntVec3>();
			planters = new List<Thing>();
			cores = new List<Building>();
			bool unlockedJT = false;
			if (WorldComp.Unlocks.Contains("JTDriveToo"))
				unlockedJT = true;
			bool ideoActive = false;
			if (ModsConfig.IdeologyActive && (fac != Faction.OfAncientsHostile || fac != Faction.OfAncients || fac != Faction.OfMechanoids))
				ideoActive = true;
			bool royActive = false;
            bool isMechs = false; //for roy mech turret override
            if (ModsConfig.RoyaltyActive)
            {
                royActive = true;
				if (fac == Faction.OfMechanoids)
					isMechs = true;
            }

            Dictionary<IntVec3, Tuple<int,ColorInt,bool>> spawnLights = new Dictionary<IntVec3, Tuple<int, ColorInt, bool>>();

			int size = shipDef.sizeX * shipDef.sizeZ;
			List<Building> wreckDestroy = new List<Building>();
			List<Pawn> pawnsOnShip = new List<Pawn>();
			List<ShipShape> partsToGenerate = new List<ShipShape>();
			List<IntVec3> cargoCells = new List<IntVec3>();
			IntVec3 offset = new IntVec3(0, 0, 0);

			if (shipDef.saveSysVer == 2)
			{
				if (offsetX < 0 || offsetZ < 0) //unset offset, use offset from shipdef
					offset = new IntVec3(shipDef.offsetX, 0, shipDef.offsetZ);
				else
					offset = new IntVec3(offsetX, 0, offsetZ);
			}
			else //old system, from center
				offset = map.Center;

			if (clearArea) //clear ship area extended by 1 - better as actual ship area extended by 1
			{
				CellRect rect;
				if (shipDef.saveSysVer == 2)
					rect = new CellRect(offset.x - 1, offset.z - 1, shipDef.sizeX + 1, shipDef.sizeZ + 1);
				else //V1 legacy
				{
					IntVec3 min = new IntVec3(map.Size.x, 0, map.Size.z);
					IntVec3 max = new IntVec3(0, 0, 0);
					foreach (ShipShape shape in shipDef.parts)
					{
						if (shape.x < min.x)
							min.x = shape.x;
						if (shape.x > max.x)
							max.x = shape.x;
						if (shape.z < min.z)
							min.z = shape.z;
						if (shape.z > max.z)
							max.z = shape.z;
					}
					rect = new CellRect(offset.x + min.x - 1, offset.z + min.z - 1, offset.x + max.x - min.x + 1, offset.z + max.z - min.z + 1);
				}
				List<Thing> DestroyTheseThings = new List<Thing>();
				foreach (IntVec3 pos in rect.Cells)
				{
					foreach (Thing t in map.thingGrid.ThingsAt(pos))
					{
						if (t.def.mineable || t.def.fillPercent > 0.5f)
							DestroyTheseThings.Add(t);
					}
				}
				foreach (Thing t in DestroyTheseThings)
				{
					t.Destroy();
				}
			}
			if (!shipDef.core.shapeOrDef.NullOrEmpty() && wreckLevel < 3)
			{
                Building_ShipBridge bridge = (Building_ShipBridge)ThingMaker.MakeThing(ThingDef.Named(shipDef.core.shapeOrDef));
				bridge.SetFactionDirect(fac);
				GenSpawn.Spawn(bridge, new IntVec3(offset.x + shipDef.core.x, 0, offset.z + shipDef.core.z), map, shipDef.core.rot);
				bridge.TryGetComp<CompPowerTrader>().PowerOn = true;
				cores.Add(bridge);
				bridge.ShipName = shipDef.label;
			}
			//color navy ships without custom paint
			bool rePaint = false; 
			if (navyDef != null && !shipDef.customPaintjob && navyDef.colorPrimary != Color.clear)
				rePaint = true;
			//turrets randomized per ship
			int randomSmall = Rand.RangeInclusive(0, 2);
			int randomLarge = Rand.RangeInclusive(0, 2);
			int randomSpinal = Rand.RangeInclusive(0, 2);
			//generate normal parts
			foreach (ShipShape shape in shipDef.parts)
			{
				try
                {
                    IntVec3 adjPos = new IntVec3(offset.x + shape.x, 0, offset.z + shape.z);
                    if (shape.shapeOrDef.Equals("PawnSpawnerGeneric"))
					{
						PawnGenerationRequest req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed(shape.stuff), shape.faction ?? fac);
						Pawn pawn = PawnGenerator.GeneratePawn(req);
						lord?.AddPawn(pawn);
                        GenSpawn.Spawn(pawn, adjPos, map);
						pawnsOnShip.Add(pawn);
					}
					else if (DefDatabase<EnemyShipPartDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
					{
						partsToGenerate.Add(shape);
					}
					else if (DefDatabase<PawnKindDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
					{
						PawnGenerationRequest req;
						if (navyDef != null)
						{
							if (shape.shapeOrDef.Equals("SpaceCrewMarineHeavy"))
								req = new PawnGenerationRequest(navyDef.marineHeavyDef, fac);
							else if (shape.shapeOrDef.Equals("SpaceCrewMarine"))
								req = new PawnGenerationRequest(navyDef.marineDef, fac);
							else
								req = new PawnGenerationRequest(navyDef.crewDef, fac);
						}
						else
							req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed(shape.shapeOrDef), fac);
						Pawn pawn = PawnGenerator.GeneratePawn(req);
						lord?.AddPawn(pawn);
						GenSpawn.Spawn(pawn, adjPos, map);
						pawnsOnShip.Add(pawn);
					}
					else if (shape.shapeOrDef == "SoSLightEnabler")
                    {
						spawnLights.Add(adjPos, new Tuple<int, ColorInt, bool> (shape.rot.AsInt, ColorIntUtility.AsColorInt(shape.color != Color.clear ? shape.color : Color.white), shape.alt));
					}
					else if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
					{
						bool isWrecked = false;
						Thing thing = null;
						ThingDef def = ThingDef.Named(shape.shapeOrDef);
						//def replacers
						if (def.IsBuildingArtificial)
						{
							if (wreckLevel > 2 && wreckDictionary.ContainsKey(def)) //replace ship walls/floor
							{
								def = wreckDictionary[def];
								isWrecked = true;
							}
							else if (!unlockedJT && def.HasComp(typeof(CompEngineTrail))) //replace JT drives if not unlocked via story
							{
								if (def == ResourceBank.ThingDefOf.Ship_Engine_Interplanetary)
									def = ResourceBank.ThingDefOf.Ship_Engine;
								else if (def == ResourceBank.ThingDefOf.Ship_Engine_Interplanetary_Large)
									def = ResourceBank.ThingDefOf.Ship_Engine_Large;
                            }
                            else if (isMechs && def.building.IsTurret) //replace turets with mech version if ROY active
                            {
                                if (def == ThingDefOf.Turret_MiniTurret)
                                    def = ThingDefOf.Turret_AutoMiniTurret;
                                else if (def == ResourceBank.ThingDefOf.Turret_Autocannon)
                                    def = DefDatabase<ThingDef>.GetNamed("Turret_AutoChargeBlaster");
                                else if (def == ResourceBank.ThingDefOf.Turret_Sniper)
                                    def = DefDatabase<ThingDef>.GetNamed("Turret_AutoInferno");
                            }
                            else if (!royActive && def.Equals(ThingDefOf.Throne))
                            {
								def = DefDatabase<ThingDef>.GetNamed("Armchair");
							}
						}
						//make thing
						if (def.MadeFromStuff)
						{
							if (shape.stuff != null)
								thing = ThingMaker.MakeThing(def, ThingDef.Named(shape.stuff));
							else
								thing = ThingMaker.MakeThing(def, GenStuff.DefaultStuffFor(def));
						}
						else
							thing = ThingMaker.MakeThing(def);

						//spawn thing
						GenSpawn.Spawn(thing, adjPos, map, shape.rot);
                        //post spawn
                        thing.TryGetComp<CompQuality>()?.SetQuality(QualityUtility.GenerateQualityBaseGen(), ArtGenerationContext.Outsider);
                        var colorcomp = thing.TryGetComp<CompColorable>();
                        if (colorcomp != null)
                        {
                            if (shape.color != Color.clear)
                                thing.SetColor(shape.color);
                        }
                        if (thing is Building b)
                        {
                            var partComp = thing.TryGetComp<CompSoShipPart>();
                            if (rePaint && colorcomp != null) //color unpainted navy ships
                            {
                                if (partComp?.Props.isHull ?? false)
                                    thing.SetColor(navyDef.colorPrimary);
                                else if (def.defName.StartsWith("Ship_Corner"))
                                    thing.SetColor(navyDef.colorSecondary);
                            }
                            if (wreckLevel > 1 && !isWrecked)
								wreckDestroy.Add(b);

							if (thing.def.CanHaveFaction) //set faction
                            {
                                if (partComp != null && partComp.Props.isPlating && !partComp.Props.isHull)
                                {
                                    if (fac == Faction.OfPlayer) //only set faction to player hull
                                        thing.SetFactionDirect(fac);
                                    cellsToFog.Add(thing.Position);
                                    continue;
                                }
                                //no faction for pillars, wrecked airlocks except player
                                if ((thing.def == ResourceBank.ThingDefOf.ShipAirlockWrecked && fac != Faction.OfPlayer) || thing.def.thingClass == typeof(Building_ArchotechPillar))
								{
									continue;
                                }
                                thing.SetFactionDirect(fac);
                            }
							var batComp = b.TryGetComp<CompPowerBattery>();
							if (batComp != null)
							{
								if (wreckLevel < 2)
									batComp.AddEnergy(batComp.AmountCanAccept);
								else if (wreckLevel == 2)
									batComp.AddEnergy(batComp.AmountCanAccept * Rand.Gaussian(0.1f, 0.02f));
							}
							var refuelComp = b.TryGetComp<CompRefuelable>();
							if (refuelComp != null)
							{
								float refuel;
								if (wreckLevel < 3)
								{
									refuel = refuelComp.Props.fuelCapacity * Rand.Gaussian(0.7f, 0.2f);
									var reactorComp = b.TryGetComp<CompPowerTraderOverdrivable>();
									if (reactorComp != null && shipActive && reactorComp.overdriveSetting != 1)
										reactorComp.FlickOverdrive(1);
								}
								else
									refuel = refuelComp.Props.fuelCapacity * Rand.Gaussian(0.1f, 0.02f);
								refuelComp.Refuel(refuel);
							}
							var powerComp = b.TryGetComp<CompPowerTrader>();
							if (powerComp != null)
								powerComp.PowerOn = true;
							if (ideoActive && b.def.CanBeStyled() && fac.ideos.PrimaryIdeo.style.StyleForThingDef(thing.def) != null)
							{
								b.SetStyleDef(fac.ideos.PrimaryIdeo.GetStyleFor(thing.def));
							}
							if (b is Building_Storage storage)
							{
								storage.settings.Priority = StoragePriority.Low;
							}
							else if (b is Building_ShipTurret turret)
							{
								turret.burstCooldownTicksLeft = 300;
								if (b is Building_ShipTurretTorpedo torp)
								{
									for (int i = 0; i < torp.torpComp.Props.maxTorpedoes; i++)
									{
										if (size > 10000 && Rand.Chance(0.05f))
											torp.torpComp.LoadShell(ResourceBank.ThingDefOf.ShipTorpedo_Antimatter, 1);
										else if (size > 5000 && Rand.Chance(0.15f))
											torp.torpComp.LoadShell(ResourceBank.ThingDefOf.ShipTorpedo_EMP, 1);
										else if (size < 2500 && Rand.Chance(0.2f))
											continue;
										else
											torp.torpComp.LoadShell(ResourceBank.ThingDefOf.ShipTorpedo_HighExplosive, 1);
									}
								}
							}
							else if (b is Building_PlantGrower)
                            {
								planters.Add(b);
                            }
							else if (b is Building_ShipBridge shipBridge)
								shipBridge.ShipName = shipDef.label;
							else
							{
								var shieldComp = b.TryGetComp<CompShipCombatShield>();
								if (shieldComp != null)
								{
									shieldComp.radiusSet = 40;
									shieldComp.radius = 40;
									if (shape.radius != 0)
									{
										shieldComp.radiusSet = shape.radius;
										shieldComp.radius = shape.radius;
									}
									if (shipActive)
										b.TryGetComp<CompFlickable>().SwitchIsOn = true;
								}
							}
						}
						else
						{
                            if (thing.def.stackLimit > 1)
                            {
                                thing.stackCount = Math.Min(Rand.RangeInclusive(5, 30), thing.def.stackLimit);
                                if (thing.stackCount * thing.MarketValue > 500)
                                    thing.stackCount = (int)Mathf.Max(500 / thing.MarketValue, 1);
                            }
                            if (thing.def.CanHaveFaction)
                                thing.SetFactionDirect(fac);
						}
					}
					else if (DefDatabase<TerrainDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
					{
						TerrainDef terrain = DefDatabase<TerrainDef>.GetNamed(shape.shapeOrDef);
						IntVec3 pos = new IntVec3(shape.x, 0, shape.z);
						if (shipDef.saveSysVer == 2)
							pos = adjPos;
						if (pos.InBounds(map))
							map.terrainGrid.SetTerrain(pos, terrain);
						if (wreckLevel < 3 && terrain.fertility > 0 && pos.GetEdifice(map) == null)
						{
							Plant plant = ThingMaker.MakeThing(randomPlants.RandomElement()) as Plant;
							if (plant != null)
							{
								plant.Growth = Rand.Range(0.5f, 1f); ;
								plant.Position = pos;
								plant.SpawnSetup(map, false);
							}
						}
					}
				}
				catch (Exception e)
				{
					Log.Warning("Ship part was not generated properly: " + shape.shapeOrDef + " at " + offset.x + shape.x + ", " + offset.z + shape.z + " Shipdef pos: |" + shape.x + "," + shape.z + ",0,*|\n" + e);
				}
			}
			//generate SOS2 shapedefs
			int randomTurretPoints = shipDef.randomTurretPoints;
			partsToGenerate.Shuffle();
			foreach (ShipShape shape in partsToGenerate)
			{
				try
                {
                    IntVec3 adjPos = new IntVec3(offset.x + shape.x, 0, offset.z + shape.z);
                    EnemyShipPartDef def = DefDatabase<EnemyShipPartDef>.GetNamed(shape.shapeOrDef);
					if (randomTurretPoints >= def.randomTurretPoints)
						randomTurretPoints -= def.randomTurretPoints;
					else
						def = DefDatabase<EnemyShipPartDef>.GetNamed("Cargo");

					if (def.defName.Equals("CasketFilled"))
					{
						Thing thing = ThingMaker.MakeThing(ThingDefOf.CryptosleepCasket);
						thing.SetFactionDirect(fac);
						Pawn sleeper = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Slave, Faction.OfAncients, forceGenerateNewPawn: true, certainlyBeenInCryptosleep: true));
						((Building_CryptosleepCasket)thing).TryAcceptThing(sleeper);
						GenSpawn.Spawn(thing, adjPos, map, shape.rot);
					}
					else if (def.defName.Length > 8 && def.defName.Substring(def.defName.Length - 8) == "_SPAWNER")
					{
						Thing thing;
						PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(def.defName.Substring(0, def.defName.Length - 8));
						if (kind != null)
							thing = PawnGenerator.GeneratePawn(kind);
						else
							thing = ThingMaker.MakeThing(ThingDef.Named(def.defName.Substring(0, def.defName.Length - 8)));
						if (thing is Pawn p)
						{
							if (p.RaceProps.IsMechanoid)
								p.SetFactionDirect(Faction.OfMechanoids);
							else if (p.RaceProps.BloodDef.defName.Equals("Filth_BloodInsect"))
								p.SetFactionDirect(Faction.OfInsects);
							p.ageTracker.AgeBiologicalTicks = 36000000;
							p.ageTracker.AgeChronologicalTicks = 36000000;
							if (lord != null)
								lord.AddPawn(p);
							pawnsOnShip.Add(p);
						}
						else if (thing is Hive)
							thing.SetFactionDirect(Faction.OfInsects);
						else
							thing.SetFactionDirect(fac);
						GenSpawn.Spawn(thing, adjPos, map);
					}
					else if (!def.defName.Equals("Cargo")) //everything else
					{
						ThingDef thingy;
						if (def.defName.Equals("ShipPartTurretSmall"))
							thingy = def.things[randomSmall];
						else if (def.defName.Equals("ShipPartTurretLarge"))
							thingy = def.things[randomLarge];
						else if (def.defName.Equals("ShipPartTurretSpinal"))
							thingy = def.things[randomSpinal];
						else
							thingy = def.things.RandomElement();
						Thing thing;
						PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamedSilentFail(thingy.defName);
						if (kind != null)
							thing = PawnGenerator.GeneratePawn(kind);
						else
							thing = ThingMaker.MakeThing(thingy);
						if (thing.def.CanHaveFaction)
						{
							if (thing is Pawn p)
							{
								if (p.RaceProps.IsMechanoid)
									p.SetFactionDirect(Faction.OfMechanoids);
								else if (p.RaceProps.BloodDef.defName.Equals("Filth_BloodInsect"))
									p.SetFactionDirect(Faction.OfInsects);
								p.ageTracker.AgeBiologicalTicks = 36000000;
								p.ageTracker.AgeChronologicalTicks = 36000000;
								if (lord != null)
									lord.AddPawn(p);
								pawnsOnShip.Add(p);
							}
							else if (thing is Hive)
								thing.SetFactionDirect(Faction.OfInsects);
							else
								thing.SetFactionDirect(fac);
						}
						if (thing is Building_ShipTurret turret)
							turret.burstCooldownTicksLeft = 300;
						if (thing.TryGetComp<CompColorable>() != null)
							thing.TryGetComp<CompColorable>().SetColor(shape.color);
						GenSpawn.Spawn(thing, adjPos, map, shape.rot);
					}
					else //cargo
					{
						for (int ecks = offset.x + shape.x; ecks <= offset.x + shape.x + shape.width; ecks++)
						{
							for (int zee = offset.z + shape.z; zee <= offset.z + shape.z + shape.height; zee++)
							{
								cargoCells.Add(new IntVec3(ecks, 0, zee));
							}
						}
					}
				}
				catch (Exception e)
				{
					Log.Warning("Ship shape was not generated properly: " + shape.shapeOrDef + " at " + offset.x + shape.x + ", " + offset.z + shape.z + " Shipdef pos: |" + shape.x + "," + shape.z + ",0,*|\n" + e);
				}
			}
			//cargo
			if (cargoCells.Any() && wreckLevel < 3)
			{
				List<Thing> loot;
				if (passingShip is TradeShip tradeShip)
				{
					loot = tradeShip.GetDirectlyHeldThings().ToList();
				}
				else
				{
					ThingSetMakerParams parms = default(ThingSetMakerParams);
					parms.makingFaction = fac;
					parms.totalMarketValueRange = new FloatRange(shipDef.cargoValue * 0.75f, shipDef.cargoValue * 1.25f);
					parms.traderDef = DefDatabase<TraderKindDef>.AllDefs.Where(t => t.orbital == true).RandomElement();
					loot = ThingSetMakerDefOf.TraderStock.root.Generate(parms).InRandomOrder().ToList();
					//So, uh, this didn't work the way I thought it did. Hence ships had way, waaaaaaay too much loot. Fixing that now.
					float actualTotalValue = parms.totalMarketValueRange.Value.RandomInRange * 10;
					if (actualTotalValue == 0)
						actualTotalValue = 500f;
					if (actualTotalValue > 5000f)
						actualTotalValue = 5000f;
					List<Thing> actualLoot = new List<Thing>();
					while (loot.Count > 0 && actualTotalValue > 0)
					{
						Thing random = loot.RandomElement();
						actualTotalValue -= random.MarketValue * random.stackCount;
						actualLoot.Add(random);
						loot.Remove(random);
					}
					loot = actualLoot;
				}
				foreach (Thing t in loot)
				{
					IntVec3 cell = cargoCells.RandomElement();
					GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Near);
					cargoCells.Remove(cell);
					if (t is Pawn p && !p.RaceProps.Animal)
						t.SetFactionDirect(fac);
				}
			}
			//wreck
			//1 (light damage - starting ships): outer explo few
			//2: outer explo more, destroy some buildings, some dead crew, chance for more invaders
			//3: wreck all hull, outer explo lots, chance to split, destroy most buildings, most crew dead, chance for invaders
			//4: planetside wreck - no invaders
			if (wreckLevel > 0)
			{
				bool madeLines = false;
				int holeNum = 0;
				//split
				if ((wreckLevel == 2 || wreckLevel == 3) && size > 1000 && Rand.Chance(0.7f))
                {
                    MakeLines(shipDef, map, fac, wreckLevel, offset);
					madeLines = true;
				}
				if ((wreckLevel == 2 || wreckLevel == 3) && size > 8000)
				{
					MakeLines(shipDef, map, fac, wreckLevel, offset);
				}
				//holes, surounded by wreck
				int adj = 1 + (size / 1000);
				//Log.Message("holenum: "+adj);
				holeNum = Rand.RangeInclusive(adj, adj - 1 + (wreckLevel * 2));
				if (size > 4000 && wreckLevel > 1 && !madeLines)
					holeNum += Rand.RangeInclusive(adj, adj - 1 + (wreckLevel * 2));
				CellRect rect = new CellRect(offset.x - 1, offset.z - 1, shipDef.sizeX + 1, shipDef.sizeZ + 1);
				MakeHoles(FindCellOnOuterHull(map, holeNum, rect), map, fac, wreckLevel, 1.9f, 4.9f);
				//buildings
				List<Building> toKill = new List<Building>();
				if (wreckLevel > 2)
				{
					foreach (Building b in wreckDestroy.Where(t => !t.Destroyed))
					{
						if (Rand.Chance(0.8f))
							toKill.Add(b);
					}
				}
				foreach (Building b in toKill.Where(t => !t.Destroyed))
				{
					if (wreckLevel == 4)
						GenExplosion.DoExplosion(b.Position, map, Rand.Range(1.9f, 4.9f), DamageDefOf.Flame, null);
					var refuelComp = b.TryGetComp<CompRefuelable>();
					if (refuelComp != null)
						refuelComp.ConsumeFuel(refuelComp.Fuel);
					b.Destroy();
				}
				//td remove floor, roof?

				//pawns
				List<Pawn> pawnsToKill = new List<Pawn>();
				foreach (Pawn p in pawnsOnShip)
				{
					if (wreckLevel > 2)
						HealthUtility.DamageUntilDead(p);
					if (wreckLevel == 2)
                    {
						int chance = Rand.RangeInclusive(1, 3);
						if (chance == 1)
							HealthUtility.DamageUntilDead(p);
						else if (chance == 2)
							HealthUtility.DamageUntilDowned(p);
					}
				}
				//invaders - pick faction, spawn lord + pawns
				Faction invaderFac = null;
				if ((wreckLevel == 2 && Rand.Chance(0.8f)) || (wreckLevel ==3 && Rand.Chance(0.6f)))
				{
					SpaceNavyDef navy = ValidRandomNavy(Faction.OfPlayer);
					if (navy != null)
					{
						var mapComp = map.GetComponent<ShipHeatMapComp>();
						if (mapComp.InvaderLord == null) //spawn only one invader lord
						{
							invaderFac = Find.FactionManager.AllFactions.Where(f => navy.factionDefs.Contains(f.def)).RandomElement();
							if (wreckLevel == 2)
								mapComp.InvaderLord = LordMaker.MakeNewLord(invaderFac, new LordJob_AssaultShip(invaderFac, false), map);
							else
								mapComp.InvaderLord = LordMaker.MakeNewLord(invaderFac, new LordJob_DefendShip(invaderFac, map.Center), map);
							Log.Message("Spawned invaders from: " + invaderFac);
						}
						else
							invaderFac = mapComp.InvaderLord.faction;

						foreach (Pawn p in pawnsOnShip.Where(p => p.Downed || p.Dead))
						{
							if ((wreckLevel == 2 && Rand.Chance(0.6f)) || (wreckLevel == 3 && Rand.Chance(0.3f)))
							{
								PawnKindDef req;
								int chance = Rand.RangeInclusive(1, 3);
								if (chance == 3)
									req = navy.marineHeavyDef;
								else
									req = navy.marineDef;
								Pawn pawn = PawnGenerator.GeneratePawn(req, invaderFac);
								GenSpawn.Spawn(pawn, p.Position, map);
								mapComp.InvaderLord.AddPawn(pawn);
							}
						}
					}
				}
				//chance for ship battle
				if ((wreckLevel == 2 && Rand.Chance(0.6f)) || (wreckLevel == 3 && Rand.Chance(0.3f) && invaderFac != null))
				{
					IncidentParms parms = new IncidentParms();
					Map check = FindPlayerShipMap();
					if (check != null)
					{
						parms.target = check;
						parms.forced = true;
						if (invaderFac != null && Rand.Chance(0.8f)) //most likely invading ship
							parms.faction = invaderFac;
						else
						{
							if (navyDef != null) //if ship from a navy, likely aided by it
								parms.faction = Find.FactionManager.AllFactions.Where(f => navyDef.factionDefs.Contains(f.def)).RandomElement();
						}
						QueuedIncident qi = new QueuedIncident(new FiringIncident(IncidentDef.Named("ShipCombat"), null, parms), Find.TickManager.TicksGame + Rand.RangeInclusive(2000, 8000));
						Find.Storyteller.incidentQueue.Add(qi);
					}
				}
			}
			//SL SpawnLights(map, spawnLights);
        }
        public static void SpawnLights(Map map, Dictionary<IntVec3, Tuple<int, ColorInt, bool>> shape)
        {
            //Dictionary<IntVec3, Color> spawnLights
            //shape.color != Color.clear ? shape.color : Color.white
            foreach (IntVec3 position in shape.Keys)
            {
                Building edifice = position.GetEdifice(map);
                if (edifice != null)
                {
                    CompSoShipLight part = edifice.TryGetComp<CompSoShipLight>();
                    if (part != null)
                    {
                        part.SpawnLight(shape[position].Item1, shape[position].Item2, shape[position].Item3);
                    }
                }
            }
        }
        public static void PostGenerateShipDef(Map map, bool clearArea, List<IntVec3> shipArea, List<Thing> planters)
		{
			//HashSet<Room> validRooms = new HashSet<Room>();
			map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
			if (!clearArea)
			{
				//all cells, except if outdoors+outdoors border
				Room outdoors = new IntVec3(0, 0, 0).GetRoom(map); //td make this find first cell outside
				List<IntVec3> excludeCells = new List<IntVec3>();
				foreach (IntVec3 cell in outdoors.BorderCells.Where(c => c.InBounds(map)))
				{
					//find larger than 1x1 edifice buildings and remove their cells from tofog
					Building edifice = cell.GetEdifice(map);
					if (edifice != null && edifice.def.MakeFog && (edifice.def.size.x > 1 || edifice.def.size.z > 1))
					{
						foreach (IntVec3 v in edifice.OccupiedRect())//.ExpandedBy(1))
						{
							//if (v.GetEdifice(map) != null)
							shipArea.Remove(v);
						}
					}
				}
				foreach (IntVec3 cell in shipArea.Except(outdoors.Cells.Concat(outdoors.BorderCells.Where(c => c.InBounds(map)))))
				{
					map.fogGrid.fogGrid[map.cellIndices.CellToIndex(cell)] = true;
					//validRooms.Add(cell.GetRoom(map));
				}
			}
			/*
			HashSet<Room> validRooms = new HashSet<Room>();
			foreach (IntVec3 v in shipArea)
			{
				Room r = v.GetRoom(map);
				if (r != null)
					validRooms.Add(r);
			}
			if (validRooms.Any())
			{
				Log.Message("set temp in rooms: " + validRooms.Count);
				foreach (Room r in validRooms.Where(r => r != null))
				{
					r.Temperature = 21;
				}
			}*/
			foreach (Room r in map.regionGrid.allRooms)
				r.Temperature = 21;
			foreach (Thing t in planters)
			{
				if (t.GetRoom() == null || ExposedToOutside(t.GetRoom()))
					continue;
				ThingDef def = Rand.Element(ThingDef.Named("Plant_Rice"), ThingDefOf.Plant_Potato, ThingDef.Named("Plant_Strawberry"));
				//randomPlants.Where(d => d.plant.sowTags.Contains("Hydroponic") && !d.plant.cavePlant && d.plant.sowResearchPrerequisites == null).RandomElement();
				if (def != null)
				{
					foreach (IntVec3 pos in t.OccupiedRect())
					{
						Plant plant = ThingMaker.MakeThing(def) as Plant;
						plant.Growth = Rand.Range(0.5f, 1f);
						plant.Position = pos;
						plant.SpawnSetup(map, false);
					}
				}
			}
			map.mapDrawer.MapMeshDirty(map.Center, MapMeshFlag.Things | MapMeshFlag.FogOfWar);
			if (Current.ProgramState == ProgramState.Playing)
				map.mapDrawer.RegenerateEverythingNow();
            map.GetComponent<ShipHeatMapComp>().RecacheMap();
        }
		public static List<IntVec3> FindCellOnOuterHull(Map map, int max, CellRect shipArea)
		{
			//targets outer cells
			Room outdoors = new IntVec3(0, 0, 0).GetRoom(map);
			List<IntVec3> targetCells = new List<IntVec3>();
			List<IntVec3> validCells = new List<IntVec3>();
			foreach (IntVec3 cell in outdoors.BorderCells.Where(c => c.InBounds(map)).Intersect(shipArea))
				validCells.Add(cell);
			validCells.Shuffle();
			int i = 0;
			while (i < max)
			{
				targetCells.Add(validCells[i]);
				i++;
				if (i > validCells.Count || i > 30)
					break;
			}
			return targetCells;
		}
		public static void MakeLines(EnemyShipDef shipDef, Map map, Faction fac, int wreckLevel, IntVec3 offset)
		{
			List<IntVec3> detVecs = new List<IntVec3>();
			IntVec3 from = new IntVec3(Rand.RangeInclusive(offset.x + 10, offset.x + shipDef.sizeX - 10), 0, offset.z);
			IntVec3 to = new IntVec3(Rand.RangeInclusive(offset.x + 10, offset.x + shipDef.sizeX - 10), 0, offset.z + shipDef.sizeZ);
			float angle = (to - from).AngleFlat;
			IntVec3 curVec = IntVec3.Zero;
			while ((from.z + curVec.z) < to.z)
			{
				curVec += new Vector3(4 * Mathf.Sin(Mathf.Deg2Rad * angle), 0, 4 * Mathf.Cos(Mathf.Deg2Rad * angle)).ToIntVec3();
				detVecs.Add(from + curVec);
				//Log.Message("vec: " + (from + curVec));
				if (Rand.Chance(0.05f))
					break;
			}
			MakeHoles(detVecs, map, fac, wreckLevel, 2.9f, 3.9f);
		}
		public static void MakeHoles(List<IntVec3> targets, Map map, Faction fac, int wreckLevel, float minSize, float maxSize)
		{
			if (targets.NullOrEmpty())
				return;
            List<Thing> toDestroy = new List<Thing>();
			List<Building> toReplace = new List<Building>();
            HashSet<IntVec3> area = new HashSet<IntVec3>();
            foreach (IntVec3 v in targets)
            {
                List<Thing> toDestroySet = new List<Thing>();
                List<Building> toReplaceSet = new List<Building>();
                HashSet<IntVec3> areaSet = new HashSet<IntVec3>();
                float exploRadius = Rand.Range(minSize, maxSize);
				foreach (IntVec3 vec in GenRadial.RadialCellsAround(v, exploRadius, true))
				{
                    areaSet.Add(vec);
					foreach (Thing t in vec.GetThingList(map).Where(t => !t.Destroyed))
					{
                        toDestroySet.Add(t);
					}
				}
				foreach (IntVec3 vec in GenRadial.RadialCellsAround(v, exploRadius, exploRadius + 1))
				{
					foreach (Thing t in vec.GetThingList(map).Where(t => t is Building && !t.Destroyed))
					{
						if (wreckDictionary.ContainsKey(t.def))
						{
                            toReplaceSet.Add(t as Building);
						}
					}
                }
                if (wreckLevel == 1 && (toDestroySet.Any(t => t is Building_ShipBridge || t.TryGetComp<CompEngineTrail>() != null)))
                {
                    continue; //prevent destroying cruical buildings on starting ships
                }
				else
				{
                    toDestroy.AddRange(toDestroySet);
					toReplace.AddRange(toReplaceSet);
					area.AddRange(areaSet);
                }
            }
			foreach (IntVec3 v in area)
            {
                map.roofGrid.SetRoof(v, null);
				map.GetComponent<ShipHeatMapComp>().MapShipCells.Remove(v);
            }
			foreach (Building b in toReplace)
			{
				IntVec3 v = b.Position;
				Thing thing = ThingMaker.MakeThing(wreckDictionary[b.def]);
				if (thing.def.CanHaveFaction && fac == Faction.OfPlayer)
					thing.SetFactionDirect(fac);
                if (!b.Destroyed)
					b.Destroy();
				GenSpawn.Spawn(thing, v, map);
			}
			foreach (Thing t in toDestroy.Where(t => !t.Destroyed))
			{
				if (t is Building b)
				{
					var refuelComp = b.TryGetComp<CompRefuelable>();
					refuelComp?.ConsumeFuel(refuelComp.Fuel);
					if (wreckLevel == 4 && b.def.CostList != null && b.def.CostList.Any(cost => cost.thingDef == ThingDefOf.ComponentSpacer) && Rand.Chance(0.2f))
						GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDef.Named("ShipChunkSalvage")), b.Position, map, ThingPlaceMode.Near);
					if (!b.Destroyed)
						b.Destroy();
				}
				else if (t is Pawn p)
                {
					HealthUtility.DamageUntilDowned(p);
				}
			}
			//secondaries?
		}
		private static void GenerateHull(List<IntVec3> border, List<IntVec3> interior, Faction fac, Map map)
		{
			foreach (IntVec3 vec in border)
			{
				if (!GenSpawn.WouldWipeAnythingWith(vec, Rot4.South, ResourceBank.ThingDefOf.Ship_Beam, map, (Thing x) => x.def.category == ThingCategory.Building) && !vec.GetThingList(map).Where(t => t.TryGetComp<CompSoShipPart>()?.Props.isPlating ?? false).Any())
				{
					Thing wall = ThingMaker.MakeThing(ResourceBank.ThingDefOf.Ship_Beam);
					wall.SetFactionDirect(fac);
					GenSpawn.Spawn(wall, vec, map);
				}
			}
			foreach (IntVec3 vec in interior)
			{
				Thing floor = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipHullTile);
				if (fac == Faction.OfPlayer)
					floor.SetFactionDirect(fac);
				GenSpawn.Spawn(floor, vec, map);
			}
		}
		public static void RectangleUtility(int xCorner, int zCorner, int x, int z, ref List<IntVec3> border,
			ref List<IntVec3> interior)
		{
			for (int ecks = xCorner; ecks < xCorner + x; ecks++)
			{
				for (int zee = zCorner; zee < zCorner + z; zee++)
				{
					if (ecks == xCorner || ecks == xCorner + x - 1 || zee == zCorner || zee == zCorner + z - 1)
						border.Add(new IntVec3(ecks, 0, zee));
					else
						interior.Add(new IntVec3(ecks, 0, zee));
				}
			}
		}
		public static void CircleUtility(int xCenter, int zCenter, int radius, ref List<IntVec3> border,
			ref List<IntVec3> interior)
		{
			border = CircleBorder(xCenter, zCenter, radius);
			int reducedRadius = radius - 1;
			while (reducedRadius > 0)
			{
				List<IntVec3> newCircle = CircleBorder(xCenter, zCenter, reducedRadius);
				foreach (IntVec3 vec in newCircle)
					interior.Add(vec);
				reducedRadius--;
			}
			interior.Add(new IntVec3(xCenter, 0, zCenter));
		}
		public static List<IntVec3> CircleBorder(int xCenter, int zCenter, int radius)
		{
			HashSet<IntVec3> border = new HashSet<IntVec3>();
			bool foundDiagonal = false;
			int radiusSquared = radius * radius;
			IntVec3 pos = new IntVec3(radius, 0, 0);
			AddOctants(pos, ref border);
			while (!foundDiagonal)
			{
				int left = ((pos.x - 1) * (pos.x - 1)) + (pos.z * pos.z);
				int up = ((pos.z + 1) * (pos.z + 1)) + (pos.x * pos.x);
				if (Math.Abs(radiusSquared - up) > Math.Abs(radiusSquared - left))
					pos = new IntVec3(pos.x - 1, 0, pos.z);
				else
					pos = new IntVec3(pos.x, 0, pos.z + 1);
				AddOctants(pos, ref border);
				if (pos.x == pos.z)
					foundDiagonal = true;
			}

			List<IntVec3> output = new List<IntVec3>();
			foreach (IntVec3 vec in border)
			{
				output.Add(new IntVec3(vec.x + xCenter, 0, vec.z + zCenter));
			}

			return output;
		}
		private static void AddOctants(IntVec3 pos, ref HashSet<IntVec3> border)
		{
			border.Add(pos);
			border.Add(new IntVec3(pos.x * -1, 0, pos.z));
			border.Add(new IntVec3(pos.x, 0, pos.z * -1));
			border.Add(new IntVec3(pos.x * -1, 0, pos.z * -1));
			border.Add(new IntVec3(pos.z, 0, pos.x));
			border.Add(new IntVec3(pos.z * -1, 0, pos.x));
			border.Add(new IntVec3(pos.z, 0, pos.x * -1));
			border.Add(new IntVec3(pos.z * -1, 0, pos.x * -1));
		}
		
		public class TimeHelper
		{
			private System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
			public struct TimeMeasure
			{
				public string name;
				public TimeSpan time;
			}

			public List<TimeMeasure> measures = new List<TimeMeasure>();

			public void Record(string name)
			{
				measures.Add(new TimeMeasure { name = name, time = watch.Elapsed });
				watch.Restart();
			}

			public string MakeReport()
			{
				var sb = new StringBuilder();
				foreach (var r in measures)
				{
					sb.AppendFormat("{0}={1}ms\n", r.name, r.time.TotalMilliseconds);
				}
				return sb.ToString();
			}
		}
		public static bool IsRock(TerrainDef def)
		{
			return rockTerrains.Contains(def);
		}
		public static bool IsHull(TerrainDef def)
		{
			return def == ResourceBank.TerrainDefOf.FakeFloorInsideShip || def == ResourceBank.TerrainDefOf.FakeFloorInsideShipMech || def == ResourceBank.TerrainDefOf.FakeFloorInsideShipArchotech;
		}
		//td change or make new for call on ship direct
        public static void MoveShip(Building core, Map targetMap, IntVec3 adjustment, Faction fac = null, byte rotNum = 0, bool includeRock = false)
		{
			bool devMode = false;
			var watch = new TimeHelper();
			if (Prefs.DevMode)
            {
				devMode = true;
			}
			HashSet<Thing> toSave = new HashSet<Thing>();
			List<Thing> toDestroy = new List<Thing>();
			List<CompPower> toRePower = new List<CompPower>();
			List<Zone> zonesToCopy = new List<Zone>();
			List<Room> roomsToTemp = new List<Room>();
			List<Tuple<IntVec3, float>> posTemp = new List<Tuple<IntVec3, float>>();
			List<Tuple<IntVec3, TerrainDef>> terrainToCopy = new List<Tuple<IntVec3, TerrainDef>>();
			List<Tuple<IntVec3, RoofDef>> roofToCopy = new List<Tuple<IntVec3, RoofDef>>();
			List<IntVec3> fireExplosions = new List<IntVec3>();
			List<CompEngineTrail> nukeExplosions = new List<CompEngineTrail>();
			IntVec3 rot = IntVec3.Zero;
			int rotb = 4 - rotNum;

			// Transforms vector from initial position to final according to desired movement/rotation.
			Func<IntVec3, IntVec3> Transform;
			if (rotb == 2)
				Transform = (IntVec3 from) => new IntVec3(targetMap.Size.x - from.x, 0, targetMap.Size.z - from.z) + adjustment;
			else if (rotb == 3)
				Transform = (IntVec3 from) => new IntVec3(targetMap.Size.x - from.z, 0, from.x) + adjustment;
			else
				Transform = (IntVec3 from) => from + adjustment;
			if (devMode)
				watch.Record("prepare");

            AirlockBugFlag = true;
            shipOriginMap = null;
			bool playerMove = core.Faction == Faction.OfPlayer;
			Map sourceMap = core.Map;
			bool sourceMapIsSpace = sourceMap.IsSpace();
            var sourceMapComp = sourceMap.GetComponent<ShipHeatMapComp>();
            int shipIndex = sourceMapComp.MapShipCells[core.Position].Item1;
            HashSet<int> shipIndexes = new HashSet<int> { shipIndex };
            var ship = sourceMapComp.ShipsOnMapNew[shipIndex];
			HashSet<IntVec3> sourceArea = new HashSet<IntVec3>(ship.Area);
			if (sourceMapComp.Docked.Any()) //undock all
			{
				sourceMapComp.UndockAllFrom(shipIndex);
            }

            if (targetMap == null)
                targetMap = core.Map;
			bool targetMapIsSpace = targetMap.IsSpace();
            var targetMapComp = targetMap.GetComponent<ShipHeatMapComp>();
            HashSet<IntVec3> targetArea = new HashSet<IntVec3>();

            if (targetMap != sourceMap) //ship cache: if moving to different map, move cache
            {
                Log.Message("SOS2: ".Colorize(Color.cyan) + sourceMap + " Ship ".Colorize(Color.green) + shipIndex + " MoveShip to map: " + targetMap);
				if (targetMapComp.ShipsOnMapNew.ContainsKey(shipIndex))
                {
                    Log.Error("SOS2: ".Colorize(Color.cyan) + " Ship ".Colorize(Color.green) + shipIndex + " MoveShip abort, already on map: " + targetMap);
                    return;
				}
                targetMapComp.ShipsOnMapNew.Add(shipIndex, sourceMapComp.ShipsOnMapNew[shipIndex]);
                ship = targetMapComp.ShipsOnMapNew[shipIndex];
                ship.Map = targetMap;
                if (adjustment != IntVec3.Zero && ship.BuildingsDestroyed.Any())
                {
                    HashSet<Tuple<BuildableDef, IntVec3, Rot4>> buildingsDestroyed = new HashSet<Tuple<BuildableDef, IntVec3, Rot4>>(ship.BuildingsDestroyed);
                    ship.BuildingsDestroyed.Clear();
                    foreach (var sh in buildingsDestroyed)
                    {
                        ship.BuildingsDestroyed.Add(new Tuple<BuildableDef, IntVec3, Rot4>(sh.Item1, Transform(sh.Item2), sh.Item3));
                    }
                    buildingsDestroyed.Clear();
                }
                sourceMapComp.RemoveShipFromCache(shipIndex);
            }
            //Log.Message("Area: " + ship.Area.Count);
            if (adjustment != IntVec3.Zero)
            {
                ship.Area.Clear();
                foreach (IntVec3 pos in sourceArea)
                {
                    ship.Area.Add(Transform(pos));
                }
                //Log.Message("Area: " + ship.Area.Count);
            }

            foreach (IntVec3 pos in sourceArea)
			{
				IntVec3 adjustedPos = Transform(pos);
                //ship cache: move ShipCells
                targetMapComp.MapShipCells.Add(adjustedPos, new Tuple<int, int>(sourceMapComp.MapShipCells[pos].Item1, sourceMapComp.MapShipCells[pos].Item2));
                sourceMapComp.MapShipCells.Remove(pos);
				//store room temps
				Room room = pos.GetRoom(sourceMap);
				if (room != null && !roomsToTemp.Contains(room) && !ExposedToOutside(room))
				{
					roomsToTemp.Add(room);
					float temp = room.Temperature;
					posTemp.Add(new Tuple<IntVec3, float>(adjustedPos, temp));
				}
				//add to target area and ship
				targetArea.Add(adjustedPos);
				//clear LZ
				foreach (Thing t in adjustedPos.GetThingList(targetMap))
				{
					if (!toDestroy.Contains(t) && !(t is Building_SteamGeyser))
						toDestroy.Add(t);
				}
				if (!targetMapIsSpace)
					targetMap.snowGrid.SetDepth(adjustedPos, 0f);
				//add all things from area
				List<Pawn> pawns = new List<Pawn>();
				foreach (Thing t in pos.GetThingList(sourceMap))
				{
					if (t is Building b)
                    {
						if (b is Building_SteamGeyser)
							continue;
						if (b is Building_ShipAirlock a && a.docked)
						{
							a.DeSpawnDock();
						}
						else
						{
                            b.TryGetComp<CompEngineTrail>()?.Off();
                            var transportComp = b.TryGetComp<CompTransporter>();
                            if (transportComp != null)
                            {
                                toSave.AddRange(transportComp.innerContainer.ToList());
                                transportComp.CancelLoad();
                            }
                        }
                    }
					else if (t is Pawn p)
                    {
                        pawns.Add(p);
                        if (!sourceMapIsSpace && p.Faction != Faction.OfPlayer && !p.IsPrisoner)
                        {
                            //do not allow kidnapping other fac pawns/animals
                            Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipLaunchFailPawns", p.Name.ToStringShort), null, MessageTypeDefOf.NegativeEvent);
                            return;
                        }
                        /*else if (p.Faction == Faction.OfPlayer && p.holdingOwner is Building) //pawns in containers, abort
                        {
							Log.Message("Pawn holding thing: " + p.holdingOwner);
                            Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipMoveFailPawns", p.holdingOwner.Owner.ToString()), null, MessageTypeDefOf.NegativeEvent);
                            return;
                        }*/
                    }
                    toSave.Add(t);
                }
                foreach (Pawn p in pawns) //drop carried things, add to move list
                {
					if (p.IsCarrying() && p.carryTracker.TryDropCarriedThing(p.Position, ThingPlaceMode.Direct, out Thing carriedt))
                    {
                        toSave.Add(carriedt);
                    }
					//p.CurJob.Clear();
                }

				if (sourceMap.zoneManager.ZoneAt(pos) != null && !zonesToCopy.Contains(sourceMap.zoneManager.ZoneAt(pos)))
				{
					zonesToCopy.Add(sourceMap.zoneManager.ZoneAt(pos));
				}
				//store non ship terrain
				var sourceTerrain = sourceMap.terrainGrid.TerrainAt(pos);
				if (sourceTerrain.layerable && !IsHull(sourceTerrain))
				{
					terrainToCopy.Add(new Tuple<IntVec3, TerrainDef>(adjustedPos, sourceTerrain));
					sourceMap.terrainGrid.RemoveTopLayer(pos, false);
				}
				else if (includeRock && IsRock(sourceTerrain))
				{
					terrainToCopy.Add(new Tuple<IntVec3, TerrainDef>(adjustedPos, sourceTerrain));
					sourceMap.terrainGrid.SetTerrain(pos, ResourceBank.TerrainDefOf.EmptySpace);
				}

                RoofDef sourceRoof = sourceMap.roofGrid.RoofAt(pos);
				if (IsRoofDefAirtight(sourceRoof))
				{
					roofToCopy.Add(new Tuple<IntVec3, RoofDef>(adjustedPos, sourceRoof));
				}
				sourceMap.roofGrid.SetRoof(pos, null);
				if (core is Building_ShipBridge && playerMove) //home zone ships
				{
					sourceMap.areaManager.Home[pos] = false;
					targetMap.areaManager.Home[adjustedPos] = true;
				}
			}
			if (!targetMapIsSpace) //clear ground map of all floors (would be beter to store it and load it on takeoff)
            {
                foreach (IntVec3 pos in targetArea)
                {
                    targetMap.terrainGrid.RemoveTopLayer(pos, false);
                }
            }
            if (adjustment != IntVec3.Zero) //ship cache: offset area, find adjacent ships
            {
                ship.Area = targetArea;
                foreach (IntVec3 pos in targetArea)
                {
                    foreach (IntVec3 vec in GenAdj.CellsAdjacentCardinal(pos, Rot4.North, new IntVec2(1, 1)).Where(v => !targetArea.Contains(v) && targetMapComp.MapShipCells.ContainsKey(v)))
                    {
                        shipIndexes.Add(targetMapComp.MapShipCells[vec].Item1);
                    }
                }
                Log.Message("Adjacent ships found in area: " + shipIndexes.Count);
            }
            if (devMode)
				watch.Record("processSourceArea");

			//move live pawns out of target area, destroy non buildings
			foreach (Thing thing in toDestroy)
			{
				if (thing is Pawn pawn && (!pawn.Dead || !pawn.Downed))
				{
					pawn.pather.StopDead();
					thing.Position = CellFinder.RandomClosewalkCellNear(thing.Position, targetMap, 50, (IntVec3 x) => !targetArea.Contains(x));
					pawn.pather.nextCell = pawn.Position.RandomAdjacentCell8Way();
				}
				else if (!thing.Destroyed)
					thing.Destroy();
			}
			if (devMode)
				watch.Record("destroySource");

			//move map - draw fuel
			if (core is Building_ShipBridge && playerMove)
			{
				float fuelNeeded = ship.Mass;
				float fuelStored = 0f;
				List<CompEngineTrail> engines = new List<CompEngineTrail>();
				foreach (CompEngineTrail engine in ship.Engines.Where(e => e.flickComp.SwitchIsOn && !e.Props.energy && !e.Props.reactionless && e.refuelComp.Fuel > 0 && (!targetMapIsSpace || e.Props.takeOff)))
                {
                    fuelStored += engine.refuelComp.Fuel;
                    if (engine.PodFueled)
                    {
                        fuelStored += engine.refuelComp.Fuel;
                        if (ModsConfig.BiotechActive && !sourceMapIsSpace)
                        {
                            foreach (IntVec3 v in engine.ExhaustArea)
                                v.Pollute(sourceMap, true);
                        }
                    }
                    engines.Add(engine);
                }
				if (sourceMapIsSpace)
                {
                    if (targetMapIsSpace) //space map 1%
                        fuelNeeded *= 0.01f;
                    else //to ground 10%
                        fuelNeeded *= 0.1f;
                }
				foreach (CompEngineTrail engine in engines)
                {
                    engine.refuelComp.ConsumeFuel(fuelNeeded * engine.refuelComp.Fuel / fuelStored);
                    if (targetMapIsSpace)
                    {
                        if (engine.parent.Rotation.AsByte == 0)
                            fireExplosions.Add(engine.parent.Position + new IntVec3(0, 0, -3));
                        else if (engine.parent.Rotation.AsByte == 1)
                            fireExplosions.Add(engine.parent.Position + new IntVec3(-3, 0, 0));
                        else if (engine.parent.Rotation.AsByte == 2)
                            fireExplosions.Add(engine.parent.Position + new IntVec3(0, 0, 3));
                        else
                            fireExplosions.Add(engine.parent.Position + new IntVec3(3, 0, 0));
                    }
                }
				if (devMode)
					watch.Record("takeoffEngineEffects");
			}

			//move things
			foreach (Thing spawnThing in toSave)
			{
				if (!spawnThing.Destroyed)
				{
					try
					{
						if (spawnThing.Spawned)
							spawnThing.DeSpawn();

						int adjz = 0;
						int adjx = 0;
						if (rotb == 3)
						{
							//CCW rot, breaks non rot, uneven things
							if (spawnThing.def.rotatable)
							{
								spawnThing.Rotation = new Rot4(spawnThing.Rotation.AsByte + rotb);
							}
							else if (spawnThing.def.rotatable == false && spawnThing.def.size.x % 2 == 0)
								adjx -= 1;
							rot.x = targetMap.Size.x - spawnThing.Position.z + adjx;
							rot.z = spawnThing.Position.x;
							spawnThing.Position = rot + adjustment;
						}
						else if (rotb == 2)
						{
							//flip using 2x CCW rot
							if (spawnThing.def.rotatable)
							{
								spawnThing.Rotation = new Rot4(spawnThing.Rotation.AsByte + rotb);
							}
							else if (spawnThing.def.rotatable == false && spawnThing.def.size.x % 2 == 0)
								adjx -= 1;
							if (spawnThing.def.rotatable == false && spawnThing.def.size.x != spawnThing.def.size.z)
							{
								if (spawnThing.def.size.z % 2 == 0) //5x2
									adjz -= 1;
								else //6x3,6x7
									adjz += 1;
							}
							rot.x = targetMap.Size.x - spawnThing.Position.z + adjx;
							rot.z = spawnThing.Position.x;
							IntVec3 tempPos = rot;
							rot.x = targetMap.Size.x - tempPos.z + adjx;
							rot.z = tempPos.x + adjz;
							spawnThing.Position = rot + adjustment;
						}
						else
							spawnThing.Position += adjustment;
						try
						{
							if (!spawnThing.Destroyed && spawnThing.TryGetComp<CompShipLight>()==null)
							{
								spawnThing.SpawnSetup(targetMap, false);
							}
						}
						catch (Exception e)
						{
							var sb = new StringBuilder();
							sb.AppendFormat("Error spawning {0}: {1}\n", spawnThing.def.label, e.Message);
							if (devMode)
								sb.AppendLine(e.StackTrace);
							Log.Warning(sb.ToString());
						}

						//post move
						if (fac != null && spawnThing is Building && spawnThing.def.CanHaveFaction)
							spawnThing.SetFaction(fac);
						if (spawnThing is Pawn pawn)
						{
                            WorldComp.PawnsInSpaceCache.Remove(pawn.thingIDNumber);
						}
					}
					catch (Exception e)
					{
						var sb = new StringBuilder();
						sb.AppendFormat("Error moving {0}: {1}\n", spawnThing.def.label, e.Message);
						if (devMode)
							sb.AppendLine(e.StackTrace);
						Log.Error(sb.ToString());
					}
				}
			}
			if (devMode)
				watch.Record("moveThings");
			AirlockBugFlag = false;
			if (shipIndexes.Count > 1) //ship cache: adjacent ships found, merge in order: largest ship, ship, wreck
            {
                Log.Message("SOS2: ".Colorize(Color.cyan) + " ship move found adjacent ships, merging!");
                targetMapComp.CheckAndMerge(shipIndexes);
            }
            //move zones
            if (zonesToCopy.Any())
			{
				foreach (Zone zone in zonesToCopy) //only move fully contained zones
				{
					bool allOn = true;
					foreach (IntVec3 v in zone.Cells)
					{
						if (!sourceArea.Contains(v))
						{
							allOn = false;
							break;
						}
					}
					if (allOn)
					{
						sourceMap.zoneManager.DeregisterZone(zone);
						zone.zoneManager = targetMap.zoneManager;
						List<IntVec3> newCells = new List<IntVec3>();
						foreach (IntVec3 cell in zone.cells)
							newCells.Add(Transform(cell));
						zone.cells = newCells;
						targetMap.zoneManager.RegisterZone(zone);
					}
				}
				targetMap.zoneManager.RebuildZoneGrid();
				sourceMap.zoneManager.RebuildZoneGrid();

				//regen affected map layers
				List<Section> sourceSec = new List<Section>();
				foreach (IntVec3 pos in sourceArea)
				{
					var sec = sourceMap.mapDrawer.SectionAt(pos);
					if (!sourceSec.Contains(sec))
						sourceSec.Add(sec);
				}
				foreach (Section sec in sourceSec)
				{
					sec.RegenerateLayers(MapMeshFlag.Zone);
				}
				List<Section> targetSec = new List<Section>();
				foreach (IntVec3 pos in targetArea)
				{
					var sec = targetMap.mapDrawer.SectionAt(pos);
					if (!targetSec.Contains(sec))
						targetSec.Add(sec);
				}
				foreach (Section sec in targetSec)
				{
					sec.RegenerateLayers(MapMeshFlag.Zone);
				}
			}
			if (devMode)
				watch.Record("moveZones");

			//move terrain
			try
            {
                foreach (Tuple<IntVec3, TerrainDef> tup in terrainToCopy)
                {
                    var targetTile = targetMap.terrainGrid.TerrainAt(tup.Item1);
                    if (!targetTile.layerable || IsHull(targetTile))
                    {
                        targetMap.terrainGrid.SetTerrain(tup.Item1, tup.Item2);
                    }
                }
                if (includeRock)
                {
                    foreach (IntVec3 pos in sourceArea)
                    {
                        sourceMap.terrainGrid.SetTerrain(pos, ResourceBank.TerrainDefOf.EmptySpace);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Warning("" + e);
            }
			if (devMode)
				watch.Record("moveTerrain");

			//move roofs
			foreach (Tuple<IntVec3, RoofDef> tup in roofToCopy)
			{
				targetMap.roofGrid.SetRoof(tup.Item1, tup.Item2);
			}
			if (devMode)
				watch.Record("moveRoof");

			//restore temp in ship
			foreach (Tuple<IntVec3, float> t in posTemp)
			{
				Room room = t.Item1.GetRoom(targetMap);
				room.Temperature = t.Item2;
			}

			//landing - remove space map if no pawns or cores
			if (!targetMapIsSpace && !sourceMap.spawnedThings.Any((Thing t) => (t is Pawn || (t is Building_ShipBridge b && b.mannableComp == null)) && !t.Destroyed))
			{
				WorldObject oldParent = sourceMap.Parent;
				Current.Game.DeinitAndRemoveMap_NewTemp(sourceMap, false);
				Find.World.worldObjects.Remove(oldParent);
			}

			//takeoff - explosions
			if (!sourceMapIsSpace)
			{
				foreach (IntVec3 pos in fireExplosions)
				{
					GenExplosion.DoExplosion(pos, sourceMap, 3.9f, DamageDefOf.Flame, null);
				}
			}

            //power
            if (sourceMap != targetMap)
            {
                sourceMap.powerNetManager.UpdatePowerNetsAndConnections_First();
                targetMap.powerNetManager.UpdatePowerNetsAndConnections_First();
            }

            //heat
            targetMap.GetComponent<ShipHeatMapComp>().heatGridDirty = true;

			if (devMode)
			{
				watch.Record("finalize");
                Log.Message("SOS2: ".Colorize(Color.cyan) + sourceMap + " Ship ".Colorize(Color.green) + shipIndex + " Moved ship with: " + core + "\n" + watch.MakeReport());

            }
        }
		public static void SaveShip(Building_ShipBridge core, string file)
		{
			List<Thing> toSave = new List<Thing>();
			List<Zone> zones = new List<Zone>();

            Map map = core.Map;
            var mapComp = map.GetComponent<ShipHeatMapComp>();
            var ship = mapComp.ShipsOnMapNew[core.ShipIndex];
            HashSet<IntVec3> area = ship.Area;

			HashSet<Ideo> ideosAboardShip = new HashSet<Ideo>();
			HashSet<CustomXenotype> xenosAboardShip = new HashSet<CustomXenotype>();
			HashSet<Pawn> pawnsAboardShip = new HashSet<Pawn>();
			List<IntVec3> roofPos = new List<IntVec3>();
			List<RoofDef> roofDefs = new List<RoofDef>();
			List<IntVec3> terrainPos = new List<IntVec3>();
			List<TerrainDef> terrainDefs = new List<TerrainDef>();

			foreach (IntVec3 pos in area)
			{
				//add all things, terrain from area
				List<Thing> allTheThings = pos.GetThingList(map);
				foreach (Thing t in allTheThings)
				{
					if (t is Building b)
					{
						if (b is Building_CryptosleepCasket cc && cc.HasAnyContents)
						{
							if (cc.ContainedThing is Pawn p)
								pawnsAboardShip.Add(p);
						}
						else if (t is Building_ShipAirlock a && a.docked)
						{
							a.DeSpawnDock();
						}
						var engineComp = t.TryGetComp<CompEngineTrail>();
						if (engineComp != null)
							engineComp.Off();
					}
					else if (t is Pawn p)
                    {
						if (p.jobs != null)
						{
							p.jobs.ClearQueuedJobs();
							p.jobs.EndCurrentJob(JobCondition.Incompletable);
						}
						if (ModsConfig.RoyaltyActive && p.royalty != null && p.royalty.HasAnyTitleIn(Faction.OfEmpire))
                            p.royalty.SetTitle(Faction.OfEmpire, null, false);
                    }
					if (t.Map.zoneManager.ZoneAt(t.Position) != null && !zones.Contains(t.Map.zoneManager.ZoneAt(t.Position)))
					{
						zones.Add(t.Map.zoneManager.ZoneAt(t.Position));
					}
					if (!toSave.Contains(t))
					{
						toSave.Add(t);
					}
				}

				var sourceTerrain = map.terrainGrid.TerrainAt(pos);
				if (sourceTerrain.layerable && !IsHull(sourceTerrain))
				{
					terrainPos.Add(pos);
					terrainDefs.Add(sourceTerrain);
					map.terrainGrid.RemoveTopLayer(pos, false);
				}

				var sourceRoof = map.roofGrid.RoofAt(pos);
				if (IsRoofDefAirtight(sourceRoof))
				{
					roofPos.Add(pos);
					roofDefs.Add(sourceRoof);
				}
				map.areaManager.Home[pos] = false;
			}

			//remove non fully contained zones
			List<Zone> dirtyZones = new List<Zone>();
			foreach (Zone zone in zones)
			{
				foreach (IntVec3 v in zone.Cells)
				{
					if (!area.Contains(v))
					{
						if (!dirtyZones.Contains(zone))
							dirtyZones.Add(zone);
						break;
					}
				}
			}
			foreach (Zone zone in dirtyZones)
			{
				zones.Remove(zone);
			}

			StringBuilder stringBuilder = new StringBuilder();
			foreach (Pawn pawn in pawnsAboardShip)
			{
				stringBuilder.AppendLine("   " + pawn.LabelCap);
				Find.StoryWatcher.statsRecord.colonistsLaunched++;
				TaleRecorder.RecordTale(TaleDefOf.LaunchedShip, new object[] { pawn });

				if (pawn.Ideo != null && pawn.Ideo != Faction.OfPlayer.ideos.PrimaryIdeo)
					ideosAboardShip.Add(pawn.Ideo);
				if (pawn.genes != null && pawn.genes.CustomXenotype != null)
					xenosAboardShip.Add(pawn.genes.CustomXenotype);

				List<DirectPawnRelation> toPrune = new List<DirectPawnRelation>();
				foreach (DirectPawnRelation relation in pawn.relations.DirectRelations)
				{
					if (!pawnsAboardShip.Contains(relation.otherPawn))
						toPrune.Add(relation);
				}
				foreach (DirectPawnRelation relation in toPrune)
					pawn.relations.RemoveDirectRelation(relation);
			}

			List<GameComponent> components = new List<GameComponent>();
			foreach (GameComponent comp in Current.Game.components)
			{
				//Don't copy ours or vanilla components. We'll probably need a more general-purpose way to add more exceptions if other mods' components shouldn't be moved.
				if (!(comp is EnvironmentCachingUtility || comp is GameComponent_Bossgroup || comp is GameComponent_DebugTools || comp is GameComponent_OnetimeNotification))
					components.Add(comp);
			}

			string playerFactionName = Faction.OfPlayer.Name;
			Ideo playerFactionIdeo = Faction.OfPlayer.ideos.PrimaryIdeo;

			SafeSaver.Save(file, "SoS2Ship", (Action)(() =>
			{
				ScribeMetaHeaderUtility.WriteMetaHeader();

				Scribe_Defs.Look<FactionDef>(ref Faction.OfPlayer.def, "playerFactionDef");
				Scribe_Values.Look(ref playerFactionName, "playerFactionName");
				//typeof(GameDataSaveLoader).GetField("isSavingOrLoadingExternalIdeo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, true);
				Scribe_Deep.Look(ref playerFactionIdeo, "playerFactionIdeo");
				//typeof(GameDataSaveLoader).GetField("isSavingOrLoadingExternalIdeo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, false);

				Scribe_Deep.Look<TickManager>(ref Current.Game.tickManager, true, "tickManager");
				Scribe_Deep.Look<PlaySettings>(ref Current.Game.playSettings, true, "playSettings");
				Scribe_Deep.Look<StoryWatcher>(ref Current.Game.storyWatcher, true, "storyWatcher");
				Scribe_Deep.Look<ResearchManager>(ref Current.Game.researchManager, true, "researchManager");
				Scribe_Deep.Look<TaleManager>(ref Current.Game.taleManager, true, "taleManager");
				Scribe_Deep.Look<PlayLog>(ref Current.Game.playLog, true, "playLog");
				Scribe_Deep.Look<OutfitDatabase>(ref Current.Game.outfitDatabase, true, "outfitDatabase");
				Scribe_Deep.Look<DrugPolicyDatabase>(ref Current.Game.drugPolicyDatabase, true, "drugPolicyDatabase");
				Scribe_Deep.Look<FoodRestrictionDatabase>(ref Current.Game.foodRestrictionDatabase, true, "foodRestrictionDatabase");
				Scribe_Deep.Look<UniqueIDsManager>(ref Current.Game.uniqueIDsManager, true, "uniqueIDsManager");

				//typeof(GameDataSaveLoader).GetField("isSavingOrLoadingExternalIdeo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, true);
				Scribe_Collections.Look<Ideo>(ref ideosAboardShip, "ideos", LookMode.Deep);
				//typeof(GameDataSaveLoader).GetField("isSavingOrLoadingExternalIdeo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).SetValue(null, false);
				Scribe_Collections.Look<CustomXenotype>(ref xenosAboardShip, "xenotypes", LookMode.Deep);
				Scribe_Deep.Look<CustomXenogermDatabase>(ref Current.Game.customXenogermDatabase, true, "customXenogermDatabase");
				Scribe_Collections.Look<GameComponent>(ref components, "components", LookMode.Deep);

				Scribe_Collections.Look<Thing>(ref toSave, "shipThings", LookMode.Deep);
				Scribe_Collections.Look<Zone>(ref zones, "shipZones", LookMode.Deep);
				Scribe_Collections.Look<IntVec3>(ref terrainPos, "terrainPos");
				Scribe_Collections.Look<TerrainDef>(ref terrainDefs, "terrainDefs");
				Scribe_Collections.Look<IntVec3>(ref roofPos, "roofPos");
				Scribe_Collections.Look<RoofDef>(ref roofDefs, "roofDefs");
			}));

			Log.Message("Saved ship with building " + core);

			GameVictoryUtility.ShowCredits(GameVictoryUtility.MakeEndCredits("GameOverShipPlanetLeaveIntro".Translate(), "GameOverShipPlanetLeaveEnding".Translate(), stringBuilder.ToString(), "GameOverColonistsEscaped", null), null, false, 5f);

			RemoveShip(core, true);
		}
        public static void SaveShipToFile(Building_ShipBridge bridge)
        {
            KillAllColonistsNotInCrypto(bridge.Map, bridge);
            string folder = Path.Combine(GenFilePaths.SaveDataFolderPath, "SoS2");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            string filename = Path.Combine(folder, Faction.OfPlayer.Name + "_" + bridge.ShipName + ".sos2");
            SaveShip(bridge, filename);
        }
        public static void SpaceTravelWarning(Action action)
        {
            DiaNode theNode;
            theNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("ShipAbandonColoniesWarning"));

            DiaOption accept = new DiaOption("Accept");
            accept.resolveTree = true;
            accept.action = action;
            theNode.options.Add(accept);

            DiaOption cancel = new DiaOption("Cancel");
            cancel.resolveTree = true;
            theNode.options.Add(cancel);

            Dialog_NodeTree dialog_NodeTree = new Dialog_NodeTree(theNode, true, false, null);
            dialog_NodeTree.silenceAmbientSound = false;
            dialog_NodeTree.closeOnCancel = true;
            Find.WindowStack.Add(dialog_NodeTree);
        }
        public static void KillAllColonistsNotInCrypto(Map shipMap, Building_ShipBridge bridge)
        {
            List<Pawn> toKill = new List<Pawn>();
            foreach (Pawn p in shipMap.mapPawns.AllPawns)
            {
                if (p.RaceProps != null && p.RaceProps.IsFlesh && (!p.InContainerEnclosed) && (!IsHologram(p) || p.health.hediffSet.GetFirstHediff<HediffPawnIsHologram>().consciousnessSource.Map != shipMap))
                    toKill.Add(p);
            }
            foreach (Pawn p in toKill)
                p.Kill(null);
            foreach (Thing t in shipMap.spawnedThings)
            {
                if (t is Corpse c)
                {
                    var compRot = c.GetComp<CompRottable>();
                    if (t.GetRoom() != null && t.GetRoom().Temperature > 0 && compRot != null)
                        compRot.RotProgress = compRot.PropsRot.TicksToDessicated;
                }
            }
            WorldComp.renderedThatAlready = false;

            float EnergyCost = 100000;
            foreach (CompPowerBattery capacitor in bridge.PowerComp.PowerNet.batteryComps)
            {
                if (capacitor.StoredEnergy <= EnergyCost)
                {
                    capacitor.SetStoredEnergyPct(capacitor.StoredEnergyPct - (EnergyCost / capacitor.Props.storedEnergyMax));
                    EnergyCost = 0;
                    break;
                }
                else
                {
                    EnergyCost -= capacitor.StoredEnergy;
                    capacitor.SetStoredEnergyPct(0.05f);
                }
            }

            //Oddly enough, interstellar flight takes a lot of time
            /*int years = Rand.RangeInclusive(minTravelTime.Value, maxTravelTime.Value);
            Current.Game.tickManager.DebugSetTicksGame(Current.Game.tickManager.TicksAbs + 3600000 * years);
            Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoSTimePassedLabel"), TranslatorFormattedStringExtensions.Translate("SoSTimePassed",years), LetterDefOf.NeutralEvent);*/
        }
		public static void RemoveShip(Building core, bool planetTravel = false, HashSet<IntVec3> area = null, Map map = null)
        {
			if (core != null)
            {
                map = core.Map;
            }
            var mapComp = map.GetComponent<ShipHeatMapComp>();
			int index = -1;
            if (core != null)
            {
                index = mapComp.ShipIndexOnVec(core.Position);
                if (index != -1)
                {
                    var ship = mapComp.ShipsOnMapNew[index];
                    area = ship.Area;
                    mapComp.RemoveShipFromCache(index);
                }
            }
            AirlockBugFlag = true;
            List<Thing> things = new List<Thing>();
            List<Zone> zones = new List<Zone>();
            foreach (IntVec3 pos in area)
            {
                mapComp.MapShipCells.Remove(pos);
                things.AddRange(pos.GetThingList(map));
				if (map.zoneManager.ZoneAt(pos) != null && !zones.Contains(map.zoneManager.ZoneAt(pos)))
				{
					zones.Add(map.zoneManager.ZoneAt(pos));
				}
			}
            foreach (Thing t in things)
			{
				try
				{
					if (!planetTravel && t is Pawn)
						t.Kill();
					if (t.def.destroyable && !t.Destroyed)
					{
						CompRefuelable refuelable = t.TryGetComp<CompRefuelable>();
						if (refuelable != null)
							refuelable.ConsumeFuel(refuelable.Fuel); //To avoid CompRefuelable.PostDestroy
						t.Destroy(DestroyMode.Vanish);
					}
				}
				catch (Exception e)
				{
					Log.Warning("" + e);
				}
			}
			foreach (IntVec3 pos in area)
			{
				map.terrainGrid.SetTerrain(pos, ResourceBank.TerrainDefOf.EmptySpace);
                map.roofGrid.SetRoof(pos, null);
            }
			AirlockBugFlag = false;

            if (index != -1)
            {
                mapComp.RemoveShipFromCache(index);
            }
            //regen affected map layers
            List<Section> sourceSec = new List<Section>();
			if (zones.Any())
			{
				foreach (Zone zone in zones) //only remove fully contained zones
				{
					bool allOn = true;
					foreach (IntVec3 v in zone.Cells)
                    {
						if (!area.Contains(v))
                        {
							allOn = false;
							break;
						}
                    }
					if (allOn)
						map.zoneManager.DeregisterZone(zone);
				}
				map.zoneManager.RebuildZoneGrid();
				foreach (IntVec3 pos in map)
				{
					var sec = map.mapDrawer.SectionAt(pos);
					if (!sourceSec.Contains(sec))
						sourceSec.Add(sec);
				}
				foreach (Section sec in sourceSec)
				{
					sec.RegenerateLayers(MapMeshFlag.Zone);
				}
				List<Section> targetSec = new List<Section>();
				foreach (IntVec3 pos in area)
				{
					var sec = map.mapDrawer.SectionAt(pos);
					if (!targetSec.Contains(sec))
						targetSec.Add(sec);
				}
				foreach (Section sec in targetSec)
				{
					sec.RegenerateLayers(MapMeshFlag.Zone);
				}
			}
			map.GetComponent<ShipHeatMapComp>().heatGridDirty = true;
        }
        public static HashSet<IntVec3> FindAreaAttached(Building root, bool includeRock = false)
        {
            if (root == null || root.Destroyed)
                return new HashSet<IntVec3>();

            var map = root.Map;
            var cellsTodo = new HashSet<IntVec3>();
            var cellsDone = new HashSet<IntVec3>();
            var cellsFound = new HashSet<IntVec3>();
            cellsTodo.AddRange(GenAdj.CellsOccupiedBy(root));
            cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(root));
            while (cellsTodo.Count > 0)
            {
                var current = cellsTodo.First();
                cellsTodo.Remove(current);
                cellsDone.Add(current);
                if (current.GetThingList(map).Any(t => t is Building b && (b.def.building.shipPart || (includeRock && b.def.building.isNaturalRock))) || (includeRock && IsRock(current.GetTerrain(map))))
                {
                    cellsFound.Add(current);
                    cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(current, Rot4.North, new IntVec2(1, 1)).Where(v => !cellsDone.Contains(v)));
                }
            }
            return cellsFound;
        }
        public static bool CompatibleWithShipLoad(ScenPart item)
		{
			return !(item is ScenPart_PlayerFaction || item is ScenPart_PlayerPawnsArriveMethod || item is ScenPart_ScatterThings || item is ScenPart_Naked || item is ScenPart_NoPossessions || item is ScenPart_GameStartDialog || item is ScenPart_AfterlifeVault || item is ScenPart_SetNeedLevel || item is ScenPart_StartingAnimal || item is ScenPart_StartingMech || item is ScenPart_StartingResearch || item is ScenPart_StartingThing_Defined || item is ScenPart_StartInSpace || item is ScenPart_ThingCount || item is ScenPart_ConfigPage_ConfigureStartingPawnsBase);
		}
		public static bool IsHologram(Pawn pawn)
		{
			return pawn.health.hediffSet.GetFirstHediff<HediffPawnIsHologram>() != null;
		}
		public static bool ExposedToOutside(Room room)
		{
			return room == null || room.OpenRoofCount > 0 || room.TouchesMapEdge;
		}
		public static bool AnyAdjRoomNotOutside(IntVec3 pos, Map map)
		{
			foreach (IntVec3 v in GenAdj.CellsAdjacentCardinal(pos, Rot4.North, new IntVec2(1, 1)))
			{
                if (!ExposedToOutside(v.GetRoom(map)))
					return true;
			}
			return false;
		}
        public static byte EVAlevel(Pawn pawn)
		{
			/*
			8 - natural, unremovable, boosted: no rechecks
			7 - boosted EVA: reset on equip change
			6 - natural, unremovable: no rechecks
			5 - proper EVA: reset on equip/hediff change
			4 - active belt: reset on end
			3 - inactive belt: trigger in weather
			2 - skin only: reset on hediff change
			1 - air only: reset on hediff change
			0 - none: dead soon
			*/
			if (WorldComp.PawnsInSpaceCache.TryGetValue(pawn.thingIDNumber, out byte eva))
				return eva;
			byte result = EVAlevelSlow(pawn);
            //Log.Message("EVA slow lvl: " + result + " on pawn " + pawn.Name);
            WorldComp.PawnsInSpaceCache[pawn.thingIDNumber] = result;
			return result;
		}
		public static byte EVAlevelSlow(Pawn pawn)
		{
			if (pawn.RaceProps.IsMechanoid || IsHologram(pawn) || !pawn.RaceProps.IsFlesh)
				return 8;
			if (pawn.def.tradeTags?.Contains("AnimalInsectSpace") ?? false)
				return 6;
			if (pawn.apparel == null)
				return 0;
			bool hasHelmet = false;
			bool hasSuit = false;
			bool hasBelt = false;
			foreach (Apparel app in pawn.apparel.WornApparel)
			{
				if (app.def.apparel.tags.Contains("EVA"))
				{
					if (app.def.apparel.layers.Contains(ApparelLayerDefOf.Overhead))
						hasHelmet = true;
					if (app.def.apparel.layers.Contains(ApparelLayerDefOf.Shell) || app.def.apparel.layers.Contains(ApparelLayerDefOf.Middle))
						hasSuit = true;
				}
				else if (app.def.defName.Equals("Apparel_SpaceSurvivalBelt"))
				{
					hasBelt = true;
				}
			}
			if (hasHelmet && hasSuit)
				return 7;
			bool hasLung = false;
			bool hasSkin = false;
			if (pawn.health.hediffSet.GetFirstHediffOfDef(ResourceBank.HediffDefOf.SoSArchotechLung) != null)
				hasLung = true;
			if (pawn.health.hediffSet.GetFirstHediffOfDef(ResourceBank.HediffDefOf.SoSArchotechSkin) != null)
				hasSkin = true;
			if (hasLung && hasSkin)
				return 5;
			if (pawn.health.hediffSet.GetFirstHediffOfDef(ResourceBank.HediffDefOf.SpaceBeltBubbleHediff) != null)
				return 4;
			if (hasBelt)
				return 3;
			if (hasSkin)
				return 2;
			if (hasLung)
				return 1;
			return 0;
		}
	}
}