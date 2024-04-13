using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using RimWorld;

namespace SaveOurShip2
{
	public class JobDriver_ManShipBridge : JobDriver
	{
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			Toil gotoTurret = Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
			yield return gotoTurret;
			Toil man = new Toil();
			man.tickAction = delegate
			{
				Pawn actor = man.actor;
				Building building = (Building)actor.CurJob.targetA.Thing;
				building.GetComp<CompMannable>().ManForATick(actor);
				man.actor.rotationTracker.FaceCell(building.Position);
			};
			man.handlingFacing = true;
			man.defaultCompleteMode = ToilCompleteMode.Never;
			man.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
			yield return man;
		}
	}
}
