using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class DamageWorker_PsychicFlayer : DamageWorker
	{
		public override DamageResult Apply(DamageInfo dinfo, Thing victim)
		{
			DamageResult damageResult = base.Apply(dinfo, victim);
			if (victim is Pawn && !((Pawn)victim).Dead && victim.GetStatValue(StatDefOf.PsychicSensitivity) > 0)
			{
				damageResult.stunned = true;
				Pawn pawn = (Pawn)victim;
				Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.PsychicShock, pawn);
				BodyPartRecord result = null;
				pawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).TryRandomElement(out result);
				pawn.health.AddHediff(hediff, result);
				if(Rand.Value < 0.1f)
				{
					BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
					if (brain != null)
					{
						int num = Rand.RangeInclusive(1, 5);
						pawn.TakeDamage(new DamageInfo(DamageDefOf.Flame, num, 0f, -1f, null, brain, null));
					}
				}
			}
			return damageResult;
		}

		public override IEnumerable<IntVec3> ExplosionCellsToHit(IntVec3 center, Map map, float radius, IntVec3? needLOSToCell1 = null, IntVec3? needLOSToCell2 = null, FloatRange? affectedAngle = null)
		{
			List<IntVec3> cells = new List<IntVec3>();
			float sqrRad = radius * radius;
			for(int x = -(int)radius;x<=(int)radius;x++)
			{
				for(int z = -(int)radius;z<=(int)radius;z++)
				{
					if (x * x + z * z <= sqrRad)
						cells.Add(new IntVec3(x, 0, z) + center);
				}
			}
			return cells;
		}
	}
}
