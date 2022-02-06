using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    public class ShipHeatNet
    {
        List<CompShipHeat> connectors=new List<CompShipHeat>();
        List<CompShipHeatSource> sources=new List<CompShipHeatSource>();
        List<CompShipHeatSink> sinks=new List<CompShipHeatSink>();
        public int GridID;
        public float StorageCapacity;
        public float StorageUsed;

        public void Register(CompShipHeat comp)
        {
            if (comp is CompShipHeatSink)
            {
                if (!sinks.Contains((CompShipHeatSink)comp))
                    sinks.Add((CompShipHeatSink)comp);
            }
            else if (comp is CompShipHeatSource)
            {
                if (!sources.Contains((CompShipHeatSource)comp))
                    sources.Add((CompShipHeatSource)comp);
            }
            else if(!connectors.Contains(comp))
                connectors.Add(comp);
        }

        public void DeRegister(CompShipHeat comp)
        {
            if (comp is CompShipHeatSink)
                sinks.Remove((CompShipHeatSink)comp);
            else if (comp is CompShipHeatSource)
                sources.Remove((CompShipHeatSource)comp);
            else
                connectors.Remove(comp);
        }

        public void Tick()
        {
            StorageCapacity = 0;
            StorageUsed = 0;
            foreach(CompShipHeatSink sink in sinks)
            {
                StorageCapacity += sink.Props.heatCapacity;
                StorageUsed += sink.heatStored;
            }
        }

        public bool AddHeat(float amount, bool remove=false)
        {
            int sinkCount = 0;
            foreach(CompShipHeatSink sink in sinks)
            {
                if ((!sink.Props.ventHeatToSpace || sink.notInsideShield))
                    sinkCount++;
            }
            foreach (CompShipHeatSink sink in sinks)
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
    }
}
