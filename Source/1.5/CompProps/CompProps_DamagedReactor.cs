using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_DamagedReactor : CompProperties
	{

		public CompProps_DamagedReactor()
		{
			this.compClass = typeof(CompDamagedReactor);
		}
	}
}