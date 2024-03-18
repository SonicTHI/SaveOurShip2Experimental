using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
	public class CompProperties_LongRangeMineralScannerSpace : CompProperties
	{
		public float minShuttleFuelPercent = 5f;

		public float maxShuttleFuelPercent = 25f;

		public float mtbDays = 9.2f;

		public float guaranteedToFindLumpAfterDaysWorking = 8f;

		public CompProperties_LongRangeMineralScannerSpace()
		{
			this.compClass = typeof(CompLongRangeMineralScannerSpace);
		}
	}
}