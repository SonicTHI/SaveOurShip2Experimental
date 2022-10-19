using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    public class IncidentWorker_ShipCombat : IncidentWorker
    {
        public static int LastAttackTick = 0;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            foreach (Building_ShipCloakingDevice cloak in ((Map)parms.target).GetComponent<ShipHeatMapComp>().Cloaks)
            {
                if (cloak.active)
                    return false;
            }
            return !((Map)parms.target).GetComponent<ShipHeatMapComp>().InCombat && ShipInteriorMod2.frequencySoS > 0 && Find.TickManager.TicksGame > LastAttackTick + 180000/ ShipInteriorMod2.frequencySoS.Value;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            LastAttackTick = Find.TickManager.TicksGame;
            ((Map)parms.target).GetComponent<ShipHeatMapComp>().StartShipEncounter((Building)((Map)parms.target).listerThings.AllThings.Where(t => t is Building_ShipBridge).FirstOrDefault());
            return true;
        }
    }
}
