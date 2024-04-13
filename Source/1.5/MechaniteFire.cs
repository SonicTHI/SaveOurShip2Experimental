using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class MechaniteFire : Fire
	{
		int lastDamageTick = Find.TickManager.TicksGame;

		public override string Label
		{
			get
			{
				if (parent != null)
				{
					return "MechanitesOn".Translate(parent.LabelCap, parent);
				}
				return def.label;
			}
		}

		public override string InspectStringAddon => "MechanitesDisassembling".Translate() + " (" + "MechaniteSizeLower".Translate((fireSize * 100f).ToString("F0")) + ")";

		public void DoFireDamage(Thing targ)
		{
			int num = GenMath.RoundRandom(Mathf.Clamp(0.025f + 0.0072f * fireSize, 0.025f, 0.1f) * 1200f);
			if (num < 1)
			{
				num = 1;
			}
			Pawn pawn = targ as Pawn;
			if (pawn != null)
			{
				BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(pawn, RulePackDefOf.DamageEvent_Fire);
				Find.BattleLog.Add(battleLogEntry_DamageTaken);
				DamageInfo dinfo = new DamageInfo(DamageDefOf.Flame, num, 0f, -1f, this);
				dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
				targ.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_DamageTaken);
				if (pawn.apparel != null && pawn.apparel.WornApparel.TryRandomElement(out Apparel result))
				{
					result.TakeDamage(new DamageInfo(DamageDefOf.Flame, num, 0f, -1f, this));
				}
				lastDamageTick = Find.TickManager.TicksGame;
			}
			else
			{
				if (targ.def.useHitPoints && targ.HitPoints < num && Rand.Chance(0.3f))
				{
					targ.Destroy(DestroyMode.Deconstruct);
				}
				else
					targ.TakeDamage(new DamageInfo(DamageDefOf.Flame, num, 0f, -1f, this));
				lastDamageTick = Find.TickManager.TicksGame;
			}
		}

		public override void Tick()
		{
			base.Tick();
			if (Find.TickManager.TicksGame > lastDamageTick + 600)
				Destroy();
		}
	}
}
