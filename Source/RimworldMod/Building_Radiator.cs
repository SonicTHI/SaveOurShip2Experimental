using UnityEngine;
using Verse;

namespace RimWorld
{
    public class Building_Radiator : Building_TempControl
    {
        private const float HeatOutputMultiplier = 1.25f;
        private const float EfficiencyLossPerDegreeDifference = 0.007692308f;
        private const int EVAL_TIME = 60;
        private int timeTillEval = EVAL_TIME;

        private UnfoldComponent unfoldComponent;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            unfoldComponent = GetComp<UnfoldComponent>();
        }

        public override void Tick()
        {
            base.Tick();
            timeTillEval--;
            if (timeTillEval <= 0)
            {
                if (this.compPowerTrader.PowerOn && this.TryGetComp<CompShipHeatSink>().notInsideShield)
                {
                    IntVec3 intVec3_1 = this.Position + IntVec3.North.RotatedBy(this.Rotation);

                    IntVec3 intVec3_2 = this.Position + IntVec3.South.RotatedBy(this.Rotation);
                    IntVec3 intVec3_3 = this.Position + (IntVec3.South.RotatedBy(this.Rotation) * 2);
                    IntVec3 intVec3_4 = this.Position + (IntVec3.South.RotatedBy(this.Rotation) * 3);
                    bool flag = false;
                    if (!intVec3_4.Impassable(this.Map) && !intVec3_3.Impassable(this.Map) && !intVec3_2.Impassable(this.Map) && !intVec3_1.Impassable(this.Map))
                    {
                        float temperature1 = intVec3_2.GetTemperature(this.Map);
                        float temperature2 = intVec3_1.GetTemperature(this.Map);
                        float num1 = temperature1 - temperature2;
                        if (temperature1 - 40.0f > num1)
                            num1 = temperature1 - 40f;
                        float num2 = (1.0f - num1 * (1.0f / 130.0f));
                        if (num2 < 0.0f)
                            num2 = 0.0f;
                        float energyLimit = ((this.compTempControl.Props.energyPerSecond) * num2 * 4.1667f);
                        float tempChange = GenTemperature.ControlTemperatureTempChange(intVec3_1, this.Map, energyLimit, this.compTempControl.targetTemperature);
                        flag = !Mathf.Approximately(tempChange, 0.0f);
                        if (flag)
                        {
                            intVec3_1.GetRoom(this.Map).Temperature += tempChange;
                            GenTemperature.PushHeat(intVec3_2, this.Map, (-energyLimit * HeatOutputMultiplier));
                        }
                        else if(GetComp<CompShipHeatSink>().heatStored>0)
                        {
                            flag = true;
                        }
                    }
                    CompProperties_Power props = this.compPowerTrader.Props;
                    if (flag)
                        this.compPowerTrader.PowerOutput = -props.basePowerConsumption;
                    else
                        this.compPowerTrader.PowerOutput = -props.basePowerConsumption * this.compTempControl.Props.lowPowerConsumptionFactor;
                    this.compTempControl.operatingAtHighPower = flag;

                    unfoldComponent.Target = flag ? 1.0f : 0.0f;
                }
                else
                {
                    unfoldComponent.Target = 0.0f;
                }

                timeTillEval = EVAL_TIME;
            }
        }
    }
}
