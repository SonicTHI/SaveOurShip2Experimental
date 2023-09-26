using RimWorld.Planet;
using RimworldMod;
using RimWorld;
using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
	[Flags]
	public enum ShipStartFlags
	{
		None = 0,
		Ship = 1,
		Station = 2,
		LoadShip = 4 //if this can be merged?
	}
	class ScenPart_StartInSpace : ScenPart
	{

		//ship selection - not sure how much of this is actually needed for this to work, also a bit convoluted random option
		public EnemyShipDef enemyShipDef;
        public bool damageStart;
		public ShipStartFlags startType;
        public override bool CanCoexistWith(ScenPart other) //not working in menu
        {
            return !(other is ScenPart_AfterlifeVault || other is ScenPart_LoadShip);
        }
        public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look<EnemyShipDef>(ref enemyShipDef, "enemyShipDef");
		}
		public override void DoEditInterface(Listing_ScenEdit listing)
		{
            Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 3f);
			Rect rect1 = new Rect(scenPartRect.x, scenPartRect.y, scenPartRect.width, scenPartRect.height / 3f);
			Rect rect2 = new Rect(scenPartRect.x, scenPartRect.y + scenPartRect.height / 3f, scenPartRect.width, scenPartRect.height / 3f);
			Rect rect3 = new Rect(scenPartRect.x, scenPartRect.y + 2 * scenPartRect.height / 3f, scenPartRect.width, scenPartRect.height / 3f);
			//selection 1
			if (Widgets.ButtonText(rect1, "Start on: " + startType.ToString(), true, true, true))
			{
				List<FloatMenuOption> toggleType = new List<FloatMenuOption>();
				toggleType.Add(new FloatMenuOption("Start on: ship", delegate ()
				{
					startType = ShipStartFlags.Ship;
					enemyShipDef = DefDatabase<EnemyShipDef>.GetNamed("0");
				}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
				toggleType.Add(new FloatMenuOption("Start on: station", delegate ()
				{
					startType = ShipStartFlags.Station;
					enemyShipDef = DefDatabase<EnemyShipDef>.GetNamed("0");
				}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
				Find.WindowStack.Add(new FloatMenu(toggleType));

			}
			//selection 2
			if (Widgets.ButtonText(rect2, enemyShipDef.label, true, true, true))
			{
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				foreach (EnemyShipDef localTd2 in DefDatabase<EnemyShipDef>.AllDefs.Where(t => t.defName == "0" || (startType == ShipStartFlags.Ship && t.startingShip == true && t.startingDungeon == false) || (startType == ShipStartFlags.Station && t.startingShip == true && t.startingDungeon == true)).OrderBy(t => t.defName))
				{
					EnemyShipDef localTd = localTd2;
					list.Add(new FloatMenuOption(localTd.label + " (" + localTd.defName + ")", delegate ()
                    {
                        enemyShipDef = localTd;
					}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
				}
				Find.WindowStack.Add(new FloatMenu(list));
			}
			//selection 3
			if (startType == ShipStartFlags.Ship && Widgets.ButtonText(rect3, "Damage ship: " + damageStart.ToString(), true, true, true))
			{
				List<FloatMenuOption> toggleDamage = new List<FloatMenuOption>();
				toggleDamage.Add(new FloatMenuOption("Damage ship: True", delegate ()
				{
					damageStart = true;
				}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
				toggleDamage.Add(new FloatMenuOption("Damage ship: False", delegate ()
				{
					damageStart = false;
				}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
				Find.WindowStack.Add(new FloatMenu(toggleDamage));

			}
		}
		public override string Summary(Scenario scen)
		{
			return "ScenPart_PlayerFaction".Translate(enemyShipDef.label);
		}

		public override void Randomize()
		{
			enemyShipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => def.startingShip == true && def.startingDungeon == true).RandomElement();
		}
		public override bool HasNullDefs()
		{
			return base.HasNullDefs() || enemyShipDef == null;
		}
		public override IEnumerable<string> ConfigErrors()
		{
			if (enemyShipDef == null)
			{
				yield return "thingDef is null";
			}
			yield break;
		}
		//ship selection end

		public void DoEarlyInit() //Scenario.GetFirstConfigPage call via patch 
        {
			WorldSwitchUtility.StartShipFlag = true;
		}

		public static Map GenerateShipSpaceMap() //MapGenerator.GenerateMap override via patch
		{
			int newTile = ShipInteriorMod2.FindWorldTilePlayer();
			Map spaceMap = GetOrGenerateMapUtility.GetOrGenerateMap(newTile, DefDatabase<WorldObjectDef>.GetNamed("ShipOrbiting"));
			((WorldObjectOrbitingShip)spaceMap.Parent).radius = 150;
			((WorldObjectOrbitingShip)spaceMap.Parent).theta = 2.75f;
			Current.ProgramState = ProgramState.MapInitializing;

			ScenPart_StartInSpace scen = (ScenPart_StartInSpace)Current.Game.Scenario.parts.FirstOrDefault(s => s is ScenPart_StartInSpace);

			if (scen.startType == ShipStartFlags.Station && scen.enemyShipDef.defName == "0") //random dungeon
			{
				scen.enemyShipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => def.startingShip == true && def.startingDungeon == true).RandomElement();
				scen.damageStart = false;
			}
			else if (scen.enemyShipDef.defName == "0") //random ship, damage lvl 1
			{
				scen.enemyShipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => def.startingShip == true && def.startingDungeon == false && def.defName != "0").RandomElement();
			}
			List<Building> cores = new List<Building>();
			ShipInteriorMod2.GenerateShip(scen.enemyShipDef, spaceMap, null, Faction.OfPlayer, null, out cores, false, false, scen.damageStart ? 1 : 0, (spaceMap.Size.x - scen.enemyShipDef.sizeX) / 2, (spaceMap.Size.z - scen.enemyShipDef.sizeZ) / 2);

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

			if (scen.startType == ShipStartFlags.Ship) //defog and homezone ships
			{
				spaceMap.fogGrid.ClearAllFog();
				foreach (Building b in spaceMap.listerThings.AllThings.Where(t => t is Building))
				{
					foreach (IntVec3 v in b.OccupiedRect())
					{
						spaceMap.areaManager.Home[v] = true;
					}
				}
			}
			CameraJumper.TryJump(spaceMap.Center, spaceMap);
			spaceMap.weatherManager.curWeather = ResourceBank.WeatherDefOf.OuterSpaceWeather;
			spaceMap.weatherManager.lastWeather = ResourceBank.WeatherDefOf.OuterSpaceWeather;
			spaceMap.Parent.SetFaction(Faction.OfPlayer);
			Find.MapUI.Notify_SwitchedMap();
			spaceMap.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
			foreach (Room r in spaceMap.regionGrid.allRooms)
				r.Temperature = 21;
			AccessExtensions.Utility.RecacheSpaceMaps();
			return spaceMap;
		}
		public override void PostGameStart() //spawn pawns, things
		{
			ScenPart_StartInSpace scen = (ScenPart_StartInSpace)Current.Game.Scenario.parts.FirstOrDefault(s => s is ScenPart_StartInSpace);
			Map spaceMap = Find.CurrentMap;
			List<List<Thing>> list = new List<List<Thing>>();
			foreach (Pawn startingAndOptionalPawn in Find.GameInitData.startingAndOptionalPawns)
			{
                List<Thing> list2 = new List<Thing>{ startingAndOptionalPawn };
                list.Add(list2);
			}
			List<Thing> list3 = new List<Thing>();
			foreach (ScenPart allPart in Find.Scenario.AllParts)
			{
				list3.AddRange(allPart.PlayerStartingThings());
			}
			int num = 0;
			foreach (Thing item in list3)
			{
				if (!(item is Pawn))
				{
					if (item.def.CanHaveFaction)
					{
						item.SetFactionDirect(Faction.OfPlayer);
					}
					list[num].Add(item);
					num++;
					if (num >= list.Count)
					{
						num = 0;
					}
				}
			}
			List<Building> spawns = new List<Building>();
			List<IntVec3> spawnPos = GetSpawnCells(spaceMap, out spawns);
			foreach (List<Thing> thingies in list)
			{
				IntVec3 nextPos = spaceMap.Center;
				nextPos = spawnPos.RandomElement();
				spawnPos.Remove(nextPos);
				if (spawnPos.Count == 0)
					spawnPos = GetSpawnCells(spaceMap, out spawns); //reuse spawns

				foreach (Thing thingy in thingies)
				{
					thingy.SetForbidden(true, false);
					GenPlace.TryPlaceThing(thingy, nextPos, spaceMap, ThingPlaceMode.Near);
				}
				if (scen.startType == ShipStartFlags.Station)
					FloodFillerFog.FloodUnfog(nextPos, spaceMap);
			}
			foreach (Building b in spawns.Where(b => !b.Destroyed)) //remove spawn points
			{
				b.Destroy();
			}
			spawns.Clear();
			spawnPos.Clear();
		}
		static List<IntVec3> GetSpawnCells(Map spaceMap, out List<Building> spawns) //spawn placer > crypto > salvbay > bridge
		{
			spawns = new List<Building>();
			List<IntVec3> spawncells = new List<IntVec3>();
			foreach (Building b in spaceMap.listerBuildings.allBuildingsColonist.Where(b => b.def.defName.Equals("PawnSpawnerStart")))
			{
				spawncells.Add(b.Position);
				spawns.Add(b);
			}
			if (spawncells.Any())
			{
				return spawncells;
			}
			//backups
			List<IntVec3> salvBayCells = new List<IntVec3>();
			List<IntVec3> bridgeCells = new List<IntVec3>();
			foreach (Building b in spaceMap.listerBuildings.allBuildingsColonist)
			{
				if (b is Building_CryptosleepCasket c && !c.HasAnyContents)
				{
					spawncells.Add(b.InteractionCell);
				}
				else if (b.TryGetComp<CompShipSalvageBay>() != null)
				{
					salvBayCells.AddRange(b.OccupiedRect().ToList());
				}
				else if (b is Building_ShipBridge && b.def.hasInteractionCell && b.GetRoom() != null)
				{
					bridgeCells.AddRange(b.GetRoom().Cells.ToList());
				}
			}
			if (spawncells.Any())
				return spawncells;
			else if (salvBayCells.Any())
				return salvBayCells;
			else if (bridgeCells.Any())
				return bridgeCells;
			spawncells.Add(spaceMap.Center);
			return spawncells;
		}
    }
}
