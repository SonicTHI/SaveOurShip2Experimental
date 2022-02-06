using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

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
            if(myNet!=null)
                output+= TranslatorFormattedStringExtensions.Translate("ShipHeatStored",Mathf.Round(myNet.StorageUsed),myNet.StorageCapacity);
            else
                output+="Not connected to a thermal network";
            if (this.Props.energyToFire > 0)
            {
                output += "\nEnergy to fire: ";
                if (this.parent.TryGetComp<CompSpinalMount>() != null)
                {
                    if (((Building_ShipTurret)this.parent).AmplifierCount != -1)
                        output += this.Props.energyToFire * (1 + ((Building_ShipTurret)this.parent).AmplifierDamageBonus) + " Wd";
                    else
                        output += "N/A";
                }
                else
                    output += this.Props.energyToFire + " Wd";
            }
            return output;
        }

        public float AvailableCapacityInNetwork()
        {
            if (myNet == null)
            {
                //Log.Error("Null heatnet for " + parent);
                return 0;
            }
            return myNet.StorageCapacity - myNet.StorageUsed;
        }
    }
}
