using System;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_DockExtender : CompProperties
	{
		public bool extender = false;
		public bool isPlating = false;
		public CompProps_DockExtender()
		{
			this.compClass = typeof(CompDockExtender);
		}
	}
}

