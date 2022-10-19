using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.AI;
using Verse;

namespace RimWorld
{
    class JobDriver_InstallConsciousness : JobDriver
    {
        private const int Duration = 600;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return ReservationUtility.Reserve(pawn, job.targetA, job) && ReservationUtility.Reserve(pawn, job.targetB, job) && TargetA.Thing.TryGetComp<CompBuildingConsciousness>().Consciousness==null;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            ToilFailConditions.FailOnDestroyedOrNull<JobDriver_InstallConsciousness>(this, (TargetIndex)1);
            ToilFailConditions.FailOnDestroyedOrNull<JobDriver_InstallConsciousness>(this, (TargetIndex)2);
            yield return Toils_Reserve.Reserve((TargetIndex)1);
            yield return Toils_Reserve.Reserve((TargetIndex)2);
            yield return ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(ToilFailConditions.FailOnDespawnedNullOrForbidden<Toil>(Toils_Goto.GotoThing((TargetIndex)2, (PathEndMode)3), (TargetIndex)2), (TargetIndex)1);
            yield return Toils_Haul.StartCarryThing((TargetIndex)2);
            yield return Toils_Goto.GotoThing((TargetIndex)1, (PathEndMode)2);
            yield return ToilEffects.WithProgressBarToilDelay(ToilFailConditions.FailOnDestroyedNullOrForbidden<Toil>(ToilFailConditions.FailOnDestroyedNullOrForbidden<Toil>(Toils_General.Wait(Duration, (TargetIndex)0), (TargetIndex)2), (TargetIndex)1), (TargetIndex)1, false, -0.5f);
            Toil val = new Toil();
            val.initAction = delegate
            {
                TargetA.Thing.TryGetComp<CompBuildingConsciousness>().InstallConsciousness(TargetB.Thing);
            };
            val.defaultCompleteMode = (ToilCompleteMode)1;
            yield return val;
        }
    }
}
