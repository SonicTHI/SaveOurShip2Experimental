using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
	public class CompProperties_LongRangeMineralScannerSpaceAI : CompProperties_LongRangeMineralScannerSpace
	{
		public CompProperties_LongRangeMineralScannerSpaceAI()
		{
			mtbDays = 18.4f;
			this.compClass = typeof(CompLongRangeMineralScannerSpaceAI);
		}
	}
}