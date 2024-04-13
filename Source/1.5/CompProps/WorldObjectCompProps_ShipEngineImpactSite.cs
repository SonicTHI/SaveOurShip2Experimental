using System;
using RimWorld;

namespace SaveOurShip2
{
	public class WorldObjectCompProps_ShipEngineImpactSite : WorldObjectCompProperties
	{

		public WorldObjectCompProps_ShipEngineImpactSite()
		{
			this.compClass = typeof(ImpactSiteComp);
		}
	}
}