using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
	class QuestPart_RoyalAscentShip : QuestPart
	{

		public string inSignal;
		public override void Notify_QuestSignalReceived(Signal signal)
		{
			base.Notify_QuestSignalReceived(signal);
			if (signal.tag == this.inSignal)
			{
				Map originMap = Find.CurrentMap;
				Map map;
				EnemyShipDef shipDef = DefDatabase<EnemyShipDef>.GetNamed("RewardEmpireDestroyer");
				List<Building> cores = new List<Building>();
				if (ShipInteriorMod2.FindPlayerShipMap() != null)
				{
					map = GetOrGenerateMapUtility.GetOrGenerateMap(ShipInteriorMod2.FindWorldTilePlayer(), new IntVec3(250, 1, 250), ResourceBank.WorldObjectDefOf.ShipEnemy);
					map.GetComponent<ShipHeatMapComp>().IsGraveyard = true;
					((WorldObjectOrbitingShip)map.Parent).radius = 150f;
					((WorldObjectOrbitingShip)map.Parent).theta = -3 - 0.1f + 0.002f * Rand.Range(0, 20);
					((WorldObjectOrbitingShip)map.Parent).phi = 0 - 0.01f + 0.001f * Rand.Range(-20, 20);
				}
				else
				{
					map = ShipInteriorMod2.GeneratePlayerShipMap(originMap.Size);
                }
                ShipInteriorMod2.GenerateShip(shipDef, map, null, Faction.OfPlayer, null, out cores, false, false, 0, (map.Size.x - shipDef.sizeX) / 2, (map.Size.z - shipDef.sizeZ) / 2);
                map.fogGrid.ClearAllFog();

                /*if (!Find.TickManager.Paused)
				{
					Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
				}
				List<Pawn> list;
				if (!signal.args.TryGetArg<List<Pawn>>("SENTCOLONISTS", out list))
				{
					list = null;
				}
				StringBuilder stringBuilder = new StringBuilder();
				if (list != null)
				{
					for (int i = 0; i < list.Count; i++)
					{
						stringBuilder.AppendLine("   " + list[i].LabelCap);
					}
					Find.StoryWatcher.statsRecord.colonistsLaunched += list.Count;
				}
				//ShipCountdown.InitiateCountdown(GameVictoryUtility.MakeEndCredits(this.introText, this.endingText, stringBuilder.ToString(), "GameOverColonistsEscaped", null));
				if (list != null)
				{
					for (int j = 0; j < list.Count; j++)
					{
						if (!list[j].Destroyed)
						{
							list[j].Destroy(DestroyMode.Vanish);
						}
					}
				}*/
            }
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<string>(ref this.inSignal, "inSignal", null, false);
		}

		public override void AssignDebugData()
		{
			base.AssignDebugData();
			this.inSignal = "DebugSignal" + Rand.Int;
		}
	}
}
