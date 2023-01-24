using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    class Projectile_ExplosiveShipCombatAntigrain : Projectile_TorpedoShipCombat
    {
        public override void Impact(Thing hitThing, bool blockedByShield = false)
        {
            Map map = base.Map;
            base.Impact(hitThing);
            GenExplosion.DoExplosion(base.Position, map, base.def.projectile.explosionRadius, DefDatabase<DamageDef>.GetNamed("ShipTorpedoBombSuper"), base.launcher, base.DamageAmount, base.ArmorPenetration, null, base.equipmentDef, base.def, intendedTarget: intendedTarget.Thing, postExplosionSpawnChance: 0.2f, postExplosionSpawnThingCount: 1, applyDamageToExplosionCellsNeighbors: false, preExplosionSpawnThingDef: null, preExplosionSpawnChance: 0f, preExplosionSpawnThingCount: 1, chanceToStartFire: 0.4f);
            CellRect cellRect = CellRect.CenteredOn(base.Position, 6);
            cellRect.ClipInsideMap(map);
            for (int i = 0; i < 8; i++)
            {
                IntVec3 randomCell = cellRect.RandomCell;
                DoFireExplosion(randomCell, map, 8.9f);
            }
        }

        protected void DoFireExplosion(IntVec3 pos, Map map, float radius)
        {
            GenExplosion.DoExplosion(pos, map, radius, DefDatabase<DamageDef>.GetNamed("ShipTorpedoBombSuper"), launcher, base.DamageAmount, base.ArmorPenetration, null, equipmentDef, def, intendedTarget.Thing);
        }
    }
}
