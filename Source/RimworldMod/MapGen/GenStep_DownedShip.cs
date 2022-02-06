using RimWorld.BaseGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI.Group;
using RimWorld.Planet;
using SaveOurShip2;

namespace RimWorld
{
    class GenStep_DownedShip : GenStep_Scatterer
    {

        public override int SeedPart
        {
            get
            {
                return 694201337;
            }
        }

        protected override bool CanScatterAt(IntVec3 c, Map map)
        {
            return true;
        }

        protected override void ScatterAt(IntVec3 c, Map map, GenStepParams stepparams, int stackCount = 1)
        {
            Building core = null;
            try
            {
                ShipInteriorMod2.GenerateShip(DefDatabase<EnemyShipDef>.AllDefs.Where(def=>def.spaceSite).RandomElement(), map, null, Faction.OfAncients, null, out core, false, true, true);
            }
            catch(Exception e)
            {
                Log.Error(e.ToString());
            }
            List<Building> toKill = new List<Building>();
            List<Building> toKillAlso = new List<Building>();
            foreach (Building building in map.listerBuildings.allBuildingsNonColonist)
            {
                if (building.Spawned && building.def.CostList != null && building.def.CostList.Any(cost => cost.thingDef == ThingDefOf.ComponentSpacer))
                    toKill.Add(building);
                else if (building is Building_Turret)
                    toKillAlso.Add(building);
                if(building.def==ThingDefOf.FirefoamPopper)
                {
                    if (Rand.Chance(0.8f))
                        toKill.Add(building);
                    else
                        building.TryGetComp<CompExplosive>().StartWick();
                }
            }
            for (int i = 0; i < Rand.Range(23, 69); i++)
                toKillAlso.Add(map.listerBuildings.allBuildingsNonColonist.RandomElement());
            foreach (Building building in toKill)
            {
                GenExplosion.DoExplosion(building.Position, map, Rand.Range(1.9f, 4.9f), DamageDefOf.Flame, null);
                building.Destroy();
                GenPlace.TryPlaceThing(ThingMaker.MakeThing(ThingDef.Named("ShipChunkSalvage")), building.Position, map, ThingPlaceMode.Near);
            }
            foreach (Building building in toKillAlso)
            {
                GenExplosion.DoExplosion(building.Position, map, Rand.Range(1.9f, 4.9f), DamageDefOf.Flame, null);
                building.Kill();
            }
            foreach (Pawn pawn in map.mapPawns.PawnsInFaction(Faction.OfAncients))
                HealthUtility.DamageUntilDowned(pawn);
            foreach (Pawn pawn in map.mapPawns.PawnsInFaction(Faction.OfInsects))
                HealthUtility.DamageUntilDowned(pawn);
            foreach (Pawn pawn in map.mapPawns.PawnsInFaction(Faction.OfMechanoids))
                HealthUtility.DamageUntilDowned(pawn);
            map.Parent.GetComponent<TimedDetectionRaids>().alertRaidsArrivingIn = true;
        }
    }
}
