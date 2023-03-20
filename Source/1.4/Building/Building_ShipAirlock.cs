using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;
using Verse.AI.Group; 
using Verse.Sound;
using HarmonyLib;
using SaveOurShip2;
using RimworldMod;
using UnityEngine;

namespace RimWorld
{
    public class Building_ShipAirlock : Building_Door
    {
        List<Building> extenders = new List<Building>();

        public ShipHeatMapComp mapComp;
        public UnfoldComponent unfoldComp;
        public bool hacked = false;
        public bool failed = false;
        public bool docked = false;
        int polarity = 0;
        int dist = 0;
        int startTick = 0;

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn pawn)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (FloatMenuOption op in base.GetFloatMenuOptions(pawn))
                options.Add(op);
            if (this.Map != null && this.Map.Parent != null && this.Map.Parent.def.defName.Equals("SiteSpace")) //To prevent cheesing the starship bow quest
                return options;
            if (this.Faction != Faction.OfPlayer)
            {
                if (!hacked && !failed && !pawn.skills.GetSkill(SkillDefOf.Intellectual).TotallyDisabled && pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) > 0)
                {
                    options.Add(new FloatMenuOption("Hack", delegate { Job hackAirlock = new Job(DefDatabase<JobDef>.GetNamed("HackAirlock"), this); pawn.jobs.TryTakeOrderedJob(hackAirlock); }));
                }
                if (!hacked && !pawn.skills.GetSkill(SkillDefOf.Construction).TotallyDisabled && pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) > 0)
                {
                    options.Add(new FloatMenuOption("Breach", delegate { Job breachAirlock = new Job(DefDatabase<JobDef>.GetNamed("BreachAirlock"), this); pawn.jobs.TryTakeOrderedJob(breachAirlock); }));
                }
            }
            return options;
        }
        //hacked - chance to open and set to neutral
        public void HackMe(Pawn pawn)
        {
            if (Rand.Chance(0.045f * pawn.skills.GetSkill(SkillDefOf.Intellectual).levelInt + 0.05f))
            {
                this.hacked = true;
                pawn.skills.GetSkill(SkillDefOf.Intellectual).Learn(200);
                this.SetFaction(Faction.OfAncients);
                this.DoorOpen();
                this.def.building.soundDoorOpenManual.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
                if (pawn.Faction == Faction.OfPlayer)
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipAirlockHacked"), this, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                this.failed = true;
                pawn.skills.GetSkill(SkillDefOf.Intellectual).Learn(100);
                if (pawn.Faction == Faction.OfPlayer)
                    Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipAirlockHackFailed"), this, MessageTypeDefOf.NegativeEvent);
            }
        }
        //breached - will open and stay open
        public void BreachMe(Pawn pawn)
        {
            this.hacked = true;
            if (!pawn.RaceProps.IsMechanoid)
                pawn.skills.GetSkill(SkillDefOf.Construction).Learn(200);
            this.DoorOpen();
            Traverse.Create(this).Field("holdOpenInt").SetValue(true);
            this.def.building.soundDoorOpenManual.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
            if (pawn.Faction == Faction.OfPlayer)
                Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipAirlockBreached"), this, MessageTypeDefOf.PositiveEvent);
            this.TakeDamage(new DamageInfo(DamageDefOf.Cut,200));
        }
		public override bool PawnCanOpen(Pawn p)
        {
            if (p.RaceProps.FenceBlocked && p.RaceProps.Roamer && p.CurJobDef != JobDefOf.FollowRoper) return false;
            //enemy pawns can pass through their doors if outside or with EVA when player is present
            if (p.Map.IsSpace() && p.Faction != Faction.OfPlayer && this.Outerdoor())
            {
                if (ShipInteriorMod2.ExposedToOutside(p.GetRoom()) || (ShipInteriorMod2.EVAlevel(p)>3 && (!mapComp.InCombat || p.Map.mapPawns.AnyColonistSpawned))) { }
                else return false;
            }
            Lord lord = p.GetLord();
            return base.PawnCanOpen(p) && ((lord != null && lord.LordJob != null && lord.LordJob.CanOpenAnyDoor(p)) || WildManUtility.WildManShouldReachOutsideNow(p) || base.Faction == null || (p.guest != null && p.guest.Released) || GenAI.MachinesLike(base.Faction, p));
        }
        public bool Outerdoor()
        {
            foreach (IntVec3 pos in GenAdj.CellsAdjacentCardinal(this))
            {
                Room room = pos.GetRoom(this.Map);
                if (room != null && (room.OpenRoofCount > 0 || room.TouchesMapEdge))
                {
                    return true;
                }
            }
            return false;
        }
        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder(base.GetInspectString());
            if (Prefs.DevMode)
            {
                if (this.Outerdoor())
                {
                    stringBuilder.AppendLine("outerdoor");
                }
                else
                {
                    stringBuilder.AppendLine("innerdoor");
                }
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<bool>(ref hacked, "hacked", false);
            Scribe_Values.Look<bool>(ref failed, "failed", false);
            Scribe_Values.Look<bool>(ref docked, "docked", false);
            Scribe_Values.Look<int>(ref dist, "dist", 0);
            Scribe_Values.Look<int>(ref startTick, "startTick", 0);
            Scribe_Collections.Look<Building>(ref extenders, "extenders", LookMode.Reference);
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mapComp = this.Map.GetComponent<ShipHeatMapComp>();
            unfoldComp = this.TryGetComp<UnfoldComponent>();
        }
        //docking - doors dont have proper rot, this could be remade but would need components for extenders, etc.
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (docked)
            {
                UnDock();
            }
            base.Destroy(mode);
        }
        public override void Tick()
        {
            base.Tick();
            //create area after animation
            if (startTick > 0 && Find.TickManager.TicksGame > startTick)
            {
                startTick = 0;
                if (!mapComp.InCombat)
                    Dock();
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (this.Faction == Faction.OfPlayer && (Outerdoor() || docked))
            {
                IntVec3 facing = this.Rotation.FacingCell;
                IntVec3 rightSide = this.Rotation.RighthandCell;

                Building b1 = (this.Position - rightSide).GetFirstBuilding(this.Map);
                Building b2 = (this.Position + rightSide).GetFirstBuilding(this.Map);
                if (HasDocking(b1, b2))
                {
                    Command_Toggle toggleDock = new Command_Toggle
                    {
                        toggleAction = delegate
                        {
                            if (!docked)
                            {
                                startTick = Find.TickManager.TicksGame + 170;
                                unfoldComp.Target = (dist - 1) * 0.33f;
                            }
                            else
                                UnDock();
                        },
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideToggleDock"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideToggleDockDesc"),
                        isActive = () => docked
                    };
                    if (docked)
                        toggleDock.icon = ContentFinder<Texture2D>.Get("UI/DockingOn");
                    else
                        toggleDock.icon = ContentFinder<Texture2D>.Get("UI/DockingOff");

                    if (startTick > 0 || mapComp.InCombat || !powerComp.PowerOn || !CanDock(facing, rightSide))
                    {
                        toggleDock.Disable();
                    }
                    yield return toggleDock;
                }
            }
        }
        // Checks if airlock is surrounded by docking beams.
        public bool HasDocking(Building b1, Building b2)
        {
            //check LR for extender, same rot
            if (b1 != null && b2 != null && b1.def == ResourceBank.ThingDefOf.ShipAirlockBeam && b2.def == ResourceBank.ThingDefOf.ShipAirlockBeam)
            {
                polarity = 0;
                var r1 = b1.Rotation.AsByte;
                var r2 = b2.Rotation.AsByte;
                if ((this.Rotation.AsByte == 0 && r1 == 0 && r2 == 0) || (this.Rotation.AsByte == 1 && r1 == 1 && r2 == 1))
                {
                    polarity = -1;
                    return true;
                }
                else if ((this.Rotation.AsByte == 0 && r1 == 2 && r2 == 2) || (this.Rotation.AsByte == 1 && r1 == 3 && r2 == 3))
                {
                    polarity = 1;
                    return true;
                }
            }
            return false;
        }
        public bool CanDock(IntVec3 facing, IntVec3 rightSide)
        {
            //check if all clear, set dist
            if (docked)
                return true;
            dist = 0;
            for (int i = 1; i < 4; i++)
            {
                IntVec3 center = this.Position + facing * i * polarity;
                IntVec3 loc1 = center - rightSide;
                IntVec3 loc3 = center + rightSide;
                var grid = this.Map.thingGrid;
                if (grid.ThingsAt(loc1).Any() || grid.ThingsAt(center).Any() || grid.ThingsAt(loc3).Any())
                {
                    if (i == 1)
                        return false;
                    dist = i;
                    return true;
                }
            }
            dist = 4;
            return true;
        }
        public void Dock()
        {
            //place fake walls, floor, extend
            IntVec3 facing = this.Rotation.FacingCell;
            IntVec3 rightSide = this.Rotation.RighthandCell;
            
            for (int i = 1; i < dist; i++)
            {
                IntVec3 center = this.Position + facing * i * polarity;
                Thing thing;
                thing = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipAirlockBeamWall);
                GenSpawn.Spawn(thing, center - rightSide, this.Map);
                extenders.Add(thing as Building);
                thing = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipAirlockBeamTile);
                GenSpawn.Spawn(thing, center, this.Map);
                extenders.Add(thing as Building);
                thing = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipAirlockBeamWall);
                GenSpawn.Spawn(thing, center + rightSide, this.Map);
                extenders.Add(thing as Building);
            }
            docked = true;
            //Log.Message($"Dock R={Rotation} F={facing} polarity={polarity} dist={dist} spawned={extenders.Count}");
        }
        public void UnDock()
        {
            if (extenders.Any())
            {
                //Log.Message($"UnDock R={Rotation} polarity={polarity} dist={dist} spawned={extenders.Count}");
                if (extenders.Count > 3)
                {
                    FleckMaker.ThrowDustPuff(extenders[extenders.Count - 1].Position, this.Map, 1f);
                    FleckMaker.ThrowDustPuff(extenders[extenders.Count - 3].Position, this.Map, 1f);
                }
                List<Building> toDestroy = new List<Building>();
                foreach (Building building in extenders.Where(b => !b.Destroyed))
                {
                    toDestroy.Add(building);
                }
                foreach (Building building in toDestroy)
                {
                    building.Destroy();
                }
                unfoldComp.Target = 0.0f;
                docked = false;
                extenders.Clear();
            }
        }
    }
}
