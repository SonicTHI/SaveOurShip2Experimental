using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;
using SaveOurShip2;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class CompShipSalvageBay : ThingComp
    {
        public static int salvageCapacity = 2500;
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
			var mapComp = this.parent.Map.GetComponent<ShipHeatMapComp>();

			foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            if (parent.Faction == Faction.OfPlayer && this.parent.Map.Parent.def.defName.Equals("ShipOrbiting") || (Prefs.DevMode && ModLister.HasActiveModWithName("Save Our Ship Creation Kit")))
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
                        sourceMap = this.parent.Map,
                        targetMap = map,
						icon = ContentFinder<Texture2D>.Get("UI/SalvageShip"),
						defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipSalvageCommand") + " (" + map + ")",
						defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipSalvageCommandDesc") + map
					};
					if (mapComp.InCombat)
						retrieveShipEnemy.Disable(TranslatorFormattedStringExtensions.Translate("ShipSalvageDisabled"));
					yield return retrieveShipEnemy;
                }
                Command_VerbTargetWreckMap moveWreck = new Command_VerbTargetWreckMap
                {
                    salvageBay = (Building)this.parent,
                    sourceMap = this.parent.Map,
                    targetMap = this.parent.Map,
                    icon = ContentFinder<Texture2D>.Get("UI/SalvageMove"),
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipMoveWreckCommand"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipMoveWreckCommandDesc")
                };
                Command_VerbTargetWreckMap moveWreckRot = new Command_VerbTargetWreckMap
                {
                    salvageBay = (Building)this.parent,
                    sourceMap = this.parent.Map,
                    targetMap = this.parent.Map,
                    rotb = 2,
                    icon = ContentFinder<Texture2D>.Get("UI/SalvageMoveRot"),
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipMoveWreckRotCommand"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipMoveWreckRotCommandDesc")
                };
                Command_Action claim = new Command_Action
                {
                    action = delegate
                    {
                        List<Building> buildings = new List<Building>();
                        List<Thing> things = new List<Thing>();
                        foreach (Thing t in this.parent.Map.listerThings.AllThings)
                        {
                            if (t is Building b && b.def.CanHaveFaction && b.Faction != Faction.OfPlayer)
                            {
                                buildings.Add(b);
                            }
                            else if (t is DetachedShipPart)
                                things.Add(t);
                        }
                        if (buildings.Any())
                        {
                            foreach (Building b in buildings)
                            {
                                if (b is Building_Storage s)
                                    s.settings.filter.SetDisallowAll();
                                b.SetFaction(Faction.OfPlayer);
                            }
                            Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipClaimWrecksSuccess", buildings.Count), parent, MessageTypeDefOf.PositiveEvent);
                        }
                        //remove floating tiles
                        foreach (Thing t in things)
                        {
                            t.Destroy();
                        }
                    },
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipClaimWrecksCommand"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipClaimWrecksCommandDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/SalvageClaim")
                };
                Command_VerbTargetWreck removeTargetWreck = new Command_VerbTargetWreck
                {
                    //abandon target wreck (rem rock floor)
                    targetMap = this.parent.Map,
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipRemoveWrecksCommand"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipRemoveWrecksCommandDesc"),
                    icon = ContentFinder<Texture2D>.Get("UI/SalvageCancel")
                };
                if (mapComp.InCombat || this.parent.Map.mapPawns.AllPawns.Where(p => p.HostileTo(Faction.OfPlayer)).Any())
                {
                    moveWreck.Disable(TranslatorFormattedStringExtensions.Translate("ShipSalvageDisabled"));
                    moveWreckRot.Disable(TranslatorFormattedStringExtensions.Translate("ShipSalvageDisabled"));
                    claim.Disable(TranslatorFormattedStringExtensions.Translate("ShipClaimWrecksDisabled"));
                    removeTargetWreck.Disable(TranslatorFormattedStringExtensions.Translate("ShipClaimWrecksDisabled"));
                }
                yield return moveWreck;
                yield return moveWreckRot;
                yield return claim;
                yield return removeTargetWreck;
            }
		}
        public override void CompTickRare()
        {
            base.CompTickRare();
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