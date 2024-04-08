using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld.QuestGen
{
	class QuestNode_GenerateThingSetArchotech : QuestNode
	{
		public SlateRef<FloatRange?> totalMarketValueRange;

		public SlateRef<QualityGenerator?> qualityGenerator;

		protected override void RunInt()
		{
			Slate slate = QuestGen.slate;
			ThingSetMakerParams parms = default(ThingSetMakerParams);
			parms.totalMarketValueRange = totalMarketValueRange.GetValue(slate);
			parms.qualityGenerator = qualityGenerator.GetValue(slate);
			parms.techLevel = TechLevel.Archotech;
			ThingSetMaker maker = new ThingSetMaker_ArchotechGift();

			List<Thing> list = maker.Generate(parms);
			QuestPart_Choice choice = new QuestPart_Choice();
			choice.choices = new List<QuestPart_Choice.Choice>();
			QuestPart_Choice.Choice theChoice = new QuestPart_Choice.Choice();
			Reward_Items items = new Reward_Items();
			items.items = list;
			theChoice.rewards.Add(items);
			QuestPart_ArchotechSpawn spawn = new QuestPart_ArchotechSpawn();
			spawn.mapParent = QuestGen.slate.Get<Map>("map").Parent;
			spawn.Things = list;
			spawn.quest = QuestGen.quest;
			spawn.inSignal = slate.Get<string>("inSignal");
			theChoice.questParts.Add(spawn);
			QuestGen.quest.AddPart(spawn);
			choice.choices.Add(theChoice);

			list = maker.Generate(parms);
			theChoice = new QuestPart_Choice.Choice();
			items = new Reward_Items();
			items.items = list;
			theChoice.rewards.Add(items);
			spawn = new QuestPart_ArchotechSpawn();
			spawn.mapParent = QuestGen.slate.Get<Map>("map").Parent;
			spawn.Things = list;
			spawn.quest = QuestGen.quest;
			spawn.inSignal = slate.Get<string>("inSignal");
			theChoice.questParts.Add(spawn);
			QuestGen.quest.AddPart(spawn);
			choice.choices.Add(theChoice);

			list = maker.Generate(parms);
			theChoice = new QuestPart_Choice.Choice();
			items = new Reward_Items();
			items.items = list;
			theChoice.rewards.Add(items);
			spawn = new QuestPart_ArchotechSpawn();
			spawn.mapParent = QuestGen.slate.Get<Map>("map").Parent;
			spawn.Things = list;
			spawn.quest = QuestGen.quest;
			spawn.inSignal= slate.Get<string>("inSignal");
			theChoice.questParts.Add(spawn);
			QuestGen.quest.AddPart(spawn);
			choice.choices.Add(theChoice);

			QuestGen.quest.AddPart(choice);
			choice.quest = QuestGen.quest;
		}

		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}
	}
}
