using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
	public class CompHullFoamDistributor : ThingComp
	{
		public CompProperties_HullFoamDistributor Props
		{
			get
			{
				return (CompProperties_HullFoamDistributor)props;
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
