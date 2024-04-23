using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SaveOurShip2
{
	class Building_ShipConduit : Building
	{
		public override Graphic Graphic => CompShipHeat.ShipHeatGraphic;
	}
}
