using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using SaveOurShip2;

namespace RimWorld
{
    //generates heat per tick
    public class CompShipHeatSource : CompShipHeat
    {
        public CompFlickable flickComp;
        public CompPowerTraderOverdrivable overdriveComp;
        public CompRefuelable refuelComp;
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            flickComp = parent.GetComp<CompFlickable>();
            overdriveComp = parent.GetComp<CompPowerTraderOverdrivable>();
            refuelComp = parent.GetComp<CompRefuelable>();
        }
        public override void CompTick()
        {
            base.CompTick();
            if (this.parent.IsHashIntervalTick(60) && (flickComp==null || flickComp.SwitchIsOn) && (refuelComp==null || refuelComp.HasFuel))
            {
                float heatGenerated = Props.heatPerSecond;
                if (overdriveComp != null)
                    heatGenerated *= 1 + Mathf.Pow(overdriveComp.overdriveSetting, 1.5f);
                if (!AddHeatToNetwork(heatGenerated))
                    GenTemperature.PushHeat(parent, heatGenerated);
            }
        }
    }
}
