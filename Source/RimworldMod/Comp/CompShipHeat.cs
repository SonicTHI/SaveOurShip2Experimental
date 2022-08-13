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
    [StaticConstructorOnStartup]
    public class CompShipHeat : ThingComp
    {
        public static Graphic ShipHeatOverlay = new GraphicShipHeatPipe_Overlay(GraphicDatabase.Get<Graphic_Single>("Things/Building/Ship/ShipHeat_Overlay_Atlas", ShaderDatabase.MetaOverlay));
        public static Graphic ShipHeatConnectorBase = GraphicDatabase.Get<Graphic_Single>("Things/Special/Power/OverlayBase", ShaderDatabase.MetaOverlay);
        public static Graphic ShipHeatGraphic = new Graphic_LinkedShipConduit(GraphicDatabase.Get<Graphic_Single>("Things/Building/Ship/Atlas_CoolantConduit", ShaderDatabase.Cutout));

        public ShipHeatNet myNet=null;

        public CompProperties_ShipHeat Props
        {
            get { return props as CompProperties_ShipHeat; }
        }

        public void PrintForGrid(SectionLayer layer)
        {
            ShipHeatOverlay.Print(layer, (Thing)(object)base.parent, 0);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            ((ShipHeatMapComp)this.parent.Map.components.Where(t => t is ShipHeatMapComp).FirstOrDefault()).Register(this);
        }

        public override string CompInspectStringExtra()
        {
            string output = "";
            if (myNet != null)
            {
                output += TranslatorFormattedStringExtensions.Translate("ShipHeatStored", Mathf.Round(myNet.StorageUsed), myNet.StorageCapacity);
                output += "\nGrid:" + myNet.GridID + " Ratio:" + RatioInNetwork() + "Temp: " + Mathf.Lerp(0, 200, RatioInNetwork());
            }
            else
                output+="Not connected to a thermal network";

            if (this.Props.energyToFire > 0)
            {
                output += "\nEnergy to fire: ";
                if (this.parent is Building_ShipTurret t && t.spinalComp != null)
                {
                    if (t.AmplifierCount != -1)
                        output += this.Props.energyToFire * (1 + t.AmplifierDamageBonus) + " Wd";
                    else
                        output += "N/A";
                }
                else
                    output += this.Props.energyToFire + " Wd";
            }
            return output;
        }
        public bool AddHeatToNetwork(float amount)
        {
            if (myNet == null || amount > AvailableCapacityInNetwork())
                return false;
            myNet.AddHeat(amount);
            return true;
        }
        public bool RemHeatFromNetwork(float amount)
        {
            if (myNet == null || amount > myNet.StorageUsed)
                return false;
            myNet.RemoveHeat(amount);
            return true;
        }
        public float AvailableCapacityInNetwork()
        {
            return myNet.StorageCapacity - myNet.StorageUsed;
        }
        public float RatioInNetwork()
        {
            if (myNet == null || myNet.StorageCapacity == 0)
            {
                //Log.Error("Null heatnet for " + parent);
                return 0;
            }
            return myNet.StorageUsed / myNet.StorageCapacity;
        }
        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (myNet != null)
                myNet.DeRegister(this);
        }
    }
}
