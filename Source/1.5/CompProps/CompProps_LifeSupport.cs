using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_LifeSupport : CompProperties
	{
		public CompProps_LifeSupport()
		{
			compClass = typeof(CompShipLifeSupport);
		}
	}
}
