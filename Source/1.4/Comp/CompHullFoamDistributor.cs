using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    public class CompHullFoamDistributor : ThingComp
    {
        public CompProperties_HullFoamDistributor Props
        {
            get
            {
                return (CompProperties_HullFoamDistributor)props;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.GetComponent<ShipHeatMapComp>().HullFoamDistributors.Add(this); //SC rem
        }

        public override void PostDeSpawn(Map map)
        {
            map.GetComponent<ShipHeatMapComp>().HullFoamDistributors.Remove(this); //SC rem
            base.PostDeSpawn(map);
        }
    }
}
