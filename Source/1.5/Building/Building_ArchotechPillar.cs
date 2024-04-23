using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SaveOurShip2
{
	class Building_ArchotechPillar : Building
	{
		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			base.Destroy(mode);
			SendStupidPlayerLetter();
		}

		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			base.DeSpawn(mode);
			if(mode==DestroyMode.Vanish)
				UnlockThis(); //When minified
		}

		void SendStupidPlayerLetter()
		{
			Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.ArchotechLostPillar"), TranslatorFormattedStringExtensions.Translate("SoS.ArchotechLostPillarDesc"), LetterDefOf.NegativeEvent);
			UnlockThis();
		}

		public void UnlockThis()
		{
			string unlock = ((CompProps_ResearchUnlock)this.TryGetComp<CompResearchUnlock>().props).unlock;
			if (!ShipInteriorMod2.WorldComp.Unlocks.Contains(unlock))
				ShipInteriorMod2.WorldComp.Unlocks.Add(unlock);
		}
	}
}
