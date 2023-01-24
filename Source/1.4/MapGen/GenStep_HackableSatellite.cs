using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    class GenStep_HackableSatellite : GenStep_Scatterer
    {

        public override int SeedPart
        {
            get
            {
                return Rand.Range(1337, 69420);
            }
        }

        protected override bool CanScatterAt(IntVec3 c, Map map)
        {
            return true;
        }

        protected override void ScatterAt(IntVec3 c, Map map, GenStepParams stepparams, int stackCount = 1)
        {
            int radius = Rand.RangeInclusive(5, 7);
            List<IntVec3> border = new List<IntVec3>();
            List<IntVec3> interior = new List<IntVec3>();
            ShipInteriorMod2.CircleUtility(c.x,c.z,radius,ref border, ref interior);
            foreach (IntVec3 vec in interior)
            {
                Thing floor = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipHullTile);
                GenSpawn.Spawn(floor, vec, map);
            }
            foreach (IntVec3 vec in border)
            {
                Thing wall = ThingMaker.MakeThing(ThingDef.Named("Ship_Beam"));
                wall.SetFaction(Faction.OfMechanoids);
                GenSpawn.Spawn(wall, vec, map);
            }
            Thing core = ThingMaker.MakeThing(ThingDef.Named("Space_Satellite_Core"));
            core.SetFaction(Faction.OfMechanoids);
            GenSpawn.Spawn(core, c, map);
            Thing solar = ThingMaker.MakeThing(ThingDef.Named("ShipInside_SolarGenerator"));
            solar.SetFaction(Faction.OfMechanoids);
            GenSpawn.Spawn(solar, new IntVec3(c.x + radius, 0, c.z), map);
            solar.Rotation = Rot4.West;
            solar = ThingMaker.MakeThing(ThingDef.Named("ShipInside_SolarGenerator"));
            solar.SetFaction(Faction.OfMechanoids);
            GenSpawn.Spawn(solar, new IntVec3(c.x - radius, 0, c.z), map);
            solar.Rotation = Rot4.East;
            solar = ThingMaker.MakeThing(ThingDef.Named("ShipInside_SolarGenerator"));
            solar.SetFaction(Faction.OfMechanoids);
            GenSpawn.Spawn(solar, new IntVec3(c.x, 0, c.z+radius), map);
            solar.Rotation = Rot4.South;
            GenSpawn.Spawn(ThingDef.Named("ShipAirlock"), new IntVec3(c.x, 0, c.z - radius), map);
        }
    }
}
