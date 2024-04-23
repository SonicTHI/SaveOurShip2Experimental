using System;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_ResearchUnlock : CompProperties
	{
		public string unlock;

		public CompProps_ResearchUnlock()
		{
			this.compClass = typeof(CompResearchUnlock);
		}
	}
}
