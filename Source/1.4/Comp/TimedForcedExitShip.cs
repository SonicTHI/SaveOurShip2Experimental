using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimworldMod;
using SaveOurShip2;

namespace RimWorld.Planet
{
	public class TimedForcedExitShip : WorldObjectComp
	{
		private int ticksLeftToForceExitAndRemoveMap = -1;
		private static List<Pawn> tmpPawns = new List<Pawn>();
		public bool ForceExitAndRemoveMapCountdownActive
		{
			get
			{
				return this.ticksLeftToForceExitAndRemoveMap >= 0;
			}
		}
		public string ForceExitAndRemoveMapCountdownTimeLeftString
		{
			get
			{
				if (!this.ForceExitAndRemoveMapCountdownActive)
				{
					return "";
				}
				return GetForceExitAndRemoveMapCountdownTimeLeftString(this.ticksLeftToForceExitAndRemoveMap);
			}
		}
		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref this.ticksLeftToForceExitAndRemoveMap, "ticksLeftToForceExitAndRemoveMapShip", -1, false);
		}
		public void ResetForceExitAndRemoveMapCountdown()
		{
			this.ticksLeftToForceExitAndRemoveMap = -1;
			if (parent.Biome != null && parent.Biome == ShipInteriorMod2.OuterSpaceBiome && parent.GetComponent<TimeoutComp>() != null)
				this.ticksLeftToForceExitAndRemoveMap = parent.GetComponent<TimeoutComp>().TicksLeft;
		}
		public void StartForceExitAndRemoveMapCountdown()
		{
			this.StartForceExitAndRemoveMapCountdown(60000);
		}
		public void StartForceExitAndRemoveMapCountdown(int duration)
		{
			this.ticksLeftToForceExitAndRemoveMap = duration;
		}
		public override string CompInspectStringExtra()
		{
			if (this.ForceExitAndRemoveMapCountdownActive)
			{
				return "ShipForceExitAndRemoveMapCountdown".Translate(this.ForceExitAndRemoveMapCountdownTimeLeftString) + ".";
			}
			return null;
		}
		public override void CompTick()
		{
			MapParent mapParent = (MapParent)this.parent;
			if (this.ForceExitAndRemoveMapCountdownActive)
			{
				if (mapParent.HasMap)
				{
					this.ticksLeftToForceExitAndRemoveMap--;
					if (this.ticksLeftToForceExitAndRemoveMap <= 0)
					{
						ForceReform(mapParent);
						return;
					}
				}
				else
				{
					this.ticksLeftToForceExitAndRemoveMap = -1;
				}
			}
		}
		public static string GetForceExitAndRemoveMapCountdownTimeLeftString(int ticksLeft)
		{
			if (ticksLeft < 0)
			{
				return "";
			}
			return ticksLeft.ToStringTicksToPeriod(true, false, true, true);
		}
		public static void ForceReform(MapParent mapParent)
		{
			if (mapParent.Map.IsSpace())
			{
				List<Pawn> deadPawns = new List<Pawn>();
				foreach (Thing t in mapParent.Map.spawnedThings)
				{
					if (t is Pawn p)
						deadPawns.Add(p);
				}
				foreach (Pawn p in deadPawns)
				{
					p.Kill(new DamageInfo(DamageDefOf.Bomb, 99999));
				}
				if (deadPawns.Any(p => p.Faction == Faction.OfPlayer))
				{
					string letterString = TranslatorFormattedStringExtensions.Translate("LetterPawnsLostReEntry") + "\n\n";
					foreach (Pawn deadPawn in deadPawns)
						letterString += deadPawn.LabelShort + "\n";
					Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("LetterLabelPawnsLostReEntry"), letterString,
						LetterDefOf.NegativeEvent);
				}
				if (mapParent.Map.GetComponent<ShipHeatMapComp>().ShipCombatMaster)
				{
					mapParent.Map.GetComponent<ShipHeatMapComp>().BurnUpSet = true;
				}
				else
					Find.WorldObjects.Remove(mapParent);
			}
			/*if (Dialog_FormCaravan.AllSendablePawns(mapParent.Map, true).Any((Pawn x) => x.IsColonist))
			{
				Messages.Message("MessageYouHaveToReformCaravanNow".Translate(), new GlobalTargetInfo(mapParent.Tile), MessageTypeDefOf.NeutralEvent, true);
				Current.Game.CurrentMap = mapParent.Map;
				Dialog_FormCaravan window = new Dialog_FormCaravan(mapParent.Map, true, delegate ()
				{
					if (mapParent.HasMap)
					{
						mapParent.Destroy();
					}
				}, true);
				Find.WindowStack.Add(window);
				return;
			}
			tmpPawns.Clear();
			tmpPawns.AddRange(from x in mapParent.Map.mapPawns.AllPawns where x.Faction == Faction.OfPlayer || x.HostFaction == Faction.OfPlayer select x);
			if (tmpPawns.Any((Pawn x) => CaravanUtility.IsOwner(x, Faction.OfPlayer)))
			{
				CaravanExitMapUtility.ExitMapAndCreateCaravan(tmpPawns, Faction.OfPlayer, mapParent.Tile, mapParent.Tile, -1, true);
			}
			tmpPawns.Clear();
			mapParent.Destroy();*/
		}
	}
}
