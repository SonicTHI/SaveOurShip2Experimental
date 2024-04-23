using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld.QuestGen;

namespace SaveOurShip2
{
	class QuestNode_ArchotechSpawn : QuestNode
	{
		[NoTranslate]
		public SlateRef<string> inSignal;

		public SlateRef<IEnumerable<Thing>> contents;

		private const string RootSymbol = "root";

		protected override bool TestRunInt(Slate slate)
		{
			return slate.Exists("map");
		}

		protected override void RunInt()
		{
			Slate slate = QuestGen.slate;
			if (contents.GetValue(slate) != null)
			{
				QuestPart_ArchotechSpawn dropPods = new QuestPart_ArchotechSpawn();
				dropPods.inSignal = (QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? QuestGen.slate.Get<string>("inSignal"));
				
				dropPods.mapParent = QuestGen.slate.Get<Map>("map").Parent;
				dropPods.items.AddRange(contents.GetValue(slate));

				QuestGen.quest.AddPart(dropPods);
			}
		}
	}
}
