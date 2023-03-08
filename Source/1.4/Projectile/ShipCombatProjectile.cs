using Verse;

namespace RimWorld
{
    public class ShipCombatProjectile : IExposable
    {
        public Building_ShipTurret turret;
        public LocalTargetInfo target;
        public float range;
        public float rangeAtStart;
        public ThingDef spawnProjectile;
        public float missRadius;
        public IntVec3 burstLoc;
        public float speed;
        public Map Map;

        public void ExposeData()
        {
            Scribe_References.Look<Building_ShipTurret>(ref turret, "turret");
            Scribe_TargetInfo.Look(ref target, "target");
            Scribe_Values.Look<float>(ref range, "range");
            Scribe_Values.Look<float>(ref rangeAtStart, "rangeAtStart");
            Scribe_Defs.Look<ThingDef>(ref spawnProjectile, "projectile");
            Scribe_Values.Look<float>(ref missRadius, "missRadius");
            Scribe_Values.Look<IntVec3>(ref burstLoc, "burstLoc");
            Scribe_Values.Look<float>(ref speed, "speed");
            Scribe_References.Look<Map>(ref Map, "map");
        }
    }
}
