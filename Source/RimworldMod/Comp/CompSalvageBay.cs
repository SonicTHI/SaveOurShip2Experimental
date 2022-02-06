using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class CompShipSalvageBay : ThingComp
    {
        public static int salvageCapacity = 3000;
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
			var mapComp = this.parent.Map.GetComponent<ShipHeatMapComp>();

			foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            if (parent.Faction == Faction.OfPlayer && this.parent.Map.Parent.def.defName.Equals("ShipOrbiting"))
			{
				List<Map> salvagableMaps = new List<Map>();
				foreach (Map map in Find.Maps)
				{
					if (map.GetComponent<ShipHeatMapComp>().IsGraveyard)
						salvagableMaps.Add(map);
				}
				foreach (Map map in salvagableMaps)
				{
                    Command_VerbTargetWreckMap retrieveShipEnemy = new Command_VerbTargetWreckMap
                    {
                        salvageBay = (Building)this.parent,
                        salvageBayNum = this.parent.Map.listerBuildings.allBuildingsColonist.Where(b => b.TryGetComp<CompShipSalvageBay>() != null).Count(),
                        targetMap = map,
						icon = ContentFinder<Texture2D>.Get("UI/SalvageShip"),
						defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipSalvageCommand") + " (" + map + ")",
						defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipSalvageCommandDesc") + map
					};
					if (mapComp.InCombat)
						retrieveShipEnemy.Disable(TranslatorFormattedStringExtensions.Translate("ShipSalvageDisabled"));
					yield return retrieveShipEnemy;
                }
                Command_Action claim = new Command_Action
                {
                    action = delegate
                    {
                        List<Building> buildings = new List<Building>();
                        foreach (Building b in this.parent.Map.listerBuildings.allBuildingsNonColonist)
                        {
                            if (b.def.CanHaveFaction)
                                buildings.Add(b);
                        }
                        if (buildings.Count > 0)
                        {
                            foreach (Building b in buildings)
                            {
                                b.SetFaction(Faction.OfPlayer);
                            }
                            Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipClaimWrecksSuccess", buildings.Count), parent, MessageTypeDefOf.PositiveEvent);
                        }
                    },
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipClaimWrecksCommand"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipClaimWrecksCommandDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/SalvageShip")
                };
                Command_VerbTargetWreck removeTargetWreck = new Command_VerbTargetWreck
                {
                    //abandon target wreck (rem rock floor)
                    targetMap = this.parent.Map,
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipRemoveWrecksCommand"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipRemoveWrecksCommandDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/SalvageCancel")
                };
                if (mapComp.InCombat || this.parent.Map.mapPawns.AllPawns.Where(p => p.Faction != Faction.OfPlayer && p.Faction.PlayerRelationKind == FactionRelationKind.Hostile && !p.Downed && !p.Dead && !p.IsPrisoner && !p.IsSlave).Any())
                {
                    claim.Disable(TranslatorFormattedStringExtensions.Translate("ShipClaimWrecksDisabled"));
                    removeTargetWreck.Disable(TranslatorFormattedStringExtensions.Translate("ShipClaimWrecksDisabled"));
                }
                yield return claim;
                yield return removeTargetWreck;
            }
		}
        public override void CompTickRare()
        {
            base.CompTickRare();
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
		}

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("ShipSalvageBase".Translate());
            return stringBuilder.ToString();
            //return base.CompInspectStringExtra();
        }
    }
}