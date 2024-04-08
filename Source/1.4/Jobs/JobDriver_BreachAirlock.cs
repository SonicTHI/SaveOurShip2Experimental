using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.Sound;
using HarmonyLib;

namespace RimWorld
{
	class JobDriver_BreachAirlock : JobDriver
	{
		float workDone;
		public static int breachWorkAmmount = 1000;
		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(TargetA, job);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			if (TargetA != LocalTargetInfo.Invalid)
				this.FailOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.Goto(TargetIndex.A, PathEndMode.ClosestTouch);
			Toil breachIt = Toils_General.Wait(breachWorkAmmount, TargetA != LocalTargetInfo.Invalid ? TargetIndex.A : TargetIndex.None);
			breachIt.defaultCompleteMode = ToilCompleteMode.Delay;
			breachIt.initAction = delegate
			{
				workDone = 0;
			};
			breachIt.tickAction = delegate
			{
				workDone++;
			};
			breachIt.endConditions = new List<Func<JobCondition>>();
			breachIt.WithProgressBar(TargetIndex.A, () => workDone / breachWorkAmmount);
			breachIt.WithEffect(EffecterDefOf.ConstructMetal, TargetIndex.A);
			breachIt.AddFinishAction(delegate {
				if (workDone >= breachWorkAmmount - 10 && pawn.health.State == PawnHealthState.Mobile && TargetA.HasThing && !TargetA.Thing.DestroyedOrNull())
				{
					if (TargetA.Thing is Building_ShipAirlock)
					{
						((Building_ShipAirlock)TargetA.Thing).BreachMe(pawn);
					}
					else if (TargetA.Thing is Building_Door)
					{
						//DoorOpen is protected
						Building_Door b = TargetA.Thing as Building_Door;
						Traverse.Create(b).Field("openInt").SetValue(true);
						Traverse.Create(b).Field("ticksUntilClose").SetValue(110);
						Traverse.Create(b).Method("CheckClearReachabilityCacheBecauseOpenedOrClosed");
						Traverse.Create(b).Field("holdOpenInt").SetValue(true);
						if (b.DoorPowerOn)
							b.def.building.soundDoorOpenPowered.PlayOneShot(new TargetInfo(b.Position, b.Map, false));
						else
							b.def.building.soundDoorOpenManual.PlayOneShot(new TargetInfo(b.Position, b.Map, false));
						TargetA.Thing.TakeDamage(new DamageInfo(DamageDefOf.Cut, 200));
					}
				}
			});
			yield return breachIt;
		}
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<float>(ref workDone, "WorkDone");
		}
	}
}
