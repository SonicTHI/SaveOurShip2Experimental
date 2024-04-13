using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_HullFoamDistributor : CompProperties
	{
		public CompProps_HullFoamDistributor()
		{
			compClass = typeof(CompHullFoamDistributor);
		}
	}
}
