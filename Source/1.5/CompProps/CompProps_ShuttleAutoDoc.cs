using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_ShuttleAutoDoc : CompProperties
	{
		public FloatRange tendQualityRange;

		public CompProps_ShuttleAutoDoc()
		{
			this.compClass = typeof(CompShuttleAutoDoc);
		}
	}
}