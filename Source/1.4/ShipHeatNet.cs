﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
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
        public List<CompShipHeat> Turrets = new List<CompShipHeat>();
        public List<CompShipHeat> Cloaks = new List<CompShipHeat>();
        public int GridID;
        public float StorageCapacity { get; private set; }
        public float StorageUsed { get; private set; }

        private bool ratioDirty = true; //if we add/rem heat, etc
        private float ratioInNetwork = 0;
        public float RatioInNetwork
        {
            get
            {
                if (ratioDirty)
                {
                    if (float.IsNaN(StorageUsed))
                    {
                        Log.Warning("NaN prevented in RatioInNetwork!");
                        StorageUsed = 0;
                    }
                    if (StorageCapacity <= 0)
                    {
                        return StorageCapacity = 0;
                    }
                    ratioInNetwork = Mathf.Clamp(StorageUsed / StorageCapacity, 0, 1);
                }
                return ratioInNetwork;
            }
        }

        public void Register(CompShipHeat comp)
        {
            if (comp is CompShipHeatSink sink)
            {
                if (!Sinks.Contains(sink))
                {
                    //add to net
                    //Log.Message("grid: " + GridID + " add:" + bank.heatStored + " Total:" + StorageUsed + "/" + StorageCapacity);
                    StorageCapacity += sink.Props.heatCapacity;
                    StorageUsed += sink.heatStored;
                    sink.heatStored = 0;
                    //Log.Message("grid: "+ GridID +" add:"+ bank.heatStored + " Total:" + StorageUsed +"/"+ StorageCapacity);
                    Sinks.Add(sink);
                    ratioDirty = true;
                    if (comp is CompShipHeatPurge purge)
                    {
                        HeatPurges.Add(purge);
                    }
                }
            }
            else if (comp.parent is Building_ShipTurret)
                Turrets.Add(comp);
            else if (comp is CompShipHeatSource source)
            {
                if (!Sources.Contains(source))
                {
                    Sources.Add(source);
                    if (source.parent is Building_ShipCloakingDevice)
                        Cloaks.Add(source);
                }
            }
            else if (comp is CompShipCombatShield shield)
                Shields.Add(shield);
            else if (!Connectors.Contains(comp))
                Connectors.Add(comp);
        }
        public void DeRegister(CompShipHeat comp)
        {
            if (comp is CompShipHeatSink sink)
            {
                //rem from net with a factor
                if (float.IsNaN(StorageUsed))
                {
                    Log.Warning("NaN prevented in DeRegister!");
                    StorageUsed = 0;
                }
                if (StorageCapacity <= 0)
                    sink.heatStored = 0;
                else
                    sink.heatStored = Mathf.Clamp(StorageUsed * sink.Props.heatCapacity / StorageCapacity, 0, sink.Props.heatCapacity);
                RemoveHeat(sink.heatStored);
                StorageCapacity -= sink.Props.heatCapacity;
                //Log.Message("grid: " + GridID + " rem:" + bank.heatStored + " Total:" + StorageUsed + "/" + StorageCapacity);
                Sinks.Remove(sink);
                ratioDirty = true;
                if (comp is CompShipHeatPurge purge)
                    HeatPurges.Remove(purge);
            }
            else if (comp.parent is Building_ShipTurret)
                Turrets.Remove(comp);
            else if (comp is CompShipHeatSource source)
            {
                Sources.Remove(source);
                if (source.parent is Building_ShipCloakingDevice)
                    Cloaks.Remove(source);
            }
            else if (comp is CompShipCombatShield shield)
                Shields.Remove(shield);
            else
                Connectors.Remove(comp);
        }
        public void AddHeat(float amount)
        {
            StorageUsed += amount;
            ratioDirty = true;
        }
        public void RemoveHeat(float amount)
        {
            StorageUsed -= amount;
            if (float.IsNaN(StorageUsed))
            {
                Log.Warning("NaN prevented in RemoveHeat!");
                StorageUsed = 0;
            }
            if (StorageUsed < 0)
                StorageUsed = 0;
            ratioDirty = true;
        }
        public bool AnyShieldOn()
        {
            return Shields.Any(s => s.flickComp.SwitchIsOn == true);
        }
        public bool AnyCloakOn()
        {
            return Cloaks.Any(c => c.parent.TryGetComp<CompFlickable>().SwitchIsOn == true);
        }
        public void ShieldsOn()
        {
            foreach (var shield in Shields)
            {
                shield.flickComp.SwitchIsOn = true;
            }
        }
        public void ShieldsOff()
        {
            foreach (var shield in Shields)
            {
                shield.flickComp.SwitchIsOn = false;
            }
        }
        public void TurretsOff()
        {
            foreach (var turret in Turrets)
            {
                ((Building_ShipTurret)turret.parent).ResetForcedTarget();
            }
        }
    }
}
