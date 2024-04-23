using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld.BaseGen;

namespace SaveOurShip2
{
	class SymbolResolver_Interior_Security_Triangle : SymbolResolver
	{
		public override void Resolve(ResolveParams rp)
		{
			Map map = BaseGen.globalSettings.map;
			if (rp.disableHives.HasValue && rp.disableHives.Value)
			{
				Thing thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.AllDefs.Where(def => (typeof(Building_Turret)).IsAssignableFrom(def.thingClass) && def.Size.x == 1 && def.Size.z == 1).RandomElement());
				thing.SetFaction(rp.faction);
				GenSpawn.Spawn(thing, new IntVec3(rp.rect.maxX - 4, 0, rp.rect.minZ - 4), map);
				thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.AllDefs.Where(def => (typeof(Building_Turret)).IsAssignableFrom(def.thingClass) && def.Size.x == 1 && def.Size.z == 1).RandomElement());
				thing.SetFaction(rp.faction);
				GenSpawn.Spawn(thing, new IntVec3(rp.rect.maxX - 8, 0, rp.rect.minZ - 4), map);
				thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.AllDefs.Where(def => (typeof(Building_Turret)).IsAssignableFrom(def.thingClass) && def.Size.x == 1 && def.Size.z == 1).RandomElement());
				thing.SetFaction(rp.faction);
				GenSpawn.Spawn(thing, new IntVec3(rp.rect.maxX - 4, 0, rp.rect.minZ - 8), map);
				GenSpawn.Spawn(ThingDef.Named("Ship_SecurityConsole"), new IntVec3(rp.rect.maxX, 0, rp.rect.minZ - 4), map);
				GenSpawn.Spawn(ThingDefOf.Heater, new IntVec3(rp.rect.maxX, 0, rp.rect.minZ - 2), map);
				GenSpawn.Spawn(ThingDefOf.StandingLamp, new IntVec3(rp.rect.maxX - 2, 0, rp.rect.minZ - 2), map);
			}
			else
			{
				foreach (IntVec3 current in rp.rect)
				{
					if (!current.GetThingList(map).Any(t => t.def == ResourceBank.ThingDefOf.ShipHullTileWrecked))
						continue;
					if (Rand.Chance(0.025f))
					{
						Thing thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.AllDefs.Where(def => (typeof(Building_Turret)).IsAssignableFrom(def.thingClass) && def.Size.x == 1 && def.Size.z == 1).RandomElement());
						thing.SetFaction(rp.faction);
						GenSpawn.Spawn(thing, current, map);
					}
					else if (Rand.Chance(0.0125f))
					{
						Thing thing = ThingMaker.MakeThing(ThingDef.Named("TrapIED_HighExplosive"));
						thing.SetFaction(rp.faction);
						GenSpawn.Spawn(thing, current, map);
					}
				}
				GenSpawn.Spawn(ThingDef.Named("Ship_SecurityConsole"), new IntVec3(rp.rect.maxX, 0, rp.rect.minZ + 2), map);
				GenSpawn.Spawn(ThingDefOf.Heater, new IntVec3(rp.rect.maxX, 0, rp.rect.minZ), map);
				GenSpawn.Spawn(ThingDefOf.StandingLamp, new IntVec3(rp.rect.maxX - 2, 0, rp.rect.minZ), map);
			}
		}

		public override bool CanResolve(ResolveParams rp)
		{
			return true;
		}
	}
}
