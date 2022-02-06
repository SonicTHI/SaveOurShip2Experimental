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
    public class CompShipHeatSource : CompShipHeat
    {
        private CompFlickable flickComp;
        private CompPowerTraderOverdrivable overdriveComp;
        private CompRefuelable refuelComp;
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            flickComp = parent.GetComp<CompFlickable>();
            overdriveComp = parent.GetComp<CompPowerTraderOverdrivable>();
            refuelComp = parent.GetComp<CompRefuelable>();
        }
        public bool AddHeatToNetwork(float amount, bool remove=false)
        {
            if (myNet == null)
                return false;
            return myNet.AddHeat(amount, remove);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (Props.heatGeneratedPerTickActive > 0 && (flickComp==null || flickComp.SwitchIsOn) && (refuelComp==null || refuelComp.HasFuel))
            {
                float heatGenerated = Props.heatGeneratedPerTickActive;
                if (overdriveComp != null)
                    heatGenerated *= (1 + Mathf.Pow(overdriveComp.overdriveSetting, 1.5f));
                if (!AddHeatToNetwork(heatGenerated,false))
                {
                    GenTemperature.PushHeat(parent, heatGenerated * ShipInteriorMod2.HeatPushMult);
                }
            }
        }
    }
}
