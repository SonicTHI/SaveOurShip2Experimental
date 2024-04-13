using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_BlackBoxConsole : CompProperties
	{

		public CompProps_BlackBoxConsole()
		{
			this.compClass = typeof(CompBlackBoxConsole);
		}
	}
}