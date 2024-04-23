using RimWorld.Planet;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Grammar;
using RimWorld.QuestGen;

namespace SaveOurShip2
{
	public class QuestNode_GenerateSpaceSite : QuestNode
	{
		public SlateRef<IEnumerable<SitePartDefWithParams>> sitePartsParams;

		public SlateRef<Faction> faction;

		public SlateRef<int> tile;

		[NoTranslate]
		public SlateRef<string> storeAs;

		public SlateRef<RulePack> singleSitePartRules;

		public SlateRef<Pawn> worker;

		private const string RootSymbol = "root";

		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}

		protected override void RunInt()
		{
			Slate slate = QuestGen.slate;
			IEnumerable<SitePartDefWithParams> enumerable = this.sitePartsParams.GetValue(slate);
			SpaceSite site = (SpaceSite)WorldObjectMaker.MakeWorldObject(ResourceBank.WorldObjectDefOf.SiteSpace);
			site.SetFaction(null);
			site.Tile = tile.GetValue(slate);
			SitePartDef core = DefDatabase<SitePartDef>.AllDefs.Where(def => def.tags != null && def.tags.Contains("SpaceCore") && ((!ShipInteriorMod2.WorldComp.Unlocks.Contains("BlackBoxShipDefeated") && Find.QuestManager.QuestsListForReading.Where(q=>(q.State!=QuestState.EndedFailed&&q.State!=QuestState.EndedOfferExpired&&q.State!=QuestState.EndedUnknownOutcome)&&(q.name.Equals(TranslatorFormattedStringExtensions.Translate("SoS.FoundOrbitalSite"))||q.name.Equals("Orbital Site Found")||q.name.Equals("Starship Bow"))).EnumerableNullOrEmpty()) || !def.tags.Contains("SpaceBlackBox"))).RandomElement();
			site.AddPart(new SitePart(site,core,new SitePartParams()));
			site.customLabel = core.label;
			site.desiredThreatPoints = site.ActualThreatPoints;
			site.theta = slate.Get<float>("theta");
			site.phi = slate.Get<float>("phi");
			site.radius = slate.Get<float>("radius");
			List<Rule> list = new List<Rule>();
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			List<string> list2 = new List<string>();
			int num = 0;
			for (int i = 0; i < site.parts.Count; i++)
			{
				List<Rule> list3 = new List<Rule>();
				Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
				site.parts[i].def.Worker.Notify_GeneratedByQuestGen(site.parts[i], QuestGen.slate, list3, dictionary2);
				if (!site.parts[i].hidden)
				{
					if (this.singleSitePartRules.GetValue(slate) != null)
					{
						List<Rule> expr_110 = new List<Rule>();
						expr_110.AddRange(list3);
						expr_110.AddRange(this.singleSitePartRules.GetValue(slate).Rules);
						string text = QuestGenUtility.ResolveLocalText(expr_110, dictionary2, "root", false);
						list.Add(new Rule_String("sitePart" + num + "_description", text));
						if (!text.NullOrEmpty())
						{
							list2.Add(text);
						}
					}
					for (int j = 0; j < list3.Count; j++)
					{
						Rule rule = list3[j].DeepCopy();
						Rule_String rule_String = rule as Rule_String;
						if (rule_String != null && num != 0)
						{
							rule_String.keyword = string.Concat(new object[]
							{
								"sitePart",
								num,
								"_",
								rule_String.keyword
							});
						}
						list.Add(rule);
					}
					foreach (KeyValuePair<string, string> current in dictionary2)
					{
						string text2 = current.Key;
						if (num != 0)
						{
							text2 = string.Concat(new object[]
							{
								"sitePart",
								num,
								"_",
								text2
							});
						}
						if (!dictionary.ContainsKey(text2))
						{
							dictionary.Add(text2, current.Value);
						}
					}
					num++;
				}
			}
			if (!list2.Any<string>())
			{
				list.Add(new Rule_String("allSitePartsDescriptions", TranslatorFormattedStringExtensions.Translate("HiddenOrNoSitePartDescription")));
				list.Add(new Rule_String("allSitePartsDescriptionsExceptFirst", TranslatorFormattedStringExtensions.Translate("HiddenOrNoSitePartDescription")));
			}
			else
			{
				list.Add(new Rule_String("allSitePartsDescriptions", list2.ToClauseSequence()));
				if (list2.Count >= 2)
				{
					list.Add(new Rule_String("allSitePartsDescriptionsExceptFirst", list2.Skip(1).ToList<string>().ToClauseSequence()));
				}
				else
				{
					list.Add(new Rule_String("allSitePartsDescriptionsExceptFirst", TranslatorFormattedStringExtensions.Translate("HiddenOrNoSitePartDescription")));
				}
			}
			if (this.storeAs.GetValue(slate) != null)
			{
				QuestGen.slate.Set<Site>(this.storeAs.GetValue(slate), site, false);
			}
			if (core.tags.Contains("SpaceBlackBox"))
			{
				for (int j = 0; j < QuestGen.quest.PartsListForReading.Count; j++)
				{
					QuestPart_WorldObjectTimeout questPart_WorldObjectTimeout = QuestGen.quest.PartsListForReading[j] as QuestPart_WorldObjectTimeout;
					if (questPart_WorldObjectTimeout != null && questPart_WorldObjectTimeout.State == QuestPartState.Enabled && questPart_WorldObjectTimeout.worldObject == site)
					{
						((List<QuestPart>)typeof(Quest).GetField("parts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(QuestGen.quest)).Remove(questPart_WorldObjectTimeout);
					}
				}

				List<Rule> listBlackBox = new List<Rule>
				{
					new Rule_String("questName", TranslatorFormattedStringExtensions.Translate("SoS.FoundOrbitalSite"))
				};
				if (Find.Scenario.AllParts.Any(part=>part.def.defName.Equals("SoSDerelict") || part.def.defName.Equals("SoSDungeon")))
				{
					if (worker.GetValue(slate) != null)
						listBlackBox.Add(new Rule_String("questDescription", TranslatorFormattedStringExtensions.Translate("SoS.FoundSiteSpecialSpace", worker.GetValue(slate).LabelShort, slate.Get<int>("fuelCost"))));
					else
						listBlackBox.Add(new Rule_String("questDescription", TranslatorFormattedStringExtensions.Translate("SoS.FoundSiteSpecialSpace", "its AI", slate.Get<int>("fuelCost"))));
				}
				else if (Faction.OfPlayer.def == FactionDefOf.PlayerTribe)
				{
					if (worker.GetValue(slate) != null)
						listBlackBox.Add(new Rule_String("questDescription", TranslatorFormattedStringExtensions.Translate("SoS.FoundSiteSpecialTribal",worker.GetValue(slate).LabelShort, slate.Get<int>("fuelCost"))));
					else
						listBlackBox.Add(new Rule_String("questDescription", TranslatorFormattedStringExtensions.Translate("SoS.FoundSiteSpecialTribal","its AI", slate.Get<int>("fuelCost"))));
				}
				else
				{
					if (worker.GetValue(slate) != null)
						listBlackBox.Add(new Rule_String("questDescription", TranslatorFormattedStringExtensions.Translate("SoS.FoundSiteSpecial",worker.GetValue(slate).LabelShort, slate.Get<int>("fuelCost"))));
					else
						listBlackBox.Add(new Rule_String("questDescription", TranslatorFormattedStringExtensions.Translate("SoS.FoundSiteSpecial","its AI", slate.Get<int>("fuelCost"))));
				}
				ShipInteriorMod2.WorldComp.Unlocks.Add("BlackBoxShipSpawned");
				QuestGen.AddQuestDescriptionRules(listBlackBox);
				QuestGen.AddQuestNameRules(listBlackBox);
			}
			else
			{
				if (worker.GetValue(slate) == null)
				{
					dictionary.Add("worker_definite", "its AI");
					list.Add(new Rule_String("worker_definite", "its AI"));
				}
				QuestGen.AddQuestDescriptionRules(list);
				QuestGen.AddQuestNameRules(list);
			}
			QuestGen.AddQuestDescriptionConstants(dictionary);
			QuestGen.AddQuestNameConstants(dictionary);
			QuestGen.AddQuestNameRules(new List<Rule>
			{
				new Rule_String("site_label", site.Label)
			});
		}
	}
}