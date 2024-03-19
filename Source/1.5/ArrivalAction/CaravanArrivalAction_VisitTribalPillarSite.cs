using System;
using System.Collections.Generic;
using Verse;

namespace RimWorld.Planet
{
	public class CaravanArrivalAction_VisitTribalPillarSite : CaravanArrivalAction
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

		public CaravanArrivalAction_VisitTribalPillarSite()
		{
		}

		public CaravanArrivalAction_VisitTribalPillarSite(TribalPillarSiteComp escapeShip)
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
			return CaravanArrivalAction_VisitTribalPillarSite.CanVisit(caravan, this.target);
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
			Map orGenerateMap = GetOrGenerateMapUtility.GetOrGenerateMap(this.target.Tile, new IntVec3(250, 1, 250), null);
			Pawn t = caravan.PawnsListForReading[0];
			CaravanEnterMapUtility.Enter(caravan, orGenerateMap, CaravanEnterMode.Edge, CaravanDropInventoryMode.UnloadIndividually, false, null);
			Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("LetterLabelCaravanEnteredMap",this.target), TranslatorFormattedStringExtensions.Translate("LetterCaravanEnteredMap",caravan.Label, this.target).CapitalizeFirst(), LetterDefOf.NeutralEvent, t, null, null);
		}

		public static FloatMenuAcceptanceReport CanVisit(Caravan caravan, MapParent escapeShip)
		{
			return true;
		}

		public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, MapParent escapeShip)
		{
			return CaravanArrivalActionUtility.GetFloatMenuOptions<CaravanArrivalAction_VisitTribalPillarSite>(() => CaravanArrivalAction_VisitTribalPillarSite.CanVisit(caravan, escapeShip), () => new CaravanArrivalAction_VisitTribalPillarSite(escapeShip.GetComponent<TribalPillarSiteComp>()), TranslatorFormattedStringExtensions.Translate("VisitEscapeShip",escapeShip.Label), caravan, escapeShip.Tile, escapeShip);
		}
	}
}