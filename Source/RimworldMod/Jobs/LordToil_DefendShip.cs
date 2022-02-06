using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    class LordToil_DefendShip : LordToil
    {
        static DutyDef defendShip = DefDatabase<DutyDef>.GetNamed("SoSDefendShip");
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
			    lord.ownedPawns[i].mindState.duty = new PawnDuty(defendShip, baseCenter);
		    }
	    }
    }
}
