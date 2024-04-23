using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_ShipScanner : CompProperties
	{
		public float minShuttleFuelPercent = 5f;

		public float maxShuttleFuelPercent = 25f;

		public float mtbDays = 9.2f;

		public float guaranteedToFindLumpAfterDaysWorking = 8f;

		public CompProps_ShipScanner()
		{
			this.compClass = typeof(CompShipScanner);
		}
	}
}