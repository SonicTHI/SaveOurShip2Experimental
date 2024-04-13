using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;
using RimWorld;

namespace SaveOurShip2
{
	class DeathActionWorker_AntigrainExplosion : DeathActionWorker
	{
		public override RulePackDef DeathRules => RulePackDefOf.Transition_DiedExplosive;

		public override bool DangerousInMelee => true;

		public override void PawnDied(Corpse corpse, Lord prevLord)
		{
			GenExplosion.DoExplosion(radius: (corpse.InnerPawn.ageTracker.CurLifeStageIndex == 0) ? 4.9f : ((corpse.InnerPawn.ageTracker.CurLifeStageIndex != 1) ? 14.9f : 9.9f), center: corpse.Position, map: corpse.Map, damType: DefDatabase<DamageDef>.GetNamed("BombSuper"), instigator: corpse.InnerPawn);
		}
	}
}
