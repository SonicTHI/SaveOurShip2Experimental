using RimWorld.QuestGen;
using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class Building_ArchotechSpore : Building_ShipBridge
    {
        private static Graphic eyeGraphic = GraphicDatabase.Get(typeof(Graphic_Single), "Things/Building/Ship/Archotech_DeusXMachine_Eye", ShaderDatabase.MetaOverlay, new Vector2(5, 8), Color.white, Color.white);
        public float mood => Consciousness==null ? 1 : Consciousness.needs.mood.CurLevel * 2; //1 is neutral, 2 is super happy, 0 is MURDER
        public float fieldStrength = 0f;
        static int lastGiftTick = 0;
        int lastPrankTick = 0;
        bool unlockedPsy = false;
        bool ideoCrisis = false;
        ResearchProjectDef PsyProj = ResearchProjectDef.Named("ArchotechPsychicField");
        ResearchProjectDef ManipProj = ResearchProjectDef.Named("ArchotechPsychicManipulation");
        ResearchProjectDef PsylinkProj = ResearchProjectDef.Named("ArchotechPsylink");
        StatDef pillars = StatDef.Named("ArchotechPillarsConnected");
        public bool newSpore = false;
        int MeditationTicks = 0;

        Pawn Consciousness => ConsciousnessComp.Consciousness;
        CompBuildingConsciousness ConsciousnessComp
        {
            get
            {
                if (cachedConsciousnessComp == null)
                {
                    cachedConsciousnessComp = this.TryGetComp<CompBuildingConsciousness>();
                }
                return cachedConsciousnessComp;
            }
        }
        CompBuildingConsciousness cachedConsciousnessComp = null;

        static int endgameFriendly;
        static int endgameNeutral;
        static int endgameEnemy;
        static List<Faction> alliedFactions, neutralFactions, enemyFactions;

        public int NumConnectedPillars => (int)this.GetStatValue(pillars);

        public override void Draw()
        {
            base.Draw();
            if (Consciousness == null)
                return;
            Color eyeColor = Color.red;
            if (mood < 1f)
                eyeColor = new Color(1f, mood, 0);
            else
                eyeColor = new Color(2f - mood, 2f-mood, mood - 1f);
            eyeColor.a = Mathf.Cos(Mathf.PI * (float)Find.TickManager.TicksGame / 256f);
            eyeGraphic.GetColoredVersion(ShaderDatabase.MetaOverlay, eyeColor, eyeColor).Draw(new Vector3(this.DrawPos.x, this.DrawPos.y + 1f, this.DrawPos.z), this.Rotation, this);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoSSporeBuilt"), TranslatorFormattedStringExtensions.Translate("SoSSporeBuiltDesc"), LetterDefOf.PositiveEvent);
                newSpore = true;
            }
            if(Consciousness==null && !newSpore)
            {
                ConsciousnessComp.GenerateAIPawn();
            }
        }

        public override void TickRare()
        {
            base.TickRare();
            if (Consciousness == null)
                return;
            int tick = Find.TickManager.TicksGame;
            if (!WorldSwitchUtility.PastWorldTracker.startedEndgame)
            {
                WorldSwitchUtility.PastWorldTracker.startedEndgame = true;
                WorldSwitchUtility.PastWorldTracker.Unlocks.Add("ArchotechUplink");
                Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoSFindPillars"), TranslatorFormattedStringExtensions.Translate("SoSFindPillarsDesc"), LetterDefOf.PositiveEvent);
                //lastGiftTick = tick;
                lastPrankTick = tick+Rand.Range(40000,80000);
                foreach(Building b in ShipUtility.ShipBuildingsAttachedTo(this))
                {
                    if (b is Building_ShipBridge)
                        this.ShipName = ((Building_ShipBridge)b).ShipName;
                }
            }

            if(unlockedPsy || PsyProj.IsFinished)
            {
                unlockedPsy = true;
                fieldStrength += 0.001f * (1+ NumConnectedPillars);
            }

            if (tick-lastGiftTick > 90000 * (5- NumConnectedPillars) * (3-mood))
            {
                int numUnlock = 0;
                foreach (ArchotechGiftDef def in DefDatabase<ArchotechGiftDef>.AllDefs)
                {
                    if (def.research.IsFinished)
                    {
                        numUnlock++;
                    }
                }
                if (numUnlock > 5)
                    numUnlock = 5;
                if (numUnlock > 0)
                {
                    lastGiftTick = tick;
                    Slate slate = new Slate();
                    slate.Set<string>("quest_name", "A Gift From " + Consciousness.Name.ToStringFull);
                    slate.Set<string>("archotech_name", Consciousness.Name.ToStringShort);
                    slate.Set<Map>("map", this.Map);
                    slate.Set<int>("value", numUnlock*1000);
                    Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(DefDatabase<QuestScriptDef>.GetNamed("ArchotechGiftQuest"), slate);
                    Find.LetterStack.ReceiveLetter(quest.name, quest.description, LetterDefOf.PositiveEvent, null, null, quest, null, null);
                }
                else
                {
                    lastGiftTick += 60000;
                }
            }
            if (mood < 0.8f && tick > lastPrankTick)
                PlayPrank(tick);

            if(!ideoCrisis && tick % 20000 == 0 && ModLister.IdeologyInstalled)
            {
                ideoCrisis = true;

                int highestRelation = -100;
                Pawn highestPawn = Consciousness;
                foreach(Pawn p in this.Map.mapPawns.AllPawnsSpawned)
                {
                    if(Consciousness.relations.OpinionOf(p)>highestRelation)
                    {
                        highestRelation = Consciousness.relations.OpinionOf(p);
                        highestPawn = p;
                    }
                }

                DiaNode node = new DiaNode(TranslatorFormattedStringExtensions.Translate("ArchotechIdeoChoice", Consciousness.Name.ToStringShort, highestPawn.Name.ToStringShort));
                DiaOption keepIdeo = new DiaOption("Discover the truth of "+Consciousness.Ideo.name);
                DiaOption newIdeo = new DiaOption("Found a new ideoligion");
                node.options.Add(keepIdeo);
                node.options.Add(newIdeo);
                DiaNode nodeKeepFaith;
                if (Consciousness.Ideo.memes.Contains(ShipInteriorMod2.Archism))
                    nodeKeepFaith = new DiaNode(TranslatorFormattedStringExtensions.Translate("ArchotechIdeoCertaintyArchist", Consciousness.Name.ToStringShort, Consciousness.Ideo.name));
                else if (Consciousness.Ideo.memes.Contains(MemeDefOf.Structure_Ideological))
                    nodeKeepFaith = new DiaNode(TranslatorFormattedStringExtensions.Translate("ArchotechIdeoCertaintyEthical", Consciousness.Name.ToStringShort, Consciousness.Ideo.name));
                else if (Consciousness.Ideo.memes.Contains(DefDatabase<MemeDef>.GetNamed("Structure_Animist")))
                    nodeKeepFaith = new DiaNode(TranslatorFormattedStringExtensions.Translate("ArchotechIdeoCertaintySpirits", Consciousness.Name.ToStringShort, Consciousness.Ideo.name));
                else
                    nodeKeepFaith = new DiaNode(TranslatorFormattedStringExtensions.Translate("ArchotechIdeoCertaintyGod", Consciousness.Name.ToStringShort, Consciousness.Ideo.KeyDeityName, Consciousness.Ideo.name));
                nodeKeepFaith.options.Add(new DiaOption("All hail the one truth!"));
                nodeKeepFaith.options[0].resolveTree = true;
                keepIdeo.link = nodeKeepFaith;
                newIdeo.action = delegate
                {
                    NotNowIdeology.ArchoFlag = true;
                    Page_ConfigureIdeo page = new Page_ConfigureIdeo();
                    page.nextAct = delegate
                    {
                        Ideo ideo = page.ideo;
                        DiaNode newNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("ArchotechIdeoHeresy", ideo));
                        newNode.options.Add(new DiaOption("All hail the one truth!"));
                        newNode.options[0].resolveTree = true;
                        Dialog_NodeTree newTree = new Dialog_NodeTree(newNode);
                        Consciousness.ideo.SetIdeo(ideo);
                        foreach (Pawn convertee in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists_NoLodgers)
                        {
                            convertee.ideo.IdeoConversionAttempt(0.9f, ideo);
                        }
                        Find.WindowStack.Add(newTree);
                    };
                    Find.WindowStack.Add(page);
                };
                newIdeo.resolveTree = true;
                Dialog_NodeTree dialog_NodeTree = new Dialog_NodeTree(node, true, false, "Crisis of Faith");
                dialog_NodeTree.silenceAmbientSound = false;
                Find.WindowStack.Add(dialog_NodeTree);
            }
        }

        public void AbsorbMind(Pawn mind)
        {
            foreach(SkillRecord skill in mind.skills.skills)
            {
                SkillRecord mySkill = Consciousness.skills.GetSkill(skill.def);
                if (mySkill!=null)
                {
                    if (skill.levelInt > mySkill.levelInt)
                    {
                        int limit = 10;
                        while (skill.levelInt > mySkill.levelInt && limit > 0)
                        {
                            mySkill.Learn(mySkill.XpRequiredForLevelUp);
                            limit--;
                        }
                    }
                    else
                        Consciousness.skills.Learn(skill.def, skill.levelInt * 200f);
                }
            }
            Consciousness.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("ArchotechSporeAbsorbedMind"));
        }

        public void MeditationTick()
        {
            fieldStrength += 0.00005f;
            MeditationTicks++;
            if(MeditationTicks > 6000)
            {
                MeditationTicks = 0;
                Consciousness.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("ArchotechSporeMeditation"));
            }
        }

        void PlayPrank(int tick)
        {
            lastPrankTick = tick + Rand.Range(40000, 80000);
            int prank = Rand.RangeInclusive(0, 5);
            if (prank == 0)
            {
                Find.LetterStack.ReceiveLetter("ArchotechPrank".Translate(), "ArchotechPrankCancer".Translate(), LetterDefOf.NegativeEvent);
                int numCancers = Rand.RangeInclusive(2, 5);
                for (int i = 0; i < numCancers; i++)
                {
                    Pawn victim = this.Map.mapPawns.FreeColonistsAndPrisonersSpawned.RandomElement();
                    HediffGiverUtility.TryApply(victim, HediffDefOf.Carcinoma, null, true);
                }
            }
            else if (prank == 1)
            {
                Find.LetterStack.ReceiveLetter("ArchotechPrank".Translate(), "ArchotechPrankDamage".Translate(), LetterDefOf.NegativeEvent);
                int numBrokenDevices = Rand.RangeInclusive(5, 12);
                for (int i = 0; i < numBrokenDevices; i++)
                {
                    IEnumerable<Building> breakableThings = this.Map.listerBuildings.allBuildingsColonist.Where(b => b.TryGetComp<CompBreakdownable>() != null && !b.TryGetComp<CompBreakdownable>().BrokenDown);
                    if (breakableThings.EnumerableNullOrEmpty())
                        break;
                    breakableThings.RandomElement().TryGetComp<CompBreakdownable>().DoBreakdown();
                }
            }
            else if (prank == 2)
            {
                Pawn worstA = null;
                Pawn worstB = null;
                float worstScore = 9999;
                List<Pawn> pawns = this.Map.mapPawns.FreeColonistsAndPrisonersSpawned;
                List<Pawn> pawns2 = this.Map.mapPawns.FreeColonistsAndPrisonersSpawned;
                foreach (Pawn pawn in pawns)
                {
                    foreach (Pawn p in pawns2)
                    {
                        if (pawn == p || LovePartnerRelationUtility.ExistingLovePartner(pawn)==p || ((pawn.gender == p.gender) && (!p.story.traits.HasTrait(TraitDefOf.Gay) || pawn.story.traits.HasTrait(TraitDefOf.Gay))))
                            continue;
                        float score = pawn.relations.SecondaryRomanceChanceFactor(p) * Mathf.InverseLerp(5f, 100f, pawn.relations.OpinionOf(p));
                        if (score < worstScore)
                        {
                            worstScore = score;
                            worstA = pawn;
                            worstB = p;
                        }
                    }
                }
                if (worstA != null && worstB != null)
                {
                    Find.LetterStack.ReceiveLetter("ArchotechPrank".Translate(), "ArchotechPrankLovers".Translate(worstA.Label, worstB.Label), LetterDefOf.NegativeEvent);
                    InteractionWorker_RomanceAttempt worker = new InteractionWorker_RomanceAttempt();
                    object[] parms = new object[] { worstA, null };
                    typeof(InteractionWorker_RomanceAttempt).GetMethod("BreakLoverAndFianceRelations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(worker, parms);
                    for (int i = 0; i < ((List<Pawn>)parms[1]).Count; i++)
                    {
                        typeof(InteractionWorker_RomanceAttempt).GetMethod("TryAddCheaterThought", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(worker, new object[] { ((List<Pawn>)parms[1])[i], worstA });
                    }
                    object[] parms2 = new object[] { worstB, null };
                    typeof(InteractionWorker_RomanceAttempt).GetMethod("BreakLoverAndFianceRelations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(worker, parms2);
                    for (int i = 0; i < ((List<Pawn>)parms2[1]).Count; i++)
                    {
                        typeof(InteractionWorker_RomanceAttempt).GetMethod("TryAddCheaterThought", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(worker, new object[] { ((List<Pawn>)parms2[1])[i], worstB });
                    }
                    worstA.relations.TryRemoveDirectRelation(PawnRelationDefOf.ExLover, worstB);
                    worstA.relations.AddDirectRelation(PawnRelationDefOf.Lover, worstB);
                    TaleRecorder.RecordTale(TaleDefOf.BecameLover, worstA, worstB);
                    if (worstA.needs.mood != null)
                    {
                        worstA.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.BrokeUpWithMe, worstB);
                        worstA.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.FailedRomanceAttemptOnMe, worstB);
                        worstA.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.FailedRomanceAttemptOnMeLowOpinionMood, worstB);
                    }
                    if (worstB.needs.mood != null)
                    {
                        worstB.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.BrokeUpWithMe, worstA);
                        worstB.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.FailedRomanceAttemptOnMe, worstA);
                        worstB.needs.mood.thoughts.memories.RemoveMemoriesOfDefWhereOtherPawnIs(ThoughtDefOf.FailedRomanceAttemptOnMeLowOpinionMood, worstA);
                    }
                    if (PawnUtility.ShouldSendNotificationAbout(worstB) || PawnUtility.ShouldSendNotificationAbout(worstA))
                    {
                        object[] parms3 = new object[] { worstA, worstB, parms[1], parms2[1], null, null, null, null };
                        typeof(InteractionWorker_RomanceAttempt).GetMethod("GetNewLoversLetter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(worker, parms3);
                        Find.LetterStack.ReceiveLetter((string)parms3[5], ((string)parms3[4]).Translate(parms3[0], parms3[1], parms3[2], parms3[3]), LetterDefOf.PositiveEvent);
                    }
                    LovePartnerRelationUtility.TryToShareBed(worstA, worstB);
                }
            }
            else if(prank==3)
            {
                Find.LetterStack.ReceiveLetter("ArchotechPrank".Translate(), "ArchotechPrankSquirrels".Translate(), LetterDefOf.NegativeEvent);
                int numSquirrels = this.Map.mapPawns.ColonistsSpawnedCount;
                for(int i=0;i<numSquirrels;i++)
                {
                    Pawn p=PawnGenerator.GeneratePawn(PawnKindDef.Named("Squirrel"));
                    p.Position = this.Position;
                    p.SpawnSetup(this.Map, false);
                    p.health.AddHediff(HediffDefOf.Scaria);
                    p.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.ManhunterPermanent);
                }
            }
            else if(prank==4)
            {
                Faction fac = Find.FactionManager.AllFactions.Where(f => f.def.CanEverBeNonHostile && !f.def.isPlayer).RandomElement();
                fac.TryAffectGoodwillWith(Faction.OfPlayer, -10);
                Find.LetterStack.ReceiveLetter("ArchotechPrank".Translate(), "ArchotechPrankReputation".Translate(fac.Name), LetterDefOf.NegativeEvent);
            }
            else if(prank==5)
            {
                Pawn victim = this.Map.mapPawns.FreeColonists.RandomElement();
                Find.LetterStack.ReceiveLetter("ArchotechPrank".Translate(), "ArchotechPrankSecret".Translate(victim), LetterDefOf.NegativeEvent);
                foreach(Pawn p in this.Map.mapPawns.FreeColonistsAndPrisoners)
                {
                    if (p == victim)
                        continue;
                    p.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("ArchotechToldSecret"), victim);
                }
            }
            Consciousness.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.Catharsis);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref fieldStrength, "FieldStrength");
            Scribe_Values.Look<int>(ref lastGiftTick, "LastGift");
            Scribe_Values.Look<bool>(ref newSpore, "NewSpore");
            Scribe_Values.Look<bool>(ref ideoCrisis, "IdeoCrisis");
        }

        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            text += "\nMood: " + Mathf.Round(mood * 50f) + "\nPsychic field strength: "+fieldStrength;
            return text;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            List<Gizmo> giz = new List<Gizmo>();
            giz.AddRange(base.GetGizmos());
            if (Prefs.DevMode)
            {
                Command_Action BoostField = new Command_Action
                {
                    action = delegate
                    {
                        fieldStrength++;
                    },
                    defaultLabel = "Dev: +1 field strength",
                };
                giz.Add(BoostField);
                /*Command_Action MoodUp = new Command_Action
                {
                    action = delegate
                    {
                        mood+=0.2f;
                        if (mood > 2)
                            mood = 2;
                    },
                    defaultLabel = "Dev: +10 mood",
                };
                giz.Add(MoodUp);
                Command_Action MoodDown = new Command_Action
                {
                    action = delegate
                    {
                        mood-=0.2f;
                        if (mood < 0)
                            mood = 0;
                    },
                    defaultLabel = "Dev: -10 mood",
                };
                giz.Add(MoodDown);*/

                Command_Action Prank = new Command_Action
                {
                    action = delegate
                    {
                        PlayPrank(Find.TickManager.TicksGame);
                    },
                    defaultLabel = "Dev: Play Prank",
                };
                giz.Add(Prank);
            }
            if(ManipProj.IsFinished)
            {
                Command_Action GenerateSoothe = new Command_Action
                {
                    action = delegate
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        foreach(Map map in Find.Maps)
                        {
                            if (!map.gameConditionManager.ConditionIsActive(GameConditionDefOf.PsychicSoothe))
                            {
                                FloatMenuOption op = new FloatMenuOption(map.Parent.Label, delegate { //Affects both genders, so we have to do this twice
                                    GameCondition_PsychicEmanation soothe = (GameCondition_PsychicEmanation)GameConditionMaker.MakeCondition(GameConditionDefOf.PsychicSoothe, 60000);
                                    soothe.conditionCauser = this; 
                                    soothe.gender = Gender.Female; 
                                    map.gameConditionManager.RegisterCondition(soothe);
                                    soothe = (GameCondition_PsychicEmanation)GameConditionMaker.MakeCondition(GameConditionDefOf.PsychicSoothe, 60000);
                                    soothe.conditionCauser = this;
                                    soothe.gender = Gender.Male;
                                    map.gameConditionManager.RegisterCondition(soothe);
                                    SoundDefOf.PsychicSootheGlobal.PlayOneShotOnCamera(Find.CurrentMap); 
                                    FleckMaker.Static(Position, Map, FleckDefOf.PsycastAreaEffect, 10f); 
                                    fieldStrength -= 2; 
                                });
                                options.Add(op);
                            }
                        }
                        if (options.Count > 0)
                        {
                            FloatMenu menu = new FloatMenu(options);
                            Find.WindowStack.Add(menu);
                        }
                    },
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ArchotechCommandSoothe"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ArchotechCommandSootheDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/ArchotechCommandSoothe")
                };
                if (fieldStrength < 2)
                {
                    GenerateSoothe.disabled = true;
                    GenerateSoothe.disabledReason = TranslatorFormattedStringExtensions.Translate("ArchotechNotEnoughFieldStrength");
                }
                giz.Add(GenerateSoothe);
                Command_Action shockABrain = new Command_Action
                {
                    action = delegate
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        foreach (Map map in Find.Maps)
                        {
                            FloatMenuOption op = new FloatMenuOption(map.Parent.Label, delegate {
                                CameraJumper.TryJump(Position, map);
                                TargetForBrainShock();
                            });
                            options.Add(op);
                        }
                        if (options.Count > 0)
                        {
                            FloatMenu menu = new FloatMenu(options);
                            Find.WindowStack.Add(menu);
                        }
                    },
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ArchotechCommandShock"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ArchotechCommandShockDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/ArchotechCommandShock")
                };
                if (fieldStrength < 0.5f)
                {
                    shockABrain.disabled = true;
                    shockABrain.disabledReason = TranslatorFormattedStringExtensions.Translate("ArchotechNotEnoughFieldStrength");
                }
                giz.Add(shockABrain);
                Command_Action insaneInTheBrain = new Command_Action
                {
                    action = delegate
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        foreach (Map map in Find.Maps)
                        {
                            FloatMenuOption op = new FloatMenuOption(map.Parent.Label, delegate {
                                CameraJumper.TryJump(Position, map);
                                TargetForInsanity();
                            });
                            options.Add(op);
                        }
                        if (options.Count > 0)
                        {
                            FloatMenu menu = new FloatMenu(options);
                            Find.WindowStack.Add(menu);
                        }
                    },
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ArchotechCommandInsanity"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ArchotechCommandInsanityDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/ArchotechCommandInsanity")
                };
                if (fieldStrength < 1f)
                {
                    insaneInTheBrain.disabled = true;
                    insaneInTheBrain.disabledReason = TranslatorFormattedStringExtensions.Translate("ArchotechNotEnoughFieldStrength");
                }
                giz.Add(insaneInTheBrain);
            }
            if (ModLister.RoyaltyInstalled && Find.ResearchManager.GetProgress(PsylinkProj) >= PsylinkProj.CostApparent)
            {
                Command_Action formPsylink = new Command_Action
                {
                    action = delegate
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        foreach (Map map in Find.Maps)
                        {
                            FloatMenuOption op = new FloatMenuOption(map.Parent.Label, delegate {
                                CameraJumper.TryJump(Position, map);
                                Find.Targeter.BeginTargeting(TargetingParameters.ForAttackAny(), delegate (LocalTargetInfo target) {
                                    if (target.HasThing && target.Thing is Pawn)
                                    {
                                        Pawn pawn = (Pawn)target.Thing;
                                        if (pawn.GetPsylinkLevel() < 6)
                                        {
                                            pawn.ChangePsylinkLevel(1);
                                            fieldStrength -= 10f;
                                            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(Find.CurrentMap);
                                            FleckMaker.Static(Position, Map, FleckDefOf.PsycastAreaEffect, 10f);
                                        }
                                    }
                                });
                            });
                            options.Add(op);
                        }
                        if (options.Count > 0)
                        {
                            FloatMenu menu = new FloatMenu(options);
                            Find.WindowStack.Add(menu);
                        }
                    },
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ArchotechCommandPsylink"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ArchotechCommandPsylinkDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/ArchotechCommandPsylink")
                };
                if (fieldStrength < 10f)
                {
                    formPsylink.disabled = true;
                    formPsylink.disabledReason = TranslatorFormattedStringExtensions.Translate("ArchotechNotEnoughFieldStrength");
                }
                giz.Add(formPsylink);
            }
            if(NumConnectedPillars>=4)
            {
                Command_Action winGame = new Command_Action
                {
                    action = delegate
                    {
                        DiaNode node = new DiaNode(TranslatorFormattedStringExtensions.Translate("SoSAllPillarsDesc", Consciousness.Name.ToStringFull));
                        DiaOption end = new DiaOption("Remake the world");
                        DiaOption cancel = new DiaOption("Remain mortal a while longer");
                        cancel.resolveTree = true;
                        node.options.Add(end);
                        node.options.Add(cancel);

                        DiaNode friendlyNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("SoSWinGameAllies"));
                        DiaOption friendlyAlly = new DiaOption("Give them a choice - join or don't");
                        DiaOption friendlyTakeover = new DiaOption("Annex them by force");
                        friendlyAlly.action = delegate { endgameFriendly = 0; };
                        friendlyTakeover.action = delegate { endgameFriendly = 1; };
                        friendlyNode.options.Add(friendlyAlly);
                        friendlyNode.options.Add(friendlyTakeover);

                        DiaNode neutralNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("SoSWinGameNeutral"));
                        DiaOption neutralAlly = new DiaOption("Give them a choice - join or don't");
                        DiaOption neutralTakeover = new DiaOption("Annex them by force");
                        DiaOption neutralKill = new DiaOption("Destroy them");
                        neutralAlly.action = delegate { endgameNeutral = 0; };
                        neutralTakeover.action = delegate { endgameNeutral = 1; };
                        neutralKill.action = delegate { endgameNeutral = 2; };
                        neutralNode.options.Add(neutralAlly);
                        neutralNode.options.Add(neutralTakeover);
                        neutralNode.options.Add(neutralKill);

                        DiaNode enemyNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("SoSWinGameEnemies"));
                        DiaOption enemyTakeover = new DiaOption("Annex them by force");
                        DiaOption enemyKill = new DiaOption("Destroy them");
                        enemyTakeover.action = delegate { endgameEnemy = 1; };
                        enemyKill.action = delegate { endgameEnemy = 2; };
                        enemyNode.options.Add(enemyTakeover);
                        enemyNode.options.Add(enemyKill);

                        alliedFactions = Find.FactionManager.AllFactionsVisible.Where(fac => fac!=Faction.OfPlayer && fac.PlayerRelationKind == FactionRelationKind.Ally).ToList();
                        neutralFactions = Find.FactionManager.AllFactionsVisible.Where(fac => fac != Faction.OfPlayer && fac.PlayerRelationKind == FactionRelationKind.Neutral).ToList();
                        enemyFactions = Find.FactionManager.AllFactionsVisible.Where(fac => fac != Faction.OfPlayer && fac.PlayerRelationKind == FactionRelationKind.Hostile && fac.def.CanEverBeNonHostile).ToList();

                        List<DiaOption> lastOptions = new List<DiaOption> { end };

                        if(alliedFactions.Count>0)
                        {
                            foreach(DiaOption op in lastOptions)
                            {
                                op.link=friendlyNode;
                            }
                            lastOptions = friendlyNode.options;
                        }
                        if (neutralFactions.Count > 0)
                        {
                            foreach (DiaOption op in lastOptions)
                            {
                                op.link = neutralNode;
                            }
                            lastOptions = neutralNode.options;
                        }
                        if (enemyFactions.Count > 0)
                        {
                            foreach (DiaOption op in lastOptions)
                            {
                                op.link = enemyNode;
                            }
                            lastOptions = enemyNode.options;
                        }

                        foreach(DiaOption op in lastOptions)
                        {
                            Delegate clone = (Delegate)op.action.Clone();
                            op.action = delegate { clone.DynamicInvoke(); doSoSCredits(); };
                            op.resolveTree = true;
                        }

                        Dialog_NodeTree dialog_NodeTree = new Dialog_NodeTree(node, true, false, null);
                        dialog_NodeTree.silenceAmbientSound = false;
                        dialog_NodeTree.closeOnCancel = true;
                        Find.WindowStack.Add(dialog_NodeTree);
                    },
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ArchotechSporeEvolve"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ArchotechSporeEvolveDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/Glitterworld_end_icon")
                };
                giz.Add(winGame);
            }
            return giz;
        }

        void TargetForBrainShock()
        {
            Find.Targeter.BeginTargeting(TargetingParameters.ForAttackAny(), delegate (LocalTargetInfo target) {
                if (target.HasThing && target.Thing is Pawn)
                {
                    Pawn pawn = (Pawn)target.Thing;
                    Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.PsychicShock, pawn);
                    BodyPartRecord result = null;
                    pawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).TryRandomElement(out result);
                    pawn.health.AddHediff(hediff, result);
                    fieldStrength -= 0.5f;
                    SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(Find.CurrentMap);
                    if (KeyBindingDefOf.QueueOrder.IsDownEvent && fieldStrength>=0.5f)
                        TargetForBrainShock();
                }
            });
        }

        void TargetForInsanity()
        {
            Find.Targeter.BeginTargeting(TargetingParameters.ForAttackAny(), delegate (LocalTargetInfo target) {
                if (target.HasThing && target.Thing is Pawn)
                {
                    Pawn pawn = (Pawn)target.Thing;
                    pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, null, forceWake: true);
                    fieldStrength -= 1f;
                    SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(Find.CurrentMap);
                    if (KeyBindingDefOf.QueueOrder.IsDownEvent && fieldStrength >= 1f)
                        TargetForInsanity();
                }
            });
        }

        void doSoSCredits()
        {
            //Log.Message("Doing credits");
            StringBuilder builder = new StringBuilder();

            builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoS",Consciousness.Name.ToStringFull,Find.World.info.name));
            builder.AppendLine();

            if(alliedFactions.Count>0)
            {
                if(endgameFriendly==0) //Choice
                {
                    builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSAllyA", Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                    builder.AppendLine();

                    foreach (Faction fac in alliedFactions)
                    {
                        if(fac.def.techLevel > TechLevel.Neolithic && fac.def.techLevel < TechLevel.Ultra) //Outlanders and modded factions
                        {
                            if(fac.def.naturalEnemy) //Rough
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSAllyAOutlanderRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                            else //Civil
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSAllyAOutlander", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                        }
                        else if(fac.def.techLevel<=TechLevel.Neolithic) //Tribals
                        {
                            if (fac.def.naturalEnemy) //Rough
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSAllyATribalRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                            else //Civil
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSAllyATribal", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                        }
                        else //Empire
                        {
                            builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSAllyAEmpire", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                            builder.AppendLine();
                        }
                    }
                }
                else //Annex
                {
                    builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSAllyB", Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                    builder.AppendLine();

                    foreach (Faction fac in alliedFactions)
                    {
                        if (fac.def.techLevel > TechLevel.Neolithic && fac.def.techLevel < TechLevel.Ultra) //Outlanders and modded factions
                        {
                            if (fac.def.naturalEnemy) //Rough
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSAllyBOutlanderRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                            else //Civil
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSAllyBOutlander", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                        }
                        else if (fac.def.techLevel <= TechLevel.Neolithic) //Tribals
                        {
                            if (fac.def.naturalEnemy) //Rough
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSAllyBTribalRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                            else //Civil
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSAllyBTribal", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                        }
                        else //Empire
                        {
                            builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSAllyBEmpire", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                            builder.AppendLine();
                        }
                    }
                }
            }

            if (neutralFactions.Count > 0)
            {
                if (endgameNeutral == 0) //Choice
                {
                    builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralA", Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                    builder.AppendLine();

                    foreach (Faction fac in neutralFactions)
                    {
                        if (fac.def.techLevel > TechLevel.Neolithic && fac.def.techLevel < TechLevel.Ultra) //Outlanders and modded factions
                        {
                            if (fac.def.naturalEnemy) //Rough
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralAOutlanderRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                            else //Civil
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralAOutlander", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                        }
                        else if (fac.def.techLevel <= TechLevel.Neolithic) //Tribals
                        {
                            if (fac.def.naturalEnemy) //Rough
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralATribalRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                            else //Civil
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralATribal", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                        }
                        else //Empire
                        {
                            builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralAEmpire", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                            builder.AppendLine();
                        }
                    }
                }
                else if(endgameNeutral==1) //Annex
                {
                    builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralB", Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                    builder.AppendLine();

                    foreach (Faction fac in neutralFactions)
                    {
                        if (fac.def.techLevel > TechLevel.Neolithic && fac.def.techLevel < TechLevel.Ultra) //Outlanders and modded factions
                        {
                            if (fac.def.naturalEnemy) //Rough
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralBOutlanderRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                            else //Civil
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralBOutlander", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                        }
                        else if (fac.def.techLevel <= TechLevel.Neolithic) //Tribals
                        {
                            if (fac.def.naturalEnemy) //Rough
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralBTribalRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                            else //Civil
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralBTribal", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                        }
                        else //Empire
                        {
                            builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralBEmpire", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                            builder.AppendLine();
                        }
                    }
                }

                else //Exterminate
                {
                    builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralC", Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                    builder.AppendLine();

                    foreach (Faction fac in neutralFactions)
                    {
                        if (fac.def.techLevel > TechLevel.Neolithic && fac.def.techLevel < TechLevel.Ultra) //Outlanders and modded factions
                        {
                            if (fac.def.naturalEnemy) //Rough
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralCOutlanderRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                            else //Civil
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralCOutlander", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                        }
                        else if (fac.def.techLevel <= TechLevel.Neolithic) //Tribals
                        {
                            if (fac.def.naturalEnemy) //Rough
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralCTribalRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                            else //Civil
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralCTribal", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                        }
                        else //Empire
                        {
                            builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralCEmpire", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                            builder.AppendLine();
                        }
                    }
                }
            }

            if (enemyFactions.Count > 0)
            {
                if (endgameEnemy == 1) //Annex
                {
                    builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSEnemyA", Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                    builder.AppendLine();

                    foreach (Faction fac in alliedFactions)
                    {
                        if (fac.def.techLevel > TechLevel.Neolithic && fac.def.techLevel < TechLevel.Ultra) //Outlanders and modded factions
                        {
                            if (fac.def.naturalEnemy) //Rough
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSEnemyAOutlanderRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                            else //Civil
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSEnemyAOutlander", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                        }
                        else if (fac.def.techLevel <= TechLevel.Neolithic) //Tribals
                        {
                            if (fac.def.naturalEnemy) //Rough
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSEnemyATribalRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                            else //Civil
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSEnemyATribal", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                        }
                        else //Empire
                        {
                            builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSEnemyAEmpire", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                            builder.AppendLine();
                        }
                    }
                }
                else //Exterminate
                {
                    builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSEnemyB", Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                    builder.AppendLine();

                    foreach (Faction fac in alliedFactions)
                    {
                        if (fac.def.techLevel > TechLevel.Neolithic && fac.def.techLevel < TechLevel.Ultra) //Outlanders and modded factions
                        {
                            if (fac.def.naturalEnemy) //Rough
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralCOutlanderRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                            else //Civil
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralCOutlander", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                        }
                        else if (fac.def.techLevel <= TechLevel.Neolithic) //Tribals
                        {
                            if (fac.def.naturalEnemy) //Rough
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralCTribalRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                            else //Civil
                            {
                                builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralCTribal", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                                builder.AppendLine();
                            }
                        }
                        else //Empire
                        {
                            builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSNeutralCEmpire", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
                            builder.AppendLine();
                        }
                    }
                }
            }

            builder.AppendLine(TranslatorFormattedStringExtensions.Translate("GameOverSoSColonists", Consciousness.Name.ToStringFull));
            foreach(Pawn p in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
            {
                builder.AppendLine(p.NameFullColored);
            }
            builder.AppendLine();

            builder.AppendLine(GameVictoryUtility.InMemoryOfSection());

            builder.AppendLine("Save Our Ship 2 was developed by Kentington and Thain, with additional code by SonicTHI");
            builder.AppendLine("Special thanks to art/code contributors Oskar Potocki, K', Sarg, Karim, Saakra, and Revolus");
            builder.AppendLine();
            builder.AppendLine("Shipwrights: (Insert Boi here), AlfadorZero, choppytehbear, Dammerung, DianaWinters, Foxtrot, Inert, Jameson, Moonshine Dusk");
            builder.AppendLine("Testing squad: Phsarjk, i am has yes, Fuji, Reviire, Ian, choppytehbear, Jameson, Generic Scout, Waipa, Xanthos, BUTTERSKY, Karim, firethestars, Haldicar, jamhax, Jenden, maraworf, Red_Bunny, rostock, sprocket, El Jojo, Zahc, Dutchman, Zero Jackal, Tiberiumkyle, swordspell, SonicTHI, Shabm, Reviire, Kasodus, Red_Bunny, melandor, Madman, K', Jenden, Insert Witty Joke, Ifailatgaming, choppytehbear, Capitão Escarlate, Bunkier, Bumblybear, Bubbadoge, Abraxas, (Insert Boi here)");

            //Log.Message(builder.ToString());

            ShipInteriorMod2.SoSWin = true;
            GameVictoryUtility.ShowCredits(builder.ToString());
        }
    }
}
