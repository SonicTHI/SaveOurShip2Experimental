using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Grammar;

namespace RimWorld.QuestGen
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
				((List<Thing>)typeof(QuestPart_DropPods).GetField("items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dropPods)).AddRange(contents.GetValue(slate));

				QuestGen.quest.AddPart(dropPods);
			}
		}
	}
}
