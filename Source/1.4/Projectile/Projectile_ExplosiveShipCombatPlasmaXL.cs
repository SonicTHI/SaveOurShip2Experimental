using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    class Projectile_ExplosiveShipCombatPlasmaXL : Projectile_ExplosiveShipCombat
    {
        protected override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            GenExplosion.DoExplosion(base.Position, map, base.def.projectile.explosionRadius, DefDatabase<DamageDef>.GetNamed("ShipPlasmaLarge"), base.launcher, base.DamageAmount, base.ArmorPenetration, null, base.equipmentDef, base.def, postExplosionSpawnThingDef: ThingDefOf.Filth_Fuel, intendedTarget: intendedTarget.Thing, postExplosionSpawnChance: 0.2f, postExplosionSpawnThingCount: 1, applyDamageToExplosionCellsNeighbors: false, preExplosionSpawnThingDef: null, preExplosionSpawnChance: 0f, preExplosionSpawnThingCount: 1, chanceToStartFire: 0.4f);
            CellRect cellRect = CellRect.CenteredOn(base.Position, 10);
            cellRect.ClipInsideMap(map);
            for (int i = 0; i < 5; i++)
            {
                IntVec3 randomCell = cellRect.RandomCell;
                DoFireExplosion(randomCell, map, 3.3f);
            }
        }

        protected void DoFireExplosion(IntVec3 pos, Map map, float radius)
        {
            GenExplosion.DoExplosion(pos, map, radius * Mathf.Min(weaponDamageMultiplier, 2), DefDatabase<DamageDef>.GetNamed("ShipPlasmaSmall"), launcher, base.DamageAmount, base.ArmorPenetration, null, equipmentDef, def, intendedTarget.Thing);
        }
    }
}
