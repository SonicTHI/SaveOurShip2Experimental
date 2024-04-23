using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SaveOurShip2
{
	class CompProps_Archolife : CompProperties
	{
		public float shield;
		public bool purr = false;
		public bool scintillate = false;

		public CompProps_Archolife()
		{
			this.compClass = typeof(CompArcholife);
		}
	}
}
