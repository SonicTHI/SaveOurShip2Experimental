using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace RimWorld
{
    public class JobDriver_LoadTorpedoTube : JobDriver
    {
        private const int Duration = 600;

        private Building_ShipTurretTorpedo Tube
        {
            get
            {
                return (Building_ShipTurretTorpedo)base.job.GetTarget(TargetIndex.A);
            }
        }

        protected Thing Torpedo
        {
            get
            {
                return this.job.GetTarget(TargetIndex.B).Thing;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (Tube.torpComp.FullyLoaded)
                return false;
            return ReservationUtility.Reserve(base.pawn, base.job.targetA, base.job, 1, 1, null, true);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.job.count = 1;
            ToilFailConditions.FailOnDespawnedNullOrForbidden(this, TargetIndex.A);
            ToilFailConditions.FailOnBurningImmobile(this, TargetIndex.A);
            yield return Toils_Reserve.Reserve(TargetIndex.A, 1, 1,null);
            Toil reserveTorpedo = Toils_Reserve.Reserve(TargetIndex.B, 1, 1, null);
            yield return reserveTorpedo;
            yield return ToilFailConditions.FailOnSomeonePhysicallyInteracting(ToilFailConditions.FailOnDespawnedNullOrForbidden(Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch), TargetIndex.B), TargetIndex.B);
            yield return ToilFailConditions.FailOnDestroyedNullOrForbidden(Toils_Haul.StartCarryThing(TargetIndex.B, false, true, false), TargetIndex.B);
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveTorpedo, TargetIndex.B, TargetIndex.None, true, null);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return ToilEffects.WithProgressBarToilDelay(ToilFailConditions.FailOnDestroyedNullOrForbidden(ToilFailConditions.FailOnDestroyedNullOrForbidden(Toils_General.Wait(Duration, TargetIndex.None), TargetIndex.B), TargetIndex.A), TargetIndex.A, false, -0.5f);
            Toil toil = new Toil();
            toil.initAction = delegate
            {
                Tube.torpComp.LoadShell(Torpedo.def,1);
                Torpedo.Destroy(DestroyMode.Vanish);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
        }
    }
}
