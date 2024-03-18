using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
	public class MechaniteSpark : Projectile
	{
		protected override void Impact(Thing hitThing, bool blockedByShield = false)
		{
			Map map = base.Map;
			MechaniteFire fire = base.Position.GetFirstThing<MechaniteFire>(base.Map);
			if (fire == null)
			{
				MechaniteFire obj = (MechaniteFire)ThingMaker.MakeThing(ThingDef.Named("MechaniteFire"));
				obj.fireSize = Rand.Range(0.1f, 0.2f);
				GenSpawn.Spawn(obj, base.Position, map, Rot4.North);
			}
			else
			{
				fire.fireSize += 0.2f;
				if (fire.fireSize > 1.75f)
					fire.fireSize = 1.75f;
			}
			base.Impact(hitThing);
		}
	}
}
