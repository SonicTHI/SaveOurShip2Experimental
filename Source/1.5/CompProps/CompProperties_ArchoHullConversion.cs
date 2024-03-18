using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
	class CompProperties_ArchoHullConversion : CompProperties
	{
		public SimpleCurve radiusPerDayCurve;

		public CompProperties_ArchoHullConversion()
		{
			compClass = typeof(CompArchoHullConversion);
		}
	}
}
