using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;
using SaveOurShip2;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class CompShipHeatPurge : CompShipHeatSink
    {
        static readonly float HEAT_PURGE_RATIO = 20;

        public bool purging = false;
        bool start = false;
        bool hiss = false;
        public ShipHeatMapComp mapComp;
        public CompRefuelable fuelComp;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref purging, "purging");
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            mapComp = parent.Map.GetComponent<ShipHeatMapComp>();
            fuelComp = parent.TryGetComp<CompRefuelable>();
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            List<Gizmo> giz = new List<Gizmo>();
            giz.AddRange(base.CompGetGizmosExtra());
            if (parent.Faction == Faction.OfPlayer)
            {
                Command_Toggle purge = new Command_Toggle
                {
                    toggleAction = delegate
                    {
                        if (purging)
                            purging = false;
                        else
                            StartPurge();
                    },
                    isActive = delegate { return purging; },
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("SoSPurgeHeat"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("SoSPurgeHeatDesc"),
                    icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/HeatPurge")
                };
                giz.Add(purge);
            }
            return giz;
        }
        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned || parent.Destroyed || myNet == null)
            {
                return;
            }
            if (purging)
            {
                if (!start)
                {
                    foreach (CompShipCombatShield shield in myNet.Shields.Where(s => !s.shutDown))
                    {
                        shield.flickComp.SwitchIsOn = false;
                    }
                    foreach (Building_ShipCloakingDevice cloak in mapComp.Cloaks)
                    {
                        if (cloak.active && cloak.Map == parent.Map)
                        {
                            cloak.flickComp.SwitchIsOn = false;
                        }
                    }
                    start = true;
                }
                if (CanPurge() && fuelComp.Fuel > 0 && RemHeatFromNetwork(Props.heatPurge * HEAT_PURGE_RATIO))
                {
                    fuelComp.ConsumeFuel(Props.heatPurge);
                    FleckMaker.ThrowAirPuffUp(parent.DrawPos + new Vector3(0, 0, 1), parent.Map);
                    if (!hiss)
                    {
                        ResourceBank.SoundDefOf.ShipPurgeHiss.PlayOneShot(parent);
                        hiss = true;
                    }
                }
                else
                {
                    purging = false;
                }
            }
        }
        public void StartPurge()
        {
            purging = true;
            hiss = false;
            start = false;
        }
        public bool CanPurge()
        {
            foreach (CompShipCombatShield shield in mapComp.Shields)
            {
                if (!shield.shutDown && (parent.DrawPos - shield.parent.DrawPos).magnitude < shield.radius)
                {
                    return false;
                }
            }
            if (!mapComp.InCombat)
            {
                foreach (Building_ShipCloakingDevice cloak in mapComp.Cloaks)
                {
                    if (cloak.active && cloak.Map == parent.Map)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
