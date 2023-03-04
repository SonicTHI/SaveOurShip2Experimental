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
    [StaticConstructorOnStartup]
    public class CompShipHeatSink : CompShipHeat
    {
        public static readonly float HeatPushMult = 20f; //ratio modifier should be inverse to Building_ShipVent AddHeatToNetwork
        public float heatStored; //used only when a HB is not on a net
        public bool inSpace;
        public CompPower powerComp;
        ShipHeatMapComp mapComp;
        IntVec3 pos; //needed because no predestroy
        Map map; //needed because no predestroy

        public bool disabled;
        public bool Disabled
        {
            get
            {
                if (!mapComp.InCombat && mapComp.Cloaks.Any(c => c.active))
                {
                    disabled = true;
                }
                disabled = false;
                return disabled;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            powerComp = parent.TryGetComp<CompPower>();
            inSpace = this.parent.Map.IsSpace();
            pos = this.parent.Position;
            map = this.parent.Map;
            mapComp = this.map.GetComponent<ShipHeatMapComp>();
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            GenTemperature.PushHeat(pos, map, Props.heatCapacity * RatioInNetwork() * HeatPushMult);
            base.PostDestroy(mode, previousMap);
        }
        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            //save heat to sinks on save, value clamps
            if (myNet != null && Scribe.mode == LoadSaveMode.Saving)
            {
                heatStored = Props.heatCapacity * RatioInNetwork();
            }
            Scribe_Values.Look<float>(ref heatStored, "heatStored");
        }
        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned || parent.Destroyed || myNet == null)
            {
                return;
            }
            if (this.parent.IsHashIntervalTick(120))
            {
                if (Props.heatVent > 0 && !Props.antiEntropic && !Disabled) //radiate to space
                {
                    if (inSpace)
                        RemHeatFromNetwork(Props.heatVent);
                    else
                    {
                        //higher outdoor temp, push less heat out
                        float heat = Props.heatVent * GenMath.LerpDoubleClamped(0, 100, 2.5f, 0, map.mapTemperature.OutdoorTemp);
                        RemHeatFromNetwork(heat);
                    }
                }
                if (myNet.StorageUsed > 0)
                {
                    float ratio = RatioInNetwork();
                    if (ratio > 0.90f)
                    {
                        this.parent.TakeDamage(new DamageInfo(DamageDefOf.Burn, 10));
                    }
                    if (Props.antiEntropic) //convert heat
                    {
                        if (powerComp != null && powerComp.PowerNet != null && powerComp.PowerNet.batteryComps.Count > 0)
                        {
                            IEnumerable<CompPowerBattery> batteries = powerComp.PowerNet.batteryComps.Where(b => b.StoredEnergy <= b.Props.storedEnergyMax - 1);
                            if (batteries.Any())
                            {
                                batteries.RandomElement().AddEnergy(2);
                                RemHeatFromNetwork(Props.heatVent);
                            }
                        }
                        return;
                    }
                    //bleed into or adjacent room
                    if (PushHeat(ratio, parent.Position)) //tanks
                    {
                        return;
                    }
                    else //sinks are walls, check adjacent
                    {
                        foreach (IntVec3 vec in GenAdj.CellsAdjacent8Way(parent).ToList())
                        {
                            if (PushHeat(ratio, vec))
                                return;
                        }
                    }
                }
            }
        }
        public bool PushHeat(float ratio, IntVec3 vec, float heat = 0)
        {
            if (vec.GetRoom(parent.Map) == null || (inSpace && ShipInteriorMod2.ExposedToOutside(vec.GetRoom(map))))
                return false;
            if (RemHeatFromNetwork(Props.heatLoss))
            {
                if (heat == 0)
                    heat = Props.heatLoss * HeatPushMult * ratio;
                GenTemperature.PushHeat(vec, parent.Map, heat);
                //Log.Message("" + vec);
                return true;
            }
            return false;
        }
        public override string CompInspectStringExtra()
        {
            string toReturn = base.CompInspectStringExtra();// = "Stored heat: " + Mathf.Round(heatStored)+"/"+Props.heatCapacity;
            if (disabled)
            {
                toReturn += "\n<color=red>Cannot vent: Cloaked</color>";
            }
            return toReturn;
        }
    }
}
