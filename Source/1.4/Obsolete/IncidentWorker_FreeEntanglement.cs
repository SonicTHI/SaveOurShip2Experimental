using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    class IncidentWorker_FreeEntanglement : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("EntanglementUpdate"), TranslatorFormattedStringExtensions.Translate("EntanglementUpdateDesc"), LetterDefOf.NeutralEvent);
            ActiveDropPodInfo info = new ActiveDropPodInfo();
            info.innerContainer.TryAdd(ThingMaker.MakeThing(ThingDef.Named("SoSEntanglementManifold")));
            try
            {
                DropPodUtility.MakeDropPodAt(DropCellFinder.TradeDropSpot(Find.Maps.Where(m => m.IsPlayerHome).FirstOrDefault()), Find.Maps.Where(m => m.IsPlayerHome).FirstOrDefault(), info);
            }
            catch
            {
                DropPodUtility.MakeDropPodAt(new IntVec3(), Find.Maps.Where(m => m.IsPlayerHome).FirstOrDefault(), info);
            }
            return true;
        }
    }
}
