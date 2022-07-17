using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using SaveOurShip2;
using RimworldMod;

namespace RimWorld
{
    public class CompShipHeatSink : CompShipHeat
    {
        public float heatStored;
        public bool notInsideShield;
        public bool cloaked;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<float>(ref heatStored, "heatStored");
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned)
            {
                return;
            }

            if ((Find.TickManager.TicksGame % 60 == 0 && this.Props.ventHeatToSpace) || this is CompShipHeatPurge)
            {
                this.notInsideShield = NotInsideShield();
            }

            if (this.parent.IsHashIntervalTick(Props.heatVentTick) && heatStored > 0)
            {
                if (this.Props.ventHeatToSpace)
                {
                    if (notInsideShield)
                    {
                        heatStored--;
                    }
                }
                else if (this.Props.antiEntropic)
                {
                    heatStored--;
                    if (parent.TryGetComp<CompPower>() != null && parent.GetComp<CompPower>().PowerNet != null && parent.GetComp<CompPower>().PowerNet.batteryComps.Count > 0)
                    {
                        IEnumerable<CompPowerBattery> batteries = parent.GetComp<CompPower>().PowerNet.batteryComps.Where(b => b.StoredEnergy <= b.Props.storedEnergyMax - 2);
                        if (batteries.Any())
                            batteries.RandomElement().AddEnergy(2);
                    }
                }
                else if (!this.parent.Map.IsSpace() || (this.parent.Map.IsSpace() && !ShipInteriorMod2.ExposedToOutside(this.parent.GetRoom())))
                {
                    if (heatStored >= 1)
                        GenTemperature.PushHeat(this.parent, ShipInteriorMod2.HeatPushMult);
                    else
                        GenTemperature.PushHeat(this.parent, ShipInteriorMod2.HeatPushMult * heatStored);
                    heatStored--;
                }
            }
            if (heatStored < 0)
                heatStored = 0;
        }

        private bool NotInsideShield()
        {
            cloaked = false;
            foreach (CompShipCombatShield shield in parent.Map.GetComponent<ShipHeatMapComp>().Shields)
            {
                if (!shield.shutDown && (parent.DrawPos - shield.parent.DrawPos).magnitude < shield.radius)
                {
                    return false;
                }
            }
            if (!this.parent.Map.GetComponent<ShipHeatMapComp>().InCombat)
            {
                foreach (Building_ShipCloakingDevice cloak in parent.Map.GetComponent<ShipHeatMapComp>().Cloaks)
                {
                    if (cloak.active && cloak.Map == parent.Map)
                    {
						cloaked = true;
                        return false;
                    }
                }
            }
            return true;
        }

        public override string CompInspectStringExtra()
        {
            string toReturn = "Stored heat: " + Mathf.Round(heatStored)+"/"+Props.heatCapacity;
            if(this.Props.ventHeatToSpace && !notInsideShield)
            {
                if (cloaked)
                    toReturn += "\n<color=red>Cannot vent: Cloaked</color>";
                else
                    toReturn += "\n<color=red>Cannot vent: Inside shield</color>";
            }
            return toReturn;
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (myNet != null)
                myNet.DeRegister(this);
        }
    }
}
