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

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class Building_ShipTurret : Building_Turret
    {
        public static Material ForcedTargetLineMat = MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.Transparent, new Color(1f, 0.5f, 0.5f));
        private const int TryStartShootSomethingIntervalTicks = 10;

        public Thing gun;
        protected TurretTop top;
        protected CompPowerTrader powerComp;
        protected CompInitiatable initiatableComp;
        public CompShipHeatSource HeatSource;

        protected LocalTargetInfo currentTargetInt = LocalTargetInfo.Invalid;
        public int[] SegmentPower = new int[3];
        public int burstCooldownTicksLeft;
        protected int burstWarmupTicksLeft;
        public IntVec3 SynchronizedBurstLocation;
        public int AmplifierCount = -1;
        public float AmplifierDamageBonus = 0;
        bool selected = false;
        private bool holdFire;
        public bool PointDefenseMode;
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
                return gun.TryGetComp<CompChangeableProjectilePlural>()?.Loaded ?? false;
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
        public ShipHeatMapComp mapComp;
        public ShipHeatMapComp MapComp
        {
            get
            {
                if (this.mapComp == null)
                {
                    this.mapComp = this.Map.GetComponent<ShipHeatMapComp>();
                }
                return this.mapComp;
            }
        }
        public CompShipHeatSource heatComp;
        public CompShipHeatSource HeatComp
        {
            get
            {
                if (this.heatComp == null)
                {
                    this.heatComp = this.TryGetComp<CompShipHeatSource>();
                }
                return this.heatComp;
            }
        }
        public CompSpinalMount spinalComp;
        public CompSpinalMount SpinalComp
        {
            get
            {
                if (this.spinalComp == null)
                {
                    this.spinalComp = this.TryGetComp<CompSpinalMount>();
                }
                return this.spinalComp;
            }
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            initiatableComp = GetComp<CompInitiatable>();
            powerComp = GetComp<CompPowerTrader>();
            if (!respawningAfterLoad)
            {
                top.SetRotationFromOrientation();
                burstCooldownTicksLeft = def.building.turretInitialCooldownTime.SecondsToTicks();
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
                    TryStartShootSomething(canBeginBurstImmediately: false);
                }
            }
            if (holdFire)
            {
                Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageTurretWontFireBecauseHoldFire",def.label), this, MessageTypeDefOf.RejectInput, historical: false);
            }
            if (PointDefenseMode)
            {
                Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageTurretWontFireBecausePointDefense",def.label), this, MessageTypeDefOf.RejectInput, historical: false);
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
            if (forcedTarget.ThingDestroyed || !MapComp.InCombat || (SpinalComp != null && AmplifierCount == -1))
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
                if (MapComp.InCombat)
                {
                    //PD mode
                    if ((this.IsHashIntervalTick(10) && burstCooldownTicksLeft <= 0 && IncomingPtDefTargetsInRange()) && (PointDefenseMode || (!PlayerControlled && HeatComp.Props.pointDefense)))
                    {
                        if (Find.TickManager.TicksGame > lastPDTick + 10 && !holdFire)
                            BeginBurst(true);
                    }
                    //check if we are in range
                    else
                    {
                        float range = MapComp.ShipCombatMasterMap.GetComponent<ShipHeatMapComp>().Range;
                        if ((!useOptimalRange && HeatComp.Props.maxRange > range) || (useOptimalRange && HeatComp.Props.optRange > range))
                        {
                            //cant fire spinals opposite of heading
                            if (SpinalComp != null)
                            {
                                if ((this.Rotation == new Rot4(MapComp.EngineRot) && MapComp.Heading == -1) || (this.Rotation == new Rot4(MapComp.EngineRot + 2) && MapComp.Heading == 1))
                                {
                                    if (PlayerControlled)
                                        return;
                                    else
                                    {
                                        MapComp.Heading *= -1;
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
                                TryStartShootSomething(canBeginBurstImmediately: true);
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

        protected void TryStartShootSomething(bool canBeginBurstImmediately)
        {
            if (SpinalComp != null)
                RecalcStats();
            if (!base.Spawned || (holdFire && CanToggleHoldFire) || !AttackVerb.Available() || PointDefenseMode || !MapComp.InCombat || (SpinalComp != null && AmplifierCount == -1))
            {
                ResetCurrentTarget();
                return;
            }
            if (!this.PlayerControlled && MapComp.ShipCombatMaster)
            {
                if (SpinalComp == null || SpinalComp.Props.destroysHull || MapComp.ShipCombatOriginMap.mapPawns.FreeColonistsAndPrisoners.Count==0)
                    shipTarget = MapComp.ShipCombatOriginMap.listerThings.AllThings.RandomElement();
                else //Target pawn with the Psychic Flayer
                    shipTarget = MapComp.ShipCombatOriginMap.mapPawns.FreeColonistsAndPrisoners.RandomElement();
            }
            bool isValid = currentTargetInt.IsValid;
            if (shipTarget.IsValid)
            {
                //fire same as engine direction or opposite if retreating
                byte rotA = MapComp.EngineRot;
                int rotB = MapComp.Heading;
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
            if (!isValid && currentTargetInt.IsValid)
            {
                SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(base.Position, base.Map));
            }
            if (currentTargetInt.IsValid)
            {
                if (def.building.turretBurstWarmupTime > 0f)
                {
                    burstWarmupTicksLeft = def.building.turretBurstWarmupTime.SecondsToTicks();
                }
                else if (canBeginBurstImmediately)
                {
                    BeginBurst();
                }
                else
                {
                    burstWarmupTicksLeft = 1;
                }
            }
            else
            {
                ResetCurrentTarget();
            }
        }

        protected LocalTargetInfo TryFindNewTarget()
        {
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
            //PD mode
            if (isPtDefBurst || shipTarget == null)
                this.shipTarget = LocalTargetInfo.Invalid;
            //sync
            ((Verb_LaunchProjectileShip)AttackVerb).shipTarget = shipTarget;
            if (this.AttackVerb.verbProps.burstShotCount > 0 && MapComp.ShipCombatTargetMap != null)
                SynchronizedBurstLocation = MapComp.FindClosestEdgeCell(MapComp.ShipCombatTargetMap, shipTarget.Cell);
            else
                SynchronizedBurstLocation = IntVec3.Invalid;
            //check if we have power to fire
            if (this.TryGetComp<CompPower>() != null && HeatComp != null && this.TryGetComp<CompPower>().PowerNet.CurrentStoredEnergy() < HeatComp.Props.energyToFire * (1 + (AmplifierDamageBonus)))
            {
                //Messages.Message(TranslatorFormattedStringExtensions.Translate("CannotFireDueToPower",this.Label), this, MessageTypeDefOf.CautionInput);
                //this.shipTarget = LocalTargetInfo.Invalid;
                return;
            }
            //spinal weapons fire straight
            if (SpinalComp != null)
            {
                if (this.Rotation.AsByte == 0)
                    currentTargetInt = new LocalTargetInfo(new IntVec3(this.Position.x, 0, this.Map.Size.z-1));
                else if (this.Rotation.AsByte == 1)
                    currentTargetInt = new LocalTargetInfo(new IntVec3(this.Map.Size.x-1, 0, this.Position.z));
                else if (this.Rotation.AsByte == 2)
                    currentTargetInt = new LocalTargetInfo(new IntVec3(this.Position.x, 0, 1));
                else
                    currentTargetInt = new LocalTargetInfo(new IntVec3(1, 0, this.Position.z));
            }
            //if we do not have enough heatcap, vent heat to room/fail to fire in vacuum
            if (HeatComp != null && HeatComp.AvailableCapacityInNetwork() < HeatComp.Props.heatPerPulse*(1+(AmplifierDamageBonus)))
            {
                if (ShipInteriorMod2.RoomIsVacuum(this.GetRoom()))
                {
                    if (!PointDefenseMode && PlayerControlled)
                        Messages.Message(TranslatorFormattedStringExtensions.Translate("CannotFireDueToHeat",this.Label), this, MessageTypeDefOf.CautionInput);
                    this.shipTarget = LocalTargetInfo.Invalid;
                    return;
                }
                GenTemperature.PushHeat(this, HeatComp.Props.heatPerPulse* ShipInteriorMod2.HeatPushMult * (1 + (AmplifierDamageBonus)));
            }
            else
                HeatComp.AddHeatToNetwork(HeatComp.Props.heatPerPulse * (1 + (AmplifierDamageBonus)));
            //ammo
            if (this.TryGetComp<CompRefuelable>()!=null)
            {
                if (this.TryGetComp<CompRefuelable>().Fuel <= 0)
                {
                    if (!PointDefenseMode && PlayerControlled)
                        Messages.Message(TranslatorFormattedStringExtensions.Translate("CannotFireDueToAmmo", this.Label), this, MessageTypeDefOf.CautionInput);
                    this.shipTarget = LocalTargetInfo.Invalid;
                    return;
                }
                this.TryGetComp<CompRefuelable>().ConsumeFuel(1);
            }
            //draw the same percentage from each cap: needed*current/currenttotal
            foreach (CompPowerBattery bat in this.TryGetComp<CompPower>().PowerNet.batteryComps)
            {
                bat.DrawPower(Mathf.Min(HeatComp.Props.energyToFire * (1 + AmplifierDamageBonus) * bat.StoredEnergy / this.TryGetComp<CompPower>().PowerNet.CurrentStoredEnergy(), bat.StoredEnergy));
            }

            if (HeatComp.Props.singleFireSound != null)
            {
                HeatComp.Props.singleFireSound.PlayOneShot(this);
            }
            if (PointDefenseMode)
                lastPDTick = Find.TickManager.TicksGame;
            AttackVerb.TryStartCastOn(currentTargetInt);
            OnAttackedTarget(currentTargetInt);
            burstCooldownTicksLeft = BurstCooldownTime().SecondsToTicks(); //Seems to prevent the "turbo railgun" bug. Don't ask me why.
            if (SpinalComp != null && SpinalComp.Props.destroysHull)
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
            if (AttackVerb.verbProps.minRange > 0f)
            {
                stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("MinimumRange") + ": " + AttackVerb.verbProps.minRange.ToString("F0"));
            }
            else if (base.Spawned && burstCooldownTicksLeft > 0 && BurstCooldownTime() > 5f)
            {
                stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("CanFireIn") + ": " + burstCooldownTicksLeft.ToStringSecondsFromTicks());
            }
			if (SpinalComp != null)
            {
                if (AmplifierCount != -1)
                    stringBuilder.AppendLine("ShipAmplifierCount".Translate(AmplifierCount));
                else
                    stringBuilder.AppendLine("ShipSpinalCapNotFound".Translate());
            }
            CompChangeableProjectilePlural compChangeableProjectile = gun.TryGetComp<CompChangeableProjectilePlural>();
            if (compChangeableProjectile != null)
            {
                if (compChangeableProjectile.Loaded)
                {
                    string torps = "";
                    foreach (ThingDef t in compChangeableProjectile.LoadedShells)
                    {
                        if (torps.Length > 0)
                            torps += ", ";
                        torps += t.label;
                    }
                    stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipTorpedoLoaded",torps));
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
            /*float range = AttackVerb.verbProps.range;
            if (range < 90f)
            {
                GenDraw.DrawRadiusRing(base.Position, range);
            }
            float num = AttackVerb.verbProps.EffectiveMinRange(allowAdjacentShot: true);
            if (num < 90f && num > 0.1f)
            {
                GenDraw.DrawRadiusRing(base.Position, num);
            }
            if (burstWarmupTicksLeft > 0)
            {
                int degreesWide = (int)((float)burstWarmupTicksLeft * 0.5f);
                GenDraw.DrawAimPie(this, CurrentTarget, degreesWide, (float)def.size.x * 0.5f);
            }
            if (forcedTarget.IsValid && (!forcedTarget.HasThing || forcedTarget.Thing.Spawned))
            {
                Vector3 b = (!forcedTarget.HasThing) ? forcedTarget.Cell.ToVector3Shifted() : forcedTarget.Thing.TrueCenter();
                Vector3 a = this.TrueCenter();
                b.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                a.y = b.y;
                GenDraw.DrawLineBetween(a, b, ForcedTargetLineMat);
            }*/
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
            if (!PlayerControlled || (SpinalComp != null && AmplifierCount == -1) || MapComp.ShipCombatMaster)
                yield break;
            if (CanSetForcedTarget)
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
                CompChangeableProjectilePlural compChangeableProjectile = gun.TryGetComp<CompChangeableProjectilePlural>();
                Command_Action command_Action = new Command_Action
                {
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandExtractShipTorpedo"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandExtractShipTorpedoDesc"),
                    icon = compChangeableProjectile.LoadedShells[0].uiIcon,
                    iconAngle = compChangeableProjectile.LoadedShells[0].uiIconAngle,
                    iconOffset = compChangeableProjectile.LoadedShells[0].uiIconOffset,
                    iconDrawScale = GenUI.IconDrawScale(compChangeableProjectile.LoadedShells[0]),
                    action = delegate
                    {
                        ExtractShells();
                    }
                };
                yield return command_Action;
            }
            CompChangeableProjectilePlural compChangeableProjectile2 = gun.TryGetComp<CompChangeableProjectilePlural>();
            if (compChangeableProjectile2 != null)
            {
                StorageSettings storeSettings = compChangeableProjectile2.GetStoreSettings();
                foreach (Gizmo item in StorageSettingsClipboard.CopyPasteGizmosFor(storeSettings))
                {
                    yield return item;
                }
            }
            if (HeatComp.Props.pointDefense)
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
            else
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
            foreach(Thing t in gun.TryGetComp<CompChangeableProjectilePlural>().RemoveShells())
                GenPlace.TryPlaceThing(t, base.Position, base.Map, ThingPlaceMode.Near);
        }

        public void ResetForcedTarget()
        {
            shipTarget = LocalTargetInfo.Invalid;
            burstWarmupTicksLeft = 0;
            if (MapComp.InCombat && burstCooldownTicksLeft <= 0)
            {
                TryStartShootSomething(canBeginBurstImmediately: false);
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
            if (MapComp.ShipCombatTargetMap.GetComponent<ShipHeatMapComp>().TorpsInRange.Any())
                return true;
            foreach (TravelingTransportPods obj in Find.WorldObjects.TravelingTransportPods)
            {
                float rng = (float)Traverse.Create(obj).Field("traveledPct").GetValue();
                if (obj.destinationTile == this.Map.Parent.Tile && obj.Faction != MapComp.ShipFaction && rng > 0.75)
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
                if (amp == null || amp.Rotation != this.Rotation)
                {
                    AmplifierCount = -1;
                    break;
                }
                //found amp
                if (amp.Position == previousThingPos)
                {
                    AmplifierCount += 1;
                    ampBoost += amp.TryGetComp<CompSpinalMount>().Props.ampAmount;
                    amp.TryGetComp<CompSpinalMount>().SetColor(SpinalComp.Props.color);
                }
                //found emitter
                else if (amp.Position == previousThingPos + vec && amp.TryGetComp<CompSpinalMount>().Props.stackEnd)
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