using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_ChangeableProjectile : CompProperties
	{
		public int maxTorpedoes;
		public int tubes;

		public CompProps_ChangeableProjectile()
		{
			compClass = typeof(CompChangeableProjectile);
		}
	}
}
