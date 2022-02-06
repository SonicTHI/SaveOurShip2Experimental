using System;
using Verse;

namespace RimWorld
{
	public class CompProperties_RoofMe : CompProperties
	{
        public bool wreckage=false;
        public bool roof = true;
        public bool mechanoid = false;
		public bool archotech = false;
		public bool foam = false;

		public CompProperties_RoofMe()
		{
			this.compClass = typeof(CompRoofMe);
		}
	}
}
