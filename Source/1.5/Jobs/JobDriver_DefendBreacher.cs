using System;
using System.Collections.Generic;
using System.Text;
using Verse;
using Verse.AI;

namespace SaveOurShip2
{
	class JobDriver_DefendBreacher : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(TargetA, job);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			if (TargetA != LocalTargetInfo.Invalid)
				this.FailOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.ClosestTouch);
			yield return Toils_General.Wait(300, TargetIndex.None);
		}
	}
}
