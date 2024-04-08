using System;
using System.Collections.Generic;
using Verse;

namespace RimWorld.Planet
{
	public class CaravanArrivalAction_VisitImpactSite : CaravanArrivalAction
	{
		private MapParent target;

		public override string Label
		{
			get
			{
				return TranslatorFormattedStringExtensions.Translate("VisitEscapeShip",this.target.Label);
			}
		}

		public override string ReportString
		{
			get
			{
				return TranslatorFormattedStringExtensions.Translate("CaravanVisiting",this.target.Label);
			}
		}

		public CaravanArrivalAction_VisitImpactSite()
		{
		}

		public CaravanArrivalAction_VisitImpactSite(ImpactSiteComp escapeShip)
		{
			this.target = (MapParent)escapeShip.parent;
		}

		public override FloatMenuAcceptanceReport StillValid(Caravan caravan, int destinationTile)
		{
			FloatMenuAcceptanceReport floatMenuAcceptanceReport = base.StillValid(caravan, destinationTile);
			if (!floatMenuAcceptanceReport)
			{
				return floatMenuAcceptanceReport;
			}
			if (this.target != null && this.target.Tile != destinationTile)
			{
				return false;
			}
			return CaravanArrivalAction_VisitImpactSite.CanVisit(caravan, this.target);
		}

		public override void Arrived(Caravan caravan)
		{
			if (!this.target.HasMap)
			{
				LongEventHandler.QueueLongEvent(delegate
				{
					this.DoArrivalAction(caravan);
				}, "GeneratingMapForNewEncounter", false, null);
			}
			else
			{
				this.DoArrivalAction(caravan);
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_References.Look<MapParent>(ref this.target, "target", false);
		}

		private void DoArrivalAction(Caravan caravan)
		{
			bool flag = !this.target.HasMap;
			if (flag)
			{
				this.target.SetFaction(Faction.OfPlayer);
			}
			Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(this.target.Tile, null);
			Pawn t = caravan.PawnsListForReading[0];
			CaravanEnterMapUtility.Enter(caravan, orGenerateMap, CaravanEnterMode.Edge, CaravanDropInventoryMode.UnloadIndividually, false, null);
			if (flag)
			{
				Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
				Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("ImpactSiteFoundLabel"), TranslatorFormattedStringExtensions.Translate("ImpactSiteFound"), LetterDefOf.PositiveEvent, new GlobalTargetInfo(this.target.Map.Center, this.target.Map, false), null, null);
			}
			else
			{
				Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("LetterLabelCaravanEnteredMap",this.target), TranslatorFormattedStringExtensions.Translate("LetterCaravanEnteredMap",caravan.Label, this.target).CapitalizeFirst(), LetterDefOf.NeutralEvent, t, null, null);
			}
		}

		public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, MapParent escapeShip)
		{
			return true;
		}

		public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, MapParent escapeShip)
		{
			return CaravanArrivalActionUtility.GetFloatMenuOptions<CaravanArrivalAction_VisitImpactSite>(() => CaravanArrivalAction_VisitImpactSite.CanVisit(caravan, escapeShip), () => new CaravanArrivalAction_VisitImpactSite(escapeShip.GetComponent<ImpactSiteComp>()), TranslatorFormattedStringExtensions.Translate("VisitEscapeShip",escapeShip.Label), caravan, escapeShip.Tile, escapeShip);
		}
	}
}