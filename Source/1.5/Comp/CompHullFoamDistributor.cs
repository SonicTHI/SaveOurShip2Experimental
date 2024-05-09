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

			/*var ship = mapComp.ShipsOnMap[shipindex];
			if (ship != null)
			{
				Command_Action foamFill = new Command_Action
				{
					action = delegate
					{
						ship.FoamFill();
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
			}*/
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
