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
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            var mapComp = ((Map)parms.target).GetComponent<ShipHeatMapComp>();
            if (!mapComp.IsPlayerShipMap || mapComp.InCombat || mapComp.NextTargetMap != null || ModSettings_SoS.frequencySoS == 0 || Find.TickManager.TicksGame < mapComp.LastAttackTick + 300000 / ModSettings_SoS.frequencySoS)
                return false;

            foreach (Building_ShipCloakingDevice cloak in mapComp.Cloaks)
            {
                if (cloak.active)
                    return false;
            }
            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var mapComp = ((Map)parms.target).GetComponent<ShipHeatMapComp>();
            mapComp.LastAttackTick = Find.TickManager.TicksGame;
            mapComp.StartShipEncounter(mapComp.MapRootListAll.FirstOrDefault(), fac: parms.faction);
            return true;
        }
    }
}
