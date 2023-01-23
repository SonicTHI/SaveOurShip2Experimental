using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld
{
	public class JobDriver_FleeVacuum : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (pawn.CanReach(TargetA, PathEndMode.Touch, Danger.Deadly))
				return true;
			return false;
		}
		public override IEnumerable<Toil> MakeNewToils()
		{
			if (TargetA != LocalTargetInfo.Invalid)
				this.FailOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
		}
	}
}