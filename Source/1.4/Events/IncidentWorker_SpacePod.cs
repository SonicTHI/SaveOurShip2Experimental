using RimworldMod;
using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace RimWorld
{
    public class IncidentWorker_SpacePod : IncidentWorker
    {
        Map map;
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            if (map.GetComponent<ShipHeatMapComp>().InCombat)
                return false;
            if (map.listerBuildings.allBuildingsColonist.Any(x => x.def == ResourceBank.ThingDefOf.ShipSalvageBay))
                return true;
            return false;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            ChoiceLetter_SpacePod choiceLetter_SpacePod = (ChoiceLetter_SpacePod)LetterMaker.MakeLetter(def.letterLabel, def.letterText, def.letterDef, null, null);
            choiceLetter_SpacePod.map = (Map)parms.target;
            choiceLetter_SpacePod.StartTimeout(6000);
            Find.LetterStack.ReceiveLetter(choiceLetter_SpacePod, null);
            return true;
        }
    }
}
