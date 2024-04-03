using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
	class Projectile_ExplosiveShipCombatPsychic : Projectile_ExplosiveShipCombat
	{
		protected override void Impact(Thing hitThing, bool blockedByShield = false)
		{
				ShipCombatLaserMote obj = (ShipCombatLaserMote)(object)ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipCombatLaserMote);
				obj.origin = this.origin;
				obj.destination = this.intendedTarget.Cell.ToVector3();
				obj.color = Color.green;
				obj.large = true;
				GenSpawn.Spawn(obj, this.intendedTarget.Cell, Map, 0);
				Explode();
		}

		protected override void Explode()
		{
			GenExplosion.DoExplosion(this.intendedTarget.Cell, base.Map, base.def.projectile.explosionRadius * Mathf.Sqrt(this.weaponDamageMultiplier), base.def.projectile.damageDef, base.launcher, base.DamageAmount, base.ArmorPenetration, base.def.projectile.soundExplode, base.equipmentDef, base.def, intendedTarget.Thing, base.def.projectile.postExplosionSpawnThingDef, base.def.projectile.postExplosionSpawnChance, base.def.projectile.postExplosionSpawnThingCount, preExplosionSpawnThingDef: base.def.projectile.preExplosionSpawnThingDef, preExplosionSpawnChance: base.def.projectile.preExplosionSpawnChance, preExplosionSpawnThingCount: base.def.projectile.preExplosionSpawnThingCount, applyDamageToExplosionCellsNeighbors: base.def.projectile.applyDamageToExplosionCellsNeighbors, chanceToStartFire: base.def.projectile.explosionChanceToStartFire, damageFalloff: base.def.projectile.explosionDamageFalloff, direction: origin.AngleToFlat(destination));
			Destroy();
		}
	}
}
