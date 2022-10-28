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
    [StaticConstructorOnStartup]
    public class Building_ShipAirlock : Building_Door
    {
        public static ThingDef dockWallDef = ThingDef.Named("ShipAirlockBeamWall");
        public static ThingDef insideDef = ThingDef.Named("ShipAirlockBeamTile");
        public static ThingDef dockDef = ThingDef.Named("ShipAirlockBeam");
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
                Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipAirlockHacked"), this, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                this.failed = true;
                pawn.skills.GetSkill(SkillDefOf.Intellectual).Learn(100);
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
                IntVec3 facing;
                IntVec3 leftSide;
                IntVec3 rightSide;
                if (this.Rotation.AsByte == 0)
                {
                    facing = IntVec3.North;
                    leftSide = IntVec3.West;
                    rightSide = IntVec3.East;
                }
                else
                {
                    facing = IntVec3.East;
                    leftSide = IntVec3.North;
                    rightSide = IntVec3.South;
                }
                Building b1 = (this.Position + leftSide).GetFirstBuilding(this.Map);
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

                    if (startTick > 0 || mapComp.InCombat || !powerComp.PowerOn || !CanDock(facing, leftSide, rightSide))
                    {
                        toggleDock.Disable();
                    }
                    yield return toggleDock;
                }
            }
        }
        public bool HasDocking(Building b1, Building b2)
        {
            //check LR for extender, same rot
            if (b1 != null && b2 != null && b1.def == dockDef && b2.def == dockDef)
            {
                polarity = 0;
                if ((this.Rotation.AsByte == 0 && b1.Rotation.AsByte == 0 && b2.Rotation.AsByte == 0) || (this.Rotation.AsByte == 1 && b1.Rotation.AsByte == 1 && b2.Rotation.AsByte == 1))
                {
                    polarity = -1;
                    return true;
                }
                else if ((this.Rotation.AsByte == 0 && b1.Rotation.AsByte == 2 && b2.Rotation.AsByte == 2) || (this.Rotation.AsByte == 1 && b1.Rotation.AsByte == 3 && b2.Rotation.AsByte == 3))
                {
                    polarity = 1;
                    return true;
                }
            }
            return false;
        }
        public bool CanDock(IntVec3 facing, IntVec3 leftSide, IntVec3 rightSide)
        {
            //check if all clear, set dist
            if (docked)
                return true;
            IntVec3 loc1;
            IntVec3 loc2;
            IntVec3 loc3;
            dist = 0;
            for (int i = 1; i < 4; i++)
            {
                loc1 = this.Position + facing * i * polarity + leftSide;
                loc2 = this.Position + facing * i * polarity;
                loc3 = this.Position + facing * i * polarity + rightSide;
                if (this.Map.thingGrid.ThingsAt(loc1).Any() || this.Map.thingGrid.ThingsAt(loc2).Any() || this.Map.thingGrid.ThingsAt(loc3).Any())
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
            IntVec3 facing;
            IntVec3 leftSide;
            IntVec3 rightSide;
            if (this.Rotation.AsByte == 0)
            {
                facing = IntVec3.North;
                leftSide = IntVec3.West;
                rightSide = IntVec3.East;
            }
            else
            {
                facing = IntVec3.East;
                leftSide = IntVec3.North;
                rightSide = IntVec3.South;
            }
            IntVec3 loc1;
            IntVec3 loc2;
            IntVec3 loc3;
            for (int i = 1; i < dist; i++)
            {
                loc1 = this.Position + facing * i * polarity + leftSide;
                loc2 = this.Position + facing * i * polarity;
                loc3 = this.Position + facing * i * polarity + rightSide;
                Thing thing;
                thing = ThingMaker.MakeThing(dockWallDef);
                GenSpawn.Spawn(thing, loc1, this.Map);
                extenders.Add(thing as Building);
                thing = ThingMaker.MakeThing(insideDef);
                GenSpawn.Spawn(thing, loc2, this.Map);
                extenders.Add(thing as Building);
                thing = ThingMaker.MakeThing(dockWallDef);
                GenSpawn.Spawn(thing, loc3, this.Map);
                extenders.Add(thing as Building);
            }
            docked = true;
        }
        public void UnDock()
        {
            if (extenders.Any())
            {
                FleckMaker.ThrowDustPuff(extenders[extenders.Count - 1].Position, this.Map, 1f);
                FleckMaker.ThrowDustPuff(extenders[extenders.Count - 3].Position, this.Map, 1f);
                foreach (Building building in extenders)
                {
                    if (building.Spawned)
                        building.DeSpawn();
                }
                unfoldComp.Target = 0.0f;
                docked = false;
            }
        }
    }
}
