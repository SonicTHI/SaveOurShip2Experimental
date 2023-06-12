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
		public CompProperties_RCSThruster Props
        {
            get { return props as CompProperties_RCSThruster; }
        }
        public bool active = false;
        public ShipHeatMapComp mapComp;
        public CompPowerTrader PowerTrader;
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            PowerTrader = parent.TryGetComp<CompPowerTrader>();
            mapComp = parent.Map.GetComponent<ShipHeatMapComp>();
        }
        public override void PostDeSpawn(Map map)
        {
            mapComp = null;
            base.PostDeSpawn(map);
        }
    }
}
