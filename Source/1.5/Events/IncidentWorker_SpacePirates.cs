using RimWorld;
using System;
using System.Linq;
using Verse;

namespace SaveOurShip2
{
	public class IncidentWorker_SpacePirates : IncidentWorker
	{
		protected override bool CanFireNowSub(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			var mapComp = map.GetComponent<ShipMapComp>();
			if (mapComp.ShipMapState != ShipMapState.nominal || mapComp.NextTargetMap != null || ModSettings_SoS.frequencySoS == 0 || Find.TickManager.TicksGame < mapComp.LastAttackTick + 300000 / ModSettings_SoS.frequencySoS)
				return false;

			foreach (Building_ShipCloakingDevice cloak in mapComp.Cloaks)
			{
				if (cloak.active)
					return false;
			}
			return true;
		}

		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			//add ship, non specific - determined on attack
			PirateShip ship = new PirateShip(DefDatabase<TraderKindDef>.GetNamed("Orbital_PirateMerchant"), Faction.OfPirates);

			int rarity = Rand.RangeInclusive(1, 2);
			SpaceNavyDef navy = DefDatabase<SpaceNavyDef>.AllDefs.Where(n =>
			{
				if (n.spaceShipDefs.NullOrEmpty() || !n.pirates)
					return false;
				//any faction that has same def as navy, defeat check
				else if (Find.FactionManager.AllFactions.Any(f => n.factionDefs.Contains(f.def) && (!f.defeated || (f.defeated && n.canOperateAfterFactionDefeated))))
					return true;
				return true;
			}).RandomElement();
			if (navy != null)
			{
				ship.spaceNavyDef = navy;
				//ship.attackableShip = navy.spaceShipDefs.Where(def => !def.neverRandom && !def.neverAttacks && def.rarityLevel <= rarity).RandomElement();
				ship.shipFaction = Find.FactionManager.AllFactions.Where(f => navy.factionDefs.Contains(f.def)).RandomElement();
			}
			/*if (ship.attackableShip == null)
			{
				ship.attackableShip = DefDatabase<SpaceShipDef>.AllDefs.Where(def => !def.neverRandom && !def.neverAttacks && !def.navyExclusive && def.rarityLevel <= rarity).RandomElement();
				ship.shipFaction = Faction.OfAncientsHostile;
			}*/
			map.passingShipManager.AddShip(ship);
			ship.GenerateThings();

			int bounty = ShipInteriorMod2.WorldComp.PlayerFactionBounty;
			if (CommsConsoleUtility.PlayerHasPoweredCommsConsole(map)) //notify
			{
				string text = TranslatorFormattedStringExtensions.Translate("SoS.PirateHail");
				if (bounty > 50)
					text += TranslatorFormattedStringExtensions.Translate("SoS.PirateHailPirate");
				else
					text += TranslatorFormattedStringExtensions.Translate("SoS.PirateHailNormal");

				SendStandardLetter(def.letterLabel, text, def.letterDef, parms, new TargetInfo(map.listerBuildings.AllBuildingsColonistOfClass<Building_CommsConsole>().First().Position, map, false), Array.Empty<NamedArgument>());
			}
			else if (!(bounty > 50 && Rand.Bool)) //attack
			{
				var mapComp = map.GetComponent<ShipMapComp>();
				mapComp.StartShipEncounter(ship);
			}
			return true;
		}
	}
}
