using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SaveOurShip2
{
	public class CompHullFoamDistributor : ThingComp
	{
		public CompProps_HullFoamDistributor Props
		{
			get
			{
				return (CompProps_HullFoamDistributor)props;
			}
		}

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
		}

		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
		}
	}
}
