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
	public class Building_ShipTurret : Building_Turret
	{
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
		LocalTargetInfo shipTarget = LocalTargetInfo.Invalid;
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
		public CompEquippable GunCompEq => gun.TryGetComp<CompEquippable>();
		public override LocalTargetInfo CurrentTarget => currentTargetInt;
		public override Verb AttackVerb => GunCompEq.PrimaryVerb;
		public override bool IsEverThreat => Faction == Faction.OfPlayer && !Map.IsSpace(); //prevent player pawns auto attacking
		public bool Active
		{
			get
			{
				//if (SpinalHasNoAmps) //td req recheck system when parts placed by player, AI skip
				//	return false;
				//needs power, heat and bridge on net
				if (Spawned && heatComp.myNet != null && !heatComp.myNet.venting && (powerComp == null || powerComp.PowerOn) && (heatComp.myNet.PilCons.Any() || heatComp.myNet.AICores.Any() || heatComp.myNet.TacCons.Any()))
				{
					return true;
				}
				return false;
			}
		}
		public bool PlayerControlled => Faction == Faction.OfPlayer;
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
		List<Thing> amps = new List<Thing>();
		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			base.SpawnSetup(map, respawningAfterLoad);
			mapComp = Map.GetComponent<ShipHeatMapComp>();
			initiatableComp = GetComp<CompInitiatable>();
			powerComp = this.TryGetComp<CompPowerTrader>();
			heatComp = this.TryGetComp<CompShipHeat>();
			fuelComp = this.TryGetComp<CompRefuelable>();
			spinalComp = this.TryGetComp<CompSpinalMount>();
			torpComp = gun.TryGetComp<CompChangeableProjectilePlural>();
			if (!Map.IsSpace() && heatComp.Props.groundDefense)
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
		public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			Map map = Map;
			base.Destroy(mode);
			if (torpComp != null && !ShipInteriorMod2.AirlockBugFlag)
			{
				foreach (ThingDef def in torpComp.LoadedShells)
				{
					Thing thing = ThingMaker.MakeThing(def);
					GenPlace.TryPlaceThing(thing, Position, map, ThingPlaceMode.Near, null, null, default);
				}
			}
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
			if (holdFire)
			{
				Messages.Message(TranslatorFormattedStringExtensions.Translate("MessageTurretWontFireBecauseHoldFire", def.label), this, MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			if (PointDefenseMode)
			{
				Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.TurretInPointDefense", def.label), this, MessageTypeDefOf.RejectInput, historical: false);
				return;
			}
			if (GroundDefenseMode)
			{
				if (!targ.IsValid)
				{
					if (forcedTarget.IsValid)
					{
						ResetForcedTarget();
					}
					return;
				}
				if ((targ.Cell - base.Position).LengthHorizontal < AttackVerb.verbProps.EffectiveMinRange(targ, this))
				{
					Messages.Message("MessageTargetBelowMinimumRange".Translate(), this, MessageTypeDefOf.RejectInput, false);
					return;
				}
				if ((targ.Cell - base.Position).LengthHorizontal > AttackVerb.verbProps.range)
				{
					Messages.Message("MessageTargetBeyondMaximumRange".Translate(), this, MessageTypeDefOf.RejectInput, false);
					return;
				}
			}
			if (forcedTarget != targ)
			{
				forcedTarget = targ;
				if (burstCooldownTicksLeft <= 0)
				{
					TryStartShootSomething(false);
				}
			}
		}
		public bool InRange(LocalTargetInfo target)
		{
			float range = Position.DistanceTo(target.Cell);
			if (range > AttackVerb.verbProps.minRange && range < AttackVerb.verbProps.range)
			{
				return true;
			}
			return false;
		}
		public bool InRangeSC(float range)
		{
			if ((!useOptimalRange && heatComp.Props.maxRange > range) || (useOptimalRange && heatComp.Props.optRange > range))
			{
				return true;
			}
			return false;
		}
		private bool WarmingUp
		{
			get
			{
				return burstWarmupTicksLeft > 0;
			}
		}
		public bool SpinalHasNoAmps
		{
			get
			{
				return spinalComp != null && AmplifierCount == -1;
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
			if (forcedTarget.ThingDestroyed)
			{
				ResetForcedTarget();
			}
			if (GroundDefenseMode)
			{
				if (forcedTarget.IsValid && !CanSetForcedTarget && !InRange(forcedTarget))
				{
					ResetForcedTarget();
				}
				if (Active && !IsStunned)
				{
					GunCompEq.verbTracker.VerbsTick();
					if (AttackVerb.state != VerbState.Bursting)
					{
						if (WarmingUp)
						{
							burstWarmupTicksLeft--;
							if (burstWarmupTicksLeft == 0)
							{
								BeginBurst();
							}
						}
						else
						{
							if (burstCooldownTicksLeft > 0)
							{
								burstCooldownTicksLeft--;
							}
							if (burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
							{
								TryStartShootSomething(true);
							}
						}
						top.TurretTopTick();
						return;
					}
				}
				else
				{
					ResetCurrentTarget();
				}
			}
			else
			{
				if (mapComp.ShipMapState != ShipMapState.inCombat || SpinalHasNoAmps)
				{
					ResetForcedTarget();
				}
				if (Active && !IsStunned)
				{
					GunCompEq.verbTracker.VerbsTick();
					if (AttackVerb.state != VerbState.Bursting)
					{
						if (burstCooldownTicksLeft > 0)
						{
							burstCooldownTicksLeft--;
						}
						if (mapComp.ShipMapState == ShipMapState.inCombat && !heatComp.Venting)
						{
							if (heatComp.Props.pointDefense) //PD mode
							{
								bool pdActive = false;
								if (burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
								{
									pdActive = IncomingPtDefTargetsInRange();
									if (!PlayerControlled)
									{
										if (pdActive)
											PointDefenseMode = true;
										else
											PointDefenseMode = false;
									}
								}
								if (pdActive && PointDefenseMode)
								{
									if (Find.TickManager.TicksGame > mapComp.lastPDTick + 10 && !holdFire)
										BeginBurst();
								}
							}
							if (InRangeSC(mapComp.OriginMapComp.Range))
							{
								if (WarmingUp)
								{
									burstWarmupTicksLeft--;
									if (burstWarmupTicksLeft == 0)
									{
										BeginBurst();
									}
								}
								else if (burstCooldownTicksLeft <= 0 && this.IsHashIntervalTick(10))
								{
									TryStartShootSomething(true);
								}
							}
						}
					}
					top.TurretTopTick();
					return;
				}
				else
				{
					ResetCurrentTarget();
				}
			}
		}
		protected void TryStartShootSomething(bool canBeginBurstImmediately) //find new target and shoot at it if ready
		{
			bool isValid = currentTargetInt.IsValid;
			if (GroundDefenseMode)
			{
				if (progressBarEffecter != null)
				{
					progressBarEffecter.Cleanup();
					progressBarEffecter = null;
				}
				if (!base.Spawned || (holdFire && CanToggleHoldFire) || !AttackVerb.Available())
				{
					ResetCurrentTarget();
					return;
				}
				if (forcedTarget.IsValid && InRange(forcedTarget))
				{
					currentTargetInt = forcedTarget;
				}
				else
				{
					currentTargetInt = TryFindNewTarget();
				}
			}
			else
			{
				if (!base.Spawned || (holdFire && CanToggleHoldFire) || !AttackVerb.Available() || PointDefenseMode || mapComp.ShipMapState != ShipMapState.inCombat || SpinalHasNoAmps)
				{
					ResetCurrentTarget();
					return;
				}
				if (!PlayerControlled && mapComp.HasShipMapAI) //AI targeting
				{
					//Target pawns with the Psychic Flayer
					if (spinalComp != null && !spinalComp.Props.destroysHull && mapComp.ShipCombatTargetMap.mapPawns.FreeColonistsAndPrisoners.Any())
					{
						shipTarget = mapComp.ShipCombatTargetMap.mapPawns.FreeColonistsAndPrisoners.RandomElement();
					}
					else //try bridges, else random
					{
						if (mapComp.OriginMapComp.MapRootListAll.Any(b => !b.Destroyed))
							shipTarget = mapComp.OriginMapComp.MapRootListAll.RandomElement();
						else
							shipTarget = mapComp.ShipCombatTargetMap.listerBuildings.allBuildingsColonist.RandomElement();
					}
				}
				if (shipTarget.IsValid)
				{
					currentTargetInt = MapEdgeCell(5);
				}
				else
				{
					currentTargetInt = TryFindNewTarget();
				}
			}
			if (!isValid && currentTargetInt.IsValid)
			{
				SoundDefOf.TurretAcquireTarget.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
			}
			if (!currentTargetInt.IsValid)
			{
				ResetCurrentTarget();
				return;
			}
			float randomInRange = def.building.turretBurstWarmupTime.RandomInRange;
			if (randomInRange > 0f)
			{
				burstWarmupTicksLeft = randomInRange.SecondsToTicks();
				return;
			}
			if (canBeginBurstImmediately)
			{
				BeginBurst();
				return;
			}
			burstWarmupTicksLeft = 1;
		}
		private LocalTargetInfo MapEdgeCell (int miss)
		{
			if (miss > 0)
				miss = Rand.RangeInclusive(-miss, miss);
			//fire same as engine direction or opposite if retreating
			IntVec3 v;
			if ((mapComp.EngineRot == 0 && mapComp.Heading != -1) || (mapComp.EngineRot == 2 && mapComp.Heading == -1)) //north
				v = new IntVec3(Position.x + miss, 0, Map.Size.z - 1);
			else if ((mapComp.EngineRot == 1 && mapComp.Heading != -1) || (mapComp.EngineRot == 3 && mapComp.Heading == -1)) //east
				v = new IntVec3(Map.Size.x - 1, 0, Position.z + miss);
			else if ((mapComp.EngineRot == 2 && mapComp.Heading != -1) || (mapComp.EngineRot == 0 && mapComp.Heading == -1)) //south
				v = new IntVec3(Position.x + miss, 0, 0);
			else //west
				v = new IntVec3(0, 0, Position.z + miss);
			if (v.x < 0)
				v.x = 0; 
			else if (v.x >= Map.Size.x)
				v.x = Map.Size.x -1;
			if (v.z < 0)
				v.z = 0;
			else if (v.z >= Map.Size.z)
				v.z = Map.Size.z;
			return new LocalTargetInfo(v);
		}

		protected LocalTargetInfo TryFindNewTarget()
		{
			if (GroundDefenseMode)
			{
				IAttackTargetSearcher attackTargetSearcher = TargSearcher();
				TargetScanFlags targetScanFlags = TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable |  TargetScanFlags.NeedNotUnderThickRoof;
				return (Thing)AttackTargetFinder.BestShootTargetFromCurrentPosition(attackTargetSearcher, targetScanFlags, new Predicate<Thing>(IsValidTarget), AttackVerb.verbProps.minRange, AttackVerb.verbProps.range);
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
			if (t is Pawn p)
			{
				if (p.Faction == Faction.OfPlayer)
				{
					return false;
				}
				foreach (Thing thing in t.Position.GetThingList(Map))
				{
					if (thing is Building b && b.def.building.shipPart)
						return false;
				}
			}
			return true;
		}

		protected void BeginBurst()
		{
			//cant fire spinals opposite of heading
			if (spinalComp != null)
			{
				SpinalRecalc();
				if (AmplifierCount == -1)
				{
					return;
				}

				if ((Rotation == new Rot4(mapComp.EngineRot) && mapComp.Heading == -1) || (Rotation == new Rot4(mapComp.EngineRot + 2) && mapComp.Heading == 1))
				{
					if (mapComp.HasShipMapAI)
					{
						if (mapComp.Retreating)
							return;
						else
							mapComp.Heading *= -1;
					}
					else
					{
						return;
					}
				}
			}
			//check if we have power to fire
			if (powerComp != null && heatComp != null && powerComp.PowerNet.CurrentStoredEnergy() < heatComp.Props.energyToFire * (1 + AmplifierDamageBonus))
			{
				if (!PointDefenseMode && PlayerControlled)
					Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CannotFireDueToPower", Label), this, MessageTypeDefOf.CautionInput);
				shipTarget = LocalTargetInfo.Invalid;
				ResetCurrentTarget();
				return;
			}
			//if we do not have enough heatcap, vent heat to room/fail to fire in vacuum
			if (heatComp.Props.heatPerPulse > 0 && !heatComp.AddHeatToNetwork(heatComp.Props.heatPerPulse * (1 + AmplifierDamageBonus) * 3))
			{
				if (!PointDefenseMode && PlayerControlled)
					Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CannotFireDueToHeat", Label), this, MessageTypeDefOf.CautionInput);
				shipTarget = LocalTargetInfo.Invalid;
				ResetCurrentTarget();
				return;
			}
			//ammo
			if (fuelComp != null)
			{
				if (fuelComp.Fuel <= 0)
				{
					if (!PointDefenseMode && PlayerControlled)
						Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CannotFireDueToAmmo", Label), this, MessageTypeDefOf.CautionInput);
					shipTarget = LocalTargetInfo.Invalid;
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
			//sfx
			if (heatComp.Props.singleFireSound != null)
			{
				heatComp.Props.singleFireSound.PlayOneShot(this);
			}
			//cast
			if (GroundDefenseMode)
			{
				AttackVerb.TryStartCastOn(CurrentTarget, false, true, false);
				base.OnAttackedTarget(CurrentTarget);
			}
			else
			{
				if (shipTarget == null)
					shipTarget = LocalTargetInfo.Invalid;
				if (PointDefenseMode)
				{
					currentTargetInt = MapEdgeCell(20);
					mapComp.lastPDTick = Find.TickManager.TicksGame;
				}
				//sync
				((Verb_LaunchProjectileShip)AttackVerb).shipTarget = shipTarget;
				if (AttackVerb.verbProps.burstShotCount > 0 && mapComp.ShipCombatTargetMap != null)
					SynchronizedBurstLocation = mapComp.FindClosestEdgeCell(mapComp.ShipCombatTargetMap, shipTarget.Cell);
				else
					SynchronizedBurstLocation = IntVec3.Invalid;
				//spinal weapons fire straight and destroy things in the way
				if (spinalComp != null)
				{
					if (Rotation.AsByte == 0)
						currentTargetInt = new LocalTargetInfo(new IntVec3(Position.x, 0, Map.Size.z - 1));
					else if (Rotation.AsByte == 1)
						currentTargetInt = new LocalTargetInfo(new IntVec3(Map.Size.x - 1, 0, Position.z));
					else if (Rotation.AsByte == 2)
						currentTargetInt = new LocalTargetInfo(new IntVec3(Position.x, 0, 1));
					else
						currentTargetInt = new LocalTargetInfo(new IntVec3(1, 0, Position.z));
					if (spinalComp.Props.destroysHull)
					{
						List<Thing> thingsToDestroy = new List<Thing>();

						if (Rotation.AsByte == 0)
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
						else if (Rotation.AsByte == 1)
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
						else if (Rotation.AsByte == 2)
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
				AttackVerb.TryStartCastOn(currentTargetInt);
				OnAttackedTarget(currentTargetInt);
				BurstComplete(); //Seems to prevent the "turbo railgun" bug. Don't ask me why.
			}
		}

		protected void BurstComplete()
		{
			burstCooldownTicksLeft = BurstCooldownTime().SecondsToTicks();
			if (GroundDefenseMode)
				burstCooldownTicksLeft = (int)(burstCooldownTicksLeft * 1.5f);
		}

		protected float BurstCooldownTime()
		{
			if (def.building.turretBurstCooldownTime >= 0f)
			{
				if (GroundDefenseMode)
					return def.building.turretBurstCooldownTime * 2; //double CD on ground
				else
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
					stringBuilder.AppendLine("SoS.AmplifierCount".Translate(AmplifierCount));
				else
					stringBuilder.AppendLine("SoS.SpinalCapNotFound".Translate());
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
						if (t == ResourceBank.ThingDefOf.ShipTorpedo_EMP)
							torpEMP++;
						else if (t == ResourceBank.ThingDefOf.ShipTorpedo_Antimatter)
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
					stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("SoS.TorpedoNotLoaded"));
				}
			}
			return stringBuilder.ToString().TrimEndNewlines();
		}
		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			top.DrawTurret(DrawPos, Vector3.zero, 0);
			base.DrawAt(drawLoc, flip);
		}
		public override void DrawExtraSelectionOverlays()
		{
			if (GroundDefenseMode)
			{
				base.DrawExtraSelectionOverlays();
				float range = AttackVerb.verbProps.range;
				if (range < 90f)
				{
					GenDraw.DrawRadiusRing(base.Position, range);
				}
				float num = AttackVerb.verbProps.EffectiveMinRange(true);
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
					Vector3 vector;
					if (forcedTarget.HasThing)
					{
						vector = forcedTarget.Thing.TrueCenter();
					}
					else
					{
						vector = forcedTarget.Cell.ToVector3Shifted();
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
				SpinalRecalc();
				selected = true;
			}
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				yield return gizmo;
			}
			if (!PlayerControlled || SpinalHasNoAmps)
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
						verb = AttackVerb,
						hotKey = KeyBindingDefOf.Misc4,
						drawRadius = false
					};
					yield return command_VerbTarget;
				}
				else
				{
					Command_TargetShipCombat command_VerbTargetShip = new Command_TargetShipCombat
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
			if (torpComp != null)
			{
				if (CanExtractTorpedo)
				{
					Command_Action command_Action = new Command_Action
					{
						defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.TurretExtractTorpedo"),
						defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.TurretExtractTorpedoDesc"),
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
				List<ThingDef> torpTypes = new List<ThingDef>();
				foreach (ThingDef torp in torpComp.LoadedShells)
				{
					if (!torpTypes.Contains(torp))
						torpTypes.Add(torp);
				}
				foreach (ThingDef torp in torpTypes)
				{
					Command_Toggle command_Toggle = new Command_Toggle
					{
						defaultLabel = torp.label,
						defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.TorpedoAllowedDesc"),
						icon = torp.uiIcon,
						iconAngle = torp.uiIconAngle,
						iconOffset = torp.uiIconOffset,
						iconDrawScale = GenUI.IconDrawScale(torp),
						toggleAction = delegate
						{
							if (torpComp.PreventShells.Contains(torp))
								torpComp.PreventShells.Remove(torp);
							else
								torpComp.PreventShells.Add(torp);
						},
						isActive = () => !torpComp.PreventShells.Contains(torp)
					};
					yield return command_Toggle;
				}
				StorageSettings storeSettings = torpComp.GetStoreSettings();
				foreach (Gizmo item in StorageSettingsClipboard.CopyPasteGizmosFor(storeSettings))
				{
					yield return item;
				}
			}
			else if (heatComp.Props.pointDefense)
			{
				Command_Toggle command_Toggle = new Command_Toggle
				{
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.TurretPointDefense"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.TurretPointDefenseDesc"),
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
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.TurretOptimalRange"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.TurretOptimalRangeDesc"),
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
				forcedTarget = LocalTargetInfo.Invalid;
			else
				shipTarget = LocalTargetInfo.Invalid;
			burstWarmupTicksLeft = 0;
			if ((mapComp.ShipMapState == ShipMapState.inCombat || GroundDefenseMode) && burstCooldownTicksLeft <= 0)
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
			shipTarget = target;
		}
		public bool IncomingPtDefTargetsInRange() //PD targets are in range if they are on target map and in PD range
		{
			if (mapComp.TargetMapComp.TorpsInRange.Any())
				return true;
			foreach (TravelingTransportPods obj in Find.WorldObjects.TravelingTransportPods)
			{
				float rng = (float)Traverse.Create(obj).Field("traveledPct").GetValue();
				if (obj.destinationTile == Map.Parent.Tile && obj.Faction != mapComp.ShipFaction && rng > 0.75)
				{
					return true;
				}
			}
			return false;
		}
		public void SpinalRecalc()
		{
			if (spinalComp == null)
				return;
			if (AmplifierCount != -1 && amps.All(a => a != null && !a.Destroyed)) //no changes if all amps intact
				return;
			amps.Clear();
			AmplifierCount = -1;
			float ampBoost = 0;
			bool foundNonAmp = false;
			Thing amp = this;
			IntVec3 previousThingPos;
			IntVec3 vec;
			if (Rotation.AsByte == 0)
			{
				vec = new IntVec3(0, 0, -1);
			}
			else if (Rotation.AsByte == 1)
			{
				vec = new IntVec3(-1, 0, 0);
			}
			else if (Rotation.AsByte == 2)
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
				amp = previousThingPos.GetFirstThingWithComp<CompSpinalMount>(Map);
				CompSpinalMount ampComp = amp.TryGetComp<CompSpinalMount>();
				if (amp == null || amp.Rotation != Rotation)
				{
					AmplifierCount = -1;
					break;
				}
				//found amp
				if (amp.Position == previousThingPos)
				{
					amps.Add(amp);
					AmplifierCount += 1;
					ampBoost += ampComp.Props.ampAmount;
					ampComp.SetColor(spinalComp.Props.color);
				}
				//found emitter
				else if (amp.Position == previousThingPos + vec && ampComp.Props.stackEnd)
				{
					amps.Add(amp);
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
				AmplifierDamageBonus = ampBoost;
			}
		}
	}
}