using System;
using System.Collections.Generic;
using System.Reflection;
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
using RimworldMod.VacuumIsNotFun;
using System.Collections;
using System.Reflection.Emit;
using UnityEngine.SceneManagement;
using System.Linq.Expressions;
using static SaveOurShip2.ModSettings_SoS;

namespace SaveOurShip2
{
	
	[StaticConstructorOnStartup]
	static class Setup
	{
		static Setup()
		{
			Harmony pat = new Harmony("ShipInteriorMod2");
			
			//Legacy methods. All 3 of these could technically be merged
			ShipInteriorMod2.Initialize(pat);
			ShipInteriorMod2.DefsLoaded();
			ShipInteriorMod2.SceneLoaded();
			
			pat.PatchAll();
			//Needs an init delay
			if (useSplashScreen) LongEventHandler.QueueLongEvent(() => ShipInteriorMod2.UseCustomSplashScreen(), "ShipInteriorMod2", false, null);
		}
	}
	public class ShipInteriorMod2 : Mod
	{
		public ShipInteriorMod2(ModContentPack content) : base(content)
		{
			base.GetSettings<ModSettings_SoS>();
		}

		public static readonly float crittersleepBodySize = 0.7f;
		public static bool ArchoStuffEnabled = true;//unassigned???
		public static bool SoSWin = false;
		public static bool loadedGraphics = false;
		public static bool renderedThatAlready = false;
		public static bool AirlockBugFlag = false;//shipmove
		public static Building shipOriginRoot = null;//used for patched original launch code
		public static Map shipOriginMap = null;//used to check for shipmove map size problem, reset after move
		
		// Additional array of compatible RoofDefs from other mods.
		public static RoofDef[] compatibleAirtightRoofs;
		// Contains terrain types that are considered a "rock".
		private static TerrainDef[] rockTerrains;

		public static List<ThingDef> randomPlants;
		public static Dictionary<ThingDef, ThingDef> wreckDictionary;

		public static void Initialize(Harmony pat)
		{
			// Must be manually patched as SectionLayer_Terrain is internal
			var regenerateMethod = AccessTools.TypeByName("SectionLayer_Terrain").GetMethod("Regenerate");
			var regeneratePostfix = typeof(SectionRegenerateHelper).GetMethod("Postfix");
			pat.Patch(regenerateMethod, postfix: new HarmonyMethod(regeneratePostfix));

			if (ModLister.HasActiveModWithName("RT Fuse"))
			{
				Log.Message("SOS2: Enabling compatibility with RT Fuze");
			}
			else
			{
				var doShortCircuitMethod = typeof(ShortCircuitUtility).GetMethod("DoShortCircuit");
				var prefix = typeof(NoShortCircuitCapacitors).GetMethod("disableEventQuestionMark");
				var postfix = typeof(NoShortCircuitCapacitors).GetMethod("tellThePlayerTheDayWasSaved");
				pat.Patch(doShortCircuitMethod, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
			}

			//Similarly, with firefighting
			//TODO - temporarily disabled until we can figure out why we're getting "invalid IL" errors
			/*var FirefightMethod = AccessTools.TypeByName("JobGiver_FightFiresNearPoint").GetMethod("TryGiveJob", BindingFlags.NonPublic | BindingFlags.Instance);
            var FirefightPostfix = typeof(FixFireBugC).GetMethod("Postfix");
            HarmonyInst.Patch(FirefightMethod, postfix: new HarmonyMethod(FirefightPostfix));*/
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard options = new Listing_Standard();
			options.Begin(inRect);

			options.Label("SoS.Settings.DifficultySoS".Translate("0.5", "10", "1", Math.Round(difficultySoS, 1).ToString()), -1f, "SoS.Settings.DifficultySoS.Desc".Translate());
			difficultySoS = options.Slider((float)difficultySoS, 0.5f, 10f);

			options.Label("SoS.Settings.FrequencySoS".Translate("0.5", "10", "1", Math.Round(frequencySoS, 1).ToString()), -1f, "SoS.Settings.FrequencySoS.Desc".Translate());
			frequencySoS = options.Slider((float)frequencySoS, 0.5f, 10f);

			options.Label("SoS.Settings.NavyShipChance".Translate("0", "1", "0.2", Math.Round(navyShipChance, 1).ToString()), -1f, "SoS.Settings.NavyShipChance.Desc".Translate());
			navyShipChance = options.Slider((float)navyShipChance, 0f, 1f);

			options.Label("SoS.Settings.FleetChance".Translate("0", "1", "0.3", Math.Round(fleetChance, 1).ToString()), -1f, "SoS.Settings.FleetChance.Desc".Translate());
			fleetChance = options.Slider((float)fleetChance, 0f, 1f);

			options.Gap();
			options.CheckboxLabeled("SoS.Settings.EasyMode".Translate(), ref easyMode, "SoS.Settings.EasyMode.Desc".Translate());
			options.CheckboxLabeled("SoS.Settings.UseVacuumPathfinding".Translate(), ref useVacuumPathfinding, "SoS.Settings.UseVacuumPathfinding.Desc".Translate());
			options.Gap();

			options.Label("SoS.Settings.MinTravelTime".Translate("1", "50", "5", minTravelTime.ToString()), -1f, "SoS.Settings.MinTravelTime.Desc".Translate());
			minTravelTime = (int)options.Slider(minTravelTime, 1f, 50f);

			options.Label("SoS.Settings.MaxTravelTime".Translate("50", "1000", "100", maxTravelTime.ToString()), -1f, "SoS.Settings.MaxTravelTime.Desc".Translate());
			maxTravelTime = (int)options.Slider(maxTravelTime, 1f, 1000f);

			options.CheckboxLabeled("SoS.Settings.RenderPlanet".Translate(), ref renderPlanet, "SoS.Settings.RenderPlanet.Desc".Translate());
			options.CheckboxLabeled("SoS.Settings.UseSplashScreen".Translate(), ref useSplashScreen, "SoS.Settings.UseSplashScreens.Desc".Translate());
			options.Gap();
			options.Label("SoS.Settings.OffsetUIx".Translate(), -1f, "SoS.Settings.OffsetUIx.Desc".Translate());
			string bufferX = "0";
			options.TextFieldNumeric<int>(ref offsetUIx, ref bufferX, int.MinValue, int.MaxValue);

			options.Label("SoS.Settings.OffsetUIy".Translate(), -1f, "SoS.Settings.OffsetUIy.Desc".Translate());
			string bufferY = "0";
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
			Log.Message("SOS2EXP V80f2 active");
			randomPlants = DefDatabase<ThingDef>.AllDefs.Where(t => t.plant != null && !t.defName.Contains("Anima")).ToList();

			foreach (EnemyShipDef ship in DefDatabase<EnemyShipDef>.AllDefs.Where(d => d.saveSysVer < 2 && !d.neverRandom).ToList())
            {
				Log.Error("SOS2: mod \"" + ship.modContentPack.Name + "\" contains EnemyShipDef: \"" + ship + "\" that can spawn as a random ship but is saved with an old version of CK!");
			}

			wreckDictionary = new Dictionary<ThingDef, ThingDef>
			{
				{ThingDef.Named("ShipHullTile"), ThingDef.Named("ShipHullTileWrecked")},
				{ThingDef.Named("ShipHullTileMech"), ThingDef.Named("ShipHullTileWrecked")},
				{ThingDef.Named("ShipHullTileArchotech"), ThingDef.Named("ShipHullTileWrecked")},
				{ThingDef.Named("Ship_Beam"), ThingDef.Named("Ship_Beam_Wrecked")},
				{ThingDef.Named("Ship_BeamMech"), ThingDef.Named("Ship_Beam_Wrecked")},
				{ThingDef.Named("Ship_BeamArchotech"), ThingDef.Named("Ship_Beam_Wrecked")},
				{ThingDef.Named("Ship_Beam_Unpowered"), ThingDef.Named("Ship_Beam_Wrecked")},
				{ThingDef.Named("Ship_BeamMech_Unpowered"), ThingDef.Named("Ship_Beam_Wrecked")},
				{ThingDef.Named("Ship_BeamArchotech_Unpowered"), ThingDef.Named("Ship_Beam_Wrecked")},
				{ThingDef.Named("ShipInside_SolarGenerator"), ThingDef.Named("Ship_Beam_Wrecked")},
				{ThingDef.Named("ShipInside_SolarGeneratorMech"), ThingDef.Named("Ship_Beam_Wrecked")},
				{ThingDef.Named("ShipInside_SolarGeneratorArchotech"), ThingDef.Named("Ship_Beam_Wrecked")},
				{ThingDef.Named("ShipInside_PassiveVent"), ThingDef.Named("Ship_Beam_Wrecked")},
				{ThingDef.Named("ShipInside_PassiveVentMechanoid"), ThingDef.Named("Ship_Beam_Wrecked")},
				{ThingDef.Named("ShipInside_PassiveVentArchotech"), ThingDef.Named("Ship_Beam_Wrecked")},
				{ThingDef.Named("ShipAirlock"), ThingDef.Named("ShipAirlockWrecked")},
				{ThingDef.Named("ShipAirlockMech"), ThingDef.Named("ShipAirlockWrecked")},
				{ThingDef.Named("ShipAirlockArchotech"), ThingDef.Named("ShipAirlockWrecked")},
				{ThingDef.Named("ShipAirlockBeam"), ThingDef.Named("Ship_Beam_Wrecked")}
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
				loadedGraphics = true;
			}
		}

		/// <summary>
		/// Checks if specified RoofDef is properly airtight.
		/// </summary>
		/// <param name="roof"></param>
		/// <returns></returns>
		
		public static void UseCustomSplashScreen()
		{
			((UI_BackgroundMain)UIMenuBackgroundManager.background).overrideBGImage = ResourceBank.Splash;
		}

		public static bool IsRoofDefAirtight(RoofDef roof)
		{
			if (roof == null)
				return false;
			if (roof == ResourceBank.RoofDefOf.RoofShip)
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
			for (int i = 0; i < 420; i++)//Find.World.grid.TilesCount
			{
				if (!Find.World.worldObjects.AnyWorldObjectAt(i) && TileFinder.IsValidTileForNewSettlement(i))
				{
					//Log.Message("Generating orbiting ship at tile " + i);
					return i;
				}
			}
			return -1;
		}
		public static void GeneratePlayerShipMap(IntVec3 size, Map origin)
		{
			WorldObjectOrbitingShip orbiter = (WorldObjectOrbitingShip)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("ShipOrbiting"));
			orbiter.radius = 150;
			orbiter.theta = -3;
			orbiter.SetFaction(Faction.OfPlayer);
			orbiter.Tile = FindWorldTile();
			Find.WorldObjects.Add(orbiter);
			var mapComp = origin.GetComponent<ShipHeatMapComp>();
			mapComp.ShipCombatOriginMap = MapGenerator.GenerateMap(size, orbiter, orbiter.MapGeneratorDef);
			mapComp.ShipCombatOriginMap.fogGrid.ClearAllFog();
		}
		public static void GenerateImpactSite()
		{
			WorldObject impactSite =
				WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("ShipEngineImpactSite"));
			int tile = TileFinder.RandomStartingTile();
			impactSite.Tile = tile;
			Find.WorldObjects.Add(impactSite);
		}
		public static WorldObject GenerateArchotechPillarBSite()
		{
			WorldObject impactSite =
				WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("MoonPillarSite"));
			int tile = TileFinder.RandomStartingTile();
			impactSite.Tile = tile;
			Find.WorldObjects.Add(impactSite);
			return impactSite;
		}
		public static void GenerateArchotechPillarCSite()
		{
			WorldObject impactSite =
				WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("TribalPillarSite"));
			int tile = TileFinder.RandomStartingTile();
			impactSite.Tile = tile;
			Find.WorldObjects.Add(impactSite);
		}
		public static void GenerateArchotechPillarDSite()
		{
			WorldObject impactSite =
				WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("InsectPillarSite"));
			int tile = TileFinder.RandomStartingTile();
			impactSite.Tile = tile;
			Find.WorldObjects.Add(impactSite);
		}
		public static SpaceNavyDef ValidRandomNavy(Faction hostileTo = null, bool needsShips = true, bool bountyHunts = true)
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
			Log.Message("Spawning ship from CR: " + CR + " tradeShip: " + tradeShip + " allowNavyExc: " + allowNavyExc + " randomFleet: " + randomFleet + " minZ: " + minZ + " maxZ: " + maxZ);
			float adjCR = CR * Mathf.Clamp((float)difficultySoS, 0.1f, 5f);
			if (CR < 500) //reduce difficulty in this range - would be better as a timed thing
				adjCR *= 0.8f;
			List<EnemyShipDef> check = new List<EnemyShipDef>();
			if (randomFleet)
			{
				check = ships.Where(def => ValidShipDef(def, 0.8f * adjCR, 1.2f * adjCR, tradeShip, allowNavyExc, randomFleet, minZ, maxZ)).ToList();
				if (check.Any())
					return check.RandomElement();
			}
			Log.Message("fallback 0");
			check = ships.Where(def => ValidShipDef(def, 0.5f * adjCR, 1.5f * adjCR, tradeShip, allowNavyExc, randomFleet, minZ, maxZ)).ToList();
			if (check.Any())
				return check.RandomElement();
			Log.Message("fallback 1");
			check = ships.Where(def => ValidShipDef(def, 0.25f * adjCR, 2f * adjCR, tradeShip, allowNavyExc, randomFleet, minZ, maxZ)).ToList();
			if (check.Any())
				return check.RandomElement();
			//too high or too low adjCR - ignore difficulty
			Log.Warning("SOS2: difficulty set too low/high or no suitable ships found for your CR, using fallback");
			if (CR < 1000)
				check = ships.Where(def => ValidShipDef(def, 0f * CR, 1f * CR, tradeShip, allowNavyExc, randomFleet, minZ, maxZ)).ToList();
			else
				check = ships.Where(def => ValidShipDef(def, 0.5f * CR, 100f * CR, tradeShip, allowNavyExc, randomFleet, minZ, maxZ)).ToList();
			if (check.Any())
				return check.RandomElement();
			//last fallback, not for fleets or navy exclusive
			if (tradeShip)
			{
				Log.Message("trade ship fallback");
				check = ships.Where(def => ValidShipDef(def, 0, 100000f, tradeShip, allowNavyExc, randomFleet, minZ, maxZ)).ToList();
				if (check.Any())
					return check.RandomElement();
				Log.Warning("SOS2: navy has no trade ships, choosing any random.");
				return DefDatabase<EnemyShipDef>.AllDefs.Where(def => ValidShipDef(def, 0f, 100000f, tradeShip, false, randomFleet, minZ, maxZ)).RandomElement();
			}
			else if (!allowNavyExc && !randomFleet)
			{
				Log.Warning("SOS2: found no suitable enemy ship, choosing any random.");
				check = ships.Where(def => ValidShipDef(def, 0, 100000f, tradeShip, allowNavyExc, randomFleet, minZ, maxZ)).ToList();
				if (check.Any())
					return check.RandomElement();
				ships.Where(def => !def.neverAttacks && !def.neverRandom && (allowNavyExc || !def.navyExclusive)).RandomElement();
			}
			return null;
		}
		public static bool ValidShipDef(EnemyShipDef def, float CRmin, float CRmax, bool tradeShip, bool allowNavyExc, bool randomFleet, int minZ = 0, int maxZ = 0)
		{
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
			List<IntVec3> area = new List<IntVec3>();
			List<IntVec3> areaOut;
			cores = new List<Building>();
			List<Building> coresOut;
			if (shipDef.ships.NullOrEmpty())
			{
				GenerateShipDef(shipDef, map, passingShip, fac, lord, out coresOut, out areaOut, shipActive, clearArea, wreckLevel, offsetX, offsetZ, navyDef);
				cores.AddRange(coresOut);
				area.AddRange(areaOut);
			}
            else
			{
				for (int i = 0; i < shipDef.ships.Count; i++)
				{
					Log.Message("Spawning fleet ship nr." + i);
					var genShip = DefDatabase<EnemyShipDef>.GetNamedSilentFail(shipDef.ships[i].ship);
					if (genShip == null)
					{
						Log.Error("Fleet ship not found in database");
						return;
					}
					GenerateShipDef(DefDatabase<EnemyShipDef>.GetNamedSilentFail(shipDef.ships[i].ship), map, passingShip, fac, lord, out coresOut, out areaOut, shipActive, clearArea, wreckLevel, shipDef.ships[i].offsetX, shipDef.ships[i].offsetZ, navyDef);
					cores.AddRange(coresOut);
					area.AddRange(areaOut);
				}
			}
			PostGenerateShipDef(map, clearArea, area);
		}
		public static void GenerateFleet(float CR, Map map, PassingShip passingShip, Faction fac, Lord lord, out List<Building> cores, bool shipActive = true, bool clearArea = false, int wreckLevel = 0, SpaceNavyDef navyDef = null)
		{
			//use player points to spawn ships of the same navy, fit z, random x
			//main + twin, twin, twin + escort, squadron, tradeship + escorts, tradeship + large, tradeship + large + escort
			//60-20-20,50-50,40-40-10-10
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
			List<IntVec3> areaOut;
			cores = new List<Building>();
			List<Building> coresOut;
			bool firstLarger = Rand.Bool;
			bool escorts = Rand.Bool;
			float CRfactor = CR;
			Log.Message("Spawning random fleet from CR: " + CR + " navyDef: " + navyDef + " tradeShip: " + tradeShip + " firstLarger: " + firstLarger + " escorts: " + escorts);
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
						GenerateShipDef(shipDef, map, passingShip, fac, lord, out coresOut, out areaOut, !shipActive, false, wreckLevel, offsetX, offsetZAdj, navyDef);
						cores.AddRange(coresOut);
						area.AddRange(areaOut);
					}
				}
				i++;
			}
			PostGenerateShipDef(map, clearArea, area);
		}
		public static void GenerateShipDef(EnemyShipDef shipDef, Map map, PassingShip passingShip, Faction fac, Lord lord, out List<Building> cores, out List<IntVec3> cellsToFog, bool shipActive = true, bool clearArea = false, int wreckLevel = 0, int offsetX = -1, int offsetZ = -1, SpaceNavyDef navyDef = null)
		{
			cellsToFog = new List<IntVec3>();
			cores = new List<Building>();
			bool unlockedJT = false;
			if (WorldSwitchUtility.PastWorldTracker.Unlocks.Contains("JTDriveToo"))
				unlockedJT = true;
			bool ideoActive = false;
			if (ModsConfig.IdeologyActive && (fac != Faction.OfAncientsHostile || fac != Faction.OfAncients || fac != Faction.OfMechanoids))
				ideoActive = true;
			bool royActive = false;
			if (ModsConfig.RoyaltyActive)
				royActive = true;


			int size = shipDef.sizeX * shipDef.sizeZ;
			List<Building> wreckDestroy = new List<Building>();
			List<Pawn> pawnsOnShip = new List<Pawn>();
			List<ShipShape> partsToGenerate = new List<ShipShape>();
			List<IntVec3> cargoCells = new List<IntVec3>();
			List<IntVec3> hydroCells = new List<IntVec3>();
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
				Building bridge = (Building)ThingMaker.MakeThing(ThingDef.Named(shipDef.core.shapeOrDef));
				bridge.SetFaction(fac);
				GenSpawn.Spawn(bridge, new IntVec3(offset.x + shipDef.core.x, 0, offset.z + shipDef.core.z), map, shipDef.core.rot);
				bridge.TryGetComp<CompPowerTrader>().PowerOn = true;
				cores.Add(bridge);
				((Building_ShipBridge)bridge).ShipName = shipDef.label;
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
					if (shape.shapeOrDef.Equals("PawnSpawnerGeneric"))
					{
						PawnGenerationRequest req = new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed(shape.stuff), fac);
						Pawn pawn = PawnGenerator.GeneratePawn(req);
						if (lord != null)
							lord.AddPawn(pawn);
						GenSpawn.Spawn(pawn, new IntVec3(offset.x + shape.x, 0, offset.z + shape.z), map);
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
						if (lord != null)
							lord.AddPawn(pawn);
						GenSpawn.Spawn(pawn, new IntVec3(offset.x + shape.x, 0, offset.z + shape.z), map);
						pawnsOnShip.Add(pawn);
					}
					else if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
					{
						bool isBuilding = false;
						bool isWrecked = false;
						Thing thing = null;
						ThingDef def = ThingDef.Named(shape.shapeOrDef);
						//def replacers
						if (def.IsBuildingArtificial)
						{
							isBuilding = true;
							if (!royActive && def.Equals(ThingDefOf.Throne))
								def = DefDatabase<ThingDef>.GetNamed("Armchair");
							else if (wreckLevel > 2 && wreckDictionary.ContainsKey(def)) //replace ship walls/floor
							{
								def = wreckDictionary[def];
								isWrecked = true;
							}
							else if (!unlockedJT && def.HasComp(typeof(CompEngineTrail))) //replace JT drives if not unlocked via story
							{
								if (def.defName.Equals("Ship_Engine_Interplanetary"))
									def = DefDatabase<ThingDef>.GetNamed("Ship_Engine");
								else if (def.defName.Equals("Ship_Engine_Interplanetary_Large"))
									def = DefDatabase<ThingDef>.GetNamed("Ship_Engine_Large");
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

						var compQuality = thing.TryGetComp<CompQuality>();
						if (compQuality != null)
						{
							compQuality.SetQuality(QualityUtility.GenerateQualityBaseGen(), ArtGenerationContext.Outsider);
						}
						if (thing.TryGetComp<CompColorable>() != null)
                        {
							if (rePaint && isBuilding) //color unpainted navy ships
							{
								if (thing.TryGetComp<CompSoShipPart>()?.Props.isHull ?? false)
									thing.SetColor(navyDef.colorPrimary);
								else if (def.defName.StartsWith("Ship_Corner"))
									thing.SetColor(navyDef.colorSecondary);
							}
							if (shape.color != Color.clear)
								thing.SetColor(shape.color);
						}
						else if (thing.def.stackLimit > 1)
						{
							thing.stackCount = Math.Min(Rand.RangeInclusive(5, 30), thing.def.stackLimit);
							if (thing.stackCount * thing.MarketValue > 500)
								thing.stackCount = (int)Mathf.Max(500 / thing.MarketValue, 1);
						}
						//spawn thing
						GenSpawn.Spawn(thing, new IntVec3(offset.x + shape.x, 0, offset.z + shape.z), map, shape.rot);
						//post spawn
						if (isBuilding)
						{
							if (wreckLevel > 1 && !isWrecked)
								wreckDestroy.Add(thing as Building);
							if (thing.def.CanHaveFaction)
							{
								if (thing.TryGetComp<CompSoShipPart>()?.Props.isPlating ?? false)
								{
									cellsToFog.Add(thing.Position);
									continue;
								}
								else if (!(thing.def == ResourceBank.ThingDefOf.ShipHullTileWrecked || thing.def == ResourceBank.ThingDefOf.ShipAirlockWrecked || thing.def.thingClass == typeof(Building_ArchotechPillar)))
									thing.SetFaction(fac);
							}
							Building b = thing as Building;
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
									if (reactorComp != null && shipActive)
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
							else if (b is Building_ShipTurret turret)
							{
								turret.burstCooldownTicksLeft = 300;
								if (b is Building_ShipTurretTorpedo torp)
								{
									for (int i = 0; i < torp.torpComp.Props.maxTorpedoes; i++)
									{
										if (size > 10000 && Rand.Chance(0.05f))
											torp.torpComp.LoadShell(ThingDef.Named("ShipTorpedo_Antimatter"), 1);
										else if (size > 5000 && Rand.Chance(0.2f))
											torp.torpComp.LoadShell(ThingDef.Named("ShipTorpedo_EMP"), 1);
										else
											torp.torpComp.LoadShell(ThingDef.Named("ShipTorpedo_HighExplosive"), 1);
									}
								}
							}
							else if (b is Building_PlantGrower)
                            {
								hydroCells.AddRange(b.OccupiedRect().Cells);
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
						else if (thing.def.CanHaveFaction)
						{
							thing.SetFaction(fac);
						}
					}
					else if (DefDatabase<TerrainDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
					{
						TerrainDef terrain = DefDatabase<TerrainDef>.GetNamed(shape.shapeOrDef);
						IntVec3 pos = new IntVec3(shape.x, 0, shape.z);
						if (shipDef.saveSysVer == 2)
							pos = new IntVec3(offset.x + shape.x, 0, offset.z + shape.z);
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
					EnemyShipPartDef def = DefDatabase<EnemyShipPartDef>.GetNamed(shape.shapeOrDef);
					if (randomTurretPoints >= def.randomTurretPoints)
						randomTurretPoints -= def.randomTurretPoints;
					else
						def = DefDatabase<EnemyShipPartDef>.GetNamed("Cargo");

					if (def.defName.Equals("CasketFilled"))
					{
						Thing thing = ThingMaker.MakeThing(ThingDefOf.CryptosleepCasket);
						thing.SetFaction(fac);
						Pawn sleeper = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Slave, Faction.OfAncients, forceGenerateNewPawn: true, certainlyBeenInCryptosleep: true));
						((Building_CryptosleepCasket)thing).TryAcceptThing(sleeper);
						GenSpawn.Spawn(thing, new IntVec3(offset.x + shape.x, 0, offset.z + shape.z), map, shape.rot);
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
							thing.SetFaction(fac);
						GenSpawn.Spawn(thing, new IntVec3(offset.x + shape.x, 0, offset.z + shape.z), map);
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
								thing.SetFaction(fac);
						}
						if (thing is Building_ShipTurret turret)
							turret.burstCooldownTicksLeft = 300;
						if (thing.TryGetComp<CompColorable>() != null)
							thing.TryGetComp<CompColorable>().SetColor(shape.color);
						GenSpawn.Spawn(thing, new IntVec3(offset.x + shape.x, 0, offset.z + shape.z), map, shape.rot);
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
				if (passingShip is TradeShip)
				{
					loot = ((TradeShip)passingShip).GetDirectlyHeldThings().ToList();
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
						actualTotalValue -= random.MarketValue;
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
			//hydro
			if (hydroCells.Any() && wreckLevel < 3)
            {
				foreach (IntVec3 pos in hydroCells)
				{
					Plant plant = ThingMaker.MakeThing(randomPlants.Where(p => p.plant.sowTags.Contains("Hydroponic")).RandomElement()) as Plant;
					if (plant != null)
					{
						plant.Growth = Rand.Range(0.5f, 1f);
						plant.Position = pos;
						plant.SpawnSetup(map, false);
					}
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
                    MakeLines(shipDef, map, wreckLevel, offset);
					madeLines = true;
				}
				if ((wreckLevel == 2 || wreckLevel == 3) && size > 8000)
				{
					MakeLines(shipDef, map, wreckLevel, offset);
				}
				//holes, surounded by wreck
				int adj = 1 + (size / 1000);
				//Log.Message("holenum: "+adj);
				holeNum = Rand.RangeInclusive(adj, adj - 1 + (wreckLevel * 2));
				if (size > 4000 && wreckLevel > 1 && !madeLines)
					holeNum += Rand.RangeInclusive(adj, adj - 1 + (wreckLevel * 2));
				CellRect rect = new CellRect(offset.x - 1, offset.z - 1, shipDef.sizeX + 1, shipDef.sizeZ + 1);
				MakeHoles(FindCellOnOuterHull(map, holeNum, rect), map, wreckLevel, 1.9f, 4.9f);
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
				if ((wreckLevel == 2 && Rand.Chance(0.7f)) || (wreckLevel ==3 && Rand.Chance(0.4f)))
				{
					SpaceNavyDef navy = ValidRandomNavy(Faction.OfPlayer, false);
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
					var check = (MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def.defName.Equals("ShipOrbiting")).FirstOrDefault();
					if (check != null)
					{
						parms.target = check.Map;
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
		}
		public static void PostGenerateShipDef(Map map, bool clearArea, List<IntVec3> shipArea)
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
			/*if (validRooms.Any())
			{
				Log.Message("set temp in rooms: " + validRooms.Count);
				foreach (Room r in validRooms.Where(r => r != null))
				{
					if (r.OpenRoofCount < 2)
					{
						r.Temperature = 21;
					}
				}
			}*/
			foreach (Room r in map.regionGrid.allRooms)
				r.Temperature = 21;
			map.mapDrawer.MapMeshDirty(map.Center, MapMeshFlag.Things | MapMeshFlag.FogOfWar);
			if (Current.ProgramState == ProgramState.Playing)
				map.mapDrawer.RegenerateEverythingNow();
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
		public static void MakeLines(EnemyShipDef shipDef, Map map, int wreckLevel, IntVec3 offset)
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
			MakeHoles(detVecs, map, wreckLevel, 2.9f, 3.9f);
		}
		public static void MakeHoles(List<IntVec3> targets, Map map, int wreckLevel, float minSize, float maxSize)
		{
			if (targets.NullOrEmpty())
				return;
			List<Thing> toDestroy = new List<Thing>();
			List<Building> toReplace = new List<Building>();
			foreach (IntVec3 v in targets)
			{
				float exploRadius = Rand.Range(minSize, maxSize);
				foreach (IntVec3 vec in GenRadial.RadialCellsAround(v, exploRadius, true))
				{
					map.roofGrid.SetRoof(vec, null);
					foreach (Thing t in vec.GetThingList(map).Where(t => !t.Destroyed))
					{
						toDestroy.Add(t);
					}
				}
				foreach (IntVec3 vec in GenRadial.RadialCellsAround(v, exploRadius, exploRadius + 1))
				{
					foreach (Thing t in vec.GetThingList(map).Where(t => t is Building && !t.Destroyed))
					{
						if (wreckDictionary.ContainsKey(t.def))
						{
							toReplace.Add(t as Building);
						}
					}
				}
			}
			foreach (Building b in toReplace)
			{
				IntVec3 v = b.Position;
				Thing thing = ThingMaker.MakeThing(wreckDictionary[b.def]);
				if (!b.Destroyed)
					b.Destroy();
				GenSpawn.Spawn(thing, v, map);
			}
			foreach (Thing t in toDestroy.Where(t => !t.Destroyed))
			{
				if (t is Building b)
				{
					var refuelComp = b.TryGetComp<CompRefuelable>();
					if (refuelComp != null)
						refuelComp.ConsumeFuel(refuelComp.Fuel);
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
					wall.SetFaction(fac);
					GenSpawn.Spawn(wall, vec, map);
				}
			}
			foreach (IntVec3 vec in interior)
			{
				Thing floor = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipHullTile);
				if (fac == Faction.OfPlayer)
					floor.SetFaction(fac);
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
		public static List<Building> FindBuildingsAttached(Building root, bool includeRock = false)
		{
			if (root == null || root.Destroyed)
				return new List<Building>();

			var map = root.Map;
			var containedBuildings = new HashSet<Building>();
			var cellsTodo = new HashSet<IntVec3>();
			var cellsDone = new HashSet<IntVec3>();
			cellsTodo.AddRange(GenAdj.CellsOccupiedBy(root));
			cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(root));
			while (cellsTodo.Count > 0)
			{
				var current = cellsTodo.First();
				cellsTodo.Remove(current);
				cellsDone.Add(current);
				var containedThings = current.GetThingList(map);
				if (containedThings.Any(t => t is Building b && (b.def.building.shipPart || (includeRock && b.def.building.isNaturalRock))))
				{
					foreach (var t in containedThings)
					{
						if (t is Building b)
						{
							containedBuildings.Add(b);
							if (b.def.building.shipPart)
								cellsTodo.AddRange(GenAdj.CellsOccupiedBy(b).Concat(GenAdj.CellsAdjacentCardinal(b)).Where(v => !cellsDone.Contains(v)));
						}
					}
				}
			}
			return containedBuildings.ToList();
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
				if (current.GetThingList(map).Any(t => t is Building b && (b.def.building.shipPart || (includeRock && b.def.building.isNaturalRock))))
				{
					cellsFound.Add(current);
					cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(current, Rot4.North, new IntVec2(1, 1)).Where(v => !cellsDone.Contains(v)));
				}
			}
			return cellsFound;
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
		public static Sketch GenerateShipSketch(HashSet<IntVec3> positions, Map map, IntVec3 lowestCorner, byte rotb = 0)
		{
			Sketch sketch = new Sketch();
			IntVec3 rot = new IntVec3(0, 0, 0);
			foreach (IntVec3 pos in positions)
			{
				if (rotb == 1)
				{
					rot.x = map.Size.x - pos.z;
					rot.z = pos.x;
					sketch.AddThing(ThingDef.Named("Ship_FakeBeam"), rot - lowestCorner, Rot4.North);
				}
				else if (rotb == 2)
				{
					rot.x = map.Size.x - pos.x;
					rot.z = map.Size.z - pos.z;
					sketch.AddThing(ThingDef.Named("Ship_FakeBeam"), rot - lowestCorner, Rot4.North);
				}
				else
					sketch.AddThing(ThingDef.Named("Ship_FakeBeam"), pos - lowestCorner, Rot4.North);
			}
			return sketch;
		}
		public static void MoveShipSketch(Building b, Map targetMap, byte rotb = 0, bool salvage = false, int bMax = 0, bool includeRock = false)
		{
			if (b == null)
				return;
			List<Building> cachedParts;
			if (b is Building_ShipBridge bridge)
				cachedParts = bridge.cachedShipParts;
			else
				cachedParts = FindBuildingsAttached(b, includeRock);

			IntVec3 lowestCorner = new IntVec3(int.MaxValue, 0, int.MaxValue);
			HashSet<IntVec3> positions = new HashSet<IntVec3>();
			int bCount = 0;
			foreach (Building building in cachedParts)
			{
				if (salvage && building is Building_ShipBridge && !building.Destroyed)
				{
					Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipSalvageBridge"), MessageTypeDefOf.NeutralEvent);
					cachedParts.Clear();
					positions.Clear();
					return;
				}
				bCount++;
				if (b.Position.x < lowestCorner.x)
					lowestCorner.x = b.Position.x;
				if (b.Position.z < lowestCorner.z)
					lowestCorner.z = b.Position.z;
				foreach (IntVec3 pos in GenAdj.CellsOccupiedBy(building))
					positions.Add(pos);
			}
			if (rotb == 1)
			{
				lowestCorner.x = b.Map.Size.z - lowestCorner.z;
				lowestCorner.z = lowestCorner.x;
			}
			else if (rotb == 2)
			{
				lowestCorner.x = b.Map.Size.x - lowestCorner.x;
				lowestCorner.z = b.Map.Size.z - lowestCorner.z;
			}
			float bCountF = bCount * 2.5f;
			if (salvage && bCountF > bMax)
			{
				Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipSalvageCount", (int)bCountF, bMax), MessageTypeDefOf.NeutralEvent);
				cachedParts.Clear();
				positions.Clear();
				return;
			}
			Sketch shipSketch = GenerateShipSketch(positions, targetMap, lowestCorner, rotb);
			MinifiedThingShipMove fakeMover = (MinifiedThingShipMove)new ShipMoveBlueprint(shipSketch).TryMakeMinified();
			fakeMover.shipRoot = b;
			fakeMover.includeRock = includeRock;
			fakeMover.shipRotNum = rotb;
			fakeMover.bottomLeftPos = lowestCorner;
			shipOriginMap = b.Map;
			fakeMover.targetMap = targetMap;
			fakeMover.Position = b.Position;
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
		public static void MoveShip(Building core, Map targetMap, IntVec3 adjustment, Faction fac = null, byte rotNum = 0, bool includeRock = false)
		{
			bool devMode = false;
			var watch = new TimeHelper();
			if (Prefs.DevMode)
            {
				devMode = true;
			}
			List<Thing> toSave = new List<Thing>();
			List<Thing> toDestroy = new List<Thing>();
			List<CompPower> toRePower = new List<CompPower>();
			List<Zone> zonesToCopy = new List<Zone>();
			List<Room> roomsToTemp = new List<Room>();
			List<Tuple<IntVec3, float>> posTemp = new List<Tuple<IntVec3, float>>();
			List<Tuple<IntVec3, TerrainDef>> terrainToCopy = new List<Tuple<IntVec3, TerrainDef>>();
			List<Tuple<IntVec3, RoofDef>> roofToCopy = new List<Tuple<IntVec3, RoofDef>>();
			HashSet<IntVec3> targetArea = new HashSet<IntVec3>();
			// source area of the ship.
			HashSet<IntVec3> sourceArea = FindAreaAttached(core, includeRock);

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

			shipOriginMap = null;
			Map sourceMap = core.Map;
			if (targetMap == null)
				targetMap = core.Map;
			bool targetMapIsSpace = targetMap.IsSpace();
			bool sourceMapIsSpace = sourceMap.IsSpace();
			bool playerMove = core.Faction == Faction.OfPlayer;

			foreach (IntVec3 pos in sourceArea)
			{
				IntVec3 adjustedPos = Transform(pos);
				//store room temps
				Room room = pos.GetRoom(sourceMap);
				if (room != null && !roomsToTemp.Contains(room) && !ExposedToOutside(room))
				{
					roomsToTemp.Add(room);
					float temp = room.Temperature;
					posTemp.Add(new Tuple<IntVec3, float>(adjustedPos, temp));
				}
				//clear LZ
				targetArea.Add(adjustedPos);
				foreach (Thing t in adjustedPos.GetThingList(targetMap))
				{
					if (!toDestroy.Contains(t))
						toDestroy.Add(t);
				}
				if (!targetMapIsSpace)
					targetMap.snowGrid.SetDepth(adjustedPos, 0f);
				//add all things from area
				foreach (Thing t in pos.GetThingList(sourceMap))
				{
					if (t is Building b)
                    {
						if (b is Building_SteamGeyser)
							continue;
						if (b is Building_ShipAirlock a && a.docked)
						{
							a.UnDock();
						}
						var engineComp = b.TryGetComp<CompEngineTrail>();
						var powerComp = b.TryGetComp<CompPower>();
						if (engineComp != null)
							engineComp.Off();
						if (powerComp != null)
							toRePower.Add(powerComp);
					}
					else if (t is Pawn p && !sourceMapIsSpace && p.Faction != Faction.OfPlayer)
                    {
						Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipMoveFailPawns"), null, MessageTypeDefOf.NegativeEvent);
						return;
                    }
					if (!toSave.Contains(t))
					{
						toSave.Add(t);
					}
				}

				if (sourceMap.zoneManager.ZoneAt(pos) != null && !zonesToCopy.Contains(sourceMap.zoneManager.ZoneAt(pos)))
				{
					zonesToCopy.Add(sourceMap.zoneManager.ZoneAt(pos));
				}

				var sourceTerrain = sourceMap.terrainGrid.TerrainAt(pos);
				if (sourceTerrain.layerable && !IsHull(sourceTerrain))
				{
					terrainToCopy.Add(new Tuple<IntVec3, TerrainDef>(pos, sourceTerrain));
					sourceMap.terrainGrid.RemoveTopLayer(pos, false);
				}
				else if (includeRock && IsRock(sourceTerrain))
				{
					terrainToCopy.Add(new Tuple<IntVec3, TerrainDef>(pos, sourceTerrain));
					sourceMap.terrainGrid.SetTerrain(pos, ResourceBank.TerrainDefOf.EmptySpace);
				}

				var sourceRoof = sourceMap.roofGrid.RoofAt(pos);
				if (IsRoofDefAirtight(sourceRoof))
				{
					roofToCopy.Add(new Tuple<IntVec3, RoofDef>(pos, sourceRoof));
				}
				sourceMap.roofGrid.SetRoof(pos, null);
				if (playerMove)
				{
					sourceMap.areaManager.Home[pos] = false;
					targetMap.areaManager.Home[adjustedPos] = true;
				}
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

			//takeoff - draw fuel
			if (!sourceMapIsSpace)
			{
				float fuelNeeded = 0f;
				float fuelStored = 0f;
				List<CompRefuelable> refuelComps = new List<CompRefuelable>();

				foreach (Thing saveThing in toSave)
				{
					if (saveThing is Building)
					{
						if (saveThing.TryGetComp<CompSoShipPart>()?.Props.isPlating ?? false)
							fuelNeeded += 1f;
						else
						{
							var engineComp = saveThing.TryGetComp<CompEngineTrail>();
							if (engineComp != null && engineComp.Props.takeOff)
							{
								if (saveThing.Rotation.AsByte == 0)
									fireExplosions.Add(saveThing.Position + new IntVec3(0, 0, -3));
								else if (saveThing.Rotation.AsByte == 1)
									fireExplosions.Add(saveThing.Position + new IntVec3(-3, 0, 0));
								else if (saveThing.Rotation.AsByte == 2)
									fireExplosions.Add(saveThing.Position + new IntVec3(0, 0, 3));
								else
									fireExplosions.Add(saveThing.Position + new IntVec3(3, 0, 0));
								refuelComps.Add(engineComp.refuelComp);
								fuelStored += engineComp.refuelComp.Fuel;
								if (engineComp.refuelComp.Props.fuelFilter.AllowedThingDefs.Contains(ThingDef.Named("ShuttleFuelPods")))
								{
									fuelStored += engineComp.refuelComp.Fuel;
									if (ModsConfig.BiotechActive)
									{
										foreach (IntVec3 v in engineComp.rectToKill)
											v.Pollute(sourceMap, true);
									}
								}
							}
							fuelNeeded += (saveThing.def.size.x * saveThing.def.size.z) * 3f;
						}
					}
				}
				foreach (CompRefuelable engine in refuelComps)
				{
					engine.ConsumeFuel(fuelNeeded * engine.Fuel / fuelStored);
				}
				if (devMode)
					watch.Record("takeoffEngineEffects");

			}

			//move things
			AirlockBugFlag = true;
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
							if (!spawnThing.Destroyed)
							{
								spawnThing.SpawnSetup(targetMap, false);
							}
						}
						catch (Exception e)
						{
							Log.Warning(e.Message + "\n" + e.StackTrace);
						}

						//post move
						if (fac != null && spawnThing is Building && spawnThing.def.CanHaveFaction)
							spawnThing.SetFaction(fac);
						if (spawnThing is Pawn pawn)
						{
							Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.Remove(pawn.thingIDNumber);
						}
					}
					catch (Exception e)
					{
						//Log.Warning(e.Message+"\n"+e.StackTrace);
						Log.Error(e.Message);
					}
				}
			}
			if (devMode)
				watch.Record("moveThings");
			AirlockBugFlag = false;

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
				typeof(ZoneManager).GetMethod("RebuildZoneGrid", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(targetMap.zoneManager, new object[0]);
				typeof(ZoneManager).GetMethod("RebuildZoneGrid", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(sourceMap.zoneManager, new object[0]);

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
			foreach (Tuple<IntVec3, TerrainDef> tup in terrainToCopy)
			{
				var targetTile = targetMap.terrainGrid.TerrainAt(tup.Item1);
				if (!targetTile.layerable || IsHull(targetTile))
				{
					var targetPos = Transform(tup.Item1);
					targetMap.terrainGrid.SetTerrain(targetPos, tup.Item2);
				}
			}
			if (includeRock)
			{
				foreach (IntVec3 pos in sourceArea)
				{
					sourceMap.terrainGrid.SetTerrain(pos, ResourceBank.TerrainDefOf.EmptySpace);
				}
			}
			if (devMode)
				watch.Record("moveTerrain");

			//move roofs
			foreach (Tuple<IntVec3, RoofDef> tup in roofToCopy)
			{
				var targetPos = Transform(tup.Item1);
				targetMap.roofGrid.SetRoof(targetPos, tup.Item2);
			}
			if (devMode)
				watch.Record("moveRoof");

			//restore temp in ship
			foreach (Tuple<IntVec3, float> t in posTemp)
			{
				Room room = t.Item1.GetRoom(targetMap);
				room.Temperature = t.Item2;
			}

			//landing - remove space map
			if (sourceMap != targetMap && !sourceMap.spawnedThings.Any((Thing x) => x is Pawn && !x.Destroyed))
			{
				WorldObject oldParent = sourceMap.Parent;
				Current.Game.DeinitAndRemoveMap(sourceMap);
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
				foreach (CompPower powerComp in toRePower)
				{
					powerComp.ResetPowerVars();
				}
			}
			if (toRePower.Any())
				targetMap.powerNetManager.UpdatePowerNetsAndConnections_First();

			//heat
			targetMap.GetComponent<ShipHeatMapComp>().heatGridDirty = true;

			if (devMode)
			{
				watch.Record("finalize");
				Log.Message("Timing report:\n" + watch.MakeReport());
				Log.Message("Moved ship with building " + core);
			}
			/*Things = 1,
			FogOfWar = 2,
			Buildings = 4,
			GroundGlow = 8,
			Terrain = 16,
			Roofs = 32,
			Snow = 64,
			Zone = 128,
			PowerGrid = 256,
			BuildingsDamage = 512*/
			//targetMap.mapDrawer.RegenerateEverythingNow();
			//sourceMap.mapDrawer.RegenerateEverythingNow();
			//foreach (IntVec3 pos in posToClear)
			//sourceMap.mapDrawer.MapMeshDirty(pos, MapMeshFlag.PowerGrid);
			//rewire - call next tick
			/*foreach (Thing powerThing in targetMap.listerThings.AllThings)
			{
				CompPower powerComp = powerThing.TryGetComp<CompPower>();
				if (powerComp != null)
				{
					typeof(CompPower).GetMethod("TryManualReconnect", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(powerComp, new object[0]);
					//Traverse.Create<CompPower>().Method("TryManualReconnect", powerComp);
					//Traverse.Create(powerComp).Method("TryManualReconnect");
					//powerComp.ResetPowerVars();
				}
			}*/
			/*if (sourceMap.IsSpace() && !sourceMap.IsSpace() || (!sourceMap.IsSpace() && sourceMap.IsSpace())
			{
				foreach (Building powerThing in shipParts)
				{
					CompPower powerComp = powerThing.TryGetComp<CompPower>();
					if (powerComp is CompPowerTrader && powerComp.Props.transmitsPower == false)
					{
						CompPower compPower = PowerConnectionMaker.BestTransmitterForConnector(powerThing.Position, powerThing.Map);
						powerComp.ConnectToTransmitter(compPower, false);
					}
					powerThing.Map.mapDrawer.MapMeshDirty(powerThing.Position, MapMeshFlag.PowerGrid);
					powerThing.Map.mapDrawer.MapMeshDirty(powerThing.Position, MapMeshFlag.Things);
				}
			}*/
		}
		public static void SaveShip(Building core, string file)
		{
			List<Thing> toSave = new List<Thing>();
			List<Zone> zones = new List<Zone>();
			HashSet<IntVec3> area = FindAreaAttached(core, false);

			HashSet<Ideo> ideosAboardShip = new HashSet<Ideo>();
			HashSet<CustomXenotype> xenosAboardShip = new HashSet<CustomXenotype>();
			HashSet<Pawn> pawnsAboardShip = new HashSet<Pawn>();
			List<IntVec3> roofPos = new List<IntVec3>();
			List<RoofDef> roofDefs = new List<RoofDef>();
			List<IntVec3> terrainPos = new List<IntVec3>();
			List<TerrainDef> terrainDefs = new List<TerrainDef>();
			Map map = core.Map;

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
							a.UnDock();
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
				if (pawn.genes.CustomXenotype != null)
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

			RemoveShip(area.ToList(), map, true);
		}
		public static void RemoveShip(List<IntVec3> area, Map map, bool planetTravel)
		{
			AirlockBugFlag = true;
			List<Thing> things = new List<Thing>();
			List<Zone> zones = new List<Zone>();
			foreach (IntVec3 pos in area)
			{
				map.roofGrid.SetRoof(pos, null);
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
			}
			AirlockBugFlag = false;

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
				typeof(ZoneManager).GetMethod("RebuildZoneGrid", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(map.zoneManager, new object[0]);
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
		public static bool IsHologram(Pawn pawn)
		{
			return pawn.health.hediffSet.GetFirstHediff<HediffPawnIsHologram>() != null;
		}
		public static bool ExposedToOutside(Room room)
		{
			return room == null || room.OpenRoofCount > 0 || room.TouchesMapEdge;
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
			if (Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.TryGetValue(pawn.thingIDNumber, out byte eva))
				return eva;
			byte result = EVAlevelSlow(pawn);
			//Log.Message("EVA slow lvl: " + result + " on pawn " + pawn.Name);
			Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache[pawn.thingIDNumber] = result;
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

		public static bool CompatibleWithShipLoad(ScenPart item)
		{
			return !(item is ScenPart_PlayerFaction || item is ScenPart_PlayerPawnsArriveMethod || item is ScenPart_ScatterThings || item is ScenPart_Naked || item is ScenPart_NoPossessions || item is ScenPart_GameStartDialog || item is ScenPart_AfterlifeVault || item is ScenPart_SetNeedLevel || item is ScenPart_StartingAnimal || item is ScenPart_StartingMech || item is ScenPart_StartingResearch || item is ScenPart_StartingThing_Defined || item is ScenPart_StartInSpace || item is ScenPart_ThingCount || item is ScenPart_ConfigPage_ConfigureStartingPawnsBase);
		}
	}
	//harmony patches
	//skyfaller related patches in ShuttleMod

	//GUI
	[HarmonyPatch(typeof(ColonistBar), "ColonistBarOnGUI")]
	public static class ShipCombatOnGUI
	{
		[HarmonyPostfix]
		public static void DrawShipRange(ColonistBar __instance)
		{
			Map mapPlayer = Find.Maps.Where(m => m.GetComponent<ShipHeatMapComp>().InCombat && !m.GetComponent<ShipHeatMapComp>().ShipCombatMaster).FirstOrDefault();
			if (mapPlayer != null)
			{
				var playerShipComp = mapPlayer.GetComponent<ShipHeatMapComp>();
				var enemyShipComp = mapPlayer.GetComponent<ShipHeatMapComp>().MasterMapComp;
				if (enemyShipComp == null || playerShipComp.MapRootList.Count == 0 || playerShipComp.MapRootList[0] == null || !playerShipComp.MapRootList[0].Spawned)
					return;
				if (!playerShipComp.InCombat && playerShipComp.IsGraveyard)
				{
					Map m = playerShipComp.ShipGraveyard;
					playerShipComp = m.GetComponent<ShipHeatMapComp>();
				}
				float screenHalf = (float)UI.screenWidth / 2 + SaveOurShip2.ModSettings_SoS.offsetUIx;

				//player heat & energy bars
				float baseY = __instance.Size.y + 40 + SaveOurShip2.ModSettings_SoS.offsetUIy;
				for (int i = 0; i < playerShipComp.MapRootList.Count; i++)
				{
					try //td rem this once this is 100% safe
					{
						var bridge = (Building_ShipBridge)playerShipComp.MapRootList[i];
						baseY += 45;
						string str = bridge.ShipName;
						int strSize = 0;
						if (playerShipComp.MapRootList.Count > 1)
						{
							strSize = 5 + str.Length * 8;
						}
						Rect rect2 = new Rect(screenHalf - 630 - strSize, baseY - 40, 395 + strSize, 35);
						Verse.Widgets.DrawMenuSection(rect2);
						if (playerShipComp.MapRootList.Count > 1)
							Widgets.Label(rect2.ContractedBy(7), str);

						PowerNet net = bridge.powerComp.PowerNet;
						float capacity = 0;
						foreach (CompPowerBattery bat in net.batteryComps)
							capacity += bat.Props.storedEnergyMax;
						Rect rect3 = new Rect(screenHalf - 630, baseY - 40, 200, 35);
						Widgets.FillableBar(rect3.ContractedBy(6), net.CurrentStoredEnergy() / capacity,
							ResourceBank.PowerTex);
						Text.Font = GameFont.Small;
						rect3.y += 7;
						rect3.x = screenHalf - 615;
						rect3.height = Text.LineHeight;
						if (capacity > 0)
							Widgets.Label(rect3, "Energy: " + Mathf.Round(net.CurrentStoredEnergy()) + " / " + capacity);
						else
							Widgets.Label(rect3, "<color=red>Energy: N/A</color>");

						ShipHeatNet net2 = bridge.heatComp.myNet;
						if (net2 != null)
						{
							Rect rect4 = new Rect(screenHalf - 435, baseY - 40, 200, 35);
							Widgets.FillableBar(rect4.ContractedBy(6), bridge.heatComp.RatioInNetwork(),
								ResourceBank.HeatTex);
							rect4.y += 7;
							rect4.x = screenHalf - 420;
							rect4.height = Text.LineHeight;
							if (net2.StorageCapacity > 0)
								Widgets.Label(rect4, "Heat: " + Mathf.Round(net2.StorageUsed) + " / " + net2.StorageCapacity);
							else
								Widgets.Label(rect4, "<color=red>Heat: N/A</color>");
						}
					}
					catch (Exception e)
					{
						Log.Warning("Ship UI failed on ship: " + i + "\n" + e);
					}
				}
				//enemy heat & energy bars
				baseY = __instance.Size.y + 40 + SaveOurShip2.ModSettings_SoS.offsetUIy;
				for (int i = 0; i < enemyShipComp.MapRootList.Count; i++)
				{
					try //td rem this once this is 100% safe
					{
						var bridge = (Building_ShipBridge)enemyShipComp.MapRootList[i];
						baseY += 45;
						string str = bridge.ShipName;
						Rect rect2 = new Rect(screenHalf + 235, baseY - 40, 395, 35);
						Verse.Widgets.DrawMenuSection(rect2);

						ShipHeatNet net2 = bridge.heatComp.myNet;
						if (net2 != null)
						{
							Rect rect4 = new Rect(screenHalf + 235, baseY - 40, 200, 35);
							Widgets.FillableBar(rect4.ContractedBy(6), bridge.heatComp.RatioInNetwork(),
								ResourceBank.HeatTex);
							rect4.y += 7;
							rect4.x = screenHalf + 255;
							rect4.height = Text.LineHeight;
							if (net2.StorageCapacity > 0)
								Widgets.Label(rect4, "Heat: " + Mathf.Round(net2.StorageUsed) + " / " + net2.StorageCapacity);
							else
								Widgets.Label(rect4, "<color=red>Energy: N/A</color>");
						}

						PowerNet net = bridge.powerComp.PowerNet;
						float capacity = 0;
						foreach (CompPowerBattery bat in net.batteryComps)
							capacity += bat.Props.storedEnergyMax;
						Rect rect3 = new Rect(screenHalf + 430, baseY - 40, 200, 35);
						Widgets.FillableBar(rect3.ContractedBy(6), net.CurrentStoredEnergy() / capacity,
							ResourceBank.PowerTex);
						Text.Font = GameFont.Small;
						rect3.y += 7;
						rect3.x = screenHalf + 445;
						rect3.height = Text.LineHeight;
						if (capacity > 0)
							Widgets.Label(rect3, "Energy: " + Mathf.Round(net.CurrentStoredEnergy()) + " / " + capacity);
						else
							Widgets.Label(rect3, "<color=red>Heat: N/A</color>");
					}
					catch (Exception e)
					{
						Log.Warning("Ship UI failed on ship: " + i + "\n" + e);
					}
				}

				//range bar
				baseY = __instance.Size.y + 85 + SaveOurShip2.ModSettings_SoS.offsetUIy;
				Rect rect = new Rect(screenHalf - 225, baseY - 40, 450, 50);
				Verse.Widgets.DrawMenuSection(rect);
				Verse.Widgets.DrawTexturePart(new Rect(screenHalf - 200, baseY - 38, 400, 46),
					new Rect(0, 0, 1, 1), (Texture2D)ResourceBank.ruler.MatSingle.mainTexture);
				switch (playerShipComp.Heading)
				{
					case -1:
						Verse.Widgets.DrawTexturePart(new Rect(screenHalf - 223, baseY - 28, 36, 36),
							new Rect(0, 0, 1, 1), (Texture2D)ResourceBank.shipOne.MatSingle.mainTexture);
						break;
					case 1:
						Verse.Widgets.DrawTexturePart(new Rect(screenHalf - 235, baseY - 28, 36, 36),
							new Rect(0, 0, -1, 1), (Texture2D)ResourceBank.shipOne.MatSingle.mainTexture);
						break;
					default:
						Verse.Widgets.DrawTexturePart(new Rect(screenHalf - 235, baseY - 28, 36, 36),
							new Rect(0, 0, -1, 1), (Texture2D)ResourceBank.shipZero.MatSingle.mainTexture);
						break;
				}
				switch (enemyShipComp.Heading)
				{
					case -1:
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 216 + enemyShipComp.Range, baseY - 28, 36, 36),
							new Rect(0, 0, -1, 1), (Texture2D)ResourceBank.shipOneEnemy.MatSingle.mainTexture);
						break;
					case 1:
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 200 + enemyShipComp.Range, baseY - 28, 36, 36),
							new Rect(0, 0, 1, 1), (Texture2D)ResourceBank.shipOneEnemy.MatSingle.mainTexture);
						break;
					default:
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 200 + enemyShipComp.Range, baseY - 28, 36, 36),
							new Rect(0, 0, 1, 1), (Texture2D)ResourceBank.shipZeroEnemy.MatSingle.mainTexture);
						break;
				}
				foreach (ShipCombatProjectile proj in playerShipComp.Projectiles)
				{
					if (proj.turret != null)
					{
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 210 + proj.range, baseY - 12, 12, 12),
							new Rect(0, 0, 1, 1), (Texture2D)ResourceBank.projectile.MatSingle.mainTexture);
					} 
				}
				foreach (ShipCombatProjectile proj in enemyShipComp.Projectiles)
				{
					if (proj.turret != null)
					{
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 210 - proj.range + enemyShipComp.Range, baseY - 24, 12, 12), 
							new Rect(0, 0, -1, 1), (Texture2D)ResourceBank.projectileEnemy.MatSingle.mainTexture);
					}
				}
				foreach (TravelingTransportPods obj in Find.WorldObjects.TravelingTransportPods)
				{
					float rng = (float)Traverse.Create(obj).Field("traveledPct").GetValue();
					int initialTile = (int)Traverse.Create(obj).Field("initialTile").GetValue();
					if (obj.destinationTile == playerShipComp.ShipCombatMasterMap.Tile && initialTile == mapPlayer.Tile) 
					{
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 200 + rng * enemyShipComp.Range, baseY - 16, 12, 12),
							new Rect(0, 0, 1, 1), (Texture2D)ResourceBank.shuttlePlayer.MatSingle.mainTexture);
					}
					else if (obj.destinationTile == mapPlayer.Tile && initialTile == playerShipComp.ShipCombatMasterMap.Tile && obj.Faction != Faction.OfPlayer)
					{
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 200 + (1 - rng) * enemyShipComp.Range, baseY - 20, 12, 12),
							new Rect(0, 0, -1, 1), (Texture2D)ResourceBank.shuttleEnemy.MatSingle.mainTexture);
					}
					else if (obj.destinationTile == mapPlayer.Tile && initialTile == playerShipComp.ShipCombatMasterMap.Tile && obj.Faction == Faction.OfPlayer)
					{
						Verse.Widgets.DrawTexturePart(
							new Rect(screenHalf - 200 + (1 - rng) * enemyShipComp.Range, baseY - 20, 12, 12),
							new Rect(0, 0, -1, 1), (Texture2D)ResourceBank.shuttlePlayer.MatSingle.mainTexture);
					}
				}
				if (Mouse.IsOver(rect))
				{
					string iconTooltipText = TranslatorFormattedStringExtensions.Translate("ShipCombatTooltip");
					if (!iconTooltipText.NullOrEmpty())
					{
						TooltipHandler.TipRegion(rect, iconTooltipText);
					}
				}
			}
		}
	}
	
	[HarmonyPatch(typeof(ColonistBarColonistDrawer), "DrawGroupFrame")]
	public static class ShipIconOnPawnBar
	{
		[HarmonyPostfix]
		public static void DrawShip(int group, ColonistBarColonistDrawer __instance)
		{
			List<ColonistBar.Entry> entries = Find.ColonistBar.Entries;
			foreach (ColonistBar.Entry entry in entries)
			{
				if (entry.group == group && entry.pawn == null && entry.map.IsSpace())
				{
					Rect rect = (Rect)typeof(ColonistBarColonistDrawer)
					.GetMethod("GroupFrameRect", BindingFlags.NonPublic | BindingFlags.Instance)
					.Invoke(__instance, new object[] { group });
					var mapComp = entry.map.GetComponent<ShipHeatMapComp>();
					if (mapComp.IsGraveyard) //wreck
						Verse.Widgets.DrawTextureFitted(rect, ResourceBank.shipBarNeutral.MatSingle.mainTexture, 1);
					else if (entry.map.ParentFaction == Faction.OfPlayer)//player
						Verse.Widgets.DrawTextureFitted(rect, ResourceBank.shipBarPlayer.MatSingle.mainTexture, 1);
					else //enemy
						Verse.Widgets.DrawTextureFitted(rect, ResourceBank.shipBarEnemy.MatSingle.mainTexture, 1);
				}
			}
		}
	}

	[HarmonyPatch(typeof(LetterStack), "LettersOnGUI")]
	public static class TimerOnGUI
	{
		[HarmonyPrefix]
		public static bool DrawShipTimer(ref float baseY)
		{
			Map map = Find.CurrentMap;
			if (map != null && map.IsSpace())
			{
				var timecomp = map.Parent.GetComponent<TimedForcedExitShip>();
				if (timecomp != null && timecomp.ForceExitAndRemoveMapCountdownActive)
				{
					float num = (float)UI.screenWidth - 200f;
					Rect rect = new Rect(num, baseY - 16f, 193f, 26f);
					Text.Anchor = TextAnchor.MiddleRight;
					string detectionCountdownTimeLeftString = timecomp.ForceExitAndRemoveMapCountdownTimeLeftString;
					string text = "ShipBurnUpCountdown".Translate(detectionCountdownTimeLeftString);
					float x = Text.CalcSize(text).x;
					Rect rect2 = new Rect(rect.xMax - x, rect.y, x, rect.height);
					if (Mouse.IsOver(rect2))
					{
						Widgets.DrawHighlight(rect2);
					}
					TooltipHandler.TipRegionByKey(rect2, "ShipBurnUpCountdownTip", detectionCountdownTimeLeftString);
					Widgets.Label(rect2, text);
					Text.Anchor = TextAnchor.UpperLeft;
					baseY -= 26f;
				}
			}
			return true;
		}
	}

	//biome
	[HarmonyPatch(typeof(MapDrawer), "DrawMapMesh", null)]
	public static class RenderPlanetBehindMap
	{
		public const float altitude = 1100f;
		[HarmonyPrefix]
		public static void PreDraw()
		{
			Map map = Find.CurrentMap;

			// if we aren't in space, abort!
			if ((ShipInteriorMod2.renderedThatAlready && !SaveOurShip2.ModSettings_SoS.renderPlanet) || !map.IsSpace())
			{
				return;
			}
			//TODO replace this when interplanetary travel is ready
			//Find.PlaySettings.showWorldFeatures = false;
			RenderTexture oldTexture = Find.WorldCamera.targetTexture;
			RenderTexture oldSkyboxTexture = RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture;

			Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.Planet;
			Find.WorldCameraDriver.JumpTo(Find.CurrentMap.Tile);
			Find.WorldCameraDriver.altitude = altitude;
			Find.WorldCameraDriver.GetType()
				.GetField("desiredAltitude", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
				.SetValue(Find.WorldCameraDriver, altitude);

			float num = (float)UI.screenWidth / (float)UI.screenHeight;

			Find.WorldCameraDriver.Update();
			Find.World.renderer.CheckActivateWorldCamera();
			Find.World.renderer.DrawWorldLayers();
			WorldRendererUtility.UpdateWorldShadersParams();
			//TODO replace this when interplanetary travel is ready
			/*List<WorldLayer> layers = (List<WorldLayer>)typeof(WorldRenderer).GetField("layers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(Find.World.renderer);
            foreach(WorldLayer layer in layers)
            {
                if (layer is WorldLayer_Stars)
                    layer.Render();
            }
            Find.PlaySettings.showWorldFeatures = false;*/
			RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture = ResourceBank.target;
			RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.aspect = num;
			RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.Render();

			Find.WorldCamera.targetTexture = ResourceBank.target;
			Find.WorldCamera.aspect = num;
			Find.WorldCamera.Render();

			RenderTexture.active = ResourceBank.target;
			ResourceBank.virtualPhoto.ReadPixels(new Rect(0, 0, 2048, 2048), 0, 0);
			ResourceBank.virtualPhoto.Apply();
			RenderTexture.active = null;

			Find.WorldCamera.targetTexture = oldTexture;
			RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture = oldSkyboxTexture;
			Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.None;
			Find.World.renderer.CheckActivateWorldCamera();

			if (!((List<WorldLayer>)typeof(WorldRenderer).GetField("layers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(Find.World.renderer)).FirstOrFallback().ShouldRegenerate)
				ShipInteriorMod2.renderedThatAlready = true;
		}
	}

	[HarmonyPatch(typeof(SectionLayer), "FinalizeMesh", null)]
	public static class GenerateSpaceSubMesh
	{
		[HarmonyPrefix]
		public static bool GenerateMesh(SectionLayer __instance, Section ___section)
		{
			if (__instance.GetType().Name != "SectionLayer_Terrain")
				return true;

			bool foundSpace = false;
			foreach (IntVec3 cell in ___section.CellRect.Cells)
			{
				TerrainDef terrain1 = ___section.map.terrainGrid.TerrainAt(cell);
				if (terrain1 == ResourceBank.TerrainDefOf.EmptySpace)
				{
					foundSpace = true;
					Printer_Mesh.PrintMesh(__instance, Matrix4x4.TRS(cell.ToVector3() + new Vector3(0.5f, 0f, 0.5f), Quaternion.identity, Vector3.one), MeshMakerPlanes.NewPlaneMesh(1f), ResourceBank.PlanetMaterial);
				}
			}
			if (!foundSpace)
			{
				for (int i = 0; i < __instance.subMeshes.Count; i++)
				{
					if (__instance.subMeshes[i].material == ResourceBank.PlanetMaterial)
					{
						__instance.subMeshes.RemoveAt(i);
					}
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Map))]
	[HarmonyPatch("Biome", MethodType.Getter)]
	public static class SpaceBiomeGetter
	{
		[HarmonyPrefix]
		public static bool interceptBiome(Map __instance, out bool __state)
		{
			__state = __instance.info?.parent != null &&
						   (__instance.info.parent is WorldObjectOrbitingShip || __instance.info.parent is SpaceSite || __instance.info.parent is MoonBase || __instance.Parent.AllComps.Any(comp => comp is MoonPillarSiteComp));
			return !__state;
		}

		[HarmonyPostfix]
		public static void getSpaceBiome(Map __instance, ref BiomeDef __result, bool __state)
		{
			if (__state)
				__result = ResourceBank.BiomeDefOf.OuterSpaceBiome;
		}
	}

	[HarmonyPatch(typeof(JoyUtility), "EnjoyableOutsideNow", new Type[] { typeof(Map), typeof(StringBuilder) })]
	public static class NoNatureRunningInSpace
	{
		[HarmonyPostfix]
		public static void NoOutside(Map map, ref bool __result)
		{
			if (map.IsSpace())
            {
				__result = false;
			}
		}
	}

	[HarmonyPatch(typeof(MapTemperature))]
	[HarmonyPatch("OutdoorTemp", MethodType.Getter)]
	public static class FixOutdoorTemp
	{
		[HarmonyPostfix]
		public static void GetSpaceTemp(ref float __result, Map ___map)
		{
			if (___map.IsSpace()) __result = -100f;
		}
	}

	[HarmonyPatch(typeof(MapTemperature))]
	[HarmonyPatch("SeasonalTemp", MethodType.Getter)]
	public static class FixSeasonalTemp
	{
		[HarmonyPostfix]
		public static void getSpaceTemp(ref float __result, Map ___map)
		{
			if (___map.IsSpace()) __result = -100f;
		}
	}

	[HarmonyPatch(typeof(Room))]
	[HarmonyPatch("OpenRoofCount", MethodType.Getter)]
	public static class SpaceRoomCheck //check if cache is invalid, if roofed and in space run postfix to check the room
	{
		[HarmonyPrefix]
		public static bool DoOnlyOnCaching(ref int ___cachedOpenRoofCount, out bool __state)
		{
			__state = false;
			if (___cachedOpenRoofCount == -1)
				__state = true;
			return true;
		}
		[HarmonyPostfix]
		public static int NoAirNoRoof(int __result, Room __instance, ref int ___cachedOpenRoofCount, bool __state)
		{
			if (__state && __result == 0 && __instance.Map.IsSpace() && !__instance.TouchesMapEdge && !__instance.IsDoorway)
			{
				foreach (IntVec3 tile in __instance.Cells)
				{
					var roof = tile.GetRoof(__instance.Map);
					if (!ShipInteriorMod2.IsRoofDefAirtight(roof))
					{
						___cachedOpenRoofCount = 1;
						return ___cachedOpenRoofCount;
					}
				}
				foreach (IntVec3 vec in __instance.BorderCells)
				{
					bool hasShipPart = false;
					foreach (Thing t in vec.GetThingList(__instance.Map))
					{
						if (t is Building b)
						{
							var shipPart = b.TryGetComp<CompSoShipPart>();
							if (shipPart != null && shipPart.Props.hermetic)
							{
								hasShipPart = true;
								break;
							}
						}
					}
					if (!hasShipPart)
					{
						___cachedOpenRoofCount = 1;
						return ___cachedOpenRoofCount;
					}
				}
			}
			return ___cachedOpenRoofCount;
		}
	}

	[HarmonyPatch(typeof(GenTemperature), "EqualizeTemperaturesThroughBuilding")]
	public static class NoVentingToSpace //block vents and open airlocks in vac, closed airlocks vent slower
	{
		public static bool Prefix(Building b, ref float rate, bool twoWay)
		{
			if (!b.Map.IsSpace())
				return true;
			if (twoWay) //vent
			{
				IntVec3 vec = b.Position + b.Rotation.FacingCell;
				Room room = vec.GetRoom(b.Map);
				if (ShipInteriorMod2.ExposedToOutside(room))
				{
					return false;
				}
				vec = b.Position - b.Rotation.FacingCell;
				room = vec.GetRoom(b.Map);
				if (ShipInteriorMod2.ExposedToOutside(room))
				{
					return false;
				}
				return true;
			}
			if (b is Building_ShipAirlock a)
			{
				if (a.Open && a.Outerdoor())
					return false;
				else
					rate = 0.75f;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(RoomTempTracker), "EqualizeTemperature")]
	public static class ExposedToVacuum
	{
		[HarmonyPostfix]
		public static void SetShipTemp(RoomTempTracker __instance, ref Room ___room)
		{
			if (___room.Map.terrainGrid.TerrainAt(IntVec3.Zero) != ResourceBank.TerrainDefOf.EmptySpace)
				return;
			if (___room.Role != RoomRoleDefOf.None && ___room.OpenRoofCount > 0)
				__instance.Temperature = -100f;
		}
	}

	[HarmonyPatch(typeof(RoomTempTracker), "WallEqualizationTempChangePerInterval")]
	public static class TemperatureDoesntDiffuseFastInSpace
	{
		[HarmonyPostfix]
		public static void RadiativeHeatTransferOnly(ref float __result, RoomTempTracker __instance)
		{
			if (((Room)typeof(RoomTempTracker)
					.GetField("room", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance)).Map.IsSpace())
			{
				__result *= 0.01f;
			}
		}
	}

	[HarmonyPatch(typeof(RoomTempTracker), "ThinRoofEqualizationTempChangePerInterval")]
	public static class TemperatureDoesntDiffuseFastInSpaceToo
	{
		[HarmonyPostfix]
		public static void RadiativeHeatTransferOnly(ref float __result, RoomTempTracker __instance)
		{
			if (((Room)typeof(RoomTempTracker)
					.GetField("room", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance)).Map.IsSpace())
			{
				__result *= 0.01f;
			}
		}
	}

	[HarmonyPatch(typeof(GlobalControls), "TemperatureString")]
	public static class ShowBreathability
	{
		[HarmonyPostfix]
		public static void CheckO2(ref string __result)
		{
			if (!Find.CurrentMap.IsSpace()) return;

			if (ShipInteriorMod2.ExposedToOutside(UI.MouseCell().GetRoom(Find.CurrentMap)))
			{
				__result += " (Vacuum)";
			}
			else
            {
				if (Find.CurrentMap.GetComponent<ShipHeatMapComp>().LifeSupports.Where(s => s.active).Any())
					__result += " (Breathable Atmosphere)";
				else
					__result += " (Non-Breathable Atmosphere)";
			}
		}
	}

	[HarmonyPatch(typeof(Fire), "DoComplexCalcs")]
	public static class CannotBurnInSpace
	{
		[HarmonyPostfix]
		public static void extinguish(Fire __instance)
		{
			if (!(__instance is MechaniteFire) && __instance.Spawned && __instance.Map.IsSpace())
			{
				Room room = __instance.Position.GetRoom(__instance.Map);
				if (ShipInteriorMod2.ExposedToOutside(room))
					__instance.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 100, 0, -1f, null, null, null,
						DamageInfo.SourceCategory.ThingOrUnknown, null));
			}
		}
	}

	[HarmonyPatch(typeof(Plant), "TickLong")]
	public static class KillThePlantsInSpace
	{
		[HarmonyPostfix]
		public static void Extinguish(Plant __instance)
		{
			if (__instance.Spawned && __instance.Map.IsSpace())
			{
				if (ShipInteriorMod2.AirlockBugFlag)
					return;
				Room room = __instance.Position.GetRoom(__instance.Map);
				if (ShipInteriorMod2.ExposedToOutside(room))
				{
					__instance.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 10, 0, -1f, null, null, null,
						DamageInfo.SourceCategory.ThingOrUnknown, null));
				}
			}
		}
	}

	[HarmonyPatch(typeof(Plant), "MakeLeafless")]
	public static class DoNotKillPlantsOnMove
	{
		[HarmonyPrefix]
		public static bool Abort()
		{
			if (ShipInteriorMod2.AirlockBugFlag)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(PollutionGrid), "SetPolluted")]
	public static class DoNotPolluteSpace
	{
		[HarmonyPrefix]
		public static bool Abort(IntVec3 cell, Map ___map)
		{
			if (___map.terrainGrid.TerrainAt(cell) == ResourceBank.TerrainDefOf.EmptySpace)
            {
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(PenFoodCalculator), "ProcessTerrain")]
	public static class SpaceHasNoWildPlants
	{
		public static bool Prefix(PenFoodCalculator __instance, IntVec3 c, Map map)
		{
			if (map.IsSpace())
			{
				__instance.numCells++;
				MapPastureNutritionCalculator.NutritionPerDayPerQuadrum other = new MapPastureNutritionCalculator.NutritionPerDayPerQuadrum();
				other.quadrum[0] = 0;
				other.quadrum[1] = 0;
				other.quadrum[2] = 0;
				other.quadrum[3] = 0;
				__instance.nutritionPerDayPerQuadrum.AddFrom(other);
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(WeatherManager), "TransitionTo")]
	public static class SpaceWeatherStays
	{
		public static bool Prefix(WeatherManager __instance)
		{
			if (__instance.map.IsSpace() && __instance.curWeather == ResourceBank.WeatherDefOf.OuterSpaceWeather)
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(WeatherDecider), "StartNextWeather")]
	public static class SpaceWeatherStaysTwo
	{
		public static bool Prefix(WeatherManager __instance)
		{
			if (__instance.map.IsSpace() && __instance.curWeather == ResourceBank.WeatherDefOf.OuterSpaceWeather)
			{
				return false;
			}
			return true;
		}
	}

	//map
	[HarmonyPatch(typeof(CompShipPart), "CompGetGizmosExtra")]
	public static class NoGizmoInSpace
	{
		[HarmonyPrefix]
		public static bool CheckBiome(CompShipPart __instance, out bool __state)
		{
			__state = false;
			if (__instance.parent.Map != null && __instance.parent.Map.IsSpace())
			{
				__state = true;
				return false;
			}
			return true;
		}

		[HarmonyPostfix]
		public static void ReturnEmpty(ref IEnumerable<Gizmo> __result, bool __state)
		{
			if (__state)
				__result = new List<Gizmo>();
		}
	}

	[HarmonyPatch(typeof(SettleInExistingMapUtility), "SettleCommand")]
	public static class NoSpaceSettle
	{
		[HarmonyPostfix]
		public static void Nope(Command __result, Map map)
		{
			if (map.IsSpace())
			{
				__result.disabled = true;
				__result.disabledReason = "Cannot settle space sites";
			}
		}
	}

	[HarmonyPatch(typeof(Building), "ClaimableBy")]
	public static class NoClaimingEnemyShip
	{
		[HarmonyPostfix]
		public static void Nope(Building __instance, ref bool __result)
		{
			if (__instance.Map.IsSpace() && __instance.Map.GetComponent<ShipHeatMapComp>().ShipCombatMaster)
				__result = false;
		}
	}

	[HarmonyPatch(typeof(MapDeiniter), "Deinit_NewTemp")]
	public static class RemoveSpaceMap
	{
		public static void Postfix()
		{
			AccessExtensions.Utility.RecacheSpaceMaps();
		}
	}

	[HarmonyPatch(typeof(IncidentWorker_TraderCaravanArrival), "CanFireNowSub")]
	public static class NoTradersInSpace
	{
		[HarmonyPostfix]
		public static void Nope(IncidentParms parms, ref bool __result)
		{
			if (parms.target != null && parms.target is Map map && map.IsSpace()) __result = false;
		}
	}

	[HarmonyPatch(typeof(ExitMapGrid))]
	[HarmonyPatch("MapUsesExitGrid", MethodType.Getter)]
	public static class InSpaceNoOneCanHearYouRunAway
	{
		[HarmonyPostfix]
		public static void NoEscape(Map ___map, ref bool __result)
		{
			if (___map.IsSpace()) __result = false;
		}
	}

	[HarmonyPatch(typeof(TileFinder), "TryFindNewSiteTile")]
	public static class NoQuestsNearTileZero
	{
		[HarmonyPrefix]
		public static bool DisableOriginalMethod()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void CheckNonZeroTile(out int tile, int minDist, int maxDist, bool allowCaravans,
			TileFinderMode tileFinderMode, int nearThisTile, ref bool __result)
		{
			Func<int, int> findTile = delegate (int root) {
				int minDist2 = minDist;
				int maxDist2 = maxDist;
				Predicate<int> validator = (int x) =>
					!Find.WorldObjects.AnyWorldObjectAt(x) && TileFinder.IsValidTileForNewSettlement(x, null);
				int result;
				if (TileFinder.TryFindPassableTileWithTraversalDistance(root, minDist2, maxDist2, out result,
					validator: validator, ignoreFirstTilePassability: false, tileFinderMode, false))
				{
					return result;
				}

				return -1;
			};
			int arg;
			if (nearThisTile != -1)
			{
				arg = nearThisTile;
			}
			else if (!TileFinder.TryFindRandomPlayerTile(out arg, allowCaravans,
				(int x) => findTile(x) != -1 && (Find.World.worldObjects.MapParentAt(x) == null ||
												 !(Find.World.worldObjects.MapParentAt(x) is WorldObjectOrbitingShip))))
			{
				tile = -1;
				__result = false;
				return;
			}

			tile = findTile(arg);
			__result = (tile != -1);
		}
	}

	[HarmonyPatch(typeof(QuestNode_GetMap), "IsAcceptableMap")]
	public static class NoQuestsInSpace
	{
		[HarmonyPostfix]
		public static void Fixpost(Map map, ref bool __result)
		{
			if (map.Parent != null && map.IsSpace()) __result = false;
		}
	}

	[HarmonyPatch(typeof(QuestGen_Get), "GetMap")]
	public static class InSpaceNoQuestsCanUseThis
	{
		[HarmonyPostfix]
		public static void NoQuestsTargetSpace(ref Map __result)
		{
			if (__result != null && __result.IsSpace())
			{
				//retry and exclude space maps
				Log.Message("Tried to fire quest in space map, retrying.");
				Map map = Find.Maps.Where(m => m.IsPlayerHome && !m.IsSpace() && m.mapPawns.FreeColonists.Count >= 1).FirstOrDefault();
				if (map == null)
					map = Find.Maps.Where(m => m.IsPlayerHome && m.mapPawns.FreeColonists.Count >= 1).FirstOrDefault();
				__result = map;
			}
		}
	}

	[HarmonyPatch(typeof(RCellFinder), "TryFindRandomExitSpot")]
	public static class NoPrisonBreaksInSpace
	{
		[HarmonyPostfix]
		public static void NoExits(Pawn pawn, ref bool __result)
		{
			if (pawn.Map.IsSpace()) __result = false;
		}
	}

	[HarmonyPatch(typeof(RoofCollapseCellsFinder), "ConnectsToRoofHolder")]
	public static class NoRoofCollapseInSpace
	{
		[HarmonyPostfix]
		public static void ZeroGee(ref bool __result, Map map)
		{
			if (map.IsSpace()) __result = true;
		}
	}

	[HarmonyPatch(typeof(RoofCollapseUtility), "WithinRangeOfRoofHolder")]
	public static class NoRoofCollapseInSpace2
	{
		[HarmonyPostfix]
		public static void ZeroGee(ref bool __result, Map map)
		{
			if (map.IsSpace()) __result = true;
		}
	}

	[HarmonyPatch(typeof(FogGrid), "FloodUnfogAdjacent")]
	public static class DoNotSpamMePlease
	{
		[HarmonyPrefix]
		public static bool CheckBiome(Map ___map, out bool __state)
		{
			__state = false;
			if (___map != null && ___map.IsSpace())
			{
				__state = true;
				return false;
			}
			return true;
		}

		[HarmonyPostfix]
		public static void NoMoreAreaSpam(FogGrid __instance, Map ___map, IntVec3 c, bool __state)
		{
			if (__state)
			{
				__instance.Unfog(c);
				for (int i = 0; i < 4; i++)
				{
					IntVec3 intVec = c + GenAdj.CardinalDirections[i];
					if (intVec.InBounds(___map) && intVec.Fogged(___map))
					{
						Building edifice = intVec.GetEdifice(___map);
						if (edifice == null || !edifice.def.MakeFog)
						{
							FloodFillerFog.FloodUnfog(intVec, ___map);
						}
						else
						{
							__instance.Unfog(intVec);
						}
					}
				}
				for (int j = 0; j < 8; j++)
				{
					IntVec3 c2 = c + GenAdj.AdjacentCells[j];
					if (c2.InBounds(___map))
					{
						Building edifice2 = c2.GetEdifice(___map);
						if (edifice2 != null && edifice2.def.MakeFog)
						{
							__instance.Unfog(c2);
						}
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(RoyalTitlePermitWorker_CallAid), "CallAid")]
	public static class CallAidInSpace
	{
		[HarmonyPrefix]
		public static bool SpaceAidHasEVA(RoyalTitlePermitWorker_CallAid __instance, Pawn caller, Map map, IntVec3 spawnPos, Faction faction, bool free, float biocodeChance = 1f)
		{
			if (map != null && map.IsSpace())
			{
				IncidentParms incidentParms = new IncidentParms();
				incidentParms.target = map;
				incidentParms.faction = faction;
				incidentParms.raidArrivalModeForQuickMilitaryAid = true;
				incidentParms.biocodeApparelChance = biocodeChance;
				incidentParms.biocodeWeaponsChance = biocodeChance;
				incidentParms.spawnCenter = spawnPos;
				if (__instance.def.royalAid.pawnKindDef != null)
				{
					incidentParms.pawnKind = __instance.def.royalAid.pawnKindDef;
					//if (incidentParms.pawnKind == PawnKindDefOf.Empire_Fighter_Trooper)
					//return false;
					if (incidentParms.pawnKind == PawnKindDefOf.Empire_Fighter_Janissary)
						incidentParms.pawnKind = DefDatabase<PawnKindDef>.GetNamed("Empire_Fighter_Marine_Space");
					else if (incidentParms.pawnKind == PawnKindDefOf.Empire_Fighter_Cataphract)
						incidentParms.pawnKind = DefDatabase<PawnKindDef>.GetNamed("Empire_Fighter_Cataphract_Space");
					incidentParms.pawnCount = __instance.def.royalAid.pawnCount;
				}
				else
				{
					incidentParms.points = (float)__instance.def.royalAid.points;
				}
				faction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame;
				if (IncidentDefOf.RaidFriendly.Worker.TryExecute(incidentParms))
				{
					if (!free)
					{
						caller.royalty.TryRemoveFavor(faction, __instance.def.royalAid.favorCost);
					}
					caller.royalty.GetPermit(__instance.def, faction).Notify_Used();
					return false;
				}
				Log.Error(string.Concat(new object[] { "Could not send aid to map ", map, " from faction ", faction }));
				return false;
			}
			else
				return true;
		}
	}

	[HarmonyPatch(typeof(RoyalTitlePermitWorker_CallLaborers), "CallLaborers")]
	public static class CallLaborersInSpace
	{
		[HarmonyPrefix]
		public static bool SpaceLaborersHaveEVA(RoyalTitlePermitWorker_CallAid __instance, Pawn pawn, Map map, Faction faction, bool free)
		{
			if (map != null && map.IsSpace())
			{
				if (faction.HostileTo(Faction.OfPlayer))
				{
					return false;
				}
				QuestScriptDef permit_CallLaborers = QuestScriptDefOf.Permit_CallLaborers;
				Slate slate = new Slate();
				slate.Set<Map>("map", map, false);
				slate.Set<int>("laborersCount", __instance.def.royalAid.pawnCount, false);
				slate.Set<Faction>("permitFaction", faction, false);
				slate.Set<PawnKindDef>("laborersPawnKind", DefDatabase<PawnKindDef>.GetNamed("Empire_Space_Laborer"), false);
				slate.Set<float>("laborersDurationDays", __instance.def.royalAid.aidDurationDays, false);
				QuestUtility.GenerateQuestAndMakeAvailable(permit_CallLaborers, slate);
				pawn.royalty.GetPermit(__instance.def, faction).Notify_Used();
				if (!free)
				{
					pawn.royalty.TryRemoveFavor(faction, __instance.def.royalAid.favorCost);
				}
				return false;
			}
			else
				return true;
		}
	}

	[HarmonyPatch(typeof(RoyalTitlePermitWorker), "AidDisabled")]
	public static class RoyalTitlePermitWorkerInSpace
	{
		[HarmonyPostfix]
		public static void AllowSpacePermits(Map map, ref bool __result)
		{
			if (map != null && map.IsSpace() && __result == true)
				__result = false;
		}
	}

	[HarmonyPatch(typeof(Site), "PostMapGenerate")]
	public static class RaidsStartEarly
	{
		public static void Postfix(Site __instance)
		{
			if (__instance.parts.Where(part => part.def.tags.Contains("SoSMayday")).Any())
			{
				__instance.GetComponent<TimedDetectionRaids>().StartDetectionCountdown(Rand.Range(6000, 12000), 1);
			}
		}
	}

	//sensor
	[HarmonyPatch(typeof(MapPawns))]
	[HarmonyPatch("AnyPawnBlockingMapRemoval", MethodType.Getter)]
	public static class KeepMapAlive
	{
		public static void Postfix(MapPawns __instance, ref bool __result)
		{
			Map mapPlayer = ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def.defName.Equals("ShipOrbiting")).FirstOrDefault())?.Map;
			if (mapPlayer != null)
			{
				foreach (Building_ShipAdvSensor sensor in Find.World.GetComponent<PastWorldUWO2>().Sensors)
				{
					if (sensor.observedMap != null && sensor.observedMap.Map != null && sensor.observedMap.Map.mapPawns == __instance)
						__result = true;
				}
			}
		}
	}

	[HarmonyPatch(typeof(SettlementDefeatUtility), "IsDefeated")]
	public static class NoInstaWin
	{
		public static void Postfix(Map map, ref bool __result)
		{
			Map mapPlayer = ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def.defName.Equals("ShipOrbiting")).FirstOrDefault())?.Map;
			if (mapPlayer != null)
			{
				foreach (Building_ShipAdvSensor sensor in Find.World.GetComponent<PastWorldUWO2>().Sensors)
				{
					if (sensor.observedMap != null && sensor.observedMap.Map == map)
					{
						__result = false;
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(TimedDetectionRaids), "CompTick")]
	public static class NoScanRaids
	{
		public static bool Prefix(TimedDetectionRaids __instance)
		{
			return ((MapParent)__instance.parent).HasMap && ((MapParent)__instance.parent).Map.mapPawns.AnyColonistSpawned;
		}
	}

	//comms
	[HarmonyPatch(typeof(Building_CommsConsole), "GetFailureReason")]
	public static class NoCommsWhenCloaked
	{
		public static void Postfix(Pawn myPawn, ref FloatMenuOption __result)
		{
			foreach (Building_ShipCloakingDevice cloak in myPawn.Map.GetComponent<ShipHeatMapComp>().Cloaks)
			{
				if (cloak.active && cloak.Map == myPawn.Map)
				{
					__result = new FloatMenuOption("CannotUseCloakEnabled".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null);
					break;
				}
			}
		}
	}

	[HarmonyPatch(typeof(TradeShip), "TryOpenComms")]
	public static class ReplaceCommsIfPirate
	{
		[HarmonyPrefix]
		public static bool DisableMethod()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void OpenActualComms(TradeShip __instance, Pawn negotiator)
		{
			if (!__instance.CanTradeNow)
			{
				return;
			}
			int bounty = Find.World.GetComponent<PastWorldUWO2>().PlayerFactionBounty;

			DiaNode diaNode = new DiaNode("TradeShipComms".Translate() + __instance.TraderName);

			//trade normally if no bounty or low bounty with social check
			DiaOption diaOption = new DiaOption("TradeShipTradeWith".Translate());
			diaOption.action = delegate
			{
				Find.WindowStack.Add(new Dialog_Trade(negotiator, __instance, false));
				LessonAutoActivator.TeachOpportunity(ConceptDefOf.BuildOrbitalTradeBeacon, OpportunityType.Critical);
				PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(__instance.Goods.OfType<Pawn>(), "LetterRelatedPawnsTradeShip".Translate(Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent, false, true);
				TutorUtility.DoModalDialogIfNotKnown(ConceptDefOf.TradeGoodsMustBeNearBeacon, Array.Empty<string>());
			};
			diaOption.resolveTree = true;
			diaNode.options.Add(diaOption);
			if (negotiator.skills.GetSkill(SkillDefOf.Social).levelInt * 2 < bounty)
			{
				diaOption.Disable("TradeShipTradeDecline".Translate(__instance.TraderName));
			}

			//if in space add pirate option
			if (__instance.Map.IsSpace())
			{
				DiaOption diaOption2 = new DiaOption("TradeShipPirate".Translate());
				diaOption2.action = delegate
				{
					Building bridge = __instance.Map.listerBuildings.AllBuildingsColonistOfClass<Building_ShipBridge>().FirstOrDefault();
					if (Rand.Chance(0.025f * negotiator.skills.GetSkill(SkillDefOf.Social).levelInt + negotiator.Map.GetComponent<ShipHeatMapComp>().MapThreat(__instance.Map) / 400 - bounty / 40))
					{
						//social + shipstr vs bounty for piracy dialog
						Find.WindowStack.Add(new Dialog_Pirate(__instance.Map.listerBuildings.allBuildingsColonist.Where(t => t.def.defName.Equals("ShipSalvageBay")).Count(), __instance));
						bounty += 4;
						Find.World.GetComponent<PastWorldUWO2>().PlayerFactionBounty = bounty;
					}
					else
					{
						//check failed, ship is fleeing
						bounty += 1;
						Find.World.GetComponent<PastWorldUWO2>().PlayerFactionBounty = bounty;
						if (__instance.Faction == Faction.OfEmpire)
							Faction.OfEmpire.TryAffectGoodwillWith(Faction.OfPlayer, -25, false, true, HistoryEventDefOf.AttackedCaravan, null);
						DiaNode diaNode2 = new DiaNode(__instance.TraderName + "TradeShipTryingToFlee".Translate());
						DiaOption diaOption21 = new DiaOption("TradeShipAttack".Translate());
						diaOption21.action = delegate
						{
							negotiator.Map.GetComponent<ShipHeatMapComp>().StartShipEncounter(bridge, (TradeShip)__instance);
							if (ModsConfig.IdeologyActive)
								IdeoUtility.Notify_PlayerRaidedSomeone(__instance.Map.mapPawns.FreeColonists);
						};
						diaOption21.resolveTree = true;
						diaNode2.options.Add(diaOption21);
						DiaOption diaOption22 = new DiaOption("TradeShipFlee".Translate());
						diaOption22.action = delegate
						{
							__instance.Depart();
						};
						diaOption22.resolveTree = true;
						diaNode2.options.Add(diaOption22);
						Find.WindowStack.Add(new Dialog_NodeTree(diaNode2, true, false, null));

					}
				};
				diaOption2.resolveTree = true;
				diaNode.options.Add(diaOption2);

			}
			//pay bounty, gray if not enough money
			if (bounty > 1)
			{
				DiaOption diaOption3 = new DiaOption("TradeShipPayBounty".Translate(2500 * bounty));
				diaOption3.action = delegate
				{
					TradeUtility.LaunchThingsOfType(ThingDefOf.Silver, 2500 * bounty, __instance.Map, null);
					bounty = 0;
					Find.World.GetComponent<PastWorldUWO2>().PlayerFactionBounty = bounty;
				};
				diaOption3.resolveTree = true;
				diaNode.options.Add(diaOption3);

				if (AmountSendableSilver(__instance.Map) < 2500 * bounty)
				{
					diaOption3.Disable("NotEnoughForBounty".Translate(2500 * bounty));
				}
			}
			//quit
			DiaOption diaOption4 = new DiaOption("(" + "Disconnect".Translate() + ")");
			diaOption4.resolveTree = true;
			diaNode.options.Add(diaOption4);
			Find.WindowStack.Add(new Dialog_NodeTree(diaNode, true, false, null));
		}
		private static int AmountSendableSilver(Map map)
		{
			return (from t in TradeUtility.AllLaunchableThingsForTrade(map, null)
					where t.def == ThingDefOf.Silver
					select t).Sum((Thing t) => t.stackCount);
		}
	}

	//ship
	[HarmonyPatch(typeof(ShipUtility), "ShipBuildingsAttachedTo")]
	public static class FindAllTheShipParts
	{
		[HarmonyPrefix]
		public static bool DisableOriginalMethod()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void FindShipPartsReally(Building root, ref List<Building> __result)
		{
			if (root == null || root.Destroyed)
			{
				__result = new List<Building>();
				return;
			}

			var map = root.Map;
			var containedBuildings = new HashSet<Building>();
			var cellsTodo = new HashSet<IntVec3>();
			var cellsDone = new HashSet<IntVec3>();

			cellsTodo.AddRange(GenAdj.CellsOccupiedBy(root));
			cellsTodo.AddRange(GenAdj.CellsAdjacentCardinal(root));

			while (cellsTodo.Count > 0)
			{
				var current = cellsTodo.First();
				cellsTodo.Remove(current);
				cellsDone.Add(current);
				var containedThings = current.GetThingList(map);
				if (!containedThings.Any(t => (t as Building)?.def.building.shipPart ?? false))
					continue;

				foreach (var t in containedThings)
				{
					if (t is Building b && containedBuildings.Add(b))
					{
						cellsTodo.AddRange(GenAdj.CellsOccupiedBy(b).Concat(GenAdj.CellsAdjacentCardinal(b)).Where(cell => !cellsDone.Contains(cell)));
					}
				}
			}
			__result = containedBuildings.ToList();
		}
	}

	[HarmonyPatch(typeof(ShipUtility), "LaunchFailReasons")]
	public static class FindLaunchFailReasons
	{
		[HarmonyPrefix]
		public static bool DisableOriginalMethod()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void FindLaunchFailReasonsReally(Building rootBuilding, ref IEnumerable<string> __result)
		{
			List<string> newResult = new List<string>();
			List<Building> shipParts = ShipUtility.ShipBuildingsAttachedTo(rootBuilding);

			float fuelNeeded = 0f;
			float fuelHad = 0f;
			bool hasPilot = false;
			bool hasEngine = false;
			bool hasSensor = false;
			foreach (Building part in shipParts)
			{
				var engine = part.TryGetComp<CompEngineTrail>();
				if (engine != null && engine.Props.takeOff)
				{
					hasEngine = true;
					fuelHad += engine.refuelComp.Fuel;
					if (engine.refuelComp.Props.fuelFilter.AllowedThingDefs.Contains(ThingDef.Named("ShuttleFuelPods")))
						fuelHad += engine.refuelComp.Fuel;
				}
				else if (!hasPilot && part is Building_ShipBridge bridge && bridge.TryGetComp<CompPowerTrader>().PowerOn)
				{
					var mannable = bridge.TryGetComp<CompMannable>();
					if (mannable == null || (mannable != null && mannable.MannedNow))
						hasPilot = true;
				}
				else if (part is Building_ShipAdvSensor)
					hasSensor = true;

				if (!part.TryGetComp<CompSoShipPart>()?.Props.isPlating ?? false)
					fuelNeeded += (part.def.size.x * part.def.size.z) * 3f;
				else
					fuelNeeded += 1f;
			}
			if (!hasEngine)
				newResult.Add(TranslatorFormattedStringExtensions.Translate("ShipReportMissingPart") + ": " + ThingDefOf.Ship_Engine.label);
			if (fuelHad < fuelNeeded)
				newResult.Add(TranslatorFormattedStringExtensions.Translate("ShipNeedsMoreFuel", fuelHad, fuelNeeded));
			if (!hasSensor)
				newResult.Add(TranslatorFormattedStringExtensions.Translate("ShipReportMissingPart") + ": " + ThingDefOf.Ship_SensorCluster.label);
			if (!hasPilot)
				newResult.Add(TranslatorFormattedStringExtensions.Translate("ShipReportNeedPilot"));

			__result = newResult;
		}
	}

	[HarmonyPatch(typeof(ShipCountdown), "InitiateCountdown", new Type[] { typeof(Building) })]
	public static class InitShipRefs
	{
		[HarmonyPrefix]
		public static bool SaveStatics(Building launchingShipRoot)
		{
			ShipInteriorMod2.shipOriginRoot = launchingShipRoot;
			return true;
		}
	}

	[HarmonyPatch(typeof(ShipCountdown), "CountdownEnded")]
	public static class SaveShip
	{
		[HarmonyPrefix]
		public static bool SaveShipAndRemoveItemStacks()
		{
			if (ShipInteriorMod2.shipOriginRoot != null)
			{
				ScreenFader.StartFade(UnityEngine.Color.clear, 1f);
				WorldObjectOrbitingShip orbiter = (WorldObjectOrbitingShip)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("ShipOrbiting"));
				orbiter.radius = 150;
				orbiter.theta = -3;
				orbiter.SetFaction(Faction.OfPlayer);
				orbiter.Tile = ShipInteriorMod2.FindWorldTile();
				Find.WorldObjects.Add(orbiter);
				Map myMap = MapGenerator.GenerateMap(ShipInteriorMod2.shipOriginRoot.Map.Size, orbiter, orbiter.MapGeneratorDef);
				myMap.fogGrid.ClearAllFog();

				ShipInteriorMod2.MoveShip(ShipInteriorMod2.shipOriginRoot, myMap, IntVec3.Zero);
				myMap.weatherManager.TransitionTo(ResourceBank.WeatherDefOf.OuterSpaceWeather);
				Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("LetterLabelOrbitAchieved"),
					TranslatorFormattedStringExtensions.Translate("LetterOrbitAchieved"), LetterDefOf.PositiveEvent);
				ShipInteriorMod2.shipOriginRoot = null;
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(GameConditionManager), "ConditionIsActive")]
	public static class SpacecraftAreHardenedAgainstSolarFlares
	{
		[HarmonyPostfix]
		public static void Nope(ref bool __result, GameConditionManager __instance, GameConditionDef def)
		{
			if (def == GameConditionDefOf.SolarFlare && __instance.ownerMap != null &&
				__instance.ownerMap.IsSpace())
				__result = false;
		}
	}

	[HarmonyPatch(typeof(GameConditionManager))]
	[HarmonyPatch("ElectricityDisabled", MethodType.Getter)]
	public static class SpacecraftAreAlsoHardenedInOnePointOne
	{
		[HarmonyPostfix]
		public static void PowerOn(GameConditionManager __instance, ref bool __result)
		{
			if (__instance.ownerMap.IsSpace()) __result = false;
		}
	}

	[HarmonyPatch(typeof(Designator_Dropdown), "GetDesignatorCost")]
	public class FixDropdownDisplay
	{
		public static void Postfix(Designator des, ref ThingDef __result)
		{
			Designator_Place designator_Place = des as Designator_Place;
			if (designator_Place != null)
			{
				BuildableDef placingDef = designator_Place.PlacingDef;
				if (placingDef.designationCategory.defName.Equals("Ship"))
				{
					__result = (ThingDef)placingDef;
				}
			}
		}
	}

	[HarmonyPatch(typeof(RoofGrid), "GetCellExtraColor")]
	public static class ShowHullTilesOnRoofGrid
	{
		[HarmonyPostfix]
		public static void HullsAreColorful(RoofGrid __instance, int index, ref Color __result)
		{
			if (__instance.RoofAt(index) == ResourceBank.RoofDefOf.RoofShip)
				__result = Color.clear;
		}
	}

	[HarmonyPatch(typeof(WorkGiver_ConstructDeliverResources), "ShouldRemoveExistingFloorFirst")]
	public static class DontRemoveShipFloors
	{
		[HarmonyPostfix]
		public static void CheckShipFloor(Blueprint blue, ref bool __result)
		{
			var t = blue.Map.terrainGrid.TerrainAt(blue.Position);
			if (t == ResourceBank.TerrainDefOf.FakeFloorInsideShip || t == ResourceBank.TerrainDefOf.FakeFloorInsideShipArchotech || t == ResourceBank.TerrainDefOf.FakeFloorInsideShipMech)
			{
				__result = false;
			}
		}
	}

	[HarmonyPatch(typeof(TerrainGrid), "DoTerrainChangedEffects")]
	public static class RecreateShipTile //restores ship terrain after tile removal
	{
		[HarmonyPostfix]
		public static void NoClearTilesPlease(TerrainGrid __instance, IntVec3 c)
		{
			if (ShipInteriorMod2.AirlockBugFlag)
				return;
			Map map = (Map)typeof(TerrainGrid).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
			foreach (Thing t in map.thingGrid.ThingsAt(c))
			{
				var roofComp = t.TryGetComp<CompRoofMe>();
				if (roofComp != null)
				{
					roofComp.SetShipTerrain(c);
					break;
				}
			}
		}
	}

	[HarmonyPatch(typeof(RoofGrid), "SetRoof")] //roofing ship tiles makes ship roof
	public static class RebuildShipRoof
	{
		[HarmonyPrefix]
		public static bool SetNewRoof(IntVec3 c, RoofDef def, Map ___map, ref CellBoolDrawer ___drawerInt, ref RoofDef[]  ___roofGrid)
		{
			if (def == null || def.isThickRoof)
				return true;
			foreach (Thing t in c.GetThingList(___map))
			{
				if (t.TryGetComp<CompRoofMe>()?.Props.roof ?? false)
				{
					var cellIndex = ___map.cellIndices.CellToIndex(c);
					if (___roofGrid[cellIndex] == def)
					{
						return false;
					}

					if (ShipInteriorMod2.IsRoofDefAirtight(def))
						return true;
					//Log.Message(String.Format("Overriding roof at {0}. Set shipRoofDef instead of {1}", cellIndex, def.defName));
					___roofGrid[cellIndex] = ResourceBank.RoofDefOf.RoofShip;
					___map.glowGrid.MarkGlowGridDirty(c);
					Region validRegionAt_NoRebuild = ___map.regionGrid.GetValidRegionAt_NoRebuild(c);
					if (validRegionAt_NoRebuild != null)
					{
						validRegionAt_NoRebuild.District.Notify_RoofChanged();
					}
					if (___drawerInt != null)
					{
						___drawerInt.SetDirty();
					}
					___map.mapDrawer.MapMeshDirty(c, MapMeshFlag.Roofs);
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(RoofCollapserImmediate), "DropRoofInCells")]
	[HarmonyPatch(new Type[] { typeof(IEnumerable<IntVec3>), typeof(Map), typeof(List<Thing>) })]
	public static class SealHole
	{
		[HarmonyPostfix]
		public static void ShipRoofIsDestroyed(IEnumerable<IntVec3> cells, Map map)
		{
			foreach (IntVec3 cell in cells)
			{
				if (map.IsSpace() && !cell.Roofed(map))
				{
					var mapComp = map.GetComponent<ShipHeatMapComp>();
					if (mapComp.HullFoamDistributors.Count > 0)
					{
						foreach (CompHullFoamDistributor dist in mapComp.HullFoamDistributors)
						{
							if (dist.parent.TryGetComp<CompRefuelable>().Fuel > 0 && dist.parent.TryGetComp<CompPowerTrader>().PowerOn)
							{
								dist.parent.TryGetComp<CompRefuelable>().ConsumeFuel(1);
								map.roofGrid.SetRoof(cell, ResourceBank.RoofDefOf.RoofShip);
								//Log.Message("rebuilt roof at:" + cell);
								break;
							}
						}
					}
				}
			}
		}
	}

	//weapons
	[HarmonyPatch(typeof(BuildingProperties))]
	[HarmonyPatch("IsMortar", MethodType.Getter)]
	public static class TorpedoesCanBeLoaded
	{
		[HarmonyPostfix]
		public static void CheckThisOneToo(BuildingProperties __instance, ref bool __result)
		{
			if (__instance?.turretGunDef?.HasComp(typeof(CompChangeableProjectilePlural)) ?? false)
			{
				__result = true;
			}
		}
	}

	[HarmonyPatch(typeof(ITab_Shells))]
	[HarmonyPatch("SelStoreSettingsParent", MethodType.Getter)]
	public static class TorpedoesHaveShellTab
	{
		[HarmonyPostfix]
		public static void CheckThisOneThree(ITab_Shells __instance, ref IStoreSettingsParent __result)
		{
			Building_ShipTurret building_TurretGun = Find.Selector.SingleSelectedObject as Building_ShipTurret;
			if (building_TurretGun != null)
			{
				__result = (IStoreSettingsParent)typeof(ITab_Storage)
					.GetMethod("GetThingOrThingCompStoreSettingsParent",
						BindingFlags.Instance | BindingFlags.NonPublic)
					.Invoke(__instance, new object[] { building_TurretGun.gun });
				return;
			}
		}
	}

	[HarmonyPatch(typeof(Projectile), "CheckForFreeInterceptBetween")]
	public static class OnePointThreeSpaceProjectiles
	{
		public static void Postfix(Projectile __instance, ref bool __result)
		{
			if (__instance is Projectile_SoSFake)
				__result = false;
		}
	}
	
	[HarmonyPatch(typeof(Projectile), "Launch")]
	[HarmonyPatch(new Type[] {
		typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo),
		typeof(ProjectileHitFlags), typeof(bool), typeof(Thing), typeof(ThingDef)
	})]
	public static class TransferAmplifyBonus
	{
		[HarmonyPostfix]
		public static void OneMoreFactor(Projectile __instance, Thing equipment)
		{
			if (__instance is Projectile_ExplosiveShipCombat && equipment is Building_ShipTurret &&
				((Building_ShipTurret)equipment).AmplifierDamageBonus > 0)
			{
				typeof(Projectile)
					.GetField("weaponDamageMultiplier", BindingFlags.NonPublic | BindingFlags.Instance)
					.SetValue(__instance, 1 + ((Building_ShipTurret)equipment).AmplifierDamageBonus);
			}
		}
	}

	//buildings
	[HarmonyPatch(typeof(Building), "Destroy")]
	public static class NotifyCombatManager
	{
		[HarmonyPrefix]
		public static bool ShipPartIsDestroyed(Building __instance, DestroyMode mode, out Tuple<IntVec3, Faction, Map> __state)
		{
			__state = null;
			//only print or foam if destroyed normally
			if (!(mode == DestroyMode.KillFinalize || mode == DestroyMode.KillFinalizeLeavingsOnly))
				return true;
			if (!__instance.def.CanHaveFaction || __instance is Frame)
				return true;
			var mapComp = __instance.Map.GetComponent<ShipHeatMapComp>();
			if (!mapComp.InCombat)
				return true;
			mapComp.DirtyShip(__instance);
			if (__instance.def.blueprintDef != null)
			{
				if (mapComp.HullFoamDistributors.Count > 0 && (__instance.TryGetComp<CompSoShipPart>()?.Props.isHull ?? false))
				{
					foreach (CompHullFoamDistributor dist in mapComp.HullFoamDistributors)
					{
						if (dist.parent.TryGetComp<CompRefuelable>().Fuel > 0 && dist.parent.TryGetComp<CompPowerTrader>().PowerOn)
						{
							dist.parent.TryGetComp<CompRefuelable>().ConsumeFuel(1);
							__state = new Tuple<IntVec3, Faction, Map>(__instance.Position, __instance.Faction, __instance.Map);
							return true;
						}
					}
				}
				if (__instance.Faction == Faction.OfPlayer)
					GenConstruct.PlaceBlueprintForBuild(__instance.def, __instance.Position, __instance.Map,
					__instance.Rotation, Faction.OfPlayer, __instance.Stuff);
			}
			return true;
		}
		
		[HarmonyPostfix]
		public static void ReplaceShipPart(Tuple<IntVec3, Faction, Map> __state)
		{
			if (__state != null)
			{
				Thing newWall = ThingMaker.MakeThing(ThingDef.Named("HullFoamWall"));
				newWall.SetFaction(__state.Item2);
				GenPlace.TryPlaceThing(newWall, __state.Item1, __state.Item3, ThingPlaceMode.Direct);
			}
		}
	}

	[HarmonyPatch(typeof(SectionLayer_BuildingsDamage), "PrintDamageVisualsFrom")]
	public class FixBuildingDraw
	{
		public static bool Prefix(Building b)
		{
			if (b.Map == null)
				return false;
			return true;
		}
	}

	[HarmonyPatch(typeof(Room), "Notify_ContainedThingSpawnedOrDespawned")]
	public static class AirlockBugFix
	{
		[HarmonyPrefix]
		public static bool FixTheAirlockBug(Room __instance, ref bool ___statsAndRoleDirty)
		{
			if (ShipInteriorMod2.AirlockBugFlag)
			{
				___statsAndRoleDirty = true;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Building_Turret), "PreApplyDamage")]
	public static class HardpointsHelpTurrets
	{
		public static bool Prefix(ref DamageInfo dinfo, Building_Turret __instance)
		{
			ThingWithComps t = __instance.Position.GetFirstThingWithComp<CompRoofMe>(__instance.Map);
			if (t != null && !t.GetComp<CompRoofMe>().Props.roof && !t.GetComp<CompRoofMe>().Props.wreckage)
				dinfo.SetAmount(dinfo.Amount / 2);
			return true;
		}
	}

	[HarmonyPatch(typeof(ThingListGroupHelper), "Includes")]
	public static class ReactorsCanBeRefueled
	{
		[HarmonyPostfix]
		public static void CheckClass(ThingRequestGroup group, ThingDef def, ref bool __result)
		{
			if (group == ThingRequestGroup.Refuelable && def.HasComp(typeof(CompRefuelableOverdrivable)))
				__result = true;
		}
	}

	[HarmonyPatch(typeof(CompPower))]
	[HarmonyPatch("PowerNet", MethodType.Getter)]
	public static class FixPowerBug
	{
		public static void Postfix(CompPower __instance, ref PowerNet __result)
		{
			if (!(__instance.parent.ParentHolder is MinifiedThing) && __instance.Props.transmitsPower && __result == null && __instance.parent.Map.GetComponent<ShipHeatMapComp>().InCombat)
			{
				__instance.transNet = __instance.parent.Map.powerNetGrid.TransmittedPowerNetAt(__instance.parent.Position);
				if (__instance.transNet != null)
				{
					__instance.transNet.connectors.Add(__instance);
					if (__instance is CompPowerBattery)
						__instance.transNet.batteryComps.Add((CompPowerBattery)__instance);
					else if (__instance is CompPowerTrader)
						__instance.transNet.powerComps.Add((CompPowerTrader)__instance);
					__result = __instance.transNet;
				}
			}
		}
	}

	// This patch is applied manually in ShipInteriorMod2.Initialize.
	public static class NoShortCircuitCapacitors
	{
		[HarmonyPrefix]
		public static bool disableEventQuestionMark(Building culprit, out bool __state)
		{
			__state = false;
			PowerNet powerNet = culprit.PowerComp.PowerNet;
			if (powerNet.batteryComps.Any((CompPowerBattery x) =>
				x.parent.def == ThingDef.Named("ShipCapacitor") || x.parent.def == ThingDef.Named("ShipCapacitorSmall")))
			{
				__state = true;
				return false;
			}
			return true;
		}

		[HarmonyPostfix]
		public static void tellThePlayerTheDayWasSaved(Building culprit, bool __state)
		{
			if (__state)
			{
				Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("LetterLabelShortCircuit"), TranslatorFormattedStringExtensions.Translate("LetterLabelShortCircuitShipDesc"),
					LetterDefOf.NegativeEvent, new TargetInfo(culprit.Position, culprit.Map, false), null);
			}
		}
	}

	[HarmonyPatch(typeof(GenSpawn), "SpawningWipes")]
	public static class ConduitWipe
	{
		[HarmonyPostfix]
		public static void PerhapsNoConduitHere(ref bool __result, BuildableDef newEntDef, BuildableDef oldEntDef)
		{
			ThingDef newDef = newEntDef as ThingDef;
			if (oldEntDef.defName == "ShipHeatConduit")
			{
				if (newDef != null)
				{
					foreach (CompProperties comp in newDef.comps)
					{
						if (comp is CompProperties_ShipHeat)
							__result = true;
					}
				}
			}
		}
	}
	
	[HarmonyPatch(typeof(CompScanner))]
	[HarmonyPatch("CanUseNow", MethodType.Getter)]
	public static class NoUseInSpace
	{
		[HarmonyPostfix]
		public static bool Postfix(bool __result, CompScanner __instance)
		{
			if (__instance.parent.Map.IsSpace())
				return false;
			return __result;
		}
	}

	[HarmonyPatch(typeof(Building))]
	[HarmonyPatch("MaxItemsInCell", MethodType.Getter)]
	public static class DisableForMoveShelf
	{
		[HarmonyPostfix]
		public static int Postfix(int __result, Building __instance)
		{
			if (__result > 1 && ShipInteriorMod2.AirlockBugFlag)
				return 1;
			return __result;
		}
	}

	[HarmonyPatch(typeof(CompGenepackContainer), "EjectContents")]
	public static class DisableForMoveGene
	{
		[HarmonyPrefix]
		public static bool Prefix()
		{
			if (ShipInteriorMod2.AirlockBugFlag)
				return false;
			return true;
		}
	}
	
	[HarmonyPatch(typeof(CompThingContainer), "PostDeSpawn")]
	public static class DisableForMoveContainer
	{
		[HarmonyPrefix]
		public static bool Prefix()
		{
			if (ShipInteriorMod2.AirlockBugFlag)
				return false;
			return true;
		}
	}

	[HarmonyPatch(typeof(Building_MechGestator), "EjectContentsAndRemovePawns")]
	public static class DisableForMoveGestator
	{
		[HarmonyPrefix]
		public static bool Prefix()
		{
			if (ShipInteriorMod2.AirlockBugFlag)
				return false;
			return true;
		}
	}

	[HarmonyPatch(typeof(CompWasteProducer), "ProduceWaste")]
	public static class DisableForMoveWaste
	{
		[HarmonyPrefix]
		public static bool Prefix()
		{
			if (ShipInteriorMod2.AirlockBugFlag)
				return false;
			return true;
		}
	}

	[HarmonyPatch(typeof(CompDeathrestBindable), "PostDeSpawn")]
	public static class DisableForMoveDeath
	{
		[HarmonyPrefix]
		public static bool Prefix()
		{
			if (ShipInteriorMod2.AirlockBugFlag)
				return false;
			return true;
		}
	}
	//td rem this if patch bellow works
	/*[HarmonyPatch(typeof(Building_MechCharger), "DeSpawn")]
	public static class DisableForMoveCharger
	{
		[HarmonyPrefix]
		public static bool Prefix(ref Pawn ___currentlyChargingMech, out Pawn __state)
		{
			__state = null;
			if (ShipInteriorMod2.AirlockBugFlag)
			{
				__state = ___currentlyChargingMech;
				___currentlyChargingMech = null;
			}
			return true;
		}
		[HarmonyPostfix]
		public static void Postfix(ref Pawn ___currentlyChargingMech, Pawn __state)
		{
			if (ShipInteriorMod2.AirlockBugFlag)
			{
				___currentlyChargingMech = __state;
			}
		}
	}*/
	[HarmonyPatch]
	public class PatchCharger
	{
		[HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
		[HarmonyPatch(typeof(Building), "DeSpawn")]
		public static void Test(object instance, DestroyMode mode)
		{
		}
	}
	[HarmonyPatch(typeof(Building_MechCharger), "DeSpawn")]
	public static class DisableForMoveCharger
	{
		[HarmonyPrefix]
		public static bool Prefix(Building_MechCharger __instance, DestroyMode mode)
		{
			if (ShipInteriorMod2.AirlockBugFlag)
			{
				PatchCharger.Test(__instance, mode);
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch]
	public class PatchGrower
	{
		[HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
		[HarmonyPatch(typeof(Building), "DeSpawn")]
		public static void Test(object instance, DestroyMode mode)
		{
		}
	}
	[HarmonyPatch(typeof(Building_PlantGrower), "DeSpawn")]
	public static class DisableForMoveGrower
	{
		[HarmonyPrefix]
		public static bool Prefix(Building_PlantGrower __instance, DestroyMode mode)
		{
			if (ShipInteriorMod2.AirlockBugFlag)
			{
				PatchGrower.Test(__instance, mode);
				return false;
			}
			return true;
		}
	}

	//crypto
	[HarmonyPatch(typeof(Building_CryptosleepCasket), "FindCryptosleepCasketFor")]
	public static class AllowCrittersleepCaskets
	{
		[HarmonyPrefix]
		public static bool BlockExecution()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void CrittersCanSleepToo(ref Building_CryptosleepCasket __result, Pawn p, Pawn traveler,
			bool ignoreOtherReservations = false)
		{
			foreach (var current in GetCryptosleepDefs())
			{
				if (current == ThingDef.Named("Cryptonest"))
					continue;
				var building_CryptosleepCasket =
					(Building_CryptosleepCasket)GenClosest.ClosestThingReachable(p.Position, p.Map,
						ThingRequest.ForDef(current), PathEndMode.InteractionCell,
						TraverseParms.For(traveler), 9999f,
						delegate (Thing x) {
							bool arg_33_0;
							if (x.def.defName == "CrittersleepCasket" &&
								p.BodySize <= ShipInteriorMod2.crittersleepBodySize &&
								((ThingOwner)typeof(Building_CryptosleepCasket)
									.GetField("innerContainer", BindingFlags.NonPublic | BindingFlags.Instance)
									.GetValue((Building_CryptosleepCasket)x)).Count < 8 ||
								x.def.defName == "CrittersleepCasketLarge" &&
								p.BodySize <= ShipInteriorMod2.crittersleepBodySize &&
								((ThingOwner)typeof(Building_CryptosleepCasket)
									.GetField("innerContainer", BindingFlags.NonPublic | BindingFlags.Instance)
									.GetValue((Building_CryptosleepCasket)x)).Count < 32)
							{
								var traveler2 = traveler;
								LocalTargetInfo target = x;
								var ignoreOtherReservations2 = ignoreOtherReservations;
								arg_33_0 = traveler2.CanReserve(target, 1, -1, null, ignoreOtherReservations2);
							}
							else
							{
								arg_33_0 = false;
							}

							return arg_33_0;
						});
				if (building_CryptosleepCasket != null)
				{
					__result = building_CryptosleepCasket;
					return;
				}

				building_CryptosleepCasket = (Building_CryptosleepCasket)GenClosest.ClosestThingReachable(
					p.Position, p.Map, ThingRequest.ForDef(current), PathEndMode.InteractionCell,
					TraverseParms.For(traveler), 9999f,
					delegate (Thing x) {
						bool arg_33_0;
						if (x.def.defName != "CrittersleepCasketLarge" && x.def.defName != "CrittersleepCasket" &&
							!((Building_CryptosleepCasket)x).HasAnyContents)
						{
							var traveler2 = traveler;
							LocalTargetInfo target = x;
							var ignoreOtherReservations2 = ignoreOtherReservations;
							arg_33_0 = traveler2.CanReserve(target, 1, -1, null, ignoreOtherReservations2);
						}
						else
						{
							arg_33_0 = false;
						}

						return arg_33_0;
					});
				if (building_CryptosleepCasket != null) __result = building_CryptosleepCasket;
			}
		}

		private static IEnumerable<ThingDef> GetCryptosleepDefs()
		{
			return ModLister.HasActiveModWithName("PsiTech")
				? DefDatabase<ThingDef>.AllDefs.Where(def =>
					def != ThingDef.Named("PTPsychicTraier") &&
					typeof(Building_CryptosleepCasket).IsAssignableFrom(def.thingClass))
				: DefDatabase<ThingDef>.AllDefs.Where(def =>
					typeof(Building_CryptosleepCasket).IsAssignableFrom(def.thingClass));
		}
	}

	[HarmonyPatch(typeof(JobDriver_CarryToCryptosleepCasket), "MakeNewToils")]
	public static class JobDriverFix
	{
		[HarmonyPrefix]
		public static bool BlockExecution()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void FillThatCasket(ref IEnumerable<Toil> __result,
			JobDriver_CarryToCryptosleepCasket __instance)
		{
			Pawn Takee = (Pawn)typeof(JobDriver_CarryToCryptosleepCasket)
				.GetMethod("get_Takee", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(__instance, new object[0]);
			Building_CryptosleepCasket DropPod =
				(Building_CryptosleepCasket)typeof(JobDriver_CarryToCryptosleepCasket)
					.GetMethod("get_DropPod", BindingFlags.Instance | BindingFlags.NonPublic)
					.Invoke(__instance, new object[0]);
			List<Toil> myResult = new List<Toil>();
			__instance.FailOnDestroyedOrNull(TargetIndex.A);
			__instance.FailOnDestroyedOrNull(TargetIndex.B);
			__instance.FailOnAggroMentalState(TargetIndex.A);
			__instance.FailOn(() => !DropPod.Accepts(Takee));
			myResult.Add(Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell)
				.FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnDespawnedNullOrForbidden(TargetIndex.B)
				.FailOn(() =>
					(DropPod.def.defName != "CrittersleepCasket" &&
					 DropPod.def.defName != "CrittersleepCasketLarge") && DropPod.GetDirectlyHeldThings().Count > 0)
				.FailOn(() => !Takee.Downed)
				.FailOn(() =>
					!__instance.pawn.CanReach(Takee, PathEndMode.OnCell, Danger.Deadly, false, mode: TraverseMode.ByPawn))
				.FailOnSomeonePhysicallyInteracting(TargetIndex.A));
			myResult.Add(Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false));
			myResult.Add(Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell));
			Toil prepare = Toils_General.Wait(500);
			prepare.FailOnCannotTouch(TargetIndex.B, PathEndMode.InteractionCell);
			prepare.WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
			myResult.Add(prepare);
			myResult.Add(new Toil
			{
				initAction = delegate { DropPod.TryAcceptThing(Takee, true); },
				defaultCompleteMode = ToilCompleteMode.Instant
			});
			__result = myResult;
		}
	}

	[HarmonyPatch(typeof(FloatMenuMakerMap), "AddHumanlikeOrders")]
	public static class EggFix
	{
		[HarmonyPostfix]
		public static void FillThatNest(Vector3 clickPos, Pawn pawn, ref List<FloatMenuOption> opts)
		{
			if (pawn == null || clickPos == null)
				return;
			IntVec3 c = IntVec3.FromVector3(clickPos);
			if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
			{
				foreach (Thing current in c.GetThingList(pawn.Map))
				{
					if (current.def.IsWithinCategory(ThingCategoryDef.Named("EggsFertilized")) &&
						pawn.CanReserveAndReach(current, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true) &&
						findCryptonestFor(current, pawn, true) != null)
					{
						string text2 = "Carry to cryptonest";
						JobDef jDef = DefDatabase<JobDef>.GetNamed("CarryToCryptonest");
						Action action2 = delegate {
							Building_CryptosleepCasket building_CryptosleepCasket =
								findCryptonestFor(current, pawn, false);
							if (building_CryptosleepCasket == null)
							{
								building_CryptosleepCasket = findCryptonestFor(current, pawn, true);
							}

							if (building_CryptosleepCasket == null)
							{
								Messages.Message(
									TranslatorFormattedStringExtensions.Translate("CannotCarryToCryptosleepCasket") + ": " +
									TranslatorFormattedStringExtensions.Translate("NoCryptosleepCasket"), current, MessageTypeDefOf.RejectInput);
								return;
							}

							Job job = new Job(jDef, current, building_CryptosleepCasket);
							job.count = current.stackCount;
							int eggsAlreadyInNest =
								(typeof(Building_CryptosleepCasket)
									.GetField("innerContainer", BindingFlags.Instance | BindingFlags.NonPublic)
									.GetValue(building_CryptosleepCasket) as ThingOwner).Count;
							if (job.count + eggsAlreadyInNest > 16)
								job.count = 16 - eggsAlreadyInNest;
							pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
						};
						string label = text2;
						Action action = action2;
						opts.Add(FloatMenuUtility.DecoratePrioritizedTask(
							new FloatMenuOption(label, action, MenuOptionPriority.Default, null, current, 0f, null,
								null), pawn, current, "ReservedBy"));
					}
				}
			}
		}

		static Building_CryptosleepCasket findCryptonestFor(Thing egg, Pawn p, bool ignoreOtherReservations)
		{
			Building_CryptosleepCasket building_CryptosleepCasket =
				(Building_CryptosleepCasket)GenClosest.ClosestThingReachable(p.Position, p.Map,
					ThingRequest.ForDef(ThingDef.Named("Cryptonest")), PathEndMode.InteractionCell,
					TraverseParms.For(p, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, delegate (Thing x) {
						bool arg_33_0;
						if (((ThingOwner)typeof(Building_CryptosleepCasket)
							.GetField("innerContainer", BindingFlags.NonPublic | BindingFlags.Instance)
							.GetValue((Building_CryptosleepCasket)x)).TotalStackCount < 16)
						{
							LocalTargetInfo target = x;
							bool ignoreOtherReservations2 = ignoreOtherReservations;
							arg_33_0 = p.CanReserve(target, 1, -1, null, ignoreOtherReservations2);
						}
						else
						{
							arg_33_0 = false;
						}

						return arg_33_0;
					}, null, 0, -1, false, RegionType.Set_Passable, false);
			if (building_CryptosleepCasket != null)
			{
				return building_CryptosleepCasket;
			}

			return null;
		}
	}

	[HarmonyPatch(typeof(Building_Casket), "Tick")]
	public static class EggsDontHatch
	{
		[HarmonyPrefix]
		public static bool Nope(Building_Casket __instance)
		{
			if (__instance.def.defName.Equals("Cryptonest"))
			{
				List<ThingComp> comps = (List<ThingComp>)typeof(ThingWithComps)
					.GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
				if (comps != null)
				{
					int i = 0;
					int count = comps.Count;
					while (i < count)
					{
						comps[i].CompTick();
						i++;
					}
				}
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Building_CryptosleepCasket), "GetFloatMenuOptions")]
	public static class CantEnterCryptonest
	{
		[HarmonyPrefix]
		public static bool Nope(Building_CryptosleepCasket __instance)
		{
			if (__instance.def.defName.Equals("Cryptonest"))
			{
				return false;
			}
			return true;
		}

		[HarmonyPostfix]
		public static void AlsoNope(IEnumerable<FloatMenuOption> __result, Building_CryptosleepCasket __instance)
		{
			if (__instance.def.defName.Equals("Cryptonest"))
			{
				__result = new List<FloatMenuOption>();
			}
		}
	}

	[HarmonyPatch(typeof(Building_CryptosleepCasket), "TryAcceptThing")]
	public static class UpdateCasketGraphicsA
	{
		[HarmonyPostfix]
		public static void UpdateIt(Building_CryptosleepCasket __instance)
		{
			if (__instance.Map != null && __instance.Spawned)
				__instance.Map.mapDrawer.MapMeshDirty(__instance.Position,
					MapMeshFlag.Buildings | MapMeshFlag.Things);
		}
	}

	[HarmonyPatch(typeof(Building_CryptosleepCasket), "EjectContents")]
	public static class UpdateCasketGraphicsB
	{
		[HarmonyPostfix]
		public static void UpdateIt(Building_CryptosleepCasket __instance)
		{
			if (__instance.Map != null && __instance.Spawned)
				__instance.Map.mapDrawer.MapMeshDirty(__instance.Position,
					MapMeshFlag.Buildings | MapMeshFlag.Things);
		}
	}

	//EVA
	[HarmonyPatch(typeof(Pawn_PathFollower), "SetupMoveIntoNextCell")]
	public static class H_SpaceZoomies
	{
		[HarmonyPostfix]
		public static void GoFast(Pawn_PathFollower __instance, Pawn ___pawn)
		{
			if (___pawn.Map.terrainGrid.TerrainAt(__instance.nextCell) == ResourceBank.TerrainDefOf.EmptySpace &&
				ShipInteriorMod2.EVAlevel(___pawn)>6)
			{
				__instance.nextCellCostLeft /= 4;
				__instance.nextCellCostTotal /= 4;
			}
		}
	}
	[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelAdded))]
	public static class ApparelTracker_Notify_Added
	{
		internal static void Postfix(Pawn_ApparelTracker __instance)
		{
			Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == __instance?.pawn?.thingIDNumber);
		}
	}
	[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Notify_ApparelRemoved))]
	public static class ApparelTracker_Notify_Removed
	{
		internal static void Postfix(Pawn_ApparelTracker __instance)
		{
			Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == __instance?.pawn?.thingIDNumber);
		}
	}
	
	[HarmonyPatch(typeof(Recipe_InstallArtificialBodyPart), "ApplyOnPawn")]
	public static class LungInstall
	{
		internal static void Postfix(Pawn pawn, BodyPartRecord part, Recipe_InstallArtificialBodyPart __instance)
		{
			if (__instance.recipe.addsHediff.defName.Equals("SoSArchotechLung"))
				Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == pawn?.thingIDNumber);
		}
	}
	
	[HarmonyPatch(typeof(Recipe_RemoveBodyPart), "ApplyOnPawn")]
	public static class LungRemove
	{
		internal static void Postfix(Pawn pawn, BodyPartRecord part)
		{
			if (part.def.defName.Equals("SoSArchotechLung"))
				Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == pawn?.thingIDNumber);
		}
	}
	
	[HarmonyPatch(typeof(Recipe_InstallImplant), "ApplyOnPawn")]
	public static class SkinInstall
	{
		internal static void Postfix(Pawn pawn, BodyPartRecord part, Recipe_InstallImplant __instance)
		{
			if (__instance.recipe.addsHediff.defName.Equals("SoSArchotechSkin"))
				Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == pawn?.thingIDNumber);
		}
	}
	
	[HarmonyPatch(typeof(Recipe_RemoveImplant), "ApplyOnPawn")]
	public static class SkinRemove
	{
		internal static void Postfix(Pawn pawn, BodyPartRecord part)
		{
			if (part.def.defName.Equals("SoSArchotechSkin"))
				Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == pawn?.thingIDNumber);
		}
	}
	
	[HarmonyPatch(typeof(Pawn), "Kill")]
	public static class DeathRemove
	{
		internal static void Postfix(Pawn __instance)
		{
			Find.World.GetComponent<PastWorldUWO2>().PawnsInSpaceCache.RemoveAll(p => p.Key == __instance.thingIDNumber);
		}
	}

	//pawns
	[HarmonyPatch(typeof(PreceptComp_Apparel), "GiveApparelToPawn")]
	public static class PreventIdeoApparel
	{
		[HarmonyPrefix]
		public static bool Nope(Pawn pawn)
		{
			if (pawn.kindDef.defName.Contains("Space"))
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(PawnRelationWorker), "CreateRelation")]
	public static class PreventRelations
	{
		[HarmonyPrefix]
		public static bool Nope(Pawn generated, Pawn other)
		{
			if (!generated.RaceProps.Humanlike || !other.RaceProps.Humanlike || generated.kindDef.defName.Contains("Space") || other.kindDef.defName.Contains("Space"))
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Trigger_UrgentlyHungry), "ActivateOn")]
	public static class MechsDontEat
	{
		[HarmonyPrefix]
		public static bool DisableMaybe(Lord lord, out bool __state)
		{
			__state = false;
			foreach (Pawn p in lord.ownedPawns)
			{
				if (p.RaceProps.IsMechanoid)
				{
					__state = true;
					return false;
				}
			}
			return true;
		}

		[HarmonyPostfix]
		public static void Okay(ref bool __result, bool __state)
		{
			if (__state)
				__result = false;
		}
	}

	[HarmonyPatch(typeof(TransferableUtility), "CanStack")]
	public static class MechsCannotStack
	{
		[HarmonyPrefix]
		public static bool Nope(Thing thing, ref bool __result)
		{
			if (thing is Pawn && ((Pawn)thing).RaceProps.IsMechanoid)
			{
				__result = false;
				return false;
			}

			return true;
		}
	}

	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	public static class AnimalsHaveGizmosToo
	{
		public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance.TryGetComp<CompArcholife>() != null)
			{
				List<Gizmo> giz = new List<Gizmo>();
				giz.AddRange(__result);
				giz.AddRange(__instance.TryGetComp<CompArcholife>().CompGetGizmosExtra());
				__result = giz;
			}
		}
	}

	[HarmonyPatch(typeof(CompSpawnerPawn), "TrySpawnPawn")]
	public static class SpaceCreaturesAreHungry
	{
		[HarmonyPostfix]
		public static void HungerLevel(ref Pawn pawn, bool __result)
		{
			if (__result && (pawn?.Map?.IsSpace() ?? false) && pawn.needs?.food?.CurLevel != null)
				pawn.needs.food.CurLevel = 0.2f;
		}
	}

	[HarmonyPatch(typeof(Pawn_FilthTracker), "GainFilth", new Type[] { typeof(ThingDef), typeof(IEnumerable<string>) })]
	public static class RadioactiveAshIsRadioactive
	{
		[HarmonyPostfix]
		public static void OhNoISteppedInIt(ThingDef filthDef, Pawn_FilthTracker __instance)
		{
			if (filthDef.defName.Equals("Filth_SpaceReactorAsh"))
			{
				Pawn p = (Pawn)typeof(Pawn_FilthTracker)
					.GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
				int damage = Rand.RangeInclusive(1, 2);
				p.TakeDamage(new DamageInfo(DamageDefOf.Burn, damage));
				float num = 0.025f;
				num *= (1 - p.GetStatValue(StatDefOf.ToxicResistance, true));
				if (num != 0f)
				{
					HealthUtility.AdjustSeverity(p, HediffDefOf.ToxicBuildup, num);
				}
			}
		}
	}

	[HarmonyPatch(typeof(MapPawns))]
	[HarmonyPatch("AllPawns", MethodType.Getter)]
	public class FixCaravanThreading
	{
		public static void Postfix(ref List<Pawn> __result)
		{
			__result = __result.ListFullCopy();
		}
	}

	[HarmonyPatch(typeof(Pawn_MindState), "Notify_DamageTaken")]
	public static class ShipTurretIsNull
	{
		[HarmonyPrefix]
		public static bool AnimalsFlee(DamageInfo dinfo, Pawn_MindState __instance)
		{
			if (dinfo.Instigator is Building_ShipTurret)
			{
				if (Traverse.Create<Pawn_MindState>().Method("CanStartFleeingBecauseOfPawnAction", __instance.pawn).GetValue<bool>())
				{
					__instance.StartFleeingBecauseOfPawnAction(dinfo.Instigator);
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(RCellFinder), "CanSelfShutdown")]
	public static class AllowMechSleepShipFloor
	{
		public static bool Prefix(ref bool __result, Pawn pawn, IntVec3 c, Map map, bool allowForbidden)
		{
			if (c.GetFirstBuilding(map) != null && (c.GetFirstBuilding(map).TryGetComp<CompSoShipPart>()?.Props.isPlating ?? false))
			{
				//check all except building
				__result = true;
				if (!pawn.CanReserve(c, 1, -1, null, false))
				{
					__result = false;
					return false;
				}
				if (!pawn.CanReach(c, PathEndMode.OnCell, Danger.Some, false, false, TraverseMode.ByPawn))
				{
					__result = false;
					return false;
				}
				if (!c.Standable(map))
				{
					__result = false;
					return false;
				}
				if (!allowForbidden && c.IsForbidden(pawn))
				{
					__result = false;
					return false;
				}
				Room room = c.GetRoom(map);
				if (room != null && room.IsPrisonCell)
				{
					__result = false;
					return false;
				}
				for (int i = 0; i < GenAdj.CardinalDirections.Length; i++)
				{
					List<Thing> thingList = (c + GenAdj.CardinalDirections[i]).GetThingList(map);
					for (int j = 0; j < thingList.Count; j++)
					{
						if (thingList[j].def.hasInteractionCell && thingList[j].InteractionCell == c)
						{
							__result = false;
							return false;
						}
					}
				}
				return false;
			}
			return true;
		}
	}
	
	//mechanite "fire"
	[HarmonyPatch(typeof(Fire), "TrySpread")]
	public static class SpreadMechanites
	{
		public static bool Prefix(Fire __instance)
		{
			if (__instance is MechaniteFire)
				return false;
			return true;
		}

		public static void Postfix(Fire __instance)
		{
			if (__instance is MechaniteFire)
			{
				IntVec3 position = __instance.Position;
				bool flag;
				if (Rand.Chance(0.8f))
				{
					position = __instance.Position + GenRadial.ManualRadialPattern[Rand.RangeInclusive(1, 8)];
					flag = true;
				}
				else
				{
					position = __instance.Position + GenRadial.ManualRadialPattern[Rand.RangeInclusive(10, 20)];
					flag = false;
				}
				if (!position.InBounds(__instance.Map))
				{
					return;
				}
				if (!flag)
				{
					CellRect startRect = CellRect.SingleCell(__instance.Position);
					CellRect endRect = CellRect.SingleCell(position);
					if (GenSight.LineOfSight(__instance.Position, position, __instance.Map, startRect, endRect))
					{
						((MechaniteSpark)GenSpawn.Spawn(ThingDef.Named("MechaniteSpark"), __instance.Position, __instance.Map)).Launch(__instance, position, position, ProjectileHitFlags.All);
					}
				}
				else
				{
					MechaniteFire existingFire = position.GetFirstThing<MechaniteFire>(__instance.Map);
					if (existingFire != null)
					{
						existingFire.fireSize += 0.1f;
					}
					else
					{
						MechaniteFire obj = (MechaniteFire)ThingMaker.MakeThing(ResourceBank.ThingDefOf.MechaniteFire);
						obj.fireSize = Rand.Range(0.1f, 0.2f);
						GenSpawn.Spawn(obj, position, __instance.Map, Rot4.North);
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(Fire), "DoComplexCalcs")]
	public static class ComplexFlammability
	{
		public static bool Prefix(Fire __instance)
		{
			if (__instance is MechaniteFire)
				return false;
			return true;
		}
		public static void Postfix(Fire __instance)
		{
			if (__instance is MechaniteFire)
			{
				bool flag = false;
				List<Thing> flammableList = new List<Thing>();
				if (__instance.parent == null)
				{
					List<Thing> list = __instance.Map.thingGrid.ThingsListAt(__instance.Position);
					for (int i = 0; i < list.Count; i++)
					{
						Thing thing = list[i];
						if (thing is Building_Door)
						{
							flag = true;
						}
						if (!(thing is MechaniteFire) && thing.def.useHitPoints)
						{
							flammableList.Add(list[i]);
							if (__instance.parent == null && __instance.fireSize > 0.4f && list[i].def.category == ThingCategory.Pawn && Rand.Chance(FireUtility.ChanceToAttachFireCumulative(list[i], 150f)))
							{
								list[i].TryAttachFire(__instance.fireSize * 0.2f);
							}
						}
					}
				}
				else
				{
					flammableList.Add(__instance.parent);
				}
				if (flammableList.Count == 0 && __instance.Position.GetTerrain(__instance.Map).extinguishesFire)
				{
					__instance.Destroy();
					return;
				}
				Thing thing2 = (__instance.parent != null) ? __instance.parent : ((flammableList.Count <= 0) ? null : flammableList.RandomElement());
				if (thing2 != null && (!(__instance.fireSize < 0.4f) || thing2 == __instance.parent || thing2.def.category != ThingCategory.Pawn))
				{
					IntVec3 pos = __instance.Position;
					Map map = __instance.Map;
					((MechaniteFire)__instance).DoFireDamage(thing2);
					if (thing2.Destroyed)
						GenExplosion.DoExplosion(pos, map, 1.9f, DefDatabase<DamageDef>.GetNamed("BombMechanite"), null);
				}
				if (__instance.Spawned)
				{
					float num = __instance.fireSize * 16f;
					if (flag)
					{
						num *= 0.15f;
					}
					GenTemperature.PushHeat(__instance.Position, __instance.Map, num);
					if (Rand.Value < 0.4f)
					{
						float radius = __instance.fireSize * 3f;
						SnowUtility.AddSnowRadial(__instance.Position, __instance.Map, radius, 0f - __instance.fireSize * 0.1f);
					}
					__instance.fireSize += 0.1f;
					if (__instance.fireSize > 1.75f)
					{
						__instance.fireSize = 1.75f;
					}
					if (__instance.Map.weatherManager.RainRate > 0.01f && Rand.Value < 6f)
					{
						__instance.TakeDamage(new DamageInfo(DamageDefOf.Extinguish, 10f));
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(ThingOwner), "NotifyAdded")]
	public static class FixFireBugA
	{
		public static void Postfix(Thing item)
		{
			if (item.HasAttachment(ResourceBank.ThingDefOf.MechaniteFire))
			{
				item.GetAttachment(ResourceBank.ThingDefOf.MechaniteFire).Destroy();
			}
		}
	}

	[HarmonyPatch(typeof(Pawn_JobTracker), "IsCurrentJobPlayerInterruptible")]
	public static class FixFireBugB
	{
		public static void Postfix(Pawn_JobTracker __instance, ref bool __result)
		{
			if (((Pawn)(typeof(Pawn_JobTracker).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance))).HasAttachment(ResourceBank.ThingDefOf.MechaniteFire))
			{
				__result = false;
			}
		}
	}

	//[HarmonyPatch(typeof(JobGiver_FightFiresNearPoint),"TryGiveJob")]
	public class FixFireBugC //Manually patched since *someone* made this an internal class!
	{
		public void Postfix(ref Job __result, Pawn pawn)
		{
			Thing thing = GenClosest.ClosestThingReachable(pawn.GetLord().CurLordToil.FlagLoc, pawn.Map, ThingRequest.ForDef(ResourceBank.ThingDefOf.MechaniteFire), PathEndMode.Touch, TraverseParms.For(pawn), 25);
			if (thing != null)
			{
				__result = JobMaker.MakeJob(JobDefOf.BeatFire, thing);
			}
		}
	}

	[HarmonyPatch(typeof(JobGiver_ExtinguishSelf), "TryGiveJob")]
	public static class FixFireBugD
	{
		public static void Postfix(Pawn pawn, ref Job __result)
		{
			if (Rand.Value < 0.1f)
			{
				Fire fire = (Fire)pawn.GetAttachment(ResourceBank.ThingDefOf.MechaniteFire);
				if (fire != null)
				{
					__result = JobMaker.MakeJob(JobDefOf.ExtinguishSelf, fire);
				}
			}
		}
	}

	[HarmonyPatch(typeof(ThinkNode_ConditionalBurning), "Satisfied")]
	public static class FixFireBugE
	{
		public static void Postfix(Pawn pawn, ref bool __result)
		{
			__result = __result || pawn.HasAttachment(ResourceBank.ThingDefOf.MechaniteFire);
		}
	}

	[HarmonyPatch(typeof(Fire), "SpawnSmokeParticles")]
	public static class FixFireBugF
	{
		public static bool Prefix(Fire __instance)
		{
			return !(__instance is MechaniteFire);
		}
	}

	//archo
	[HarmonyPatch(typeof(IncidentWorker_FarmAnimalsWanderIn), "TryFindRandomPawnKind")]
	public static class NoArchoCritters
	{
		public static void Postfix(ref PawnKindDef kind, ref bool __result, Map map)
		{
			__result = DefDatabase<PawnKindDef>.AllDefs.Where((PawnKindDef x) => x.RaceProps.Animal && x.RaceProps.wildness < 0.35f && (!x.race.tradeTags?.Contains("AnimalInsectSpace") ?? true) && map.mapTemperature.SeasonAndOutdoorTemperatureAcceptableFor(x.race)).TryRandomElementByWeight((PawnKindDef k) => 0.420000017f - k.RaceProps.wildness, out kind);
		}
	}

	[HarmonyPatch(typeof(ScenPart_StartingAnimal), "RandomPets")]
	public static class NoArchotechPets
	{
		public static void Postfix(ref IEnumerable<PawnKindDef> __result)
		{
			List<PawnKindDef> newResult = new List<PawnKindDef>();
			foreach (PawnKindDef def in __result)
			{
				if (!def.race.HasComp(typeof(CompArcholife)))
					newResult.Add(def);
			}
			__result = newResult;
		}
	}

	[HarmonyPatch(typeof(MainTabWindow_Research), "PostOpen")]
	public static class HideArchoStuff
	{
		public static void Postfix(MainTabWindow_Research __instance)
		{
			if (!WorldSwitchUtility.PastWorldTracker.Unlocks.Contains("ArchotechUplink"))
			{
				IEnumerable tabs = (IEnumerable)typeof(MainTabWindow_Research).GetField("tabs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
				TabRecord archoTab = null;
				foreach (TabRecord tab in tabs)
				{
					if (tab.label.Equals("Archotech"))
						archoTab = tab;
				}
				tabs.GetType().GetMethod("Remove").Invoke(tabs, new object[] { archoTab });
			}
		}
	}

	[HarmonyPatch(typeof(Widgets), "RadioButtonLabeled")]
	public static class HideArchoStuffToo
	{
		public static bool Prefix(string labelText)
		{
			if (labelText.Equals("Sacrifice to archotech spore") && !WorldSwitchUtility.PastWorldTracker.Unlocks.Contains("ArchotechUplink"))
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(MainTabWindow_Research), "DrawUnlockableHyperlinks")]
	public static class DrawArchotechGifts
	{
		public static void Postfix(ref float __result, ref Rect rect, ResearchProjectDef project)
		{
			float yMin = rect.yMin;
			bool first = false;
			foreach (ArchotechGiftDef def in DefDatabase<ArchotechGiftDef>.AllDefs)
			{
				if (def.research == project)
				{
					if (!first)
					{
						first = true;
						Widgets.LabelCacheHeight(ref rect, TranslatorFormattedStringExtensions.Translate("ArchoGift") + ":");
						rect.yMin += 24f;
					}
					Widgets.HyperlinkWithIcon(hyperlink: new Dialog_InfoCard.Hyperlink(def.thing), rect: new Rect(rect.x, rect.yMin, rect.width, 24f));
					rect.yMin += 24f;
				}
			}
			__result = rect.yMin - yMin + __result;
		}
	}

	[HarmonyPatch(typeof(JobDriver_Meditate), "MeditationTick")]
	public static class MeditateToArchotechs
	{
		public static void Postfix(JobDriver_Meditate __instance)
		{
			int num = GenRadial.NumCellsInRadius(MeditationUtility.FocusObjectSearchRadius);
			for (int i = 0; i < num; i++)
			{
				IntVec3 c = __instance.pawn.Position + GenRadial.RadialPattern[i];
				if (c.InBounds(__instance.pawn.Map))
				{
					Building_ArchotechSpore spore = c.GetFirstThing<Building_ArchotechSpore>(__instance.pawn.Map);
					if (spore != null)
					{
						spore.MeditationTick();
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(RitualObligationTargetWorker_GraveWithTarget), "LabelExtraPart")]
	public static class NoDeathSpam
	{
		public static bool Prefix(RitualObligation obligation)
		{
			return obligation.targetA.Thing != null && obligation.targetA.Thing is Corpse && ((Corpse)obligation.targetA.Thing).InnerPawn != null;

		}
	}

	[HarmonyPatch(typeof(RitualObligationTargetWorker_Altar), "GetTargetsWorker")]
	public static class ArchotechSporesAreHoly
	{
		public static void Postfix(RitualObligation obligation, Map map, Ideo ideo, ref IEnumerable<TargetInfo> __result)
		{
			if (ideo.memes.Contains(ResourceBank.MemeDefOf.Structure_Archist) && map.listerThings.ThingsOfDef(ResourceBank.ThingDefOf.ShipArchotechSpore).Any())
			{
				List<TargetInfo> newResult = new List<TargetInfo>();
				newResult.AddRange(__result);
				foreach (Thing spore in map.listerThings.ThingsOfDef(ResourceBank.ThingDefOf.ShipArchotechSpore))
				{
					newResult.Add(spore);
				}
				__result = newResult;
			}
		}
	}

	[HarmonyPatch(typeof(IdeoBuildingPresenceDemand), "BuildingPresent")]
	public static class ArchotechSporesCountAsAltars
	{
		public static void Postfix(ref bool __result, Map map, IdeoBuildingPresenceDemand __instance)
		{
			if (__instance.parent.ideo.memes.Contains(ResourceBank.MemeDefOf.Structure_Archist) && map.listerThings.ThingsOfDef(ResourceBank.ThingDefOf.ShipArchotechSpore).Any())
				__result = true;
		}
	}

	[HarmonyPatch(typeof(IdeoBuildingPresenceDemand), "RequirementsSatisfied")]
	public static class ArchotechSporesCountAsAltarsToo
	{
		public static void Postfix(ref bool __result, Map map, IdeoBuildingPresenceDemand __instance)
		{
			if (__instance.parent.ideo.memes.Contains(ResourceBank.MemeDefOf.Structure_Archist) && map.listerThings.ThingsOfDef(ResourceBank.ThingDefOf.ShipArchotechSpore).Any())
				__result = true;
		}
	}

	[HarmonyPatch(typeof(ExecutionUtility), "DoExecutionByCut")]
	public static class ArchotechSporesAbsorbBrains
	{
		public static void Postfix(Pawn victim)
		{
			Building_ArchotechSpore ArchotechSpore = victim.Corpse.Position.GetFirstThing<Building_ArchotechSpore>(victim.Corpse.Map);
			if (ArchotechSpore != null)
			{
				SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(Find.CurrentMap);
				FleckMaker.Static(ArchotechSpore.Position, victim.Corpse.Map, FleckDefOf.PsycastAreaEffect, 10f);
				victim.health.AddHediff(HediffDefOf.MissingBodyPart, victim.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).First());
				ArchotechSpore.AbsorbMind(victim);
			}
		}
	}

	[HarmonyPatch(typeof(FactionDialogMaker), "FactionDialogFor")]
	public static class AddArchoDialogOption
	{
		public static void Postfix(Pawn negotiator, Faction faction, ref DiaNode __result)
		{
			if (faction.def.CanEverBeNonHostile && Find.ResearchManager.GetProgress(ResearchProjectDef.Named("ArchotechBroadManipulation")) >= ResearchProjectDef.Named("ArchotechBroadManipulation").CostApparent)
			{
				Building_ArchotechSpore spore = null;
				foreach (Map map in Find.Maps)
				{
					if (map.IsSpace())
					{
						foreach (Thing t in map.spawnedThings)
						{
							if (t is Building_ArchotechSpore)
							{
								spore = (Building_ArchotechSpore)t;
								break;
							}
						}
					}
				}
				DiaOption increase = new DiaOption(TranslatorFormattedStringExtensions.Translate("ArchotechGoodwillPlus"));
				DiaOption decrease = new DiaOption(TranslatorFormattedStringExtensions.Translate("ArchotechGoodwillMinus"));
				increase.action = delegate
				{
					faction.TryAffectGoodwillWith(Faction.OfPlayer, 10, canSendMessage: false);
					spore.fieldStrength -= 3;
				};
				increase.linkLateBind = (() => FactionDialogMaker.FactionDialogFor(negotiator, faction));
				if (spore == null || spore.fieldStrength < 3)
				{
					increase.disabled = true;
					increase.disabledReason = "Insufficient psychic field strength";
				}
				decrease.action = delegate
				{
					faction.TryAffectGoodwillWith(Faction.OfPlayer, -10, canSendMessage: false);
					spore.fieldStrength -= 3;
				};
				decrease.linkLateBind = (() => FactionDialogMaker.FactionDialogFor(negotiator, faction));
				if (spore == null || spore.fieldStrength < 3)
				{
					decrease.disabled = true;
					decrease.disabledReason = "Insufficient psychic field strength";
				}
				if (spore != null)
				{
					__result.options.Add(increase);
					__result.options.Add(decrease);
				}
			}
		}
	}

	//ideology
	[HarmonyPatch(typeof(IdeoManager), "CanRemoveIdeo")]
	public static class IdeosDoNotDisappear
	{
		public static void Postfix(Ideo ideo, ref bool __result)
		{
			List<Faction> factions = (List<Faction>)typeof(FactionManager).GetField("allFactions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Find.FactionManager);
			foreach (Faction allFaction in factions)
			{
				if (allFaction.ideos != null && allFaction.ideos.AllIdeos.Contains(ideo))
				{
					__result = false;
					return;
				}
			}
		}
	}

	[HarmonyPatch(typeof(Scenario), "PostIdeoChosen")]
	public static class NotNowIdeology
	{
		public static bool ArchoFlag = false;

		public static bool Prefix()
		{
			if (ArchoFlag)
			{
				ArchoFlag = false;
				return false;
			}
			return true;
		}
	}

	// Formgels - simpler than holograms!
	[HarmonyPatch(typeof(Pawn),"Kill")]
	public static class CorpseRemoval
    {
		public static void Postfix(Pawn __instance)
        {
			if(ShipInteriorMod2.IsHologram(__instance))
            {
				if(__instance.Corpse!=null)
					__instance.Corpse.Destroy();
				if(!__instance.health.hediffSet.GetFirstHediff<HediffPawnIsHologram>().consciousnessSource.Destroyed)
					ResurrectionUtility.Resurrect(__instance);
            }
        }
    }

	[HarmonyPatch(typeof(ThoughtWorker_AgeReversalDemanded), "CanHaveThought")]
	public static class NoHologramAgeReversal
    {
		public static void Postfix(ref bool __result, Pawn pawn)
        {
			if (ShipInteriorMod2.IsHologram(pawn))
				__result = false;
        }
    }

	[HarmonyPatch(typeof(SkillRecord), "Interval")]
	public static class MachineHologramsPerfectMemory
    {
		public static bool Prefix(SkillRecord __instance)
        {
			return !ShipInteriorMod2.IsHologram(__instance.Pawn);
        }
    }

	[HarmonyPatch(typeof(Pawn_StoryTracker), "get_SkinColor")]
	public static class SkinColorPostfixPostfix
    {
		[HarmonyPriority(Priority.Last)]
		public static void Postfix(Pawn ___pawn, ref Color __result, Pawn_StoryTracker __instance)
        {
			if (ShipInteriorMod2.IsHologram(___pawn) && __instance.skinColorOverride.HasValue)
				__result = __instance.skinColorOverride.Value;
        }
    }

	[HarmonyPatch(typeof(Recipe_BloodTransfusion), "AvailableOnNow")]
	public static class FormgelsHaveNoBlood
	{
		public static void Postfix(ref bool __result, Thing thing)
		{
			if (thing is Pawn && ShipInteriorMod2.IsHologram(((Pawn)thing)))
				__result = false;
		}
	}

	[HarmonyPatch(typeof(Recipe_ExtractHemogen), "AvailableOnNow")]
	public static class FormgelsStillHaveNoBlood
	{
		public static void Postfix(ref bool __result, Thing thing)
		{
			if (thing is Pawn && ShipInteriorMod2.IsHologram(((Pawn)thing)))
				__result = false;
		}
	}

	[HarmonyPatch(typeof(Recipe_InstallArtificialBodyPart), "GetPartsToApplyOn")]
	public static class FormgelsCannotUseBionics
	{
		public static void Postfix(ref IEnumerable<BodyPartRecord> __result, Pawn pawn)
		{
			if (ShipInteriorMod2.IsHologram(pawn))
				__result = new List<BodyPartRecord>();
		}
	}

	[HarmonyPatch(typeof(Recipe_InstallImplant), "GetPartsToApplyOn")]
	public static class FormgelsCannotUseImplants
	{
		public static void Postfix(ref IEnumerable<BodyPartRecord> __result, Pawn pawn)
		{
			if (ShipInteriorMod2.IsHologram(pawn))
				__result = new List<BodyPartRecord>();
		}
	}

	[HarmonyPatch(typeof(Recipe_RemoveImplant), "GetPartsToApplyOn")]
	public static class FormgelsStillCannotUseImplants
	{
		public static void Postfix(ref IEnumerable<BodyPartRecord> __result, Pawn pawn)
		{
			if (ShipInteriorMod2.IsHologram(pawn))
				__result = new List<BodyPartRecord>();
		}
	}

	[HarmonyPatch(typeof(Recipe_InstallNaturalBodyPart), "GetPartsToApplyOn")]
	public static class FormgelsCannotUseOrgans
	{
		public static void Postfix(ref IEnumerable<BodyPartRecord> __result, Pawn pawn)
		{
			if (ShipInteriorMod2.IsHologram(pawn))
				__result = new List<BodyPartRecord>();
		}
	}

	[HarmonyPatch(typeof(Recipe_RemoveBodyPart), "GetPartsToApplyOn")]
	public static class FormgelsHaveNoOrgans
	{
		public static void Postfix(ref IEnumerable<BodyPartRecord> __result, Pawn pawn)
		{
			if (ShipInteriorMod2.IsHologram(pawn))
				__result = new List<BodyPartRecord>();
		}
	}

	[HarmonyPatch(typeof(GenStep_Fog), "Generate")]
	public static class UnfogVault
    {
		public static void Postfix(Map map)
        {
			foreach (Thing casket in map.listerThings.ThingsOfDef(ThingDef.Named("Ship_AvatarCasket")))
			{
				FloodFillerFog.FloodUnfog(casket.Position, map);
			}
		}
    }
	
	//storyteller
	[HarmonyPatch(typeof(Map), "get_PlayerWealthForStoryteller")]
	public static class TechIsWealth
	{
		static SimpleCurve wealthCurve = new SimpleCurve(new CurvePoint[] { new CurvePoint(0,0), new CurvePoint(3800,0), new CurvePoint(150000,400000f), new CurvePoint(420000,700000f), new CurvePoint(666666,1000000f)});
		static SimpleCurve componentCurve = new SimpleCurve(new CurvePoint[] { new CurvePoint(0,0), new CurvePoint(10,5000), new CurvePoint(100, 25000), new CurvePoint(1000, 150000) });

		public static void Postfix(Map __instance, ref float __result)
        {
			if (Find.Storyteller.def != ResourceBank.StorytellerDefOf.Sara)
				return;
			float num = ResearchToWealth();
			int numComponents = 0;
			foreach (Building building in __instance.listerBuildings.allBuildingsColonist.Where(b => b.def.costList != null))
			{
				if (building.def.costList.Any(tdc => tdc.thingDef == ThingDefOf.ComponentIndustrial))
					numComponents++;
				if (building.def.costList.Any(tdc => tdc.thingDef == ThingDefOf.ComponentSpacer))
					numComponents += 10;
			}
			num += componentCurve.Evaluate(numComponents);
			//Log.Message("Sara Spacer calculates threat points should be " + wealthCurve.Evaluate(num) + " based on " + ResearchToWealth() + " research and " + numComponents + " component-based buildings");
			__result = wealthCurve.Evaluate(num);
        }

		static float ResearchToWealth()
        {
			float num = 0;
			foreach(ResearchProjectDef proj in DefDatabase<ResearchProjectDef>.AllDefs)
            {
				if (proj.IsFinished)
					num += proj.baseCost;
			}
			if (num > 100000)
				num = 100000;
			return num;
        }
    }

	//progression
	[HarmonyPatch(typeof(Scenario))]
	[HarmonyPatch("Category", MethodType.Getter)]
	public static class FixThatBugInParticular
	{
		[HarmonyPrefix]
		public static bool NoLongerUndefined(Scenario __instance)
		{
			if (((ScenarioCategory)typeof(Scenario).GetField("categoryInt", BindingFlags.NonPublic | BindingFlags.Instance)
				.GetValue(__instance)) == ScenarioCategory.Undefined)
				typeof(Scenario).GetField("categoryInt", BindingFlags.NonPublic | BindingFlags.Instance)
				.SetValue(__instance, ScenarioCategory.CustomLocal);
			return true;
		}
	}

	[HarmonyPatch(typeof(MapParent), "RecalculateHibernatableIncidentTargets")]
	public static class GiveMeRaidsPlease
	{
		[HarmonyPostfix]
		public static void RaidsAreFunISwear(MapParent __instance)
		{
			HashSet<IncidentTargetTagDef> hibernatableIncidentTargets =
				(HashSet<IncidentTargetTagDef>)typeof(MapParent)
					.GetField("hibernatableIncidentTargets", BindingFlags.NonPublic | BindingFlags.Instance)
					.GetValue(__instance);
			foreach (ThingWithComps current in __instance.Map.listerThings
				.ThingsOfDef(ThingDef.Named("JTDriveSalvage")).OfType<ThingWithComps>())
			{
				CompHibernatableSoS compHibernatable = current.TryGetComp<CompHibernatableSoS>();
				if (compHibernatable != null && compHibernatable.State == HibernatableStateDefOf.Starting &&
					compHibernatable.Props.incidentTargetWhileStarting != null)
				{
					if (hibernatableIncidentTargets == null)
					{
						hibernatableIncidentTargets = new HashSet<IncidentTargetTagDef>();
					}

					hibernatableIncidentTargets.Add(compHibernatable.Props.incidentTargetWhileStarting);
				}
			}

			typeof(MapParent)
				.GetField("hibernatableIncidentTargets", BindingFlags.NonPublic | BindingFlags.Instance)
				.SetValue(__instance, hibernatableIncidentTargets);
		}
	}

	[HarmonyPatch(typeof(Designator_Build)), HarmonyPatch("Visible", MethodType.Getter)]
	public static class UnlockBuildings
	{
		[HarmonyPostfix]
		public static void Unlock(ref bool __result, Designator_Build __instance)
		{
			if (__instance.PlacingDef is ThingDef && ((ThingDef)__instance.PlacingDef).HasComp(typeof(CompSoSUnlock)))
			{
				if (WorldSwitchUtility.PastWorldTracker.Unlocks.Contains(((ThingDef)__instance.PlacingDef).GetCompProperties<CompProperties_SoSUnlock>().unlock) || DebugSettings.godMode)
					__result = true;
				else
					__result = false;
			}
		}
	}

	[HarmonyPatch(typeof(Page_SelectStartingSite), "CanDoNext")]
	public static class LetMeLandOnMyOwnBase
	{
		[HarmonyPrefix]
		public static bool Nope()
		{
			return false;
		}

		[HarmonyPostfix]
		public static void CanLandPlz(ref bool __result)
		{
			int selectedTile = Find.WorldInterface.SelectedTile;
			if (selectedTile < 0)
			{
				Messages.Message(TranslatorFormattedStringExtensions.Translate("MustSelectLandingSite"), MessageTypeDefOf.RejectInput);
				__result = false;
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder();
				if (!TileFinder.IsValidTileForNewSettlement(selectedTile, stringBuilder) &&
					(Find.World.worldObjects.SettlementAt(selectedTile) == null ||
					 Find.World.worldObjects.SettlementAt(selectedTile).Faction != Faction.OfPlayer))
				{
					Messages.Message(stringBuilder.ToString(), MessageTypeDefOf.RejectInput);
					__result = false;
				}
				else
				{
					Tile tile = Find.WorldGrid[selectedTile];
					__result = true;
				}
			}
		}
	}

	[HarmonyPatch(typeof(Page_SelectStartingSite), "ExtraOnGUI")]
	public class PatchStartingSite
	{
		public static void Postfix(Page_SelectStartingSite __instance)
		{
			if (Find.Scenario.AllParts.Any(part => part is ScenPart_StartInSpace))
			{
				Find.WorldInterface.SelectedTile = TileFinder.RandomStartingTile();
				typeof(Page_SelectStartingSite).GetMethod("DoNext", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { });
			}
		}
	}

	[HarmonyPatch(typeof(IncidentWorker_PsychicEmanation), "TryExecuteWorker")]
	public static class TogglePsychicAmplifierQuest
	{
		public static void Postfix(IncidentParms parms)
		{
			if (ShipInteriorMod2.ArchoStuffEnabled && !WorldSwitchUtility.PastWorldTracker.Unlocks.Contains("ArchotechSpore"))
			{
				Map spaceMap = null;
				foreach (Map map in Find.Maps)
				{
					if (map.IsSpace() && map.spawnedThings.Where(t => t.def == ThingDefOf.Ship_ComputerCore).Any())
						spaceMap = map;
				}
				if (spaceMap != null)
				{
					Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoSPsychicAmplifier"), TranslatorFormattedStringExtensions.Translate("SoSPsychicAmplifierDesc"), LetterDefOf.PositiveEvent);
					AttackableShip ship = new AttackableShip();
					ship.attackableShip = DefDatabase<EnemyShipDef>.GetNamed("MechPsychicAmp");
					ship.spaceNavyDef = DefDatabase<SpaceNavyDef>.GetNamed("Mechanoid_SpaceNavy");
					ship.shipFaction = Faction.OfMechanoids;
					spaceMap.passingShipManager.AddShip(ship);
				}
			}
		}
	}

	[HarmonyPatch(typeof(ResearchManager), "FinishProject")]
	public static class TriggerPillarMissions
	{
		public static void Postfix(ResearchProjectDef proj)
		{
			if (proj.defName.Equals("ArchotechPillarA"))
				WorldSwitchUtility.PastWorldTracker.Unlocks.Add("ArchotechPillarAMission"); //Handled in Building_ShipBridge
			else if (proj.defName.Equals("ArchotechPillarB"))
				WorldSwitchUtility.PastWorldTracker.Unlocks.Add("ArchotechPillarBMission"); //Handled in Building_ShipBridge
			else if (proj.defName.Equals("ArchotechPillarC"))
			{
				WorldSwitchUtility.PastWorldTracker.Unlocks.Add("ArchotechPillarCMission");
				ShipInteriorMod2.GenerateArchotechPillarCSite();
			}
			else if (proj.defName.Equals("ArchotechPillarD"))
			{
				WorldSwitchUtility.PastWorldTracker.Unlocks.Add("ArchotechPillarDMission");
				ShipInteriorMod2.GenerateArchotechPillarDSite();
			}
		}
	}

	[HarmonyPatch(typeof(Window), "PostClose")]
	public static class CreditsAreTheRealEnd
	{
		public static void Postfix(Window __instance)
		{
			if (__instance is Screen_Credits && ShipInteriorMod2.SoSWin)
			{
				ShipInteriorMod2.SoSWin = false;
				GenScene.GoToMainMenu();
			}
		}
	}

	//should be in vanilla RW section
	[HarmonyPatch(typeof(CompTempControl), "CompGetGizmosExtra")]
	public static class CannotControlEnemyRadiators
	{
		public static void Postfix(CompTempControl __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance.parent.Faction != Faction.OfPlayer)
				__result = new List<Gizmo>();
		}
	}

	[HarmonyPatch(typeof(CompLaunchable), "CompGetGizmosExtra")]
	public static class CannotControlEnemyPods
	{
		public static void Postfix(CompTempControl __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance.parent.Faction != Faction.OfPlayer)
				__result = new List<Gizmo>();
		}
	}

	[HarmonyPatch(typeof(CompTransporter), "CompGetGizmosExtra")]
	public static class CannotControlEnemyPodsB
	{
		public static void Postfix(CompTempControl __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance.parent.Faction != Faction.OfPlayer)
				__result = new List<Gizmo>();
		}
	}

	[HarmonyPatch(typeof(CompRefuelable), "CompGetGizmosExtra")]
	public static class CannotControlEnemyFuel
	{
		public static void Postfix(CompTempControl __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance.parent.Faction != Faction.OfPlayer)
				__result = new List<Gizmo>();
		}
	}

   //other
   [HarmonyPatch(typeof(Thing), "SmeltProducts")]
	public static class PerfectEfficiency
	{
		public static bool Prefix(float efficiency)
		{
			if (efficiency == 0)
				return false;
			return true;
		}

		public static void Postfix(float efficiency, ref IEnumerable<Thing> __result, Thing __instance)
		{
			if (efficiency == 0)
			{
				List<Thing> actualResult = new List<Thing>();
				List<ThingDefCountClass> costListAdj = __instance.def.CostListAdjusted(__instance.Stuff);
				for (int j = 0; j < costListAdj.Count; j++)
				{
					int num = GenMath.RoundRandom((float)costListAdj[j].count);
					if (num > 0)
					{
						Thing thing = ThingMaker.MakeThing(costListAdj[j].thingDef);
						thing.stackCount = num;
						actualResult.Add(thing);
					}
				}
				__result = actualResult;
			}
		}
	}
	/* disabled till fixed
	[HarmonyPatch(typeof(DamageWorker))]
	[HarmonyPatch("ExplosionCellsToHit", new Type[] { typeof(IntVec3), typeof(Map), typeof(float), typeof(IntVec3), typeof(IntVec3) })]
	public static class FasterExplosions
	{
		public static bool Prefix(Map map, float radius)
		{
			return !map.GetComponent<ShipHeatMapComp>().InCombat || radius > 25; //Ludicrously large explosions cause a stack overflow
		}

		public static void Postfix(ref IEnumerable<IntVec3> __result, DamageWorker __instance, IntVec3 center, Map map, float radius)
		{
			if (map.GetComponent<ShipHeatMapComp>().InCombat && radius <= 25)
			{
				HashSet<IntVec3> cells = new HashSet<IntVec3>();
				List<ExplosionCell> cellsToRun = new List<ExplosionCell>();
				cellsToRun.Add(new ExplosionCell(center, new bool[4], 0));
				ExplosionCell curCell;
				while (cellsToRun.Count > 0)
				{
					curCell = cellsToRun.Pop();
					cells.Add(curCell.pos);
					if (curCell.dist <= radius)
					{
						Building edifice = null;
						if (curCell.pos.InBounds(map))
							edifice = curCell.pos.GetEdifice(map);
						if (edifice != null && edifice.HitPoints >= __instance.def.defaultDamage / 2)
							continue;
						if (!curCell.checkedDir[0]) //up
						{
							bool[] newDir = (bool[])curCell.checkedDir.Clone();
							newDir[1] = true;
							cellsToRun.Add(new ExplosionCell(curCell.pos + new IntVec3(0, 0, 1), newDir, curCell.dist + 1));
						}
						if (!curCell.checkedDir[1]) //down
						{
							bool[] newDir = (bool[])curCell.checkedDir.Clone();
							newDir[0] = true;
							cellsToRun.Add(new ExplosionCell(curCell.pos + new IntVec3(0, 0, -1), newDir, curCell.dist + 1));
						}
						if (!curCell.checkedDir[2]) //right
						{
							bool[] newDir = (bool[])curCell.checkedDir.Clone();
							newDir[3] = true;
							cellsToRun.Add(new ExplosionCell(curCell.pos + new IntVec3(1, 0, 0), newDir, curCell.dist + 1));
						}
						if (!curCell.checkedDir[3]) //left
						{
							bool[] newDir = (bool[])curCell.checkedDir.Clone();
							newDir[2] = true;
							cellsToRun.Add(new ExplosionCell(curCell.pos + new IntVec3(-1, 0, 0), newDir, curCell.dist + 1));
						}
					}
				}
				__result = cells;
			}
		}

		public struct ExplosionCell
		{
			public IntVec3 pos;
			public bool[] checkedDir;
			public int dist;

			public ExplosionCell(IntVec3 myPos, bool[] myCheckedDir, int myDist)
			{
				checkedDir = myCheckedDir;
				pos = myPos;
				dist = myDist;
			}
		}
	}
	*/
	[HarmonyPatch(typeof(MapPawns), "DeRegisterPawn")]
	public class MapPawnRegisterPatch //PsiTech "patch"
	{
		public static bool Prefix(Pawn p)
		{
			//This patch does literally nothing... and yet, somehow, it fixes a compatibility issue with PsiTech. Weird, huh?
			return true;
		}
	}

	//This is the most horrible hack that has ever been hacked, it *MUST* be removed before release
	[HarmonyPatch(typeof(District),"get_Map")]
	public static class FixMapIssue
    {
		public static bool Prefix(District __instance)
        {
			return Find.Maps.Where(map => Find.Maps.IndexOf(map)==__instance.mapIndex).Count()>0;
        }

		public static void Postfix(District __instance, ref Map __result)
        {
			if (Find.Maps.Where(map => Find.Maps.IndexOf(map) == __instance.mapIndex).Count() <= 0)
				__result = Find.Maps.FirstOrDefault();
		}
	}

	//pointless as the quest should not fire in space at all since it spawns enemy pawns
	[HarmonyPatch(typeof(QuestNode_Root_ShuttleCrash_Rescue), "TryFindShuttleCrashPosition")]
	public static class CrashOnShuttleBay
	{
		public static void Postfix(Map map, Faction faction, IntVec2 size, ref IntVec3 spot, QuestNode_Root_ShuttleCrash_Rescue __instance)
		{
			if (map.Biome == ResourceBank.BiomeDefOf.OuterSpaceBiome)
			{
				foreach (Building landingSpot in map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("ShipShuttleBay")))
				{
					ShipLandingArea area = new ShipLandingArea(landingSpot.OccupiedRect(), map);
					area.RecalculateBlockingThing();
					if (area.FirstBlockingThing == null)
					{
						spot = area.CenterCell;
						return;
					}
				}
				foreach (Building landingSpot in map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("ShipShuttleBayLarge")))
				{
					ShipLandingArea area = new ShipLandingArea(landingSpot.OccupiedRect(), map);
					area.RecalculateBlockingThing();
					if (area.FirstBlockingThing == null)
					{
						spot = area.CenterCell;
						return;
					}
				}
				QuestPart raidPart = null;
				foreach (QuestPart part in QuestGen.quest.PartsListForReading)
				{
					if (part is QuestPart_PawnsArrive)
					{
						raidPart = part;
						break;
					}
				}
				if (raidPart != null)
					QuestGen.quest.RemovePart(raidPart);
			}
		}
	}

	public class ModSettings_SoS : ModSettings
	{
		public override void ExposeData()
		{
			Scribe_Values.Look(ref difficultySoS, "difficultySoS", 1.0);
			Scribe_Values.Look(ref frequencySoS, "frequencySoS", 1.0);
			Scribe_Values.Look(ref navyShipChance, "navyShipChance", 0.2);
			Scribe_Values.Look(ref fleetChance, "fleetChance", 0.3);

			Scribe_Values.Look(ref easyMode, "easyMode");
			Scribe_Values.Look(ref useVacuumPathfinding, "useVacuumPathfinding", true);
			Scribe_Values.Look(ref renderPlanet, "renderPlanet", true);
			Scribe_Values.Look(ref useSplashScreen, "useSplashScreen", true);

			Scribe_Values.Look(ref minTravelTime, "minTravelTime", 5);
			Scribe_Values.Look(ref maxTravelTime, "maxTravelTime", 100);
			Scribe_Values.Look(ref offsetUIx, "offsetUIx");
			Scribe_Values.Look(ref offsetUIy, "offsetUIy");
			base.ExposeData();
		}

		public static double difficultySoS = 1,
			frequencySoS = 1,
			navyShipChance = 0.2,
			fleetChance = 0.3;
		public static bool easyMode,
			useVacuumPathfinding = true,
			renderPlanet = true,
			useSplashScreen = true;
		public static int minTravelTime = 5,
			maxTravelTime = 100,
			offsetUIx,
			offsetUIy;
	}

	/*[HarmonyPatch(typeof(CompShipPart),"PostSpawnSetup")]
	public static class RemoveVacuum{
		[HarmonyPostfix]
		public static void GetRidOfVacuum (CompShipPart __instance)
		{
			if (__instance.parent.Map.terrainGrid.TerrainAt (__instance.parent.Position).defName.Equals ("EmptySpace"))
				__instance.parent.Map.terrainGrid.SetTerrain (__instance.parent.Position,TerrainDef.Named("FakeFloorInsideShip"));
		}
	}*/
	/*[HarmonyPatch(typeof(GenConstruct), "BlocksConstruction")]
	public static class HullTilesDontWipe
	{
		public static void Postfix(Thing constructible, Thing t, ref bool __result)
		{
			if (constructible.def.defName.Contains("ShipHullTile") ^ t.def.defName.Contains("ShipHullTile"))
				__result = false;
		}
	}

	[HarmonyPatch(typeof(TravelingTransportPods))]
	[HarmonyPatch("TraveledPctStepPerTick", MethodType.Getter)]
	public static class InstantShuttleArrival
	{
		[HarmonyPostfix]
		public static void CloseRangeBoardingAction(int ___initialTile, TravelingTransportPods __instance, ref float __result)
		{
			if (Find.TickManager.TicksGame % 60 == 0)
			{
				var mapComp = Find.WorldObjects.MapParentAt(___initialTile).Map.GetComponent<ShipHeatMapComp>();
				if ((mapComp.InCombat && (__instance.destinationTile == mapComp.ShipCombatOriginMap.Tile ||
					__instance.destinationTile == mapComp.ShipCombatMasterMap.Tile)) || 
					__instance.arrivalAction is TransportPodsArrivalAction_MoonBase)
				{
					__result = 1f;
				}
			}

		}
	}*/

	//AI cores should be able to control mechanoids by default, and this is a hill I will die on
	[HarmonyPatch(typeof(MechanitorUtility), "IsMechanitor")]
	public static class AICoreIsMechanitor
    {
		public static void Postfix(Pawn pawn, ref bool __result)
        {
			if (pawn.health.hediffSet.HasHediff(HediffDef.Named("SoSHologramMachine")) || pawn.health.hediffSet.HasHediff(HediffDef.Named("SoSHologramArchotech")))
				__result = true;
		}
    }

	//For loading ships
	[HarmonyPatch(typeof(Page_ChooseIdeoPreset), "PostOpen")]
	public static class DoNotRemoveMyIdeo
    {
		public static bool Prefix()
        {
			return !WorldSwitchUtility.LoadShipFlag;
        }

		public static void Postfix(Page_ChooseIdeoPreset __instance)
        {
			if(WorldSwitchUtility.LoadShipFlag)
            {
				foreach (Faction allFaction in Find.FactionManager.AllFactions)
				{
					if (allFaction != Faction.OfPlayer && allFaction.ideos != null && allFaction.ideos.PrimaryIdeo.memes.NullOrEmpty())
					{
						allFaction.ideos.ChooseOrGenerateIdeo(new IdeoGenerationParms(allFaction.def));
					}
				}
				Faction.OfPlayer.ideos.SetPrimary(ScenPart_LoadShip.playerFactionIdeo);
				IdeoUIUtility.selected = ScenPart_LoadShip.playerFactionIdeo;
				ScenPart_LoadShip.AddIdeo(Faction.OfPlayer.ideos.PrimaryIdeo);
				Page_ConfigureIdeo page_ConfigureIdeo = new Page_ConfigureIdeo();
				page_ConfigureIdeo.prev = __instance.prev;
				page_ConfigureIdeo.next = __instance.next;
				__instance.next.prev = page_ConfigureIdeo;
				Find.WindowStack.Add(page_ConfigureIdeo);
				__instance.Close();
			}
        }
    }

	[HarmonyPatch(typeof(Page_ConfigureStartingPawns),"PreOpen")]
	public static class NoNeedForMorePawns
    {
		public static bool Prefix()
        {
			return !WorldSwitchUtility.LoadShipFlag;
        }

		public static void Postfix(Page_ConfigureStartingPawns __instance)
        {
			if (WorldSwitchUtility.LoadShipFlag)
			{
				if (__instance.next != null)
				{
					__instance.prev.next = __instance.next;
					__instance.next.prev = __instance.prev;
					Find.WindowStack.Add(__instance.next);
				}
				if (__instance.nextAct != null)
				{
					__instance.nextAct();
				}
				__instance.Close();
			}
        }
    }

	[HarmonyPatch(typeof(Scenario), "GetFullInformationText")]
	public static class RemoveUnwantedScenPartText
    {
		public static bool Prefix(Scenario __instance)
        {
			return __instance.AllParts.Where(part => part is ScenPart_LoadShip && ((ScenPart_LoadShip)part).HasValidFilename()).Count() == 0;
        }

		public static void Postfix(Scenario __instance, ref string __result)
        {
			if(__instance.AllParts.Where(part => part is ScenPart_LoadShip && ((ScenPart_LoadShip)part).HasValidFilename()).Count() > 0)
            {
				try
				{
					StringBuilder stringBuilder = new StringBuilder();
					foreach (ScenPart allPart in __instance.AllParts)
					{
						allPart.summarized = false;
					}
					foreach (ScenPart item in from p in __instance.AllParts
											  orderby p.def.summaryPriority descending, p.def.defName
											  where p.visible
											  select p)
					{
						if (ShipInteriorMod2.CompatibleWithShipLoad(item))
						{
							string text = item.Summary(__instance);
							if (!text.NullOrEmpty())
							{
								stringBuilder.AppendLine(text);
							}
						}
					}
					__result = stringBuilder.ToString().TrimEndNewlines();
					return;
				}
				catch (Exception ex)
				{
					Log.ErrorOnce("Exception in Scenario.GetFullInformationText():\n" + ex.ToString(), 10395878);
					__result = "Cannot read data.";
				}
			}
        }
    }

	[HarmonyPatch(typeof(GameInitData), "PrepForMapGen")]
	public static class FixPawnGen
    {
		public static bool Prefix()
        {
			return !WorldSwitchUtility.LoadShipFlag;
        }
    }

	[HarmonyPatch(typeof(MapGenerator), "GenerateMap")]
	public static class DoNotActuallyInitMap
    {
		public static bool Prefix()
        {
			return !WorldSwitchUtility.LoadShipFlag;
        }

		public static void Postfix(MapParent parent, ref Map __result)
        {
			if (WorldSwitchUtility.LoadShipFlag)
			{
				parent.Destroy();
				WorldSwitchUtility.LoadShipFlag = false;
				__result = ScenPart_LoadShip.GenerateShipSpaceMap();
			}
		}
    }

	[HarmonyPatch(typeof(Scenario),"GetFirstConfigPage")]
	public static class LoadTheUniqueIDs
    {
		public static void Postfix(Scenario __instance)
		{
			foreach (ScenPart part in __instance.AllParts)
			{
				if (part is ScenPart_LoadShip && ((ScenPart_LoadShip)part).HasValidFilename())
				{
					((ScenPart_LoadShip)part).DoEarlyInit();
				}
			}
		}
    }
}