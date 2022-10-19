using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimWorld
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
                Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("LetterLabelBlackBoxMissionPsychic"), TranslatorFormattedStringExtensions.Translate("LetterBlackBoxMissionPsychic"), LetterDefOf.NegativeEvent);
                SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(this.parent.Map);
                typeof(CompCauseGameCondition_PsychicEmanation).GetField("droneLevel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this.parent.GetComp<CompCauseGameCondition_PsychicEmanation>(), PsychicDroneLevel.BadExtreme);
            }
            if (!GreetedColonists && Find.TickManager.TicksGame % 59 == 0)
            {
                foreach (Pawn p in this.parent.Map.mapPawns.AllPawnsSpawned)
                {
                    if (p.IsColonist && p.GetRoom() != null && p.GetRoom() == RegionAndRoomQuery.RoomAt(new IntVec3(this.parent.Position.x-4, 0, this.parent.Position.z), this.parent.Map))
                    {
                        GreetedColonists = true;
                        Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("LetterLabelBlackBoxMissionAIChamber"), TranslatorFormattedStringExtensions.Translate("LetterBlackBoxMissionAIChamber"), LetterDefOf.NeutralEvent);
                    }
                }
            }
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            if(mode != DestroyMode.Vanish)
            {
                Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("LetterLabelAIDestroyed"), TranslatorFormattedStringExtensions.Translate("LetterAIDestroyed"), LetterDefOf.PositiveEvent);
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
            Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("LetterLabelAIPersuadeSuccess"), TranslatorFormattedStringExtensions.Translate("LetterAIPersuadeSuccess",pawn.LabelShort), LetterDefOf.PositiveEvent);
            SpawnCrashSite();
            GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDefOf.AIPersonaCore), this.parent.Position, this.parent.Map, ThingPlaceMode.Near);
            pawn.skills.GetSkill(SkillDefOf.Social).Learn(6000);
            this.parent.Destroy(DestroyMode.Vanish);
        }

        private void Failure(Pawn pawn)
        {
            Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("LetterLabelAIPersuadeFailure"), TranslatorFormattedStringExtensions.Translate("LetterAIPersuadeFailure",pawn.LabelShort), LetterDefOf.NegativeEvent);
            pawn.skills.GetSkill(SkillDefOf.Social).Learn(4000);
            this.AlreadyFailedPersuasion = true;
        }

        private void CriticalFailure(Pawn pawn)
        {
            Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("LetterLabelAIPersuadeFailureCritical"), TranslatorFormattedStringExtensions.Translate("LetterAIPersuadeFailureCritical",pawn.LabelShort), LetterDefOf.NegativeEvent);
            pawn.skills.GetSkill(SkillDefOf.Social).Learn(2000);
            pawn.health.AddHediff(HediffDefOf.Dementia, pawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).First());
            this.AlreadyFailedPersuasion = true;
        }

        private void SpawnCrashSite()
        {
            WorldSwitchUtility.PastWorldTracker.Unlocks.Add("BlackBoxShipDefeated");
            ShipInteriorMod2.GenerateImpactSite();
        }
    }
}
