using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld
{
    class CompHologramRelay : ThingComp
    {
        Pawn relaying;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if(parent.TryGetComp<CompPowerTrader>()!=null && parent.TryGetComp<CompPowerTrader>().PowerOn)
                return base.CompGetGizmosExtra().Append(HologramRelayGizmo());
            return base.CompGetGizmosExtra();
        }

        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            if (parent.TryGetComp<CompReloadable>().RemainingCharges>0)
                return base.CompGetWornGizmosExtra().Append(HologramRelayGizmo());
            return base.CompGetGizmosExtra();
        }

        Command_Action HologramRelayGizmo()
        {
            if (relaying == null)
            {
                return new Command_Action
                {
                    action = delegate
                      {
                          List<FloatMenuOption> options = new List<FloatMenuOption>();
                          foreach (Pawn hologram in PawnsFinder.AllMaps_FreeColonists)
                          {
                              if (hologram.health.hediffSet.GetFirstHediff<HediffPawnIsHologram>()?.consciousnessSource.Map!=ParentMap() && ShipInteriorMod2.IsHologram(hologram))
                              {
                                  options.Add(new FloatMenuOption(hologram.LabelShort, delegate
                                  {
                                      TargetingParameters tp = new TargetingParameters();
                                      tp.canTargetLocations = true;
                                      tp.canTargetPlants = false;
                                      tp.canTargetItems = false;
                                      tp.canTargetBuildings = false;
                                      Find.Targeter.BeginTargeting(tp, info => RelayHologram(hologram, info), info => { if(GenSight.LineOfSight(ParentPosition(),info.Cell,ParentMap())) { GenDraw.DrawTargetHighlight(info); } }, delegate(LocalTargetInfo info) { return GenSight.LineOfSight(ParentPosition(),info.Cell,ParentMap()); } );
                                  }));
                              }
                          }
                          if (options.Count > 0)
                              Find.WindowStack.Add(new FloatMenu(options));
                      },
                    defaultLabel = "SoSHologramRelay".Translate(),
                    defaultDesc = "SoSHologramRelayDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/SpawnHologram", true)
                };
            }
            else
            {
                return new Command_Action
                {
                    action = delegate
                    {
                        StopRelaying(true);
                    },
                    defaultLabel = "SoSHologramRelayStop".Translate(),
                    defaultDesc = "SoSHologramRelayStopDesc".Translate(relaying.LabelShort),
                    icon = ContentFinder<Texture2D>.Get("UI/DespawnHologram", true)
                };
            }
        }

        void RelayHologram(Pawn hologram, LocalTargetInfo info)
        {
            ThingWithComps relay = hologram.health.hediffSet.GetFirstHediff<HediffPawnIsHologram>().relay;
            if(relay!=null)
                relay.TryGetComp<CompHologramRelay>().StopRelaying(false);
            if(hologram.Spawned)
                hologram.DeSpawn();
            hologram.Position = info.Cell;
            hologram.SpawnSetup(ParentMap(),false);
            FleckMaker.Static(info.Cell, ParentMap(), FleckDefOf.PsycastAreaEffect,5f);
            SoundDefOf.PsychicPulseGlobal.PlayOneShotOnCamera(Find.CurrentMap);
            relaying = hologram;
            hologram.health.hediffSet.GetFirstHediff<HediffPawnIsHologram>().relay = parent;
            if (parent.TryGetComp<CompReloadable>() != null)
                parent.TryGetComp<CompReloadable>().UsedOnce();
        }

        public void StopRelaying(bool respawn)
        {
            if (relaying != null)
            {
                CompBuildingConsciousness buildingConsc = relaying.health.hediffSet.GetFirstHediff<HediffPawnIsHologram>().consciousnessSource.TryGetComp<CompBuildingConsciousness>();
                buildingConsc.HologramDestroyed(false);
                if (respawn && Find.TickManager.TicksGame > buildingConsc.HologramRespawnTick)
                {
                    buildingConsc.SpawnHologram();
                }
                relaying = null;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look<Pawn>(ref relaying, "relaying");
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            StopRelaying(true);
        }

        public override void ReceiveCompSignal(string signal)
        {
            if (signal == "PowerTurnedOff" || signal == "FlickedOff")
            {
                StopRelaying(true);
            }
        }

        Map ParentMap()
        {
            if (parent is Apparel)
                return ((Apparel)parent).Wearer.Map;
            return parent.Map;
        }

        IntVec3 ParentPosition()
        {
            if (parent is Apparel)
                return ((Apparel)parent).Wearer.Position;
            return parent.Position;
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            StopRelaying(true);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (relaying!=null && Find.TickManager.TicksGame % 30 == 0 && parent is Apparel)
            {
                Pawn wearer = ((Apparel)parent).Wearer;
                if (wearer.Dead || !wearer.Spawned || wearer.Map != relaying.Map)
                    StopRelaying(true);
            }
        }
    }
}
