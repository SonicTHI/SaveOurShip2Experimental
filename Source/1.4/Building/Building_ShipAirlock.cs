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
using System.Net.NetworkInformation;
using Verse.Noise;
using MonoMod.Utils;

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
        public Building First;
        public Building Second;
        public int firstRot = -1; //doors dont have normal rot so this is used for extender gfx
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
                Room room = pos.GetRoom(Map);
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
            Scribe_Values.Look<int>(ref firstRot, "firstRot", -1);
            Scribe_Collections.Look<Building>(ref extenders, "extenders", LookMode.Reference);
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mapComp = this.Map.GetComponent<ShipHeatMapComp>();
            unfoldComp = this.TryGetComp<UnfoldComponent>();
        }

        //docking - doors dont have proper rot
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (docked)
            {
                DeSpawnDock();
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
                if (!mapComp.InCombat && CanDock())
                {
                    SpawnDock();
                }
            }
        }
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (Faction == Faction.OfPlayer && (Outerdoor() || docked) && HasDocking())
            {
                bool canDock = CanDock();
                Command_Toggle toggleDock = new Command_Toggle
                {
                    toggleAction = delegate
                    {
                        if (!docked && canDock)
                        {
                            float d = (dist - 1) * 0.3334f;
                            startTick = Find.TickManager.TicksGame + (int)(200 * d);
                            unfoldComp.Target = d;
                        }
                        else
                        {
                            DeSpawnDock();
                        }
                    },
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideToggleDock"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideToggleDockDesc"),
                    isActive = () => docked
                };
                if (docked)
                    toggleDock.icon = ContentFinder<Texture2D>.Get("UI/DockingOn");
                else
                    toggleDock.icon = ContentFinder<Texture2D>.Get("UI/DockingOff");

                if (startTick > 0 || mapComp.InCombat || !powerComp.PowerOn || !canDock)
                {
                    toggleDock.Disable();
                }
                yield return toggleDock;
            }
        }
        public bool HasDocking() //check if airlock has docking beams
        {
            for (int i = 0; i < 2; i++) //find first extender, check opposite for other, same rot, not facing airlock
            {
                IntVec3 v = Position + GenAdj.CardinalDirections[i];
                Thing first = v.GetFirstThingWithComp<CompSoShipDocking>(Map);
                if (first == null)
                    continue;
                var firstComp = first.TryGetComp<CompSoShipDocking>();
                if (firstComp.Props.extender)
                {
                    if (i == first.Rotation.AsByte || i == first.Rotation.AsByte + 2) //cant face same or opp cardinal
                        break;
                    Thing second = (Position + GenAdj.CardinalDirections[i + 2]).GetFirstThingWithComp<CompSoShipDocking>(Map);
                    if (second != null)
                    {
                        var secondComp = second.TryGetComp<CompSoShipDocking>();
                        if (secondComp.Props.extender && first.Rotation == second.Rotation)
                        {
                            First = first as Building;
                            firstRot = first.Rotation.AsInt;
                            Second = second as Building;
                            firstComp.dockParent = this;
                            secondComp.dockParent = this;
                            return true;
                        }
                    }
                    break;
                }
            }
            First = null;
            Second = null;
            firstRot = -1;
            return false;
        }
        public bool CanDock() //check if all clear, set dist
        {
            if (docked)
                return true;
            if (First == null || First.Destroyed || Second == null || Second.Destroyed)
            {
                unfoldComp.Target = 0.0f;
                ResetDock();
                return false;
            }
            dist = 0;
            for (int i = 1; i < 4; i++)
            {
                IntVec3 offset = GenAdj.CardinalDirections[First.Rotation.AsByte] * -i;
                IntVec3 center = Position + offset;
                IntVec3 first = First.Position + offset;
                IntVec3 second = Second.Position + offset;
                var grid = Map.thingGrid;
                if (grid.ThingsAt(first).Any() || grid.ThingsAt(center).Any() || grid.ThingsAt(second).Any())
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
        public void SpawnDock()
        {
            IntVec3 rot = GenAdj.CardinalDirections[First.Rotation.AsByte];
            //place fake walls, floor, extend
            for (int i = 1; i < dist; i++)
            {
                IntVec3 offset = rot * -i;
                Thing thing;
                thing = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipAirlockBeamWall);
                GenSpawn.Spawn(thing, First.Position + offset, Map);
                thing.TryGetComp<CompSoShipDocking>().dockParent = this;
                extenders.Add(thing as Building);
                thing = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipAirlockBeamTile);
                GenSpawn.Spawn(thing, Position + offset, Map);
                thing.TryGetComp<CompSoShipDocking>().dockParent = this;
                extenders.Add(thing as Building);
                thing = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipAirlockBeamWall);
                GenSpawn.Spawn(thing, Second.Position + offset, Map);
                thing.TryGetComp<CompSoShipDocking>().dockParent = this;
                extenders.Add(thing as Building);
            }
            //set temp
            Room room = (Position - rot).GetRoom(Map);
            if (room != null && !room.UsesOutdoorTemperature)
                room.Temperature = (Position + rot).GetRoom(Map).Temperature;
            docked = true;
        }
        public void DeSpawnDock(bool force = false)
        {
            unfoldComp.Target = 0.0f;
            if (extenders.Any())
            {
                if (extenders.Count > 3)
                {
                    FleckMaker.ThrowDustPuff(extenders[extenders.Count - 1].Position, Map, 1f);
                    FleckMaker.ThrowDustPuff(extenders[extenders.Count - 3].Position, Map, 1f);
                }
                List<Building> toDestroy = new List<Building>();
                foreach (Building building in extenders.Where(b => !b.Destroyed))
                {
                    if (!force)
                    {
                        var comp = building.TryGetComp<CompSoShipDocking>();
                        if (comp != null)
                            comp.removedByDock = true;
                    }
                    toDestroy.Add(building);
                }
                foreach (Building building in toDestroy)
                {
                    if (!building.Destroyed)
                        building.Destroy();
                }
                extenders.Clear();
            }
            docked = false;
        }
        public void ResetDock()
        {
            First = null;
            Second = null;
            firstRot = -1;
            unfoldComp.extension = 0.0f;
        }
    }
}
