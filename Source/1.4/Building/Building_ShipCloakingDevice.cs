using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
    public class Building_ShipCloakingDevice : Building
    {
        public bool active;
        public CompPowerTrader powerComp;
        public CompShipHeatSource heatComp;
        public CompFlickable flickComp;
        public ShipHeatMapComp mapComp;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mapComp = map.GetComponent<ShipHeatMapComp>();
            mapComp.Cloaks.Add(this);
            powerComp = this.TryGetComp<CompPowerTrader>();
            heatComp = this.TryGetComp<CompShipHeatSource>();
            flickComp = this.TryGetComp<CompFlickable>();
        }

        public override void Tick()
        {
            base.Tick();
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                if (heatComp.myNet == null || mapComp.InCombat || heatComp.myNet.venting)
                {
                    flickComp.SwitchIsOn = false;
                    active = false;
                }
                else if (powerComp.PowerOn && flickComp.SwitchIsOn)
                    active = true;
                else
                    active = false;
                if (active)
                {
                    foreach (ShipHeatNet net in mapComp.cachedNets)
                    {
                        if (net != null && net.StorageCapacityRaw > 0)
                        {
                            float f = 1f + net.StorageCapacityRaw / 10000f;
                            if (f > net.StorageCapacity) //all cloaks off
                            {
                                foreach (Building_ShipCloakingDevice cloak in mapComp.Cloaks)
                                {
                                    cloak.flickComp.SwitchIsOn = false;
                                }
                            }
                            else
                                net.AddDepletion(f);
                        }
                    }
                }
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            mapComp.Cloaks.Remove(this);
            base.DeSpawn(mode);
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }
            if (active)
            {
                stringBuilder.AppendLine("Active");//TranslatorFormattedStringExtensions.Translate(
            }
            else
            {
                stringBuilder.AppendLine("Inactive");
                if ((this.GetRoom() == null || this.GetRoom().OpenRoofCount > 0) && heatComp.myNet == null)
                    stringBuilder.AppendLine("<color=red>In vacuum and not connected to heat net</color>");
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref active, "active", false);
        }
    }
}
