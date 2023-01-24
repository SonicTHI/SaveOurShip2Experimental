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

        private Building_ShipTurretTorpedo tube
        {
            get
            {
                LocalTargetInfo target = base.job.GetTarget((TargetIndex)1);
                return (Building_ShipTurretTorpedo)target;
            }
        }

        protected Thing torpedo
        {
            get
            {
                LocalTargetInfo target = base.job.GetTarget((TargetIndex)2);
                return (Thing)target;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (tube.torpComp.FullyLoaded)
                return false;
            return ReservationUtility.Reserve(base.pawn, base.job.targetA, base.job, 1, 1, null, true);
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.job.count = 1;
            ToilFailConditions.FailOnDespawnedNullOrForbidden<JobDriver_LoadTorpedoTube>(this, (TargetIndex)1);
            ToilFailConditions.FailOnBurningImmobile<JobDriver_LoadTorpedoTube>(this, (TargetIndex)1);
            yield return Toils_Reserve.Reserve((TargetIndex)1, 1, 1, (ReservationLayerDef)null);
            Toil reserveTorpedo = Toils_Reserve.Reserve((TargetIndex)2, 1, 1, (ReservationLayerDef)null);
            yield return reserveTorpedo;
            yield return ToilFailConditions.FailOnSomeonePhysicallyInteracting<Toil>(ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(Toils_Goto.GotoThing((TargetIndex)2, (PathEndMode)3), (TargetIndex)2), (TargetIndex)2);
            yield return ToilFailConditions.FailOnDestroyedNullOrForbidden<Toil>(Toils_Haul.StartCarryThing((TargetIndex)2, false, true, false), (TargetIndex)2);
            yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveTorpedo, (TargetIndex)2, (TargetIndex)0, true, (Predicate<Thing>)null);
            yield return Toils_Goto.GotoThing((TargetIndex)1, (PathEndMode)2);
            yield return ToilEffects.WithProgressBarToilDelay(ToilFailConditions.FailOnDestroyedNullOrForbidden<Toil>(ToilFailConditions.FailOnDestroyedNullOrForbidden<Toil>(Toils_General.Wait(Duration, (TargetIndex)0), (TargetIndex)2), (TargetIndex)1), (TargetIndex)1, false, -0.5f);
            Toil val = new Toil();
            val.initAction = delegate
            {
                tube.torpComp.LoadShell(torpedo.def,1);
                torpedo.Destroy(DestroyMode.Vanish);
            };
            val.defaultCompleteMode = (ToilCompleteMode)1;
            yield return val;
        }
    }
}
