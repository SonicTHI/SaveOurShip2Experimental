using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_ShipScannerAI : CompProps_ShipScanner
	{
		public CompProps_ShipScannerAI()
		{
			mtbDays = 18.4f;
			this.compClass = typeof(CompShipScannerAI);
		}
	}
}