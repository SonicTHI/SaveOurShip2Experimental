using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimWorld
{
	public class JobGiver_AIDefendBreacher : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn defender)
		{
			List<Pawn> breachers = defender.GetLord().ownedPawns.Where(p => p.CurJobDef == ResourceBank.JobDefOf.BreachAirlock).ToList();
			if (breachers.Any())
			{
				Pawn pawn = breachers.RandomElement();
				if (defender.CanReserve(pawn, 5))
				{
					return new Job(ResourceBank.JobDefOf.DefendBreacher, pawn);
				}
			}
			return null;
		}
	}
}
