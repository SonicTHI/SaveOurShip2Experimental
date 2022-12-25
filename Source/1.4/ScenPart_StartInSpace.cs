using RimworldMod;
using RimworldMod.VacuumIsNotFun;
using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    class ScenPart_StartInSpace : ScenPart
	{
		List<Building> spawns = new List<Building>();
		public override bool CanCoexistWith(ScenPart other)
		{
			return !(other is ScenPart_AfterlifeVault);
		}

		//ship selection - not sure how much of this is actually needed for this to work, also a bit convoluted random option
		internal EnemyShipDef enemyShipDef;
		bool damageStart = true;
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look<EnemyShipDef>(ref this.enemyShipDef, "enemyShipDef");
		}
		public override void DoEditInterface(Listing_ScenEdit listing)
		{
			Rect scenPartRect = listing.GetScenPartRect(this, ScenPart.RowHeight * 2f);
			Rect rect = new Rect(scenPartRect.x, scenPartRect.y, scenPartRect.width, scenPartRect.height / 2f);
			Rect rect2 = new Rect(scenPartRect.x, scenPartRect.y + scenPartRect.height / 2f, scenPartRect.width, scenPartRect.height / 2f);
			//selection 1
			if (Widgets.ButtonText(rect, this.enemyShipDef.label, true, true, true))
			{
				bool station = false;
				if (this.def.defName.Equals("StartInSpaceDungeon"))
					station = true;
				List<FloatMenuOption> list = new List<FloatMenuOption>();
				foreach (EnemyShipDef localTd2 in from t in DefDatabase<EnemyShipDef>.AllDefs
												  where (!station && t.startingShip == true && t.startingDungeon == false || (station && t.startingShip == true && t.startingDungeon == true || t.defName == "0"))
												  orderby t.defName
												  select t)
				{
					EnemyShipDef localTd = localTd2;
					list.Add(new FloatMenuOption(localTd.label + " (" + localTd.defName + ")", delegate ()
					{
						this.enemyShipDef = localTd;
					}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
				}
				Find.WindowStack.Add(new FloatMenu(list));
			}
			//selection 2
			if (Widgets.ButtonText(rect2, "Damage ship: " + damageStart.ToString(), true, true, true))
			{
				List<FloatMenuOption> toggleDamage = new List<FloatMenuOption>();
				toggleDamage.Add(new FloatMenuOption("Damage ship: True", delegate ()
				{
					this.damageStart = true;
				}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
				toggleDamage.Add(new FloatMenuOption("Damage ship: False", delegate ()
				{
					this.damageStart = false;
				}, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
				Find.WindowStack.Add(new FloatMenu(toggleDamage));

			}
		}
		public override string Summary(Scenario scen)
		{
			return "ScenPart_PlayerFaction".Translate(this.enemyShipDef.label);
		}
		public override void Randomize()
		{
			this.enemyShipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => def.startingShip == true && def.startingDungeon == true).RandomElement();
		}
		public override bool HasNullDefs()
		{
			return base.HasNullDefs() || this.enemyShipDef == null;
		}
		public override IEnumerable<string> ConfigErrors()
		{
			if (this.enemyShipDef == null)
			{
				yield return "thingDef is null";
			}
			yield break;
		}
		//ship selection end
		public override void PostGameStart()
        {
			if (WorldSwitchUtility.SelectiveWorldGenFlag)
				return;
            List<Pawn> startingPawns = Find.CurrentMap.mapPawns.PawnsInFaction(Faction.OfPlayer);
			int newTile = ShipInteriorMod2.FindWorldTile();
			Map spaceMap = GetOrGenerateMapUtility.GetOrGenerateMap(newTile, DefDatabase<WorldObjectDef>.GetNamed("ShipOrbiting"));
			((WorldObjectOrbitingShip)spaceMap.Parent).radius = 150;
			((WorldObjectOrbitingShip)spaceMap.Parent).theta = 2.75f;
			List<Building> cores = new List<Building>();
			Current.ProgramState = ProgramState.MapInitializing;
			bool station = this.def.defName.Equals("StartInSpaceDungeon");
			if (station && this.enemyShipDef.defName == "0") //random dungeon
			{
				enemyShipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => def.startingShip == true && def.startingDungeon == true).RandomElement();
				ShipInteriorMod2.GenerateShip(enemyShipDef, spaceMap, null, Faction.OfPlayer, null, out cores, false, false, 0);
			}
			else if (this.enemyShipDef.defName == "0") //random ship, damage lvl 1
			{
				enemyShipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => def.startingShip == true && def.startingDungeon == false && def.defName != "0").RandomElement();
				ShipInteriorMod2.GenerateShip(enemyShipDef, spaceMap, null, Faction.OfPlayer, null, out cores, false, false, damageStart ? 1 : 0);
			}
			Current.ProgramState = ProgramState.Playing;
			IntVec2 secs = (IntVec2)typeof(MapDrawer).GetProperty("SectionCount", System.Reflection.BindingFlags.NonPublic | BindingFlags.Instance).GetValue(spaceMap.mapDrawer);
			Section[,] secArray = new Section[secs.x, secs.z];
			typeof(MapDrawer).GetField("sections", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(spaceMap.mapDrawer, secArray);
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
            foreach (Pawn p in startingPawns)
            {
				if (p.InContainerEnclosed)
				{
					p.ParentHolder.GetDirectlyHeldThings().Remove(p);
				}
				else
				{
					p.DeSpawn();
					p.SpawnSetup(spaceMap, true);
				}
			}
			List<List<Thing>> list = new List<List<Thing>>();
			foreach (Pawn startingAndOptionalPawn in Find.GameInitData.startingAndOptionalPawns)
			{
				List<Thing> list2 = new List<Thing>();
				list2.Add(startingAndOptionalPawn);
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
			List<IntVec3> spawnPos = GetSpawnCells(spaceMap);
			foreach (List<Thing> thingies in list)
			{
				IntVec3 nextPos = spaceMap.Center;
				nextPos = spawnPos.RandomElement();
				spawnPos.Remove(nextPos);
				if (spawnPos.Count == 0)
					spawnPos = GetSpawnCells(spaceMap); //reuse spawns

				foreach (Thing thingy in thingies)
                {
					thingy.SetForbidden(true, false);
					GenPlace.TryPlaceThing(thingy, nextPos, spaceMap, ThingPlaceMode.Near);
                }
				if (station)
					FloodFillerFog.FloodUnfog(nextPos, spaceMap);
			}
			foreach (Building b in spawns.Where(b => !b.Destroyed)) //remove spawn points
			{
				b.Destroy();
			}
			if (!station) //defog and homezone ships
			{
				spaceMap.fogGrid.ClearAllFog();
				foreach (Building b in spaceMap.listerBuildings.allBuildingsColonist)
                {
					foreach (IntVec3 v in b.OccupiedRect())
                    {
						spaceMap.areaManager.Home[v] = true;
					}
                }
			}
			Current.Game.DeinitAndRemoveMap(Find.CurrentMap);
            CameraJumper.TryJump(spaceMap.Center, spaceMap);
			spaceMap.weatherManager.curWeather = WeatherDef.Named("OuterSpaceWeather");
			spaceMap.weatherManager.lastWeather = WeatherDef.Named("OuterSpaceWeather");
			spaceMap.Parent.SetFaction(Faction.OfPlayer);
			Find.MapUI.Notify_SwitchedMap();
			spaceMap.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
			foreach (Room r in spaceMap.regionGrid.allRooms)
				r.Temperature = 21;
			AccessExtensions.Utility.RecacheSpaceMaps();
        }

		List<IntVec3> GetSpawnCells(Map spaceMap) //spawn placer > crypto > salvbay > bridge
		{
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
