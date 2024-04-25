using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld.Planet;
using HarmonyLib;
using RimWorld;
using Vehicles;
using SaveOurShip2.Vehicles;

namespace SaveOurShip2
{
	public class Verb_LaunchProjectileShip : Verb_Shoot
	{
		public LocalTargetInfo shipTarget;

		public override ThingDef Projectile
		{
			get
			{
				if (base.EquipmentSource != null)
				{
					CompChangeableProjectile comp = base.EquipmentSource.GetComp<CompChangeableProjectile>();
					if (comp != null && comp.LoadedNotPrevent)
					{
						return comp.Projectile;
					}
				}
				return this.verbProps.spawnDef;
			}
		}

		protected override bool TryCastShot()
		{
			ThingDef projectile = Projectile;
			if (projectile == null)
			{
				return true;
			}
			Building_ShipTurret turret = this.caster as Building_ShipTurret;
			if (turret != null)
			{
				if (turret.GroundDefenseMode) //swap projectile for ground
				{
					if (turret.heatComp.Props.groundProjectile != null)
						projectile = turret.heatComp.Props.groundProjectile;
				}
				else if (turret.PointDefenseMode) //remove registered torps/pods in range
				{
					PointDefense(turret);
				}
				else //register projectile on mapComp
				{
					if (turret.torpComp == null)
						RegisterProjectile(turret, this.shipTarget, verbProps.defaultProjectile, turret.SynchronizedBurstLocation);
					else
						RegisterProjectile(turret, this.shipTarget, turret.torpComp.Projectile.interactionCellIcon, turret.SynchronizedBurstLocation); //This is a horrible kludge, but it's a way to attach one projectile's ThingDef to another projectile
				}
			}
			ShootLine resultingLine = new ShootLine(caster.Position, currentTarget.Cell);
			Thing launcher = caster;
			Thing equipment = base.EquipmentSource;
			Vector3 drawPos = caster.DrawPos;
			if (equipment != null)
			{
				base.EquipmentSource.GetComp<CompChangeableProjectile>()?.Notify_ProjectileLaunched();
			}
			Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, resultingLine.Source, caster.Map);

			if (turret.GroundDefenseMode && turret.heatComp.Props.groundMissRadius > 0.5f)
			{
				float num = turret.heatComp.Props.groundMissRadius;
				float num2 = VerbUtility.CalculateAdjustedForcedMiss(num, this.currentTarget.Cell - this.caster.Position);
				if (num2 > 0.5f)
				{
					int max = GenRadial.NumCellsInRadius(num2);
					int num3 = Rand.RangeInclusive(0, max);
					if (num3 > 0)
					{
						IntVec3 c = this.currentTarget.Cell + GenRadial.RadialPattern[num3];
						ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
						if (Rand.Chance(0.5f))
						{
							projectileHitFlags = ProjectileHitFlags.All;
						}
						if (!this.canHitNonTargetPawnsNow)
						{
							projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
						}
						projectile2.Launch(launcher, drawPos, c, this.currentTarget, projectileHitFlags, this.preventFriendlyFire, equipment, null);
						return true;
					}
				}
			}

			if (launcher is Building_ShipTurretTorpedo l)
			{
				projectile2.Launch(launcher, drawPos + l.TorpedoTubePos(), currentTarget.Cell, currentTarget.Cell, ProjectileHitFlags.None, false, equipment);
			}
			else
				projectile2.Launch(launcher, currentTarget.Cell, currentTarget.Cell, ProjectileHitFlags.None, false, equipment);

			if (projectile == ResourceBank.ThingDefOf.Bullet_Fake_Laser || projectile == ResourceBank.ThingDefOf.Bullet_Ground_Laser || projectile == ResourceBank.ThingDefOf.Bullet_Fake_Psychic)
			{
				ShipCombatLaserMote obj = (ShipCombatLaserMote)(object)ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipCombatLaserMote);
				obj.origin = drawPos;
				obj.destination = currentTarget.Cell.ToVector3Shifted();
				obj.large = this.caster.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier) > 1.0f;
				obj.color = turret.heatComp.Props.laserColor;
				obj.Attach(launcher);
				GenSpawn.Spawn(obj, launcher.Position, launcher.Map, 0);
			}
			projectile2.HitFlags = ProjectileHitFlags.None;
			return true;
		}
		public void PointDefense(Building_ShipTurret turret) // PD removes from target map
		{
			var mapComp = turret.Map.GetComponent<ShipMapComp>();
			if (mapComp.ShipCombatTargetMap != null)
			{
				//pods
				/*List<TravelingTransportPods> podsinrange = new List<TravelingTransportPods>();
				foreach (TravelingTransportPods obj in Find.WorldObjects.TravelingTransportPods)
				{
					float rng = (float)Traverse.Create(obj).Field("traveledPct").GetValue();
					if (obj.destinationTile == turret.Map.Parent.Tile && obj.Faction != mapComp.ShipFaction && rng > 0.75)
					{
						podsinrange.Add(obj);
					}
				}*/
				if (mapComp.TargetMapComp.TorpsInRange.Any() && Rand.Chance(0.1f))
				{
					ShipCombatProjectile projtr = mapComp.TargetMapComp.TorpsInRange.RandomElement();
					mapComp.TargetMapComp.Projectiles.Remove(projtr);
					mapComp.TargetMapComp.TorpsInRange.Remove(projtr);
				}
				else if(mapComp.TargetMapComp.ShuttlesInRange.Where(shuttle=>shuttle.Faction!=turret.Faction).Any())
                {
					VehiclePawn shuttleHit = mapComp.TargetMapComp.ShuttlesInRange.Where(shuttle => shuttle.Faction != turret.Faction).RandomElement();
					if(verbProps.defaultProjectile.thingClass!=typeof(Projectile_ExplosiveShipLaser) && Rand.Chance(0.75f))
                    {
						Log.Message("Shuttle dodged non-laser weapon");
                    }
					else if(Rand.Chance(1f-(shuttleHit.GetStatValue(ResourceBank.VehicleStatDefOf.SoS2CombatDodgeChance)/100f)))
					{
						if (shuttleHit.GetComp<CompShipHeatShield>() != null && shuttleHit.statHandler.componentsByKeys["shieldGenerator"].health > 0) //Shield takes the hit
						{
							Projectile dummyProjectile = (Projectile)ThingMaker.MakeThing(verbProps.defaultProjectile);
							shuttleHit.GetComp<CompShipHeatShield>().HitShield(dummyProjectile);
							Log.Message("Shuttle's shield took a hit! Its internal heatsinks are at " + shuttleHit.GetComp<CompVehicleHeatNet>().myNet.StorageUsed + " of " + shuttleHit.GetComp<CompVehicleHeatNet>().myNet.StorageCapacity + " capacity.");
							if(!dummyProjectile.Destroyed)
								dummyProjectile.Destroy();
						}
						else
						{
							shuttleHit.TakeDamage(new DamageInfo(verbProps.defaultProjectile.projectile.damageDef, verbProps.defaultProjectile.projectile.GetDamageAmount(caster)), IntVec2.Zero);
							Log.Message("Shuttle hit! It currently has " + shuttleHit.statHandler.GetStatValue(VehicleStatDefOf.BodyIntegrity) + " health.");
							if(shuttleHit.statHandler.GetStatValue(VehicleStatDefOf.BodyIntegrity)<=0)
							{
								if (shuttleHit.Faction==Faction.OfPlayer)
									Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CombatPodDestroyedPlayer"), null, MessageTypeDefOf.NegativeEvent);
								else
									Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CombatPodDestroyedEnemy"), null, MessageTypeDefOf.PositiveEvent);
								mapComp.TargetMapComp.DeRegisterShuttleMission(mapComp.TargetMapComp.ShuttleMissions.Where(mission => mission.shuttle == shuttleHit).First(), true);
								foreach (Pawn pawn in shuttleHit.AllPawnsAboard.ListFullCopy())
								{
									Log.Message("Pawn " + pawn + " is having a real bad day.");
									if (shuttleHit.Faction == Faction.OfPlayer && (ModSettings_SoS.easyMode || Rand.Chance(0.5f)))
									{
										HealthUtility.DamageUntilDowned(pawn, false);
										shuttleHit.RemovePawn(pawn);
										DropPodUtility.DropThingsNear(DropCellFinder.RandomDropSpot(mapComp.ShipCombatOriginMap), mapComp.OriginMapComp.map, new List<Thing> { pawn });
									}
									else
									{
										shuttleHit.RemovePawn(pawn);
										pawn.Kill(new DamageInfo(DamageDefOf.Bomb, 100f));
										if (shuttleHit.Faction == Faction.OfPlayer)
											DropPodUtility.DropThingsNear(DropCellFinder.RandomDropSpot(mapComp.ShipCombatOriginMap), mapComp.OriginMapComp.map, new List<Thing> { pawn.Corpse });
									}
								}
								/*foreach (Thing cargo in shuttleHit.GetDirectlyHeldThings())
									cargo.Kill();*/
							}
							else if(shuttleHit.statHandler.GetStatValue(VehicleStatDefOf.BodyIntegrity) <= ((CompShuttleLauncher)shuttleHit.CompVehicleLauncher).retreatAtHealth)
                            {
								if(shuttleHit.Faction==Faction.OfPlayer)
									Messages.Message("SoS.ShuttleRetreat".Translate(), MessageTypeDefOf.NegativeEvent);
								else
									Messages.Message("SoS.EnemyShuttleRetreat".Translate(), MessageTypeDefOf.PositiveEvent);
								mapComp.TargetMapComp.ShuttleMissions.Where(mission => mission.shuttle == shuttleHit).First().mission = ShipMapComp.ShuttleMission.RETURN;
							}
						}
					}
				}
				/*else if (!podsinrange.NullOrEmpty() && Rand.Chance(0.1f))
				{
					var groupedPods = podsinrange.RandomElement();
					List<ActiveDropPodInfo> pods = Traverse.Create(groupedPods).Field("pods").GetValue() as List<ActiveDropPodInfo>;

					//Log.Message("groupedPods: " + podsinrange.Count);
					//Log.Message("pods: " + pods.Count);
					if (!pods.NullOrEmpty())
					{
						ActiveDropPodInfo pod = pods.RandomElement();
						List<Thing> toDestroy = new List<Thing>();
						bool player = false;
						foreach (Thing t in pod.innerContainer.Where(p => p is Pawn))
						{
							if (t.Faction == Faction.OfPlayer)
							{
								player = true;
								if (ModSettings_SoS.easyMode)
								{
									HealthUtility.DamageUntilDowned((Pawn)t, false);
									continue;
								}
							}
							toDestroy.Add(t);
						}
						foreach (Thing t in toDestroy)
						{
							if (t is Pawn p)
							{
								p.Kill(new DamageInfo(DamageDefOf.Bomb, 100f));
							}
						}
						if (player && ModSettings_SoS.easyMode)
						{
							return;
						}
						pod.innerContainer.ClearAndDestroyContents();
						pods.Remove(pod);
						if (player)
							Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CombatPodDestroyedPlayer"), null, MessageTypeDefOf.NegativeEvent);
						else
							Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.CombatPodDestroyedEnemy"), null, MessageTypeDefOf.PositiveEvent);
					}
				}*/
			}
		}
		//projectiles register on turret map
		public void RegisterProjectile(Building_ShipTurret turret, LocalTargetInfo target, ThingDef spawnProjectile, IntVec3 burstLoc)
		{
			var mapComp = caster.Map.GetComponent<ShipMapComp>();

			//inc acc if any manning pawn shooting or aicore
			int accBoost = 0;
			if (turret.heatComp.myNet.TacCons.Any(b => b.mannableComp.MannedNow))
				accBoost = turret.heatComp.myNet.TacCons.Where(b => b.mannableComp.MannedNow).Max(b => b.mannableComp.ManningPawn.skills.GetSkill(SkillDefOf.Shooting).Level);
			if (accBoost < 10 && turret.heatComp.myNet.AICores.Any())
				accBoost = 10;
			ShipCombatProjectile proj = new ShipCombatProjectile
			{
				turret = turret,
				target = target,
				range = 0,
				//rangeAtStart = mapComp.Range,
				spawnProjectile = spawnProjectile,
				missRadius = this.verbProps.ForcedMissRadius,
				accBoost = accBoost,
				burstLoc = burstLoc,
				speed = turret.heatComp.Props.projectileSpeed,
				Map = turret.Map
			};
			mapComp.Projectiles.Add(proj);
		}
		public override bool CanHitTarget(LocalTargetInfo targ)
		{
			return true;
		}
	}
}
