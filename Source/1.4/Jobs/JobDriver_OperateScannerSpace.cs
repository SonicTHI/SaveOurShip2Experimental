using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace RimWorld
{
    public class JobDriver_OperateScannerSpace : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            Pawn pawn = this.pawn;
            LocalTargetInfo targetA = this.job.targetA;
            Job job = this.job;
            return pawn.Reserve(targetA, job, 1, -1, null, errorOnFailed);
        }

        [DebuggerHidden]
        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOn(delegate
            {
                CompLongRangeMineralScannerSpace compLongRangeMineralScanner = this.job.targetA.Thing.TryGetComp<CompLongRangeMineralScannerSpace>();
                return !compLongRangeMineralScanner.CanUseNow;
            });
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil work = new Toil();
            work.tickAction = delegate
            {
                Pawn actor = work.actor;
                Building building = (Building)actor.CurJob.targetA.Thing;
                CompLongRangeMineralScannerSpace comp = building.GetComp<CompLongRangeMineralScannerSpace>();
                comp.Used(actor);
                actor.skills.Learn(SkillDefOf.Intellectual, 0.035f, false);
                actor.GainComfortFromCellIfPossible();
            };
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            work.activeSkill = (() => SkillDefOf.Intellectual);
            yield return work;
        }
    }
}