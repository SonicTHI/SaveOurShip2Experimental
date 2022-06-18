using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld.Planet;
using HarmonyLib;
using SaveOurShip2;


namespace RimWorld
{
    class Verb_LaunchProjectileShip : Verb_Shoot
    {
        public LocalTargetInfo shipTarget;

        public override ThingDef Projectile
        {
            get
            {
                if (base.EquipmentSource != null)
                {
                    CompChangeableProjectilePlural comp = base.EquipmentSource.GetComp<CompChangeableProjectilePlural>();
                    if (comp != null && comp.Loaded)
                    {
                        return comp.Projectile;
                    }
                }
                return verbProps.defaultProjectile;
            }
        }

        protected override bool TryCastShot()
        {
            Building_ShipTurret turret = this.caster as Building_ShipTurret;
            if (turret != null)
            {
                if (turret.gun.TryGetComp<CompChangeableProjectilePlural>() == null)
                    RegisterProjectile(turret, this.shipTarget, this.verbProps.spawnDef, turret.SynchronizedBurstLocation);
                else
                    RegisterProjectile(turret, this.shipTarget, turret.gun.TryGetComp<CompChangeableProjectilePlural>().Projectile.interactionCellIcon, turret.SynchronizedBurstLocation); //This is a horrible kludge, but it's a way to attach one projectile's ThingDef to another projectile
            }
            ThingDef projectile = Projectile;
            if (projectile == null)
            {
                return true;
            }
            ShootLine resultingLine= new ShootLine(caster.Position, currentTarget.Cell);
            Thing launcher = caster;
            Thing equipment = base.EquipmentSource;
            Vector3 drawPos = caster.DrawPos;
            if (equipment != null)
            {
                base.EquipmentSource.GetComp<CompChangeableProjectilePlural>()?.Notify_ProjectileLaunched();
            }
            Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, resultingLine.Source, caster.Map);
            if (launcher is Building_ShipTurretTorpedo)
            {
                projectile2.Launch(launcher, (drawPos + ((Building_ShipTurretTorpedo)launcher).TorpedoTubePos()), currentTarget.Cell, currentTarget.Cell, ProjectileHitFlags.None, false, equipment);
            }
            else
                projectile2.Launch(launcher, currentTarget.Cell, currentTarget.Cell, ProjectileHitFlags.None, false, equipment);

            if (projectile.defName.Equals("Bullet_Fake_Laser"))
            {
                ShipCombatLaserMote obj = (ShipCombatLaserMote)(object)ThingMaker.MakeThing(ThingDef.Named("ShipCombatLaserMote"));
                obj.origin = drawPos;
                obj.destination = currentTarget.Cell.ToVector3Shifted();
                obj.large = this.caster.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier) > 1.0f;
                obj.color = Color.red;
                obj.Attach(launcher);
                GenSpawn.Spawn(obj, launcher.Position, launcher.Map, 0);
            }
            else if (projectile.defName.Equals("Bullet_Fake_Psychic"))
            {
                ShipCombatLaserMote obj = (ShipCombatLaserMote)(object)ThingMaker.MakeThing(ThingDef.Named("ShipCombatLaserMote"));
                obj.origin = drawPos;
                obj.destination = currentTarget.Cell.ToVector3Shifted();
                obj.large = this.caster.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier) > 1.0f;
                obj.color = Color.green;
                obj.Attach(launcher);
                GenSpawn.Spawn(obj, launcher.Position, launcher.Map, 0);
            }
            projectile2.HitFlags = ProjectileHitFlags.None;
            return true;

            /*ShotReport shotReport = ShotReport.HitReportFor(caster, this, currentTarget);
            Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
            ThingDef targetCoverDef = randomCoverToMissInto?.def;
            if (!Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
            {
                resultingLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget);
                ThrowDebugText("ToWild" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
                ThrowDebugText("Wild\nDest", resultingLine.Dest);
                ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
                if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
                {
                    projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
                }
                projectile2.Launch(launcher, drawPos, resultingLine.Dest, currentTarget, projectileHitFlags2, equipment, targetCoverDef);
                return true;
            }
            if (currentTarget.Thing != null && currentTarget.Thing.def.category == ThingCategory.Pawn && !Rand.Chance(shotReport.PassCoverChance))
            {
                ThrowDebugText("ToCover" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
                ThrowDebugText("Cover\nDest", randomCoverToMissInto.Position);
                ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
                if (canHitNonTargetPawnsNow)
                {
                    projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
                }
                projectile2.Launch(launcher, drawPos, randomCoverToMissInto, currentTarget, projectileHitFlags3, equipment, targetCoverDef);
                return true;
            }
            ProjectileHitFlags projectileHitFlags4 = ProjectileHitFlags.IntendedTarget;
            if (canHitNonTargetPawnsNow)
            {
                projectileHitFlags4 |= ProjectileHitFlags.NonTargetPawns;
            }
            if (!currentTarget.HasThing || currentTarget.Thing.def.Fillage == FillCategory.Full)
            {
                projectileHitFlags4 |= ProjectileHitFlags.NonTargetWorld;
            }
            ThrowDebugText("ToHit" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
            if (currentTarget.Thing != null)
            {
                projectile2.Launch(launcher, drawPos, currentTarget, currentTarget, projectileHitFlags4, equipment, targetCoverDef);
                ThrowDebugText("Hit\nDest", currentTarget.Cell);
            }
            else
            {
                projectile2.Launch(launcher, drawPos, resultingLine.Dest, currentTarget, projectileHitFlags4, equipment, targetCoverDef);
                ThrowDebugText("Hit\nDest", resultingLine.Dest);
            }
            return true;*/
        }
        //projectiles register on turret map, PD removes from target map
        public void RegisterProjectile(Building_ShipTurret turret, LocalTargetInfo target, ThingDef spawnProjectile, IntVec3 burstLoc)
        {
            var mapComp = caster.Map.GetComponent<ShipHeatMapComp>();
            float range = mapComp.ShipCombatMasterMap.GetComponent<ShipHeatMapComp>().Range;
            if (turret.PointDefenseMode) //PD
            {
                //pods
                List<TravelingTransportPods> podsinrange = new List<TravelingTransportPods>();
                foreach (TravelingTransportPods obj in Find.WorldObjects.TravelingTransportPods)
                {
                    float rng = (float)Traverse.Create(obj).Field("traveledPct").GetValue();
                    if (obj.destinationTile == turret.Map.Parent.Tile && obj.Faction != mapComp.ShipFaction && rng > 0.75)
                    {
                        podsinrange.Add(obj);
                    }
                }
                if (mapComp.ShipCombatTargetMap.GetComponent<ShipHeatMapComp>().TorpsInRange.Any() && Rand.Chance(0.1f))
                {
                    ShipCombatProjectile projtr = mapComp.ShipCombatTargetMap.GetComponent<ShipHeatMapComp>().TorpsInRange.RandomElement();
                    mapComp.ShipCombatTargetMap.GetComponent<ShipHeatMapComp>().Projectiles.Remove(projtr);
                    mapComp.ShipCombatTargetMap.GetComponent<ShipHeatMapComp>().TorpsInRange.Remove(projtr);
                }
                else if (!podsinrange.NullOrEmpty() && Rand.Chance(0.1f))
                {
                    var groupedPods = podsinrange.RandomElement();
                    List<ActiveDropPodInfo> pods = Traverse.Create(groupedPods).Field("pods").GetValue() as List<ActiveDropPodInfo>;
                    if (!pods.NullOrEmpty())
                    {
                        ActiveDropPodInfo pod = pods.RandomElement();
                        List<Thing> toDestroy = new List<Thing>();
                        foreach (Thing t in pod.innerContainer)
                        {
                            toDestroy.Add(t);
                        }
                        foreach (Thing t in toDestroy)
                        {
                            if (t is Pawn)
                            {
                                if (ShipInteriorMod2.easyMode && t.Faction == Faction.OfPlayer)
                                    HealthUtility.DamageUntilDowned((Pawn)t, false);
                                else
                                    t.Kill(new DamageInfo(DamageDefOf.Bomb, 100f));
                            }
                        }
                        if (toDestroy.NullOrEmpty())
                        {
                            pod.innerContainer.ClearAndDestroyContents();
                            pods.Remove(pod);
                        }
                    }
                    else
                        groupedPods.Destroy();
                }
            }
            else
            {
                ShipCombatProjectile proj = new ShipCombatProjectile
                {
                    turret = turret,
                    target = target,
                    range = 0,
                    spawnProjectile = spawnProjectile,
                    burstLoc = burstLoc,
                    speed = turret.TryGetComp<CompShipHeatSource>().Props.projectileSpeed,
                    Map = turret.Map
                };
                mapComp.Projectiles.Add(proj);
            }
        }
        public override bool CanHitTarget(LocalTargetInfo targ)
        {
            return true;
        }
    }
}
