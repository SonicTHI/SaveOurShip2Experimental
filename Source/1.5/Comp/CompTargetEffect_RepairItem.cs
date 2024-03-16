using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace RimWorld
{
    class CompTargetEffect_RepairItem : CompTargetEffect
	{
		public override void DoEffectOn(Pawn user, Thing target)
		{
			if (user.IsColonistPlayerControlled && user.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly))
			{
				Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("RepairItemWithGel"), target, parent);
				job.count = 1;
				user.jobs.TryTakeOrderedJob(job);
			}
		}
	}
}
