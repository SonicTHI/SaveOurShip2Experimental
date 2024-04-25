using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using RimWorld;
using Vehicles;

namespace SaveOurShip2
{
	public class JobGiver_RepairShields : ThinkNode
	{
		WorkGiver_FixBrokenDownBuilding fixGiver = new WorkGiver_FixBrokenDownBuilding();

		public override float GetPriority(Pawn pawn)
		{
			return 9f;
		}

		public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
		{
			var mapComp = pawn.Map.GetComponent<ShipMapComp>();
			if (mapComp.Shields.NullOrEmpty() || !(pawn.RaceProps.Humanlike || pawn.RaceProps.IsMechanoid) || pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) == 0)
				return ThinkResult.NoJob;
			if (pawn.RaceProps.IsMechanoid || !pawn.skills.GetSkill(SkillDefOf.Construction).TotallyDisabled)
			{
				foreach (CompShipHeatShield shield in mapComp.Shields.Where(s => !(s.parent is VehiclePawn) && s.breakComp.BrokenDown && pawn.CanReserveAndReach(s.parent, PathEndMode.ClosestTouch, Danger.Deadly)))
				{
					if (fixGiver.FindClosestComponent(pawn) != null)
						return new ThinkResult(fixGiver.JobOnThing(pawn, shield.parent), this);
				}
			}
			return ThinkResult.NoJob;
		}
	}
}
