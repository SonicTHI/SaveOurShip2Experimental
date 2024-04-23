using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace SaveOurShip2
{
	class CompBlackBoxAI : ThingComp
	{
		bool PsychicDroneStarted = false;
		bool GreetedColonists = false;
		bool AlreadyFailedPersuasion = false;

		public override void CompTick()
		{
			base.CompTick();
			if (!PsychicDroneStarted && Find.TickManager.TicksGame % 666 == 0)
			{
				PsychicDroneStarted = true;
				Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.BlackBoxMissionPsychic"), TranslatorFormattedStringExtensions.Translate("SoS.BlackBoxMissionPsychicDesc"), LetterDefOf.NegativeEvent);
				SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(this.parent.Map);
				parent.GetComp<CompCauseGameCondition_PsychicEmanation>().droneLevel = PsychicDroneLevel.BadExtreme;
			}
			if (!GreetedColonists && Find.TickManager.TicksGame % 59 == 0)
			{
				foreach (Pawn p in this.parent.Map.mapPawns.AllPawnsSpawned)
				{
					if (p.IsColonist && p.GetRoom() != null && p.GetRoom() == RegionAndRoomQuery.RoomAt(new IntVec3(this.parent.Position.x-4, 0, this.parent.Position.z), this.parent.Map))
					{
						GreetedColonists = true;
						Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.BlackBoxMissionAIChamber"), TranslatorFormattedStringExtensions.Translate("SoS.BlackBoxMissionAIChamberDesc"),  LetterDefOf.NeutralEvent);
					}
				}
			}
		}

		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			base.PostDestroy(mode, previousMap);
			if(mode != DestroyMode.Vanish)
			{
				Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.AIDestroyed"), TranslatorFormattedStringExtensions.Translate("SoS.AIDestroyedDesc"), LetterDefOf.PositiveEvent);
				SpawnCrashSite();
			}
		}

		public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
		{
			List<FloatMenuOption> options = new List<FloatMenuOption>();
			foreach (FloatMenuOption op in base.CompFloatMenuOptions(selPawn))
				options.Add(op);
			if(!AlreadyFailedPersuasion)
				options.Add(new FloatMenuOption("Persuade to live", delegate { Job persuadeAI = new Job(DefDatabase<JobDef>.GetNamed("PersuadeAI"), this.parent); selPawn.jobs.TryTakeOrderedJob(persuadeAI); }));
			return options;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<bool>(ref PsychicDroneStarted, "DroneStarted");
			Scribe_Values.Look<bool>(ref GreetedColonists, "MetColonists");
			Scribe_Values.Look<bool>(ref AlreadyFailedPersuasion, "FailedPersuasion");
		}

		public void PersuadeMe(Pawn pawn)
		{
			if (Rand.Chance(0.05f * pawn.skills.GetSkill(SkillDefOf.Social).levelInt - 0.25f))
			{
				Success(pawn);
			}
			else if (Rand.Chance(0.05f * (20 - pawn.skills.GetSkill(SkillDefOf.Social).levelInt)+0.25f))
			{
				CriticalFailure(pawn);
			}
			else
			{
				Failure(pawn);
			}
		}

		private void Success(Pawn pawn)
		{
			Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.AIPersuadeSuccess"), TranslatorFormattedStringExtensions.Translate("SoS.AIPersuadeSuccessDesc", pawn.LabelShort), LetterDefOf.PositiveEvent);
			SpawnCrashSite();
			GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.AIPersonaCore), this.parent.Position, this.parent.Map, ThingPlaceMode.Near);
			pawn.skills.GetSkill(SkillDefOf.Social).Learn(6000);
			this.parent.Destroy(DestroyMode.Vanish);
		}

		private void Failure(Pawn pawn)
		{
			Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.AIPersuadeFailure"), TranslatorFormattedStringExtensions.Translate("SoS.AIPersuadeFailureDesc", pawn.LabelShort), LetterDefOf.NegativeEvent);
			pawn.skills.GetSkill(SkillDefOf.Social).Learn(4000);
			this.AlreadyFailedPersuasion = true;
		}

		private void CriticalFailure(Pawn pawn)
		{
			Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.AIPersuadeFailureCritical"), TranslatorFormattedStringExtensions.Translate("SoS.AIPersuadeFailureCriticalDesc", pawn.LabelShort), LetterDefOf.NegativeEvent);
			pawn.skills.GetSkill(SkillDefOf.Social).Learn(2000);
			pawn.health.AddHediff(HediffDefOf.Dementia, pawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).First());
			this.AlreadyFailedPersuasion = true;
		}

		private void SpawnCrashSite()
		{
			ShipInteriorMod2.WorldComp.Unlocks.Add("BlackBoxShipDefeated");
			ShipInteriorMod2.GenerateSite("ShipEngineImpactSite");
		}
	}
}
