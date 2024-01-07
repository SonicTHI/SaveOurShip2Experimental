using System;
using Verse;

namespace RimWorld
{
	public class CompProperties_SalvageBay : CompProperties
    {
        public bool archo = false;
        public int weight = 5000;
        public CompProperties_SalvageBay()
		{
			compClass = typeof(CompShipSalvageBay);
		}
	}
}

