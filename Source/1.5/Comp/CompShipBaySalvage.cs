using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Verse.AI;

namespace SaveOurShip2
{
	public class CompShipBaySalvage : CompShipBay
	{
		private ShipMapComp mapComp;
		public int SalvageWeight => Props.weight;
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			mapComp = parent.Map.GetComponent<ShipMapComp>();
		}
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo item in base.CompGetGizmosExtra())
			{
				yield return item;
			}
			if ((parent.Faction != Faction.OfPlayer || !mapComp.IsPlayerShipMap) && !(Prefs.DevMode && ShipInteriorMod2.HasSoS2CK))
				yield break;

			bool nominal = mapComp.ShipMapState == ShipMapState.nominal;
			foreach (Map map in Find.Maps)
			{
				var targetMapComp = map.GetComponent<ShipMapComp>();
				if (targetMapComp.ShipMapState != ShipMapState.isGraveyard)
					continue;

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
					if (!nominal)
					{
						beam.Disable(TranslatorFormattedStringExtensions.Translate("SoS.SalvageDisabled"));
					}
					yield return beam;
				}
				if (targetMapComp.MapShipCells.Any())
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
					if (!nominal)
					{
						retrieveShipEnemy.Disable(TranslatorFormattedStringExtensions.Translate("SoS.SalvageDisabled"));
					}
					yield return retrieveShipEnemy;
				}
				if (!map.Parent.GetComponent<TimedForcedExitShip>().stabilized)
				{
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
					if (!nominal)
					{
						stablizeShipEnemy.Disable(TranslatorFormattedStringExtensions.Translate("SoS.SalvageDisabled"));
					}
					yield return stablizeShipEnemy;
				}
			}
			Command_TargetWreck moveWreck = new Command_TargetWreck
			{
				groupable = false,
				salvageBay = (Building)parent,
				sourceMap = parent.Map,
				targetMap = parent.Map,
				icon = ContentFinder<Texture2D>.Get("UI/SalvageMove"),
				defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.MoveWreckCommand"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.MoveWreckCommandDesc")
			};
			Command_TargetWreck moveWreckFlip = new Command_TargetWreck
			{
				groupable = false,
				salvageBay = (Building)parent,
				sourceMap = parent.Map,
				targetMap = parent.Map,
				rotb = 2,
				icon = ContentFinder<Texture2D>.Get("UI/SalvageMoveFlip"),
				defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.MoveWreckFlipCommand"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.MoveWreckFlipCommandDesc")
			};
			Command_TargetWreck moveWreckRot = new Command_TargetWreck
			{
				groupable = false,
				salvageBay = (Building)parent,
				sourceMap = parent.Map,
				targetMap = parent.Map,
				rotb = 1,
				icon = ContentFinder<Texture2D>.Get("UI/SalvageMoveRot"),
				defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.MoveWreckRotCommand"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.MoveWreckRotCommandDesc")
			};
			Command_Action claim = new Command_Action
			{
				action = delegate
				{
					mapComp.Claim();
				},
				defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ClaimWrecksCommand"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ClaimWrecksCommandDesc"),
				icon = ContentFinder<Texture2D>.Get("UI/SalvageClaim")
			};
			Command_TargetShipRemove removeTargetWreck = new Command_TargetShipRemove
			{
				//abandon target wreck (rem rock floor)
				groupable = false,
				targetMap = parent.Map,
				position = parent.Position,
				defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.RemoveWrecksCommand"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.RemoveWrecksCommandDesc"),
				icon = ContentFinder<Texture2D>.Get("UI/SalvageCancel")
			};
			if (!nominal || !mapComp.CanClaimNow(Faction.OfPlayer))
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
		
		public override void CompTickRare()
		{
			base.CompTickRare();
		}
		/*public override string CompInspectStringExtra()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("SoS.SalvageBase".Translate());
			return stringBuilder.ToString();
			//return base.CompInspectStringExtra();
		}*/
	}
}