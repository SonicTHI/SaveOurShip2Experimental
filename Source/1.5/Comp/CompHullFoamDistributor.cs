using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public class CompHullFoamDistributor : ThingComp
	{
		public ShipMapComp mapComp;
		public CompRefuelable fuelComp;
		public CompPowerTrader powerComp;
		public CompProps_HullFoamDistributor Props
		{
			get
			{
				return (CompProps_HullFoamDistributor)props;
			}
		}
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo item in base.CompGetGizmosExtra())
			{
				yield return item;
			}
			if ((parent.Faction != Faction.OfPlayer || !mapComp.IsPlayerShipMap) && !(Prefs.DevMode && ShipInteriorMod2.HasSoS2CK))
				yield break;

			int shipindex = mapComp.ShipIndexOnVec(parent.Position);
			if (shipindex == -1)
				yield break;

			var ship = mapComp.ShipsOnMap[shipindex];
			if (ship != null)
			{
				Command_Action foamFill = new Command_Action
				{
					action = delegate
					{
						FoamFill(ship);
					},
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.FoamFill"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.FoamFillDesc"),
					icon = ContentFinder<Texture2D>.Get("UI/SalvageClaim")
				};
				if (ship.BuildingsDestroyed.NullOrEmpty() || !ship.FoamDistributors.Any(d => d.fuelComp.Fuel > 0))
				{
					foamFill.Disable(TranslatorFormattedStringExtensions.Translate("SoS.FoamFillDisabled"));
				}
				yield return foamFill;
			}
		}
		public void FoamFill(SpaceShipCache ship) //fill in missing plating or hull
		{
			//td needs to start from an attached part and fill outward
			foreach (var b in ship.BuildingsDestroyed.Where(d => d.Item1.building.shipPart && d.Item1.Size.x == 1 && d.Item1.Size.z == 1))
			{
				var props = b.Item1.GetCompProperties<CompProps_ShipCachePart>();
				if ((props.Plating && b.Item2.GetThingList(parent.Map).Any(t => t.TryGetComp<CompShipCachePart>()?.Props.Plating ?? false)) 
					|| (props.Hull && b.Item2.GetThingList(parent.Map).Any(t => t.TryGetComp<CompShipCachePart>()?.Props.Hull ?? false)))
				{
					continue;
				}
				foreach (CompHullFoamDistributor dist in ship.FoamDistributors.Where(d => d.fuelComp.Fuel > 0 && d.powerComp.PowerOn))
				{
					dist.fuelComp.ConsumeFuel(1);
					Thing replacer;
					if (props.Hull)
						replacer = ThingMaker.MakeThing(ResourceBank.ThingDefOf.HullFoamWall);
					else
						replacer = ThingMaker.MakeThing(ResourceBank.ThingDefOf.ShipHullfoamTile);

					replacer.SetFaction(ship.Faction);
					GenPlace.TryPlaceThing(replacer, b.Item2, parent.Map, ThingPlaceMode.Direct);
					break;
				}

			}
		}
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			mapComp = parent.Map.GetComponent<ShipMapComp>();
			fuelComp = parent.TryGetComp<CompRefuelable>();
			powerComp = parent.TryGetComp<CompPowerTrader>();
		}
		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
		}
	}
}
