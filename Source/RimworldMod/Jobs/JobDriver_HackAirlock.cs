using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimWorld
{
    class JobDriver_HackAirlock : JobDriver
    {
        float workDone;
        public static int hackWorkAmmount = 200;
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            if (TargetA != LocalTargetInfo.Invalid)
                this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.ClosestTouch);
            Toil hackIt = Toils_General.Wait(hackWorkAmmount, TargetA != LocalTargetInfo.Invalid ? TargetIndex.A : TargetIndex.None);
            hackIt.defaultCompleteMode = ToilCompleteMode.Delay;
            hackIt.initAction = delegate
            {
                workDone = 0;
            };
            hackIt.tickAction = delegate
            {
                workDone++;
            };
            hackIt.endConditions = new List<Func<JobCondition>>();
            hackIt.WithProgressBar(TargetIndex.A, () => workDone / hackWorkAmmount);
            hackIt.WithEffect(EffecterDefOf.DisabledByEMP, TargetIndex.A);
            hackIt.AddFinishAction(delegate {
                if (workDone >= hackWorkAmmount-10 && pawn.health.State == PawnHealthState.Mobile && TargetA.HasThing && !TargetA.Thing.DestroyedOrNull() && TargetA.Thing is Building_ShipAirlock)
                {
                    ((Building_ShipAirlock)TargetA.Thing).HackMe(pawn);
                }
            });
            yield return hackIt;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref workDone, "WorkDone");
        }
    }
}
