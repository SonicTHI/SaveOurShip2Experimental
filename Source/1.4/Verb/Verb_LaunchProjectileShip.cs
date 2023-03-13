using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld.Planet;
using HarmonyLib;
using SaveOurShip2;
using RimworldMod;

namespace RimWorld
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
                    CompChangeableProjectilePlural comp = base.EquipmentSource.GetComp<CompChangeableProjectilePlural>();
                    if (comp != null && comp.Loaded)
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
                base.EquipmentSource.GetComp<CompChangeableProjectilePlural>()?.Notify_ProjectileLaunched();
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

            if (projectile.defName.Equals("Bullet_Fake_Laser") || projectile.defName.Equals("Bullet_Ground_Laser") || projectile.defName.Equals("Bullet_Fake_Psychic"))
            {
                ShipCombatLaserMote obj = (ShipCombatLaserMote)(object)ThingMaker.MakeThing(ThingDef.Named("ShipCombatLaserMote"));
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
            var mapComp = turret.Map.GetComponent<ShipHeatMapComp>();
            if (mapComp.ShipCombatTargetMap != null)
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
                var targetMapComp = mapComp.ShipCombatTargetMap.GetComponent<ShipHeatMapComp>();
                if (targetMapComp.TorpsInRange.Any() && Rand.Chance(0.1f))
                {
                    ShipCombatProjectile projtr = targetMapComp.TorpsInRange.RandomElement();
                    targetMapComp.Projectiles.Remove(projtr);
                    targetMapComp.TorpsInRange.Remove(projtr);
                }
                else if (!podsinrange.NullOrEmpty() && Rand.Chance(0.1f))
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
                                if (SaveOurShip2.ModSettings_SoS.easyMode)
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
                        if (player && SaveOurShip2.ModSettings_SoS.easyMode)
                        {
                            return;
                        }
                        pod.innerContainer.ClearAndDestroyContents();
                        pods.Remove(pod);
                        if (player)
                            Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipCombatPodDestroyedPlayer"), null, MessageTypeDefOf.NegativeEvent);
                        else
                            Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipCombatPodDestroyedEnemy"), null, MessageTypeDefOf.PositiveEvent);
                    }
                }
            }
        }
        //projectiles register on turret map
        public void RegisterProjectile(Building_ShipTurret turret, LocalTargetInfo target, ThingDef spawnProjectile, IntVec3 burstLoc)
        {
            var mapComp = caster.Map.GetComponent<ShipHeatMapComp>();
            ShipCombatProjectile proj = new ShipCombatProjectile
            {
                turret = turret,
                target = target,
                range = 0,
                rangeAtStart = mapComp.Range,
                spawnProjectile = spawnProjectile,
                missRadius = this.verbProps.ForcedMissRadius,
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
