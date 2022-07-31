using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace RimWorld
{
    public class Building_ShipVent : Building_TempControl
    {
        private const float heatpipeTemp = 60; //higher = more diff to push heat to net
        public bool heatWithPower=true;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override void TickRare()
        {
            if (this.compPowerTrader.PowerOn)
            {
                IntVec3 intVec3_1 = this.Position + IntVec3.North.RotatedBy(this.Rotation);
                bool flag = false; //operating at high power
                float energyLimit;
                float tempChange;
                if (!intVec3_1.Impassable(this.Map) && intVec3_1.GetRoom(this.Map).OpenRoofCount <= 0 && !intVec3_1.GetRoom(this.Map).UsesOutdoorTemperature)
                {
                    float roomTemp = intVec3_1.GetTemperature(this.Map);
                    if (roomTemp < this.compTempControl.targetTemperature - 2)
                    {
                        //heat room up to target temp if enough heat available, else use power (same as vanilla heater)
                        float conductance;
                        if (roomTemp < 20f)
                        {
                            conductance = 1f;
                        }
                        else if (roomTemp > 120f)
                        {
                            conductance = 0f;
                        }
                        else
                        {
                            conductance = Mathf.InverseLerp(120f, 20f, roomTemp);
                        }
                        energyLimit = conductance * this.compTempControl.Props.energyPerSecond * -1.3672f; //-64*-1.3672 = 21*4.1667 (0-87.5, HS=50/HU)
                        float heatConsumed = energyLimit * 0.0115f; //0-1HU
                        tempChange = GenTemperature.ControlTemperatureTempChange(intVec3_1, base.Map, energyLimit, this.compTempControl.targetTemperature);
                        if (this.TryGetComp<CompShipHeatSource>().myNet  != null && this.TryGetComp<CompShipHeatSource>().myNet.StorageUsed > heatConsumed)
                        {
                            this.TryGetComp<CompShipHeatSource>().AddHeatToNetwork(heatConsumed, true);
                            intVec3_1.GetRoom(this.Map).Temperature += tempChange;
                        }
                        else if (heatWithPower)
                        {
                            /*if (this.TryGetComp<CompShipHeatSource>().myNet.StorageUsed > 0)
                            {
                                GenTemperature.PushHeat(intVec3_1, this.Map, ShipCombatManager.HeatPushMult * 0.6f * this.TryGetComp<CompShipHeatSource>().myNet.StorageUsed);
                                this.TryGetComp<CompShipHeatSource>().myNet.StorageUsed = 0;
                            }*/
                            flag = !Mathf.Approximately(tempChange, 0f);
                            if (flag)
                            {
                                intVec3_1.GetRoom(this.Map).Temperature += tempChange;
                            }
                        }

                    }
                    else
                    {
                        //cool room: check if heatnet is there and not maxed, push heat to it (same as vanilla cooler)
                        float num1 = heatpipeTemp - roomTemp;
                        if (heatpipeTemp - 40.0 > num1)
                            num1 = heatpipeTemp - 40f;
                        float conductance = 1f - num1 * 0.0077f;
                        if (conductance < 0.0)
                            conductance = 0.0f;
                        energyLimit = this.compTempControl.Props.energyPerSecond * conductance * 4.1667f;
                        tempChange = GenTemperature.ControlTemperatureTempChange(intVec3_1, this.Map, energyLimit, this.compTempControl.targetTemperature);
                        flag = !Mathf.Approximately(tempChange, 0.0f);
                        if (flag && this.TryGetComp<CompShipHeatSource>().AvailableCapacityInNetwork() > -energyLimit * 0.05f)
                        {
                            intVec3_1.GetRoom(this.Map).Temperature += tempChange;
                            this.TryGetComp<CompShipHeatSource>().AddHeatToNetwork(-energyLimit * 0.05f);
                        }
                        else flag = false;
                    }
                }
                CompProperties_Power props = this.compPowerTrader.Props;
                if (flag)
                    this.compPowerTrader.PowerOutput = -props.basePowerConsumption;
                else
                    this.compPowerTrader.PowerOutput = -props.basePowerConsumption * this.compTempControl.Props.lowPowerConsumptionFactor;
                this.compTempControl.operatingAtHighPower = flag;
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            Command_Toggle toggleHeatWithPower = new Command_Toggle
            {
                toggleAction = delegate
                {
                    heatWithPower = !heatWithPower;
                },
                defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideHeatWithPower"),
                defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideHeatWithPowerDesc"),
                isActive = () => heatWithPower
            };
            toggleHeatWithPower.icon = ContentFinder<Texture2D>.Get("Things/Building/Misc/TempControl/Heater");
            yield return toggleHeatWithPower;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref heatWithPower, "heatWithPower",true);
        }
    }
}
