using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
	public class CompProperties_ShipLifeSupport : CompProperties
	{
		public CompProperties_ShipLifeSupport()
		{
			compClass = typeof(CompShipLifeSupport);
		}
	}
}
