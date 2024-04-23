using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace SaveOurShip2
{
	class JobDriver_MergeWithSpore : JobDriver
	{
		private const int Duration = 600;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return ReservationUtility.Reserve(pawn, job.targetA, job) && TargetA.Thing.TryGetComp<CompBuildingConsciousness>().Consciousness == null;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			ToilFailConditions.FailOnDestroyedOrNull<JobDriver_MergeWithSpore>(this, (TargetIndex)1);
			yield return Toils_Reserve.Reserve((TargetIndex)1);
			yield return Toils_Goto.GotoThing((TargetIndex)1, (PathEndMode)2);
			yield return ToilEffects.WithProgressBarToilDelay(ToilFailConditions.FailOnDestroyedNullOrForbidden<Toil>(ToilFailConditions.FailOnDestroyedNullOrForbidden<Toil>(Toils_General.Wait(Duration, (TargetIndex)0), (TargetIndex)2), (TargetIndex)1), (TargetIndex)1, false, -0.5f);
			Toil val = new Toil();
			val.initAction = delegate
			{
				TargetA.Thing.TryGetComp<CompBuildingConsciousness>().InstallConsciousness(pawn);
			};
			val.defaultCompleteMode = (ToilCompleteMode)1;
			yield return val;
		}
	}
}
