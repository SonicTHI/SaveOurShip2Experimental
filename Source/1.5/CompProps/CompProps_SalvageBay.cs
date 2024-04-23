using System;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_SalvageBay : CompProperties
	{
		public bool beam = false;
		public int weight = 5000;
		public CompProps_SalvageBay()
		{
			compClass = typeof(CompShipSalvageBay);
		}
	}
}

