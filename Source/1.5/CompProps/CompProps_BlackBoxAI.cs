using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_BlackBoxAI : CompProperties
	{

		public CompProps_BlackBoxAI()
		{
			this.compClass = typeof(CompBlackBoxAI);
		}
	}
}