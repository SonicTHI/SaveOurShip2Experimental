using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
	class DamageWorker_Mechanite : DamageWorker_AddInjury
	{
		public override void ExplosionAffectCell(Explosion explosion, IntVec3 c, List<Thing> damagedThings, List<Thing> ignoredThings, bool canThrowMotes)
		{
			base.ExplosionAffectCell(explosion, c, damagedThings, ignoredThings, canThrowMotes);
			if (Rand.Chance(0.5f))
			{
				MechaniteFire obj = (MechaniteFire)ThingMaker.MakeThing(ThingDef.Named("MechaniteFire"));
				obj.fireSize = 1.75f;
				GenSpawn.Spawn(obj, c, explosion.Map, Rot4.North);
			}
		}
	}
}
