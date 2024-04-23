using System.Linq;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class IncidentWorker_SpacePod : IncidentWorker
	{
		protected override bool CanFireNowSub(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			if (map.GetComponent<ShipMapComp>().ShipMapState != ShipMapState.nominal)
				return false;
			if (map.listerBuildings.allBuildingsColonist.Any(t => t.TryGetComp<CompShipSalvageBay>() != null))
				return true;
			return false;
		}

		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			ChoiceLetter_SpacePod choiceLetter_SpacePod = (ChoiceLetter_SpacePod)LetterMaker.MakeLetter(def.letterLabel, def.letterText, def.letterDef, null, null);
			choiceLetter_SpacePod.map = (Map)parms.target;
			choiceLetter_SpacePod.StartTimeout(6000);
			Find.LetterStack.ReceiveLetter(choiceLetter_SpacePod, null);
			return true;
		}
	}
}
