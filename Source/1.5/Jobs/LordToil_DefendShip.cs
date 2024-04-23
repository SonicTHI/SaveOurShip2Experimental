using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SaveOurShip2
{
	class LordToil_DefendShip : LordToil
	{
		public IntVec3 baseCenter;

		public override IntVec3 FlagLoc => baseCenter;

		public LordToil_DefendShip(IntVec3 baseCenter)
		{
			this.baseCenter = baseCenter;
		}

		public override void UpdateAllDuties()
		{
			for (int i = 0; i < lord.ownedPawns.Count; i++)
			{
				lord.ownedPawns[i].mindState.duty = new PawnDuty(ResourceBank.DutyDefOf.SoSDefendShip, baseCenter);
			}
		}
	}
}
