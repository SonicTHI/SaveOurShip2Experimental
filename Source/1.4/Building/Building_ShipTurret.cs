using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using SaveOurShip2;
using RimWorld.Planet;
using HarmonyLib;
using RimworldMod;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class Building_ShipTurret : Building_Turret
    {
        public static Material ForcedTargetLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.5f, 0.5f));
        private const int TryStartShootSomethingIntervalTicks = 10;

        public Thing gun;
        protected TurretTop top;
        public ShipHeatMapComp mapComp;
        public CompPowerTrader powerComp;
        public CompShipHeat heatComp;
        public CompRefuelable fuelComp;
        public CompSpinalMount spinalComp;
        public CompChangeableProjectilePlural torpComp;
        protected CompInitiatable initiatableComp;
        protected Effecter progressBarEffecter;

        protected LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;
        public int[] SegmentPower = new int[3];
        public int burstCooldownTicksLeft;
        protected int burstWarmupTicksLeft;
        public IntVec3 SynchronizedBurstLocation;
        public int AmplifierCount = -1;
        public float AmplifierDamageBonus = 0;
        bool selected = false;
        public bool holdFire;
        public bool PointDefenseMode;
        public bool GroundDefenseMode;
        public bool useOptimalRange;
        static int lastPDTick = 0;

        public bool Active
        {
            get
            {
                if ((powerComp == null || powerComp.PowerOn))
                {
                    return true;
                }
                return false;
            }
        }

        private bool CanExtractTorpedo
        {
            get
            {
                if (!PlayerControlled)
                {
                    return false;
                }
                return torpComp?.Loaded ?? false;
            }
        }

        public CompEquippable GunCompEq => gun.TryGetComp<CompEquippable>();
        public override LocalTargetInfo CurrentTarget => currentTargetInt;
        public override Verb AttackVerb => GunCompEq.PrimaryVerb;

        LocalTargetInfo shipTarget=LocalTargetInfo.Invalid;

        private bool PlayerControlled
        {
            get
            {
                if (base.Faction == Faction.OfPlayer)
                {
                    return true;
                }
                return false;
            }
        }
        private bool CanSetForcedTarget
        {
            get
            {
                return true;
            }
        }
        private bool CanToggleHoldFire => PlayerControlled;
        public Building_ShipTurret()
        {
            top = new TurretTop(this);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mapComp = this.Map.GetComponent<ShipHeatMapComp>();
            initiatableComp = GetComp<CompInitiatable>();
            powerComp = this.TryGetComp<CompPowerTrader>();
            heatComp = this.TryGetComp<CompShipHeat>();
            fuelComp = this.TryGetComp<CompRefuelable>();
            spinalComp = this.TryGetComp<CompSpinalMount>();
            torpComp = this.gun.TryGetComp<CompChangeableProjectilePlural>();
            if (!this.Map.IsSpace() && heatComp.Props.groundDefense)
                GroundDefenseMode = true;
            else
                GroundDefenseMode = false;
            if (!respawningAfterLoad)
            {
                top.SetRotationFromOrientation();
                burstCooldownTicksLeft = def.building.turretInitialCooldownTime.SecondsToTicks();
                ResetForcedTarget();
            }
        }

        public override void PostMake()
        {
            base.PostMake();
            MakeGun();
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            base.DeSpawn(mode);
            ResetCurrentTarget();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref burstCooldownTicksLeft, "burstCooldownTicksLeft", 0);
            Scribe_Values.Look(ref burstWarmupTicksLeft, "burstWarmupTicksLeft", 0);
            Scribe_TargetInfo.Look(ref currentTargetInt, "currentTarget");
            Scribe_Values.Look(ref holdFire, "holdFire", defaultValue: false);
            Scribe_Deep.Look(ref gun, "gun");
            Scribe_Values.Look<IntVec3>(ref SynchronizedBurstLocation, "burstLocation");
            Scribe_Values.Look<bool>(ref PointDefenseMode, "pointDefenseMode");
            Scribe_Values.Look<bool>(ref useOptimalRange, "useOptimalRange");
            BackCompatibility.PostExposeData(this);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                UpdateGunVerbs();
            }
        }

        public override void OrderAttack(LocalTargetInfo targ)
        {
            if (forcedTarget != targ)
            {
                forcedTarget = targ;
                if (burstCooldownTicksLeft <= 0)
                {
                    TryStartShootSomething(false);
                }
            }
            if (holdFire)
            {
                Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageTurretWontFireBecauseHoldFire", def.label), this, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            if (this.PointDefenseMode)
            {
                Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageTurretWontFireBecausePointDefense", def.label), this, MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            if (GroundDefenseMode)
            {
                if (!targ.IsValid)
                {
                    if (this.forcedTarget.IsValid)
                    {
                        this.ResetForcedTarget();
                    }
                    return;
                }
                if ((targ.Cell - base.Position).LengthHorizontal < this.AttackVerb.verbProps.EffectiveMinRange(targ, this))
                {
                    Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageTypeDefOf.RejectInput, false);
                    return;
                }
                if ((targ.Cell - base.Position).LengthHorizontal > this.AttackVerb.verbProps.range)
                {
                    Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageTypeDefOf.RejectInput, false);
                    return;
                }
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (selected && !Find.Selector.IsSelected(this))
            {
                selected = false;
            }
            if (!CanToggleHoldFire)
            {
                holdFire = false;
            }
            if (this.forcedTarget.ThingDestroyed)
            {
                this.ResetForcedTarget();
            }
            if (GroundDefenseMode)
            {
                if (this.forcedTarget.IsValid && !this.CanSetForcedTarget)
                {
                    this.ResetForcedTarget();
                }
                if (this.Active && !this.stunner.Stunned && base.Spawned)
                {
                    this.GunCompEq.verbTracker.VerbsTick();
                    if (this.AttackVerb.state != VerbState.Bursting)
                    {
                        if (this.burstWarmupTicksLeft > 0)
                        {
                            this.burstWarmupTicksLeft--;
                            if (this.burstWarmupTicksLeft == 0)
                            {
                                this.BeginBurst();
                            }
                        }
                        else
                        {
                            if (this.burstCooldownTicksLeft > 0)
                            {
                                this.burstCooldownTicksLeft--;
                            }
                            if (this.burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
                            {
                                this.TryStartShootSomething(true);
                            }
                        }
                        this.top.TurretTopTick();
                        return;
                    }
                }
                else
                {
                    this.ResetCurrentTarget();
                }
            }
            else
            {
                if (!mapComp.InCombat || (spinalComp != null && AmplifierCount == -1))
                {
                    ResetForcedTarget();
                }
                if (Active && base.Spawned)
                {
                    GunCompEq.verbTracker.VerbsTick();
                    if (stunner.Stunned || AttackVerb.state == VerbState.Bursting)
                    {
                        return;
                    }
                    else if (burstCooldownTicksLeft > 0)
                    {
                        burstCooldownTicksLeft--;
                    }
                    if (mapComp.InCombat)
                    {
                        //PD mode
                        if ((this.IsHashIntervalTick(10) && burstCooldownTicksLeft <= 0 && IncomingPtDefTargetsInRange()) && (PointDefenseMode || (!PlayerControlled && heatComp.Props.pointDefense)))
                        {
                            if (Find.TickManager.TicksGame > lastPDTick + 10 && !holdFire)
                                BeginBurst(true);
                        }
                        //check if we are in range
                        else
                        {
                            float range = mapComp.ShipCombatMasterMap.GetComponent<ShipHeatMapComp>().Range;
                            if ((!useOptimalRange && heatComp.Props.maxRange > range) || (useOptimalRange && heatComp.Props.optRange > range))
                            {
                                //cant fire spinals opposite of heading
                                if (spinalComp != null)
                                {
                                    if ((this.Rotation == new Rot4(mapComp.EngineRot) && mapComp.Heading == -1) || (this.Rotation == new Rot4(mapComp.EngineRot + 2) && mapComp.Heading == 1))
                                    {
                                        if (PlayerControlled)
                                            return;
                                        else
                                        {
                                            mapComp.Heading = 1;
                                        }
                                    }
                                }
                                if (burstWarmupTicksLeft > 0)
                                {
                                    burstWarmupTicksLeft--;
                                    if (burstWarmupTicksLeft == 0)
                                    {
                                        BeginBurst();
                                    }
                                }
                                else if (this.IsHashIntervalTick(10) && burstCooldownTicksLeft <= 0)
                                {
                                    TryStartShootSomething(true);
                                }
                            }
                        }
                    }
                    top.TurretTopTick();
                }
                else
                {
                    ResetCurrentTarget();
                }
            }
        }

        protected void TryStartShootSomething(bool canBeginBurstImmediately)
        {
            bool isValid = currentTargetInt.IsValid;
            if (GroundDefenseMode)
            {
                if (this.progressBarEffecter != null)
                {
                    this.progressBarEffecter.Cleanup();
                    this.progressBarEffecter = null;
                }
                if (!base.Spawned || (this.holdFire && this.CanToggleHoldFire) || !this.AttackVerb.Available())
                {
                    this.ResetCurrentTarget();
                    return;
                }
                if (this.forcedTarget.IsValid)
                {
                    this.currentTargetInt = this.forcedTarget;
                }
                else
                {
                    this.currentTargetInt = this.TryFindNewTarget();
                }
            }
            else
            {
                if (spinalComp != null)
                    RecalcStats();
                if (!base.Spawned || (holdFire && CanToggleHoldFire) || !AttackVerb.Available() || PointDefenseMode || !mapComp.InCombat || (spinalComp != null && AmplifierCount == -1))
                {
                    ResetCurrentTarget();
                    return;
                }
                if (!this.PlayerControlled && mapComp.ShipCombatMaster)
                {
                    if (spinalComp == null || spinalComp.Props.destroysHull || mapComp.ShipCombatOriginMap.mapPawns.FreeColonistsAndPrisoners.Count == 0)
                        shipTarget = mapComp.ShipCombatOriginMap.listerThings.AllThings.RandomElement();
                    else //Target pawn with the Psychic Flayer
                        shipTarget = mapComp.ShipCombatOriginMap.mapPawns.FreeColonistsAndPrisoners.RandomElement();
                }
                if (shipTarget.IsValid)
                {
                    //fire same as engine direction or opposite if retreating
                    int rotA = mapComp.EngineRot;
                    int rotB = mapComp.Heading;
                    if ((rotA == 0 && rotB != -1) || (rotA == 2 && rotB == -1)) //north
                        currentTargetInt = new LocalTargetInfo(new IntVec3(Rand.RangeInclusive(this.Position.x - 5, this.Position.x + 5), 0, this.Map.Size.z - 1));
                    else if ((rotA == 1 && rotB != -1) || (rotA == 3 && rotB == -1)) //east
                        currentTargetInt = new LocalTargetInfo(new IntVec3(this.Map.Size.x - 1, 0, Rand.RangeInclusive(this.Position.z - 5, this.Position.z + 5)));
                    else if ((rotA == 2 && rotB != -1) || (rotA == 0 && rotB == -1)) //south
                        currentTargetInt = new LocalTargetInfo(new IntVec3(Rand.RangeInclusive(this.Position.x - 5, this.Position.x + 5), 0, 0));
                    else//west
                        currentTargetInt = new LocalTargetInfo(new IntVec3(0, 0, Rand.RangeInclusive(this.Position.z - 5, this.Position.z + 5)));
                }
                else
                {
                    currentTargetInt = TryFindNewTarget();
                }
            }
            if (!isValid && this.currentTargetInt.IsValid)
            {
                SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
            }
            if (!this.currentTargetInt.IsValid)
            {
                this.ResetCurrentTarget();
                return;
            }
            float randomInRange = this.def.building.turretBurstWarmupTime.RandomInRange;
            if (randomInRange > 0f)
            {
                this.burstWarmupTicksLeft = randomInRange.SecondsToTicks();
                return;
            }
            if (canBeginBurstImmediately)
            {
                this.BeginBurst();
                return;
            }
            this.burstWarmupTicksLeft = 1;
        }

        protected LocalTargetInfo TryFindNewTarget()
        {
            if (GroundDefenseMode)
            {
                IAttackTargetSearcher attackTargetSearcher = this.TargSearcher();
                TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable |  TargetScanFlags.NeedNotUnderThickRoof;
                return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, new Predicate<Thing>(this.IsValidTarget), 0f, 9999f);
            }
            else
                return LocalTargetInfo.Invalid;
        }

        private IAttackTargetSearcher TargSearcher()
        {
            return this;
        }

        private bool IsValidTarget(Thing t)
        {
            Pawn pawn = t as Pawn;
            if (pawn != null)
            {
                if (pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer)
                {
                    return false;
                }
            }
            return true;
        }

        protected void BeginBurst(bool isPtDefBurst = false)
        {
            //check if we have power to fire
            if (powerComp != null && heatComp != null && powerComp.PowerNet.CurrentStoredEnergy() < heatComp.Props.energyToFire * (1 + AmplifierDamageBonus))
            {
                //Messages.Message(TranslatorFormattedStringExtensions.Translate("CannotFireDueToPower",this.Label), this, MessageTypeDefOf.CautionInput);
                //this.shipTarget = LocalTargetInfo.Invalid;
                return;
            }
            //if we do not have enough heatcap, vent heat to room/fail to fire in vacuum
            if (!heatComp.AddHeatToNetwork(heatComp.Props.heatPerPulse * (1 + AmplifierDamageBonus)))
            {
                if (!GroundDefenseMode && ShipInteriorMod2.ExposedToOutside(this.GetRoom()))
                {
                    if (!PointDefenseMode && PlayerControlled)
                        Messages.Message(TranslatorFormattedStringExtensions.Translate("CannotFireDueToHeat", this.Label), this, MessageTypeDefOf.CautionInput);
                    this.shipTarget = LocalTargetInfo.Invalid;
                    ResetCurrentTarget();
                    return;
                }
                GenTemperature.PushHeat(this, heatComp.Props.heatPerPulse * (1 + AmplifierDamageBonus));
            }
            //ammo
            if (fuelComp != null)
            {
                if (fuelComp.Fuel <= 0)
                {
                    if (!PointDefenseMode && PlayerControlled)
                        Messages.Message(TranslatorFormattedStringExtensions.Translate("CannotFireDueToAmmo", this.Label), this, MessageTypeDefOf.CautionInput);
                    this.shipTarget = LocalTargetInfo.Invalid;
                    ResetCurrentTarget();
                    return;
                }
                fuelComp.ConsumeFuel(1);
            }
            //draw the same percentage from each cap: needed*current/currenttotal
            foreach (CompPowerBattery bat in powerComp.PowerNet.batteryComps)
            {
                bat.DrawPower(Mathf.Min(heatComp.Props.energyToFire * (1 + AmplifierDamageBonus) * bat.StoredEnergy / powerComp.PowerNet.CurrentStoredEnergy(), bat.StoredEnergy));
            }
            if (heatComp.Props.singleFireSound != null)
            {
                heatComp.Props.singleFireSound.PlayOneShot(this);
            }

            if (GroundDefenseMode)
            {
                this.AttackVerb.TryStartCastOn(this.CurrentTarget, false, true, false);
                base.OnAttackedTarget(this.CurrentTarget);
            }
            else
            {
                //PD mode
                if (isPtDefBurst || shipTarget == null)
                    this.shipTarget = LocalTargetInfo.Invalid;
                //sync
                ((Verb_LaunchProjectileShip)AttackVerb).shipTarget = shipTarget;
                if (this.AttackVerb.verbProps.burstShotCount > 0 && mapComp.ShipCombatTargetMap != null)
                    SynchronizedBurstLocation = mapComp.FindClosestEdgeCell(mapComp.ShipCombatTargetMap, shipTarget.Cell);
                else
                    SynchronizedBurstLocation = IntVec3.Invalid;
                if (PointDefenseMode)
                    lastPDTick = Find.TickManager.TicksGame;
                //spinal weapons fire straight
                if (spinalComp != null)
                {
                    if (this.Rotation.AsByte == 0)
                        currentTargetInt = new LocalTargetInfo(new IntVec3(this.Position.x, 0, this.Map.Size.z - 1));
                    else if (this.Rotation.AsByte == 1)
                        currentTargetInt = new LocalTargetInfo(new IntVec3(this.Map.Size.x - 1, 0, this.Position.z));
                    else if (this.Rotation.AsByte == 2)
                        currentTargetInt = new LocalTargetInfo(new IntVec3(this.Position.x, 0, 1));
                    else
                        currentTargetInt = new LocalTargetInfo(new IntVec3(1, 0, this.Position.z));
                }
                AttackVerb.TryStartCastOn(currentTargetInt);
                OnAttackedTarget(currentTargetInt);
                burstCooldownTicksLeft = BurstCooldownTime().SecondsToTicks(); //Seems to prevent the "turbo railgun" bug. Don't ask me why.
                if (spinalComp != null && spinalComp.Props.destroysHull)
                {
                    List<Thing> thingsToDestroy = new List<Thing>();

                    if (this.Rotation.AsByte == 0)
                    {
                        for (int x = Position.x - 1; x <= Position.x + 1; x++)
                        {
                            for (int z = Position.z + 3; z < Map.Size.z; z++)
                            {
                                IntVec3 vec = new IntVec3(x, 0, z);
                                foreach (Thing thing in vec.GetThingList(Map))
                                {
                                    thingsToDestroy.Add(thing);
                                }
                            }
                        }
                    }
                    else if (this.Rotation.AsByte == 1)
                    {
                        for (int x = Position.x + 3; x < Map.Size.x; x++)
                        {
                            for (int z = Position.z - 1; z <= Position.z + 1; z++)
                            {
                                IntVec3 vec = new IntVec3(x, 0, z);
                                foreach (Thing thing in vec.GetThingList(Map))
                                {
                                    thingsToDestroy.Add(thing);
                                }
                            }
                        }
                    }
                    else if (this.Rotation.AsByte == 2)
                    {
                        for (int x = Position.x - 1; x <= Position.x + 1; x++)
                        {
                            for (int z = Position.z - 3; z > 0; z--)
                            {
                                IntVec3 vec = new IntVec3(x, 0, z);
                                foreach (Thing thing in vec.GetThingList(Map))
                                {
                                    thingsToDestroy.Add(thing);
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int x = 1; x <= Position.x - 3; x++)
                        {
                            for (int z = Position.z - 1; z <= Position.z + 1; z++)
                            {
                                IntVec3 vec = new IntVec3(x, 0, z);
                                foreach (Thing thing in vec.GetThingList(Map))
                                {
                                    thingsToDestroy.Add(thing);
                                }
                            }
                        }
                    }

                    foreach (Thing thing in thingsToDestroy)
                    {
                        GenExplosion.DoExplosion(thing.Position, thing.Map, 0.5f, DamageDefOf.Bomb, null);
                        if (!thing.Destroyed)
                            thing.Kill();
                    }
                }
            }
        }

        protected void BurstComplete()
        {
            burstCooldownTicksLeft = BurstCooldownTime().SecondsToTicks();
        }

        protected float BurstCooldownTime()
        {
            if (def.building.turretBurstCooldownTime >= 0f)
            {
                return def.building.turretBurstCooldownTime;
            }
            return AttackVerb.verbProps.defaultCooldownTime;
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            string inspectString = base.GetInspectString();
            if (!inspectString.NullOrEmpty())
            {
                stringBuilder.AppendLine(inspectString);
            }
            if (AttackVerb.verbProps.minRange > 0f && GroundDefenseMode)
            {
                stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("MinimumRange") + ": " + AttackVerb.verbProps.minRange.ToString("F0"));
            }
            if (base.Spawned && burstCooldownTicksLeft > 0 && BurstCooldownTime() > 5f)
            {
                stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("CanFireIn") + ": " + burstCooldownTicksLeft.ToStringSecondsFromTicks());
            }
			if (spinalComp != null)
            {
                if (AmplifierCount != -1)
                    stringBuilder.AppendLine("ShipAmplifierCount".Translate(AmplifierCount));
                else
                    stringBuilder.AppendLine("ShipSpinalCapNotFound".Translate());
            }
            if (torpComp != null)
            {
                if (torpComp.Loaded)
                {
                    int torp = 0;
                    int torpEMP = 0;
                    int torpAM = 0;
                    foreach (ThingDef t in torpComp.LoadedShells)
                    {
                        if (t == ThingDef.Named("ShipTorpedo_EMP"))
                            torpEMP++;
                        else if (t == ThingDef.Named("ShipTorpedo_Antimatter"))
                            torpAM++;
                        else
                            torp++;
                    }
                    if (torp > 0)
                        stringBuilder.AppendLine(torp + " HE torpedoes");
                    if (torpEMP > 0)
                        stringBuilder.AppendLine(torpEMP + " EMP torpedoes");
                    if (torpAM > 0)
                        stringBuilder.AppendLine(torpAM + " AM torpedoes");
                }
                else
                {
                    stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipTorpedoNotLoaded"));
                }
            }
            return stringBuilder.ToString().TrimEndNewlines();
        }

        public override void Draw()
        {
            top.DrawTurret(Vector3.zero,0);
            base.Draw();
        }

        public override void DrawExtraSelectionOverlays()
        {
            if (GroundDefenseMode)
            {
                base.DrawExtraSelectionOverlays();
                float range = this.AttackVerb.verbProps.range;
                if (range < 90f)
                {
                    GenDraw.DrawRadiusRing(base.Position, range);
                }
                float num = this.AttackVerb.verbProps.EffectiveMinRange(true);
                if (num < 90f && num > 0.1f)
                {
                    GenDraw.DrawRadiusRing(base.Position, num);
                }
                if (this.burstWarmupTicksLeft > 0)
                {
                    int degreesWide = (int)((float)this.burstWarmupTicksLeft * 0.5f);
                    GenDraw.DrawAimPie(this, this.CurrentTarget, degreesWide, (float)this.def.size.x * 0.5f);
                }
                if (this.forcedTarget.IsValid && (!this.forcedTarget.HasThing || this.forcedTarget.Thing.Spawned))
                {
                    Vector3 vector;
                    if (this.forcedTarget.HasThing)
                    {
                        vector = this.forcedTarget.Thing.TrueCenter();
                    }
                    else
                    {
                        vector = this.forcedTarget.Cell.ToVector3Shifted();
                    }
                    Vector3 a = this.TrueCenter();
                    vector.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                    a.y = vector.y;
                    GenDraw.DrawLineBetween(a, vector, Building_TurretGun.ForcedTargetLineMat, 0.2f);
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (!selected)
            {
                RecalcStats();
                selected = true;
            }
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            if (!PlayerControlled || (spinalComp != null && AmplifierCount == -1) || mapComp.ShipCombatMaster)
                yield break;

            if (CanSetForcedTarget)
            {
                if (GroundDefenseMode)
                {
                    Command_VerbTarget command_VerbTarget = new Command_VerbTarget
                    {
                        defaultLabel = "CommandSetForceAttackTarget".Translate(),
                        defaultDesc = "CommandSetForceAttackTargetDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack", true),
                        verb = this.AttackVerb,
                        hotKey = KeyBindingDefOf.Misc4,
                        drawRadius = false
                    };
                    yield return command_VerbTarget;
                }
                else
                {
                    Command_VerbTargetShip command_VerbTargetShip = new Command_VerbTargetShip
                    {
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandSetForceAttackTarget"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandSetForceAttackTargetDesc"),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Attack"),
                        verb = AttackVerb,
                        turrets = Find.Selector.SelectedObjects.OfType<Building_ShipTurret>().ToList(),
                        hotKey = KeyBindingDefOf.Misc4,
                        drawRadius = false
                    };
                    yield return command_VerbTargetShip;
                }
            }
            if (shipTarget.IsValid)
            {
                Command_Action command_Action2 = new Command_Action
                {
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandStopForceAttack"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandStopForceAttackDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/Halt"),
                    action = delegate
                    {
                        ResetForcedTarget();
                        SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                    }
                };
                if (!shipTarget.IsValid)
                {
                    command_Action2.Disable(TranslatorFormattedStringExtensions.Translate("CommandStopAttackFailNotForceAttacking"));
                }
                command_Action2.hotKey = KeyBindingDefOf.Misc5;
                yield return command_Action2;
            }
            if (CanToggleHoldFire)
            {
                Command_Toggle command_Toggle = new Command_Toggle
                {
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandHoldFire"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandHoldFireDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/HoldFire"),
                    hotKey = KeyBindingDefOf.Misc6,
                    toggleAction = delegate
                    {
                        holdFire = !holdFire;
                        if (holdFire)
                        {
                            ResetForcedTarget();
                        }
                    },
                    isActive = (() => holdFire)
                };
                yield return command_Toggle;
            }
            if (CanExtractTorpedo)
            {
                Command_Action command_Action = new Command_Action
                {
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandExtractShipTorpedo"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandExtractShipTorpedoDesc"),
                    icon = torpComp.LoadedShells[0].uiIcon,
                    iconAngle = torpComp.LoadedShells[0].uiIconAngle,
                    iconOffset = torpComp.LoadedShells[0].uiIconOffset,
                    iconDrawScale = GenUI.IconDrawScale(torpComp.LoadedShells[0]),
                    action = delegate
                    {
                        ExtractShells();
                    }
                };
                yield return command_Action;
            }
            if (torpComp != null)
            {
                StorageSettings storeSettings = torpComp.GetStoreSettings();
                foreach (Gizmo item in StorageSettingsClipboard.CopyPasteGizmosFor(storeSettings))
                {
                    yield return item;
                }
            }
            if (heatComp.Props.pointDefense)
            {
                Command_Toggle command_Toggle = new Command_Toggle
                {
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandShipPointDefense"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandShipPointDefenseDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/PointDefenseMode"),
                    toggleAction = delegate
                    {
                        PointDefenseMode = !PointDefenseMode;
                        if (PointDefenseMode)
                        {
                            holdFire = false;
                        }
                    },
                    isActive = (() => PointDefenseMode)
                };
                yield return command_Toggle;
            }
            if (heatComp.Props.maxRange > heatComp.Props.optRange)
            {
                Command_Toggle command_Toggle = new Command_Toggle
                {
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandShipOptimalRange"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandShipOptimalRangeDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/OptimalRangeMode"),
                    toggleAction = delegate
                    {
                        useOptimalRange = !useOptimalRange;
                    },
                    isActive = (() => useOptimalRange)
                };
                yield return command_Toggle;
            }
        }

        private void ExtractShells()
        {
            foreach(Thing t in torpComp.RemoveShells())
                GenPlace.TryPlaceThing(t, base.Position, base.Map, ThingPlaceMode.Near);
        }

        public void ResetForcedTarget()
        {
            if (GroundDefenseMode)
                this.forcedTarget = LocalTargetInfo.Invalid;
            else
                shipTarget = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
            if ((mapComp.InCombat || GroundDefenseMode) && burstCooldownTicksLeft <= 0)
            {
                TryStartShootSomething(false);
            }
        }

        private void ResetCurrentTarget()
        {
            currentTargetInt = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
        }

        public void MakeGun()
        {
            gun = ThingMaker.MakeThing(def.building.turretGunDef);
            UpdateGunVerbs();
        }

        private void UpdateGunVerbs()
        {
            List<Verb> allVerbs = gun.TryGetComp<CompEquippable>().AllVerbs;
            for (int i = 0; i < allVerbs.Count; i++)
            {
                Verb verb = allVerbs[i];
                verb.caster = this;
                verb.castCompleteCallback = BurstComplete;
            }
        }

        public void SetTarget(LocalTargetInfo target)
        {
            this.shipTarget = target;
        }
        public bool IncomingPtDefTargetsInRange() //PD targets are in range if they are on target map and in PD range
        {
            if (mapComp.ShipCombatTargetMap.GetComponent<ShipHeatMapComp>().TorpsInRange.Any())
                return true;
            foreach (TravelingTransportPods obj in Find.WorldObjects.TravelingTransportPods)
            {
                float rng = (float)Traverse.Create(obj).Field("traveledPct").GetValue();
                if (obj.destinationTile == this.Map.Parent.Tile && obj.Faction != mapComp.ShipFaction && rng > 0.75)
                {
                    return true;
                }
            }
            return false;
        }
        public void RecalcStats()
        {
            AmplifierCount = -1;
            float ampBoost = 0;
            bool foundNonAmp = false;
            Thing amp=this;
            IntVec3 previousThingPos;
            IntVec3 vec;
            if (this.Rotation.AsByte == 0)
            {
                vec = new IntVec3(0, 0, -1);
            }
            else if (this.Rotation.AsByte == 1)
            {
                vec = new IntVec3(-1, 0, 0);
            }
            else if (this.Rotation.AsByte == 2)
            {
                vec = new IntVec3(0, 0, 1);
            }
            else
            {
                vec = new IntVec3(1, 0, 0);
            }
            previousThingPos = amp.Position + vec;
            do
            {
                previousThingPos += vec;
                amp = previousThingPos.GetFirstThingWithComp<CompSpinalMount>(this.Map);
                CompSpinalMount ampComp = amp.TryGetComp<CompSpinalMount>();
                if (amp == null || amp.Rotation != this.Rotation)
                {
                    AmplifierCount = -1;
                    break;
                }
                //found amp
                if (amp.Position == previousThingPos)
                {
                    AmplifierCount += 1;
                    ampBoost += ampComp.Props.ampAmount;
                    ampComp.SetColor(spinalComp.Props.color);
                }
                //found emitter
                else if (amp.Position == previousThingPos + vec && ampComp.Props.stackEnd)
                {
                    AmplifierCount += 1;
                    foundNonAmp = true;
                }
                //found unaligned
                else
                {
                    AmplifierCount = -1;
                    foundNonAmp = true;
                }
            } while (!foundNonAmp);

            if (ampBoost > 0)
            {
                this.AmplifierDamageBonus = ampBoost;
            }
        }
    }
}