using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimWorld
{
    class JobDriver_Holodeck : JobDriver_WatchTelevision
    {

		public override IEnumerable<Toil> MakeNewToils()
		{
			this.EndOnDespawnedOrNull(TargetIndex.A);
			Toil getToHolodeck = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
			getToHolodeck.AddFinishAction(delegate
			{
				if(TargetA.Thing.TryGetComp<CompHolodeck>().CurSkill==null||(pawn.skills.skills.Where(rec=>rec.def==TargetA.Thing.TryGetComp<CompHolodeck>().CurSkill).FirstOrDefault().TotallyDisabled))
                {
					TargetA.Thing.TryGetComp<CompHolodeck>().StartHolodeck(pawn);
                }
			});
			yield return getToHolodeck;
			Toil watch = new Toil();
			watch.AddPreTickAction(delegate
			{
				WatchTickAction();
			});
			watch.AddFinishAction(delegate
			{
				int scoreStageIndex = RoomStatDefOf.Impressiveness.GetScoreStageIndex(TargetA.Thing.GetStatValue(StatDefOf.JoyGainFactor) * 80);
				if (pawn.needs.mood != null && ThoughtDefOf.AteInImpressiveDiningRoom.stages[scoreStageIndex] != null)
				{
					pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(ThoughtDefOf.JoyActivityInImpressiveRecRoom, scoreStageIndex));
				}
				if(pawn.Map.reservationManager.ReservedBy(TargetA,pawn))
					pawn.Map.reservationManager.Release(TargetA, pawn, job);
				if (!pawn.Map.reservationManager.AllReservedThings().Contains(TargetA.Thing))
                {
					TargetA.Thing.TryGetComp<CompHolodeck>().StopHolodeck();
                }
			});
			watch.defaultCompleteMode = ToilCompleteMode.Delay;
			watch.defaultDuration = job.def.joyDuration;
			watch.handlingFacing = true;
			if (base.TargetA.Thing.def.building != null && base.TargetA.Thing.def.building.effectWatching != null)
			{
				watch.WithEffect(() => base.TargetA.Thing.def.building.effectWatching, EffectTargetGetter);
			}
			yield return watch;
			LocalTargetInfo EffectTargetGetter()
			{
				return base.TargetA.Thing.OccupiedRect().RandomCell + IntVec3.North.RotatedBy(base.TargetA.Thing.Rotation);
			}
		}

		public override void WatchTickAction()
		{
			if (TargetThingA == null)
				return;
			if(pawn.HashOffsetTicks() % 69 == 0)
            {
				IntVec3 newCell = new IntVec3(Rand.RangeInclusive(-3, 3), 0, Rand.RangeInclusive(-3, 3));
				IntVec3 dist = pawn.Position + newCell - TargetLocA;
				dist.x = Math.Abs(dist.x);
				dist.z = Math.Abs(dist.z);
				if(TargetA.Thing.def.building!=null && dist.x<=TargetA.Thing.def.building.watchBuildingStandRectWidth/2 && dist.z<=TargetA.Thing.def.building.watchBuildingStandRectWidth/2)
                {
					if (RegionAndRoomQuery.RoomAt(pawn.Position+newCell,pawn.Map)==TargetA.Thing.GetRoom() && pawn.CanReach(pawn.Position + newCell, PathEndMode.OnCell, Danger.None))
					{
						pawn.pather.StartPath(pawn.Position + newCell, PathEndMode.OnCell);
						pawn.rotationTracker.FaceCell(pawn.Position + newCell);
					}
				}
            }
			if (pawn.needs != null && pawn.needs.comfort != null)
			{
				pawn.needs.comfort.ComfortUsed(0.5f);
			}
			CompHolodeck deck = TargetThingA.TryGetComp<CompHolodeck>();
			if(deck.CurSkill!=null && pawn.skills!=null && pawn.skills.GetSkill(deck.CurSkill)!=null)
				pawn.skills.GetSkill(deck.CurSkill).Learn(job.def.joyXpPerTick*TargetThingA.GetStatValue(StatDefOf.JoyGainFactor));
			JoyUtility.JoyTickCheckEnd(pawn, JoyTickFullJoyAction.EndJob, 1f, (Building)TargetThingA);
		}
    }
}
