using System;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_ShipBay : CompProperties
	{
		public bool beam = false;
		public int weight = 5000;
		public int maxShuttleSize = 1;
		public CompProps_ShipBay()
		{
			compClass = typeof(CompShipBay);
		}
	}
}

