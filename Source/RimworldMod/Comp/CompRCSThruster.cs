using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class CompRCSThruster : ThingComp
    {
		public CompProperties_EngineTrail Props
        {
            get { return props as CompProperties_EngineTrail; }
        }
        public bool active = false;
        public ShipHeatMapComp mapComp;
        public CompFlickable Flickable;
        public CompRefuelable Refuelable;
        public CompPowerTrader PowerTrader;
        public bool CanFire
        {
            get
            {
                if (Flickable.SwitchIsOn)
                {
                    return active && Refuelable.Fuel > 0;
                }
                return false;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Flickable = parent.TryGetComp<CompFlickable>();
            Refuelable = parent.TryGetComp<CompRefuelable>();
            PowerTrader = parent.TryGetComp<CompPowerTrader>();
            mapComp = parent.Map.GetComponent<ShipHeatMapComp>();
        }
        public override void PostDeSpawn(Map map)
        {
            mapComp = null;
            base.PostDeSpawn(map);
        }
        public override void CompTick()
        {
            base.CompTick();
            if (CanFire)
            {
                if (Find.TickManager.TicksGame % 60 == 0)
                {
                    Refuelable.ConsumeFuel(Props.fuelUse);
                }
            }
        }
    }
}
