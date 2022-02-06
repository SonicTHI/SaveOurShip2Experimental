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
		public override bool CanCoexistWith(ScenPart other)
		{
			return !(other is ScenPart_AfterlifeVault);
		}

		//ship selection - not sure how much of this is actually needed for this to work, also a bit convoluted random option
		internal EnemyShipDef enemyShipDef;
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Defs.Look<EnemyShipDef>(ref this.enemyShipDef, "enemyShipDef");
		}
		public override void DoEditInterface(Listing_ScenEdit listing)
		{
			if (Widgets.ButtonText(listing.GetScenPartRect(this, ScenPart.RowHeight), this.enemyShipDef.label, true, true, true))
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
			int newTile = -1;
			for (int i = 0; i < 420; i++)
			{
				if (!Find.World.worldObjects.AnyMapParentAt(i))
				{
					newTile = i;
					break;
				}
			}
			Map spaceMap = GetOrGenerateMapUtility.GetOrGenerateMap(newTile, DefDatabase<WorldObjectDef>.GetNamed("ShipOrbiting"));
			((WorldObjectOrbitingShip)spaceMap.Parent).radius = 150;
			((WorldObjectOrbitingShip)spaceMap.Parent).theta = 2.75f;
			Building core = null;
			Current.ProgramState = ProgramState.MapInitializing;
			if (this.def.defName.Equals("StartInSpaceDungeon") && this.enemyShipDef.defName == "0")//random dungeon
				enemyShipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => def.startingShip == true && def.startingDungeon == true).RandomElement();
			else if (this.enemyShipDef.defName == "0")//random ship
				enemyShipDef = DefDatabase<EnemyShipDef>.AllDefs.Where(def => def.startingShip == true && def.startingDungeon == false && def.defName != "0").RandomElement();
			ShipInteriorMod2.GenerateShip(enemyShipDef, spaceMap, null, Faction.OfPlayer,null, out core, false);

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
			List<IntVec3> cryptoPos = GetAllCryptoCells(spaceMap);
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
			foreach(List<Thing> thingies in list)
			{
				IntVec3 casketPos;
				if (!cryptoPos.NullOrEmpty())
				{
					casketPos = cryptoPos.RandomElement();
					cryptoPos.Remove(casketPos);
					if (cryptoPos.Count == 0)
						cryptoPos = GetAllCryptoCells(spaceMap); //Out of caskets, time to start double-dipping
				}
                else //no caskets, fallback - bay, bridge
                {
					Building bay = spaceMap.listerBuildings.allBuildingsColonist.Where(b => b.TryGetComp<CompShipSalvageBay>() != null).FirstOrDefault();
					if (bay != null)
						casketPos = bay.Position;
					else
						casketPos = spaceMap.listerBuildings.allBuildingsColonist.Where(b => b is Building_ShipBridge).FirstOrDefault().Position;
				}

				foreach(Thing thingy in thingies)
                {
					thingy.SetForbidden(true, false);
					GenPlace.TryPlaceThing(thingy, casketPos, spaceMap, ThingPlaceMode.Near);
                }
				if (this.def.defName.Equals("StartInSpaceDungeon"))
					FloodFillerFog.FloodUnfog(casketPos, spaceMap);								
            }
			if (!this.def.defName.Equals("StartInSpaceDungeon"))
				spaceMap.fogGrid.ClearAllFog();
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

		List<IntVec3> GetAllCryptoCells(Map spaceMap)
        {
			List<IntVec3> toReturn = new List<IntVec3>();
			foreach (Building b in spaceMap.listerBuildings.allBuildingsColonist.Where(b => b is Building_CryptosleepCasket))
			{
				if(!((Building_CryptosleepCasket)b).HasAnyContents)
					toReturn.Add(b.InteractionCell);
			}
			return toReturn;
        }
    }
}
