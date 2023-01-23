using UnityEngine;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
    public class Building_Radiator : Building_TempControl
    {
        //private const float HeatOutputMultiplier = 1.25f;

        //private UnfoldComponent unfoldComp;
        //private CompShipHeatSink heatComp;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            //unfoldComp = GetComp<UnfoldComponent>();
            //heatComp = GetComp<CompShipHeatSink>();
        }

        public override void Tick()
        {
            base.Tick();
            //dynamic replace to wall
            Map map = this.Map;
            IntVec3 pos = this.Position;
            Faction fac = this.Faction;
            Color col = Color.clear;
            if (this.TryGetComp<CompColorable>() != null)
                col = this.TryGetComp<CompColorable>().Color;
            this.Destroy(DestroyMode.Vanish);
            ThingDef def;
            if (this.def.defName.Equals("ShipInside_PassiveCoolerMechanoid"))
                def = ThingDef.Named("Ship_BeamMech");
            else if (this.def.defName.Equals("ShipInside_PassiveCoolerArchotech"))
                def = ThingDef.Named("Ship_BeamArchotech");
            else
                def = ShipInteriorMod2.beamDef;
            Thing thing = ThingMaker.MakeThing(def);
            thing.SetFaction(fac);
            if (col != Color.clear)
                thing.SetColor(col);
            GenSpawn.Spawn(thing, pos, map);
            /*if (Find.TickManager.TicksGame % 60 == 0)
            {
                if (this.compPowerTrader.PowerOn && heatComp.Disabled)
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
                        float energyLimit = this.compTempControl.Props.energyPerSecond * num2 * 4.1667f;
                        float tempChange = GenTemperature.ControlTemperatureTempChange(intVec3_1, this.Map, energyLimit, this.compTempControl.targetTemperature);
                        flag = !Mathf.Approximately(tempChange, 0.0f);
                        if (flag)
                        {
                            intVec3_1.GetRoom(this.Map).Temperature += tempChange;
                            GenTemperature.PushHeat(intVec3_2, this.Map, (-energyLimit * HeatOutputMultiplier));
                        }
                        else if (heatComp.myNet.StorageUsed > 0)
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

                    unfoldComp.Target = flag ? 1.0f : 0.0f;
                }
                else
                {
                    unfoldComp.Target = 0.0f;
                }
            }*/
        }
    }
}
