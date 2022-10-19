using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using SaveOurShip2;

namespace RimWorld
{
    public class JobGiver_RepairShields : ThinkNode
    {
		static WorkGiver_FixBrokenDownBuilding fixGiver = new WorkGiver_FixBrokenDownBuilding();

        public override float GetPriority(Pawn pawn)
        {
			return 9f;
        }

        public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
        {
			if (pawn.RaceProps.IsMechanoid || (!pawn.skills.GetSkill(SkillDefOf.Construction).TotallyDisabled && pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) > 0))
            {
				foreach(CompShipCombatShield shield in pawn.Map.GetComponent<ShipHeatMapComp>().Shields)
                {
					if(shield.breakComp.BrokenDown && pawn.CanReserveAndReach(shield.parent,PathEndMode.ClosestTouch,Danger.Deadly))
                    {
						return new ThinkResult(fixGiver.JobOnThing(pawn, shield.parent), this);
                    }
                }
            }
			return ThinkResult.NoJob;
        }
    }
}
