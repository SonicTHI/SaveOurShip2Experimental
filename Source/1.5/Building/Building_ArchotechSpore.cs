using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld.QuestGen;
using RimWorld;
using Vehicles;

namespace SaveOurShip2
{
	[StaticConstructorOnStartup]
	public class Building_ArchotechSpore : Building_ShipBridge, IThingHolder //Holds pawns who were psychically linked, and whose bodies were destroyed
	{
		private readonly static Graphic eyeGraphic = GraphicDatabase.Get(typeof(Graphic_Single), "Things/Building/Ship/Archotech_DeusXMachine_Eye", ShaderDatabase.MetaOverlay, new Vector2(5, 8), Color.white, Color.white);
		static readonly float fieldCostSoothe = 10f;
		static readonly float fieldCostShock = 2f;
		static readonly float fieldCostInsanity = 5f;
		static readonly float fieldCostPsylink = 25f;
		static readonly float fieldCostSoulLink = 10f;

		readonly StatDef pillars = StatDef.Named("ArchotechPillarsConnected");
		public float Mood => Consciousness == null ? 1 : Mathf.Clamp(Consciousness.needs.mood.CurLevel * 2, 0, 2); //1 is neutral, 2 is super happy, 0 is MURDER
		public float fieldStrength = 0f;
		int lastPrankTick = 0;
		bool GiftParticles = false;
		bool unlockedPsy = false;
		bool ideoCrisis = false;
		public bool newSpore = false;
		int MeditationTicks = 0;

		public List<Pawn> linkedPawns = new List<Pawn>();
		public ThingOwner<Pawn> soulsHeld = new ThingOwner<Pawn>();

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

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			base.DrawAt(drawLoc, flip);
			if (Consciousness == null)
				return;

			Color eyeColor = Color.red;
			if (Mood < 1f)
				eyeColor = new Color(1f, Mood, 0);
			else
				eyeColor = new Color(2f - Mood, 2f - Mood, Mood - 1f);
			eyeColor.a = Mathf.Cos(Mathf.PI * (float)Find.TickManager.TicksGame / 256f);
			eyeGraphic.GetColoredVersion(ShaderDatabase.MetaOverlay, eyeColor, eyeColor).Draw(new Vector3(drawLoc.x, drawLoc.y + 1f, drawLoc.z), Rotation, this);
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			if (!respawningAfterLoad)
			{
				Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.SporeBuilt"), TranslatorFormattedStringExtensions.Translate("SoS.SporeBuiltDesc"), LetterDefOf.PositiveEvent);
				newSpore = true;
			}
			if (Consciousness==null && !newSpore)
			{
				ConsciousnessComp.GenerateAIPawn();
			}
			if (ShipInteriorMod2.WorldComp.LastSporeGiftTick == 0)
				ShipInteriorMod2.WorldComp.LastSporeGiftTick = Find.TickManager.TicksGame;
		}

		public override void TickRare()
		{
			base.TickRare();
			if (Consciousness == null)
				return;
			int tick = Find.TickManager.TicksGame;
			if (!ShipInteriorMod2.WorldComp.startedEndgame)
			{
				ShipInteriorMod2.WorldComp.startedEndgame = true;
				ShipInteriorMod2.WorldComp.Unlocks.Add("ArchotechUplink");
				Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.FindPillars"), TranslatorFormattedStringExtensions.Translate("SoS.FindPillarsDesc"), LetterDefOf.PositiveEvent);
				lastPrankTick = tick+Rand.Range(40000,80000);
			}

			if (unlockedPsy || ResourceBank.ResearchProjectDefOf.ArchotechPsychicField.IsFinished)
			{
				unlockedPsy = true;
				fieldStrength += 0.001f * (1+ NumConnectedPillars);
			}

			if (tick - ShipInteriorMod2.WorldComp.LastSporeGiftTick > 90000 * (5- NumConnectedPillars) * (3-Mood))
			{
				int numUnlock = 0;
				foreach (ArchoGiftDef def in DefDatabase<ArchoGiftDef>.AllDefs)
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
					ShipInteriorMod2.WorldComp.LastSporeGiftTick = tick;
					MakeGift(numUnlock);
				}
				else
				{
					ShipInteriorMod2.WorldComp.LastSporeGiftTick += 60000;
				}
			}
			if (Mood < 0.8f && tick > lastPrankTick)
				PlayPrank(tick);

			if (!ideoCrisis && tick % 20000 == 0 && ModsConfig.IdeologyActive)
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

				DiaNode node = new DiaNode(TranslatorFormattedStringExtensions.Translate("SoS.ArchotechIdeoChoice", Consciousness.Name.ToStringShort, highestPawn.Name.ToStringShort));
				DiaOption keepIdeo = new DiaOption("Discover the truth of "+Consciousness.Ideo.name);
				DiaOption newIdeo = new DiaOption("Found a new ideoligion");
				node.options.Add(keepIdeo);
				node.options.Add(newIdeo);
				DiaNode nodeKeepFaith;
				if (Consciousness.Ideo.memes.Contains(ResourceBank.MemeDefOf.Structure_Archist))
					nodeKeepFaith = new DiaNode(TranslatorFormattedStringExtensions.Translate("SoS.ArchotechIdeoCertaintyArchist", Consciousness.Name.ToStringShort, Consciousness.Ideo.name));
				else if (Consciousness.Ideo.memes.Contains(DefDatabase<MemeDef>.GetNamed("Structure_Ideological")))
					nodeKeepFaith = new DiaNode(TranslatorFormattedStringExtensions.Translate("SoS.ArchotechIdeoCertaintyEthical", Consciousness.Name.ToStringShort, Consciousness.Ideo.name));
				else if (Consciousness.Ideo.memes.Contains(DefDatabase<MemeDef>.GetNamed("Structure_Animist")))
					nodeKeepFaith = new DiaNode(TranslatorFormattedStringExtensions.Translate("SoS.ArchotechIdeoCertaintySpirits", Consciousness.Name.ToStringShort, Consciousness.Ideo.name));
				else
					nodeKeepFaith = new DiaNode(TranslatorFormattedStringExtensions.Translate("SoS.ArchotechIdeoCertaintyGod", Consciousness.Name.ToStringShort, Consciousness.Ideo.KeyDeityName, Consciousness.Ideo.name));
				nodeKeepFaith.options.Add(new DiaOption("All hail the one truth!"));
				nodeKeepFaith.options[0].resolveTree = true;
				keepIdeo.link = nodeKeepFaith;
				newIdeo.action = delegate
				{
					ShipInteriorMod2.ArchoIdeoFlag = true;
					Page_ConfigureIdeo page = new Page_ConfigureIdeo();
					page.nextAct = delegate
					{
						Ideo ideo = page.ideo;
						DiaNode newNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("SoS.ArchotechIdeoHeresy", ideo));
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
			if (MeditationTicks > 6000)
			{
				MeditationTicks = 0;
				Consciousness?.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("ArchotechSporeMeditation"));
			}
		}

		void PlayPrank(int tick)
		{
			lastPrankTick = tick + Rand.Range(40000, 80000);
			int prank = Rand.RangeInclusive(0, 5);
			if (prank == 0) //cancer
			{
				Find.LetterStack.ReceiveLetter("SoS.ArchotechPrank".Translate(), "SoS.ArchotechPrankCancer".Translate(), LetterDefOf.NegativeEvent);
				int numCancers = Rand.RangeInclusive(2, 5);
				for (int i = 0; i < numCancers; i++)
				{
					Pawn victim = this.Map.mapPawns.FreeColonistsAndPrisonersSpawned.Where(pawn => !ShipInteriorMod2.IsHologram(pawn)).RandomElement();
					HediffGiverUtility.TryApply(victim, HediffDefOf.Carcinoma, null, true);
				}
			}
			else if (prank == 1) //breakdown
			{
				Find.LetterStack.ReceiveLetter("SoS.ArchotechPrank".Translate(), "SoS.ArchotechPrankDamage".Translate(), LetterDefOf.NegativeEvent);
				int numBrokenDevices = Rand.RangeInclusive(5, 12);
				for (int i = 0; i < numBrokenDevices; i++)
				{
					IEnumerable<Building> breakableThings = this.Map.listerBuildings.allBuildingsColonist.Where(b => b.TryGetComp<CompBreakdownable>() != null && !b.TryGetComp<CompBreakdownable>().BrokenDown);
					if (breakableThings.EnumerableNullOrEmpty())
						break;
					breakableThings.RandomElement().TryGetComp<CompBreakdownable>().DoBreakdown();
				}
			}
			else if (prank == 2) //romance
			{
				Pawn worstA = null;
				Pawn worstB = null;
				float worstScore = 9999;
				List<Pawn> pawns = this.Map.mapPawns.FreeColonistsAndPrisonersSpawned.Where(p => p.DevelopmentalStage == DevelopmentalStage.Adult).ToList();
				List<Pawn> pawns2 = this.Map.mapPawns.FreeColonistsAndPrisonersSpawned.Where(p => p.DevelopmentalStage == DevelopmentalStage.Adult).ToList();
				foreach (Pawn pawn in pawns)
				{
					foreach (Pawn p in pawns2)
					{
						if (pawn == p || pawn.relations.FamilyByBlood.Contains(p) || LovePartnerRelationUtility.ExistingLovePartner(pawn)==p || ((pawn.gender == p.gender) && (!p.story.traits.HasTrait(TraitDefOf.Gay) || pawn.story.traits.HasTrait(TraitDefOf.Gay))))
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
					Find.LetterStack.ReceiveLetter("SoS.ArchotechPrank".Translate(), "SoS.ArchotechPrankLovers".Translate(worstA.Label, worstB.Label), LetterDefOf.NegativeEvent);
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
						Find.LetterStack.ReceiveLetter((string)parms3[5], TranslatorFormattedStringExtensions.Translate((string)parms3[4], (string)parms3[0], (string)parms3[1], (string)parms3[2], (string)parms3[3]), LetterDefOf.PositiveEvent);
					}
					LovePartnerRelationUtility.TryToShareBed(worstA, worstB);
				}
			}
			else if(prank==3) //manhunter
			{
				Find.LetterStack.ReceiveLetter("SoS.ArchotechPrank".Translate(), "SoS.ArchotechPrankSquirrels".Translate(), LetterDefOf.NegativeEvent);
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
			else if(prank==4) //goodwill
			{
				Faction fac = Find.FactionManager.AllFactions.Where(f => f.def.CanEverBeNonHostile && !f.def.isPlayer).RandomElement();
				fac.TryAffectGoodwillWith(Faction.OfPlayer, -10);
				Find.LetterStack.ReceiveLetter("SoS.ArchotechPrank".Translate(), "SoS.ArchotechPrankReputation".Translate(fac.Name), LetterDefOf.NegativeEvent);
			}
			else if(prank==5) //secret
			{
				Pawn victim = this.Map.mapPawns.FreeColonists.RandomElement();
				Find.LetterStack.ReceiveLetter("SoS.ArchotechPrank".Translate(), "SoS.ArchotechPrankSecret".Translate(victim), LetterDefOf.NegativeEvent);
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
			Scribe_Values.Look<bool>(ref newSpore, "NewSpore");
			Scribe_Values.Look<bool>(ref ideoCrisis, "IdeoCrisis");
			Scribe_Values.Look<bool>(ref GiftParticles, "GiftParticles");
			Scribe_Collections.Look<Pawn>(ref linkedPawns, "LinkedSouls", LookMode.Reference);
			Scribe_Deep.Look(ref soulsHeld, "SoulsHeld", this);
		}

		public override string GetInspectString()
		{
			string text = base.GetInspectString();
			text += "\nMood: " + Mathf.Round(Mood * 50f) + "\nPsychic field strength: "+fieldStrength;
			if (linkedPawns.Count > 0)
			{
				text += "\nLinked: ";
				for(int i=0;i<linkedPawns.Count;i++)
                {
					text += linkedPawns[i];
					if (i < linkedPawns.Count - 1)
						text += ", ";
                }
			}
			if (soulsHeld.Count > 0)
			{
				text += "\nStored: ";
				for (int i = 0; i < soulsHeld.Count; i++)
				{
					text += soulsHeld[i];
					if (i < soulsHeld.Count - 1)
						text += ", ";
				}
			}
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
			if (ResourceBank.ResearchProjectDefOf.ArchotechPsychicManipulation.IsFinished)
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
									fieldStrength -= fieldCostSoothe; 
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
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechSoothe"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechSootheDesc", fieldCostSoothe),
					icon = ContentFinder<Texture2D>.Get("UI/ArchotechCommandSoothe")
				};
				if (fieldStrength < fieldCostSoothe)
				{
					GenerateSoothe.disabled = true;
					GenerateSoothe.disabledReason = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechFieldStrengthLow");
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
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechShock"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechShockDesc", fieldCostShock),
					icon = ContentFinder<Texture2D>.Get("UI/ArchotechCommandShock")
				};
				if (fieldStrength < fieldCostShock)
				{
					shockABrain.disabled = true;
					shockABrain.disabledReason = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechFieldStrengthLow");
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
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechInsanity"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechInsanityDesc", fieldCostInsanity),
					icon = ContentFinder<Texture2D>.Get("UI/ArchotechCommandInsanity")
				};
				if (fieldStrength < fieldCostInsanity)
				{
					insaneInTheBrain.disabled = true;
					insaneInTheBrain.disabledReason = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechFieldStrengthLow");
				}
				giz.Add(insaneInTheBrain);
			}
			if (ModsConfig.RoyaltyActive && ResourceBank.ResearchProjectDefOf.ArchotechPsylink.IsFinished)
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
									if (target.HasThing && target.Thing is Pawn p && p.GetPsylinkLevel() < 6)
									{
										p.ChangePsylinkLevel(1);
										fieldStrength -= fieldCostPsylink;
										SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(Find.CurrentMap);
										FleckMaker.Static(Position, Map, FleckDefOf.PsycastAreaEffect, 10f);
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
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechPsylink"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechPsylinkDesc", fieldCostPsylink),
					icon = ContentFinder<Texture2D>.Get("UI/ArchotechCommandPsylink")
				};
				if (fieldStrength < fieldCostPsylink)
				{
					formPsylink.disabled = true;
					formPsylink.disabledReason = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechFieldStrengthLow");
				}
				giz.Add(formPsylink);
			}
			if (ResourceBank.ResearchProjectDefOf.ArchotechPsychicSoulLink.IsFinished)
            {
				Command_Action linkSoul = new Command_Action
				{
					action = delegate
					{
						List<FloatMenuOption> options = new List<FloatMenuOption>();
						foreach (Map map in Find.Maps)
						{
							FloatMenuOption op = new FloatMenuOption(map.Parent.Label, delegate {
								CameraJumper.TryJump(Position, map);
								Find.Targeter.BeginTargeting(TargetingParameters.ForAttackAny(), delegate (LocalTargetInfo target) {
									if (target.HasThing && target.Thing is Pawn p && p.RaceProps.Humanlike && !linkedPawns.Contains(p))
									{
										linkedPawns.Add(p);
										fieldStrength -= fieldCostSoulLink;
										SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(Find.CurrentMap);
										FleckMaker.Static(Position, Map, FleckDefOf.PsycastAreaEffect, 10f);
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
					defaultLabel = "SoS.ArchotechSoulLink".Translate(),
					defaultDesc = "SoS.ArchotechSoulLinkDesc".Translate(fieldCostSoulLink),
					icon = ContentFinder<Texture2D>.Get("UI/ArchoTechUpload")
				};
				if (fieldStrength < fieldCostSoulLink)
                {
					linkSoul.disabled = true;
					linkSoul.disabledReason = "SoS.ArchotechFieldStrengthLow".Translate();
				}
				giz.Add(linkSoul);

				foreach(Pawn soul in soulsHeld)
                {
					Command_Action_PawnDrawer downloadSoul = new Command_Action_PawnDrawer
					{
						action = delegate
						{
							List<FloatMenuOption> options = new List<FloatMenuOption>();
							foreach (Map map in Find.Maps)
							{
								FloatMenuOption op = new FloatMenuOption(map.Parent.Label, delegate {
									CameraJumper.TryJump(Position, map);
									Find.Targeter.BeginTargeting(TargetingParameters.ForAttackAny(), delegate (LocalTargetInfo target) {
										if (target.HasThing && target.Thing is Building b)
										{
											CompBuildingConsciousness consc = b.GetComp<CompBuildingConsciousness>();
											if (consc != null && consc.Consciousness == null)
											{

												consc.InstallConsciousness(soul);
												consc.parent.DirtyMapMesh(consc.parent.Map);
												if (consc.Consciousness == soul) //Safety check
												{
													soulsHeld.Remove(soul);
													linkedPawns.Add(soul);
												}
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
						defaultLabel = "SoS.ArchotechSoulDownload".Translate(soul),
						defaultDesc = "SoS.ArchotechSoulDownloadDesc".Translate(soul),
						pawn = soul,
						groupable = false
                    };
					giz.Add(downloadSoul);
                }
            }

			if (Consciousness != null) //gifting
			{
				/*bool anyGiftsUnlocked = false;
				foreach (ArchotechGiftDef def in DefDatabase<ArchotechGiftDef>.AllDefs)
				{
					if (def.research.IsFinished)
					{
						anyGiftsUnlocked = true;
						break;
					}
				}*/
				if (ResourceBank.ResearchProjectDefOf.ArchotechExotics.IsFinished)
				{
					Command_Toggle toggleGiftParticles = new Command_Toggle
					{
						toggleAction = delegate
						{
							GiftParticles = !GiftParticles;
						},
						defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechGiftParticles"),
						defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechGiftParticlesDesc"),
						icon = ContentFinder<Texture2D>.Get("UI/ExoticParticles"),
						isActive = () => GiftParticles
					};
					giz.Add(toggleGiftParticles);
				}
				/*Command_Action demandGift = new Command_Action
				{
					action = delegate
					{
						MakeGift(5);
						Consciousness.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("ArchotechSporeGiftDemanded"));
					},
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechDemandGift"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechDemandGiftDesc"),
					icon = ContentFinder<Texture2D>.Get("UI/Buttons/GiftMode")
				};
				if (!anyGiftsUnlocked)
				{
					demandGift.disabled = true;
					demandGift.disabledReason = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechNoGiftsUnlocked");
				}
				giz.Add(demandGift);*/
			}

			if (NumConnectedPillars>=4)
			{
				Command_Action winGame = new Command_Action
				{
					action = delegate
					{
						DiaNode node = new DiaNode(TranslatorFormattedStringExtensions.Translate("SoS.AllPillarsDesc", Consciousness.Name.ToStringFull));
						DiaOption end = new DiaOption("Remake the world");
						DiaOption cancel = new DiaOption("Remain mortal a while longer");
						cancel.resolveTree = true;
						node.options.Add(end);
						node.options.Add(cancel);

						DiaNode friendlyNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("SoS.WinGameAllies"));
						DiaOption friendlyAlly = new DiaOption("Give them a choice - join or don't");
						DiaOption friendlyTakeover = new DiaOption("Annex them by force");
						friendlyAlly.action = delegate { endgameFriendly = 0; };
						friendlyTakeover.action = delegate { endgameFriendly = 1; };
						friendlyNode.options.Add(friendlyAlly);
						friendlyNode.options.Add(friendlyTakeover);

						DiaNode neutralNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("SoS.WinGameNeutral"));
						DiaOption neutralAlly = new DiaOption("Give them a choice - join or don't");
						DiaOption neutralTakeover = new DiaOption("Annex them by force");
						DiaOption neutralKill = new DiaOption("Destroy them");
						neutralAlly.action = delegate { endgameNeutral = 0; };
						neutralTakeover.action = delegate { endgameNeutral = 1; };
						neutralKill.action = delegate { endgameNeutral = 2; };
						neutralNode.options.Add(neutralAlly);
						neutralNode.options.Add(neutralTakeover);
						neutralNode.options.Add(neutralKill);

						DiaNode enemyNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("SoS.WinGameEnemies"));
						DiaOption enemyTakeover = new DiaOption("Annex them by force");
						DiaOption enemyKill = new DiaOption("Destroy them");
						enemyTakeover.action = delegate { endgameEnemy = 1; };
						enemyKill.action = delegate { endgameEnemy = 2; };
						enemyNode.options.Add(enemyTakeover);
						enemyNode.options.Add(enemyKill);

						alliedFactions = Find.FactionManager.AllFactionsVisible.Where(fac => fac!=Faction.OfPlayer && fac.PlayerRelationKind == FactionRelationKind.Ally && !fac.defeated).ToList();
						neutralFactions = Find.FactionManager.AllFactionsVisible.Where(fac => fac != Faction.OfPlayer && fac.PlayerRelationKind == FactionRelationKind.Neutral && !fac.defeated).ToList();
						enemyFactions = Find.FactionManager.AllFactionsVisible.Where(fac => fac != Faction.OfPlayer && fac.PlayerRelationKind == FactionRelationKind.Hostile && fac.def.CanEverBeNonHostile && !fac.defeated).ToList();

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
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechEvolve"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ArchotechEvolveDesc"),
					icon = ContentFinder<Texture2D>.Get("UI/Glitterworld_end_icon")
				};
				giz.Add(winGame);
			}
			return giz;
		}

		private void MakeGift(int numUnlock)
		{
			if (GiftParticles) //spawn particles
			{
				Thing thing = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ArchotechExoticParticles);
				thing.stackCount = Math.Min(numUnlock * Rand.RangeInclusive(7, 10), thing.def.stackLimit);
				GenSpawn.Spawn(thing, Position, Map);
			}
			else //quest
			{
				Slate slate = new Slate();
				slate.Set<string>("quest_name", "A Gift From " + Consciousness.Name.ToStringFull);
				slate.Set<string>("archotech_name", Consciousness.Name.ToStringShort);
				slate.Set<Map>("map", Map);
				slate.Set<int>("value", numUnlock * 1000);
				Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(DefDatabase<QuestScriptDef>.GetNamed("ArchotechGiftQuest"), slate);
				Find.LetterStack.ReceiveLetter(quest.name, quest.description, LetterDefOf.PositiveEvent, null, null, quest, null, null);
			}
		}

		void TargetForBrainShock()
		{
			Find.Targeter.BeginTargeting(TargetingParameters.ForAttackAny(), delegate (LocalTargetInfo target) {
				if (target.HasThing && target.Thing is Pawn pawn)
				{
					Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.PsychicShock, pawn);
					BodyPartRecord result = null;
					pawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.ConsciousnessSource).TryRandomElement(out result);
					pawn.health.AddHediff(hediff, result);
					fieldStrength -= fieldCostShock;
					SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(Find.CurrentMap);
					if (KeyBindingDefOf.QueueOrder.IsDownEvent && fieldStrength >= fieldCostShock)
						TargetForBrainShock();
				}
			});
		}

		void TargetForInsanity()
		{
			Find.Targeter.BeginTargeting(TargetingParameters.ForAttackAny(), delegate (LocalTargetInfo target) {
				if (target.HasThing && target.Thing is Pawn pawn)
				{
					pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, null, forceWake: true);
					fieldStrength -= fieldCostInsanity;
					SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(Find.CurrentMap);
					if (KeyBindingDefOf.QueueOrder.IsDownEvent && fieldStrength >= fieldCostInsanity)
						TargetForInsanity();
				}
			});
		}

		void doSoSCredits()
		{
			//Log.Message("Doing credits");
			StringBuilder builder = new StringBuilder();

			builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOver",Consciousness.Name.ToStringFull,Find.World.info.name));
			builder.AppendLine();

			if(alliedFactions.Count>0)
			{
				if(endgameFriendly==0) //Choice
				{
					builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverAllyA", Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
					builder.AppendLine();

					foreach (Faction fac in alliedFactions)
					{
						if(fac.def.techLevel > TechLevel.Neolithic && fac.def.techLevel < TechLevel.Ultra) //Outlanders and modded factions
						{
							if(fac.def.naturalEnemy) //Rough
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverAllyAOutlanderRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
							else //Civil
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverAllyAOutlander", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
						}
						else if(fac.def.techLevel<=TechLevel.Neolithic) //Tribals
						{
							if (fac.def.naturalEnemy) //Rough
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverAllyATribalRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
							else //Civil
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverAllyATribal", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
						}
						else //Empire
						{
							builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverAllyAEmpire", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
							builder.AppendLine();
						}
					}
				}
				else //Annex
				{
					builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverAllyB", Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
					builder.AppendLine();

					foreach (Faction fac in alliedFactions)
					{
						if (fac.def.techLevel > TechLevel.Neolithic && fac.def.techLevel < TechLevel.Ultra) //Outlanders and modded factions
						{
							if (fac.def.naturalEnemy) //Rough
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverAllyBOutlanderRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
							else //Civil
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverAllyBOutlander", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
						}
						else if (fac.def.techLevel <= TechLevel.Neolithic) //Tribals
						{
							if (fac.def.naturalEnemy) //Rough
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverAllyBTribalRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
							else //Civil
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverAllyBTribal", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
						}
						else //Empire
						{
							builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverAllyBEmpire", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
							builder.AppendLine();
						}
					}
				}
			}

			if (neutralFactions.Count > 0)
			{
				if (endgameNeutral == 0) //Choice
				{
					builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralA", Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
					builder.AppendLine();

					foreach (Faction fac in neutralFactions)
					{
						if (fac.def.techLevel > TechLevel.Neolithic && fac.def.techLevel < TechLevel.Ultra) //Outlanders and modded factions
						{
							if (fac.def.naturalEnemy) //Rough
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralAOutlanderRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
							else //Civil
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralAOutlander", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
						}
						else if (fac.def.techLevel <= TechLevel.Neolithic) //Tribals
						{
							if (fac.def.naturalEnemy) //Rough
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralATribalRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
							else //Civil
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralATribal", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
						}
						else //Empire
						{
							builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralAEmpire", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
							builder.AppendLine();
						}
					}
				}
				else if(endgameNeutral==1) //Annex
				{
					builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralB", Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
					builder.AppendLine();

					foreach (Faction fac in neutralFactions)
					{
						if (fac.def.techLevel > TechLevel.Neolithic && fac.def.techLevel < TechLevel.Ultra) //Outlanders and modded factions
						{
							if (fac.def.naturalEnemy) //Rough
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralBOutlanderRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
							else //Civil
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralBOutlander", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
						}
						else if (fac.def.techLevel <= TechLevel.Neolithic) //Tribals
						{
							if (fac.def.naturalEnemy) //Rough
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralBTribalRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
							else //Civil
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralBTribal", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
						}
						else //Empire
						{
							builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralBEmpire", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
							builder.AppendLine();
						}
					}
				}

				else //Exterminate
				{
					builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralC", Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
					builder.AppendLine();

					foreach (Faction fac in neutralFactions)
					{
						if (fac.def.techLevel > TechLevel.Neolithic && fac.def.techLevel < TechLevel.Ultra) //Outlanders and modded factions
						{
							if (fac.def.naturalEnemy) //Rough
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralCOutlanderRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
							else //Civil
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralCOutlander", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
						}
						else if (fac.def.techLevel <= TechLevel.Neolithic) //Tribals
						{
							if (fac.def.naturalEnemy) //Rough
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralCTribalRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
							else //Civil
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralCTribal", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
						}
						else //Empire
						{
							builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralCEmpire", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
							builder.AppendLine();
						}
					}
				}
			}

			if (enemyFactions.Count > 0)
			{
				if (endgameEnemy == 1) //Annex
				{
					builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverEnemyA", Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
					builder.AppendLine();

					foreach (Faction fac in alliedFactions)
					{
						if (fac.def.techLevel > TechLevel.Neolithic && fac.def.techLevel < TechLevel.Ultra) //Outlanders and modded factions
						{
							if (fac.def.naturalEnemy) //Rough
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverEnemyAOutlanderRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
							else //Civil
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverEnemyAOutlander", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
						}
						else if (fac.def.techLevel <= TechLevel.Neolithic) //Tribals
						{
							if (fac.def.naturalEnemy) //Rough
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverEnemyATribalRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
							else //Civil
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverEnemyATribal", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
						}
						else //Empire
						{
							builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverEnemyAEmpire", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
							builder.AppendLine();
						}
					}
				}
				else //Exterminate
				{
					builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverEnemyB", Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
					builder.AppendLine();

					foreach (Faction fac in alliedFactions)
					{
						if (fac.def.techLevel > TechLevel.Neolithic && fac.def.techLevel < TechLevel.Ultra) //Outlanders and modded factions
						{
							if (fac.def.naturalEnemy) //Rough
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralCOutlanderRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
							else //Civil
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralCOutlander", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
						}
						else if (fac.def.techLevel <= TechLevel.Neolithic) //Tribals
						{
							if (fac.def.naturalEnemy) //Rough
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralCTribalRough", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
							else //Civil
							{
								builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralCTribal", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
								builder.AppendLine();
							}
						}
						else //Empire
						{
							builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverNeutralCEmpire", fac.GetCallLabel(), Faction.OfPlayer.GetCallLabel(), Consciousness.Name.ToStringFull));
							builder.AppendLine();
						}
					}
				}
			}

			builder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.GameOverColonists", Consciousness.Name.ToStringFull));
			foreach(Pawn p in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists)
			{
				builder.AppendLine(p.NameFullColored);
			}
			builder.AppendLine();
			builder.AppendLine(GameVictoryUtility.InMemoryOfSection());
			builder.AppendLine("Save Our Ship 2 was developed by Kentington, Thain, and SonicTHI");
			builder.AppendLine();
			builder.AppendLine("Special thanks to Owlchemist for his code contributions.");
			builder.AppendLine();
			builder.AppendLine("Other contributors (code, art, design or QA):");
			builder.AppendLine("Oskar Potocki, Thamuzz1331, Trollam, Boris, K', Sarg, Karim, Saakra, Revolus, MatthewTeply, dkargin, HG, DianaWinters, UrbanMonkey, M.A.G.Gen., Epicguru, sdanchenko, m00nl1ght-dev");
			builder.AppendLine();
			builder.AppendLine("Shipwrights: Oninnaise, VVither_Skeleton, (Insert Boi here), AlfadorZero, choppytehbear, Dammerung, Foxtrot, Inert, Jameson, Moonshine Dusk");
			builder.AppendLine();
			builder.AppendLine("Testing squad: Buns Buns Cat, Phsarjk, i am has yes, Fuji, Reviire, Ian, Generic Scout, Waipa, Xanthos, BUTTERSKY, firethestars, Haldicar, jamhax, Jenden, maraworf, Red_Bunny, rostock, sprocket, El Jojo, Zahc, Dutchman, Zero Jackal, Tiberiumkyle, swordspell, Shabm, Kasodus, Red_Bunny, melandor, Madman, Jenden, Insert Witty Joke, Ifailatgaming, Capitão Escarlate, Bunkier, Bumblybear, Bubbadoge, Abraxas, Rage Nova, twsta, transcendant, thecaffiend, Manifold Paradox, WhiteGiverMa, Gago, Nerevarest");

			//Log.Message(builder.ToString());

			ShipInteriorMod2.WorldComp.SoSWin = true;
			GameVictoryUtility.ShowCredits(builder.ToString(), null);
		}

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
			foreach (Pawn pawn in soulsHeld)
				outChildren.Add(pawn);
        }

        public ThingOwner GetDirectlyHeldThings()
        {
			return soulsHeld;
        }
    }
}
