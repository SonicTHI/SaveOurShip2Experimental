using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using System.Linq;
using RimWorld.Planet;

namespace SaveOurShip2
{
	public class ImpactSiteComp : EscapeShipComp
	{
		public override void CompTick()
		{
			if (Find.TickManager.TicksGame % 60 != 0)
				return;
			MapParent mapParent = (MapParent)this.parent;
			if (mapParent.HasMap)
			{
				List<Pawn> allPawnsSpawned = mapParent.Map.mapPawns.AllPawnsSpawned.ToList();
				bool flag = mapParent.Map.mapPawns.FreeColonistsSpawnedOrInPlayerEjectablePodsCount != 0;
				bool flag2 = false;
				for (int i = 0; i < allPawnsSpawned.Count; i++)
				{
					Pawn pawn = allPawnsSpawned[i];
					if (pawn.RaceProps.Humanlike)
					{
						if (pawn.HostFaction == null)
						{
							if (!pawn.Downed)
							{
								if (pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer))
								{
									flag2 = true;
								}
							}
						}
					}
				}
				bool flag3 = false;
				Map mapPlayer = ShipInteriorMod2.FindPlayerShipMap();
				if (mapPlayer != null)
				{
					foreach (Building_ShipSensor sensor in ShipInteriorMod2.WorldComp.Sensors)
					{
						if (sensor.observedMap == this.parent)
						{
							flag3 = true;
						}
					}
				}
				if (flag2 && !flag && !flag3)
				{
					Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("ImpactSiteLostLabel"), TranslatorFormattedStringExtensions.Translate("ImpactSiteLost"), LetterDefOf.NegativeEvent);
					Find.WorldObjects.Remove(this.parent);
					ShipInteriorMod2.GenerateSite("ShipEngineImpactSite");
				}
			}
		}

		[DebuggerHidden]
		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
		{
			foreach (FloatMenuOption f in CaravanArrivalAction_VisitImpactSite.GetFloatMenuOptions(caravan, (MapParent)this.parent))
			{
				yield return f;
			}
		}

		[DebuggerHidden]
		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo giz in base.GetGizmos())
				yield return giz;
			MapParent mapParent = this.parent as MapParent;
			if (mapParent.HasMap)
			{
				bool foundDrive = false;
				foreach(Thing t in mapParent.Map.spawnedThings)
				{
					if(t.def.defName.Equals("JTDriveSalvage"))
					{
						foundDrive = true;
						break;
					}
				}
				if(!foundDrive)
					yield return SettlementAbandonUtility.AbandonCommand(mapParent);
			}
		}
	}
}