using System;
using UnityEngine;
using Verse;
using SaveOurShip2;
using System.Collections.Generic;

namespace RimWorld.BaseGen
{
	public class SymbolResolver_Ship_Pregen_New : SymbolResolver
	{
		private struct SpawnDescriptor
		{
			public IntVec3 offset;
			public Rot4 rot;
		}

		public override void Resolve(ResolveParams rp)
		{
			List<Building> cores = new List<Building>();
			try { ShipInteriorMod2.GenerateShip(DefDatabase<EnemyShipDef>.GetNamed("CharlonWhitestone"), BaseGen.globalSettings.map, null, Faction.OfPlayer, null, out cores, false, true); } catch (Exception e) { Log.Error(e.ToString()); }
			foreach(Thing thing in BaseGen.globalSettings.map.listerThings.ThingsInGroup(ThingRequestGroup.Refuelable))
			{
				((ThingWithComps)thing).TryGetComp<CompRefuelable>().Refuel(9999);
			}
			cores.FirstOrFallback().TryGetComp<CompBuildingConsciousness>().AIName = "Charlon Whitestone";
		}
	}
}
