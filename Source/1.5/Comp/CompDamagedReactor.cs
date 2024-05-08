using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class CompDamagedReactor : ThingComp
	{
		int lastWarnedTick = -1;
		public override void CompTick()
		{
			base.CompTick();
			if(Find.TickManager.TicksGame % 59 == 0 && !parent.GetComp<CompBreakdownable>().BrokenDown)
			{
				List<Pawn> pawnsToIrradiate = new List<Pawn>();
				foreach(Pawn p in this.parent.Map.mapPawns.AllPawnsSpawned)
				{
					if (p.RaceProps.IsFlesh && p.Position.DistanceTo(parent.Position)<=20)
					{
						pawnsToIrradiate.Add(p);
					}
				}
				foreach(Pawn p in pawnsToIrradiate)
				{
					int damage = Rand.RangeInclusive(3, 5);
					p.TakeDamage(new DamageInfo(DamageDefOf.Burn, damage));
					float num = 0.01f;
					num *= (1 - p.GetStatValue(StatDefOf.ToxicResistance, true));
					if (num != 0f)
					{
						HealthUtility.AdjustSeverity(p, HediffDefOf.ToxicBuildup, num);
					}
					if(lastWarnedTick < Find.TickManager.TicksGame && p.Faction==Faction.OfPlayer)
					{
						Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.DamagedReactorHurtsPawn",p), p, MessageTypeDefOf.ThreatSmall, false);
						lastWarnedTick = Find.TickManager.TicksGame + 60;
					}
				}
			}
		}
	}
}
