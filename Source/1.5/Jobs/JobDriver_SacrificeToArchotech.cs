using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimWorld
{
    public class JobDriver_SacrificeToArchotech : JobDriver
    {
		private const TargetIndex TakeeIndex = TargetIndex.A;

		private const TargetIndex BedIndex = TargetIndex.B;

		protected Pawn Takee => (Pawn)job.GetTarget(TargetIndex.A).Thing;

		protected Building_ArchotechSpore ArchotechSpore => (Building_ArchotechSpore)job.GetTarget(TargetIndex.B).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (pawn.Reserve(Takee, job, 1, -1, null, true))
			{
				return pawn.Reserve(ArchotechSpore, job, 1, 0, null, true);
			}
			return false;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.job.count = 1;
			this.FailOnDestroyedOrNull(TargetIndex.A);
			this.FailOnDestroyedOrNull(TargetIndex.B);
			this.FailOnAggroMentalState(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnDespawnedNullOrForbidden(TargetIndex.B)
				.FailOn(() => !pawn.CanReach(ArchotechSpore, PathEndMode.Touch, Danger.Deadly))
				.FailOnSomeonePhysicallyInteracting(TargetIndex.A);
			Toil toil2 = Toils_Haul.StartCarryThing(TargetIndex.A);
			yield return toil2;
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
			yield return Toils_Reserve.Release(TargetIndex.B);
			Toil execute = new Toil();
			execute.initAction = delegate
			{
				SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(Find.CurrentMap);
				FleckMaker.Static(ArchotechSpore.Position, Map, FleckDefOf.PsycastAreaEffect, 10f);
				Takee.health.AddHediff(HediffDefOf.MissingBodyPart, Takee.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).First());
				if (!Takee.Dead)
					Takee.Kill(null);
				ArchotechSpore.AbsorbMind(Takee);
				ThoughtUtility.GiveThoughtsForPawnExecuted(Takee, pawn, PawnExecutionKind.GenericBrutal);
				TaleRecorder.RecordTale(TaleDefOf.ExecutedPrisoner, pawn, Takee);
			};
			execute.defaultCompleteMode = ToilCompleteMode.Instant;
			execute.activeSkill = (() => SkillDefOf.Melee);
			yield return execute;
		}

	}
}
