using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_BuildingConsciousness : CompProperties
	{
		public bool canMergeHuman=false;
		public bool mustBeDead=false;
		public bool canMergeAI=false;
		public bool healOnMerge=false;
		public HediffDef holoHediff;
		public ThingDef holoWeapon;
		public ThingDef holoWeaponMelee;
		public ThingDef holoShield;

		public CompProps_BuildingConsciousness()
		{
			this.compClass = typeof(CompBuildingConsciousness);
		}
	}
}
