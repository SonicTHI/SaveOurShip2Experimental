using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimWorld
{
    public class JobDriver_OperateScannerSpace : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null, errorOnFailed);
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            CompLongRangeMineralScannerSpace scannerComp = this.job.targetA.Thing.TryGetComp<CompLongRangeMineralScannerSpace>();
            if (TargetA != LocalTargetInfo.Invalid)
                this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOn(() => !scannerComp.CanUseNow);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil work = ToilMaker.MakeToil("MakeNewToils");
            work.tickAction = delegate
            {
                Pawn actor = work.actor;
                Building building = (Building)actor.CurJob.targetA.Thing;
                scannerComp.Used(actor);
                actor.skills.Learn(SkillDefOf.Intellectual, 0.035f);
                actor.GainComfortFromCellIfPossible();
            };
            //work.PlaySustainerOrSound(scannerComp.Props.soundWorking, 1f);
            work.AddFailCondition(() => !scannerComp.CanUseNow);
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            work.activeSkill = (() => SkillDefOf.Intellectual);
            yield return work;
            yield break;
        }
    }
}