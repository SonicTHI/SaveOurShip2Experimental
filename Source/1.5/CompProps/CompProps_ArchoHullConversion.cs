using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SaveOurShip2
{
	class CompProps_ArchoHullConversion : CompProperties
	{
		public SimpleCurve radiusPerDayCurve;

		public CompProps_ArchoHullConversion()
		{
			compClass = typeof(CompArchoHullConversion);
		}
	}
}
