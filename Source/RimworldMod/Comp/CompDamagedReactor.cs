using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    class CompDamagedReactor : ThingComp
    {
        public override void CompTick()
        {
            base.CompTick();
            if(Find.TickManager.TicksGame % 59 == 0 && !parent.GetComp<CompBreakdownable>().BrokenDown)
            {
                List<Pawn> pawnsToIrradiate = new List<Pawn>();
                foreach(Pawn p in this.parent.Map.mapPawns.AllPawnsSpawned)
                {
                    if (p.RaceProps.IsFlesh && p.GetRoom() != null && p.GetRoom() == RegionAndRoomQuery.RoomAt(new IntVec3(this.parent.Position.x, 0, this.parent.Position.z + 5), this.parent.Map))
                    {
                        pawnsToIrradiate.Add(p);
                    }
                }
                foreach(Pawn p in pawnsToIrradiate)
                {
                    int damage = Rand.RangeInclusive(4, 7);
                    p.TakeDamage(new DamageInfo(DamageDefOf.Burn, damage));
                    float num = 0.01f;
                    num *= p.GetStatValue(StatDefOf.ToxicSensitivity, true);
                    if (num != 0f)
                    {
                        HealthUtility.AdjustSeverity(p, HediffDefOf.ToxicBuildup, num);
                    }
                }
            }
        }
    }
}
