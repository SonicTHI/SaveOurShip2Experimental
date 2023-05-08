using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
    public class CompShipLifeSupport : ThingComp
    {
        public bool active = true;
        CompPowerTrader powerComp;
        CompFlickable flickComp;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComponent<ShipHeatMapComp>().LifeSupports.Add(this);
            powerComp = parent.TryGetComp<CompPowerTrader>();
            flickComp = parent.TryGetComp<CompFlickable>();
            if (powerComp.PowerOn && flickComp.SwitchIsOn)
                active = true;
            //Log.Message("Spawned LS: " + this.parent + " on map: " + this.parent.Map);
        }
        public override void CompTick()
        {
            base.CompTick();
            if (Find.TickManager.TicksGame % 360 == 0)
            {
                if (powerComp.PowerOn && flickComp.SwitchIsOn)
                    active = true;
                else
                    active = false;
            }
        }
        public override void PostDeSpawn(Map map)
        {
            //Log.Message("Despawned LS: " + this.parent + " on map: " + map);
            map.GetComponent<ShipHeatMapComp>().LifeSupports.Remove(this);
            base.PostDeSpawn(map);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref active, "active", false);
        }
    }
}
