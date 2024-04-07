﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
	public class CompShipSalvageBay : ThingComp
	{
		public CompProperties_SalvageBay Props
		{
			get
			{
				return (CompProperties_SalvageBay)props;
			}
		}
		public int SalvageWeight => Props.weight;
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			var mapComp = parent.Map.GetComponent<ShipHeatMapComp>();

			foreach (Gizmo item in base.CompGetGizmosExtra())
			{
				yield return item;
			}
			if (parent.Faction == Faction.OfPlayer && mapComp.IsPlayerShipMap || (Prefs.DevMode && ShipInteriorMod2.HasSoS2CK))
			{
				foreach (Map map in Find.Maps.Where(m => m.GetComponent<ShipHeatMapComp>().ShipMapState == ShipMapState.isGraveyard))
				{
					Command_TargetWreck retrieveShipEnemy = new Command_TargetWreck
					{
						salvageBay = (Building)parent,
						sourceMap = parent.Map,
						targetMap = map,
						icon = ContentFinder<Texture2D>.Get("UI/SalvageShip"),
						defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.SalvageCommand", map.Parent.Label),
						defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.SalvageCommandDesc", map.Parent.Label)
					};
					if (Props.beam && (parent.TryGetComp<CompPowerTrader>()?.PowerOn ?? false))
					{
						Command_SelectShipMap beam = new Command_SelectShipMap
						{
							salvageBay = (Building)parent,
							sourceMap = parent.Map,
							targetMap = map,
							mode = SelectShipMapMode.scoop,
							defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.SalvageBeam", map.Parent.Label),
							defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.SalvageBeamDesc", map.Parent.Label),
							icon = ContentFinder<Texture2D>.Get("UI/SalvageBeam")
						};
						if (mapComp.ShipMapState != ShipMapState.nominal)
						{
							beam.Disable(TranslatorFormattedStringExtensions.Translate("SoS.SalvageDisabled"));
						}
						yield return beam;
					}
					Command_SelectShipMap stablizeShipEnemy = new Command_SelectShipMap
					{
						salvageBay = (Building)parent,
						sourceMap = parent.Map,
						targetMap = map,
						mode = SelectShipMapMode.stabilize,
						defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.SalvageStablize", map.Parent.Label),
						defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.SalvageStablizeDesc", map.Parent.Label),
						icon = ContentFinder<Texture2D>.Get("UI/StabilizeShip")
					};
					if (mapComp.ShipMapState != ShipMapState.nominal)
					{
						retrieveShipEnemy.Disable(TranslatorFormattedStringExtensions.Translate("SoS.SalvageDisabled"));
						stablizeShipEnemy.Disable(TranslatorFormattedStringExtensions.Translate("SoS.SalvageDisabled"));
					}
					yield return retrieveShipEnemy;
					yield return stablizeShipEnemy;
				}
				Command_TargetWreck moveWreck = new Command_TargetWreck
				{
					groupable = false,
					salvageBay = (Building)this.parent,
					sourceMap = this.parent.Map,
					targetMap = this.parent.Map,
					icon = ContentFinder<Texture2D>.Get("UI/SalvageMove"),
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.MoveWreckCommand"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.MoveWreckCommandDesc")
				};
				Command_TargetWreck moveWreckFlip = new Command_TargetWreck
				{
					groupable = false,
					salvageBay = (Building)this.parent,
					sourceMap = this.parent.Map,
					targetMap = this.parent.Map,
					rotb = 2,
					icon = ContentFinder<Texture2D>.Get("UI/SalvageMoveFlip"),
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.MoveWreckFlipCommand"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.MoveWreckFlipCommandDesc")
				};
				Command_TargetWreck moveWreckRot = new Command_TargetWreck
				{
					groupable = false,
					salvageBay = (Building)this.parent,
					sourceMap = this.parent.Map,
					targetMap = this.parent.Map,
					rotb = 1,
					icon = ContentFinder<Texture2D>.Get("UI/SalvageMoveRot"),
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.MoveWreckRotCommand"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.MoveWreckRotCommandDesc")
				};
				Command_Action claim = new Command_Action
				{
					action = delegate
					{
						Claim();
					},
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ClaimWrecksCommand"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ClaimWrecksCommandDesc"),
					icon = ContentFinder<Texture2D>.Get("UI/SalvageClaim")
				};
				Command_TargetShipRemove removeTargetWreck = new Command_TargetShipRemove
				{
					//abandon target wreck (rem rock floor)
					groupable = false,
					targetMap = this.parent.Map,
					position = this.parent.Position,
					defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.RemoveWrecksCommand"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.RemoveWrecksCommandDesc"),
					icon = ContentFinder<Texture2D>.Get("UI/SalvageCancel")
				};
				if (mapComp.ShipMapState != ShipMapState.nominal || GenHostility.AnyHostileActiveThreatToPlayer(parent.Map))
				{
					moveWreck.Disable(TranslatorFormattedStringExtensions.Translate("SoS.SalvageDisabled"));
					moveWreckFlip.Disable(TranslatorFormattedStringExtensions.Translate("SoS.SalvageDisabled"));
					moveWreckRot.Disable(TranslatorFormattedStringExtensions.Translate("SoS.SalvageDisabled"));
					claim.Disable(TranslatorFormattedStringExtensions.Translate("SoS.ClaimWrecksDisabled"));
				}
				if (mapComp.ShipMapState == ShipMapState.burnUpSet)
                {
                    removeTargetWreck.Disable(TranslatorFormattedStringExtensions.Translate("SoS.ClaimWrecksDisabled"));
                }
				yield return moveWreck;
				yield return moveWreckFlip;
				yield return moveWreckRot;
				yield return claim;
				yield return removeTargetWreck;
			}
		}
		private void Claim()
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
				Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.ClaimWrecksSuccess", buildings.Count), parent, MessageTypeDefOf.PositiveEvent);
			}
			//remove floating tiles
			foreach (Thing t in things)
			{
				t.Destroy();
			}
		}
		public override void CompTickRare()
		{
			base.CompTickRare();
		}
		public override string CompInspectStringExtra()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("SoS.SalvageBase".Translate());
			return stringBuilder.ToString();
			//return base.CompInspectStringExtra();
		}
	}
}