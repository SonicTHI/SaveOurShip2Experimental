using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    public class ShipHeatNet
    {
        public List<CompShipHeat> Connectors = new List<CompShipHeat>();
        public List<CompShipHeatSource> Sources = new List<CompShipHeatSource>();
        public List<CompShipHeatSink> Sinks = new List<CompShipHeatSink>();
        public List<CompShipHeatPurge> HeatPurges = new List<CompShipHeatPurge>();
        public List<CompShipCombatShield> Shields = new List<CompShipCombatShield>();
        public List<CompShipHeatSource> Turrets = new List<CompShipHeatSource>();
        public List<CompShipHeatSource> Cloaks = new List<CompShipHeatSource>();
        public int GridID;
        public float StorageCapacity;
        public float StorageUsed;

        public void Register(CompShipHeat comp)
        {
            if (comp is CompShipHeatSink sink)
            {
                if (!Sinks.Contains(sink))
                {
                    Sinks.Add(sink);
                    if (comp is CompShipHeatPurge purge)
                    {
                        HeatPurges.Add(purge);
                    }
                }
            }
            else if (comp is CompShipHeatSource source)
            {
                if (!Sources.Contains(source))
                {
                    Sources.Add(source);
                    if (comp is CompShipCombatShield shield)
                    {
                        Shields.Add(shield);
                    }
                    else if (source.parent is Building_ShipTurret)
                        Turrets.Add(source);
                    else if (source.parent is Building_ShipCloakingDevice)
                        Cloaks.Add(source);
                }
            }
            else if(!Connectors.Contains(comp))
                Connectors.Add(comp);
        }
        public void DeRegister(CompShipHeat comp)
        {
            if (comp is CompShipHeatSink sink)
            {
                Sinks.Remove(sink);
                if (comp is CompShipHeatPurge purge)
                    HeatPurges.Remove(purge);
            }
            else if (comp is CompShipHeatSource source)
            {
                Sources.Remove(source);
                if (comp is CompShipCombatShield shield)
                    Shields.Remove(shield);
                else if (source.parent is Building_ShipTurret)
                    Turrets.Remove(source);
                else if (source.parent is Building_ShipCloakingDevice)
                    Cloaks.Remove(source);
            }
            else
                Connectors.Remove(comp);
        }
        public void Tick()
        {
            StorageCapacity = 0;
            StorageUsed = 0;
            foreach(CompShipHeatSink sink in Sinks)
            {
                StorageCapacity += sink.Props.heatCapacity;
                StorageUsed += sink.heatStored;
            }
        }
        public bool AddHeat(float amount, bool remove=false)
        {
            int sinkCount = 0;
            foreach(CompShipHeatSink sink in Sinks)
            {
                if ((!sink.Props.ventHeatToSpace || sink.notInsideShield))
                    sinkCount++;
            }
            foreach (CompShipHeatSink sink in Sinks)
            {
                if (!sink.Props.ventHeatToSpace || sink.notInsideShield)
                {
                    float amountToStore = amount / (float)sinkCount;
                    amount -= amountToStore;
                    if (remove)
                    {
                        if(sink.heatStored >= amountToStore)
                        {
                            sink.heatStored -= amountToStore;
                        }
                        else
                        {
                            amount += amountToStore - sink.heatStored;
                            sink.heatStored = 0;
                        }
                    }
                    else
                    {
                        if ((sink.Props.heatCapacity - sink.heatStored) >= amountToStore)
                        {
                            sink.heatStored += amountToStore;
                        }
                        else
                        {
                            amount += amountToStore - (sink.Props.heatCapacity - sink.heatStored);
                            sink.heatStored = sink.Props.heatCapacity;
                        }
                    }
                }
            }
            return amount < 0.05f; //small fudge factor
        }
        public void TurretsOff()
        {
            foreach (var turret in Turrets)
            {
                ((Building_ShipTurret)turret.parent).ResetForcedTarget();
            }
        }
        public void ShieldsOn()
        {
            foreach (var shield in Shields)
            {
                shield.Flickable.SwitchIsOn = true;
            }
        }
        public void ShieldsOff()
        {
            foreach (var shield in Shields)
            {
                shield.Flickable.SwitchIsOn = false;
            }
        }
        public bool AnyShieldOn()
        {
            foreach (var shield in Shields)
            {
                if (shield.Flickable.SwitchIsOn == true)
                    return true;
            }
            return false;
        }
        public bool AnyCloakOn()
        {
            foreach (var cloak in Cloaks)
            {
                if (cloak.parent.TryGetComp<CompFlickable>().SwitchIsOn == true)
                    return true;
            }
            return false;
        }

    }
}
