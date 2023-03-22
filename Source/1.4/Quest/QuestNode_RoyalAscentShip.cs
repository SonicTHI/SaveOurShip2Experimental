using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Grammar;

namespace RimWorld.QuestGen
{
    class QuestNode_RoyalAscentShip : QuestNode
	{
		[NoTranslate]
		public SlateRef<string> inSignal;

		public SlateRef<QuestPart.SignalListenMode?> signalListenMode;
		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}

		protected override void RunInt()
		{
			Slate slate = QuestGen.slate;
			QuestPart_RoyalAscentShip royShip = new QuestPart_RoyalAscentShip();
			royShip.inSignal = (QuestGenUtility.HardcodedSignalWithQuestID(this.inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal", null, false));
			royShip.signalListenMode = (this.signalListenMode.GetValue(slate) ?? QuestPart.SignalListenMode.OngoingOnly);
			QuestGen.quest.AddPart(royShip);
		}
	}
}
