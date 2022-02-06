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
        static float HEAT_PURGE_RATIO = 20;
        static SoundDef HissSound = DefDatabase<SoundDef>.GetNamed("ShipPurgeHiss");

        public bool currentlyPurging = false;
        bool hiss = false;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref currentlyPurging, "purging");
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
                        currentlyPurging = !currentlyPurging;
                        notInsideShield = true;
                        if (currentlyPurging)
                        {
                            foreach (CompShipCombatShield shield in parent.Map.GetComponent<ShipHeatMapComp>().Shields)
                            {
                                shield.parent.TryGetComp<CompFlickable>().SwitchIsOn = false;
                            }
                            hiss = false;
                        }
                    },
                    isActive = delegate { return currentlyPurging; },
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
            if (currentlyPurging)
            {
                if (notInsideShield && myNet != null && parent.TryGetComp<CompRefuelable>().Fuel > 0 && myNet.StorageUsed >= Props.heatPurge * HEAT_PURGE_RATIO)
                {
                    parent.TryGetComp<CompRefuelable>().ConsumeFuel(Props.heatPurge);
                    myNet.AddHeat(Props.heatPurge * HEAT_PURGE_RATIO, remove: true);
                    FleckMaker.ThrowAirPuffUp(parent.DrawPos + new Vector3(0, 0, 1), parent.Map);
                    if (!hiss)
                    {
                        HissSound.PlayOneShot(parent);
                        hiss = true;
                    }
                }
                else
                {
                    currentlyPurging = false;
                    hiss = false;
                }
            }
        }
    }
}
