using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class CompProps_PowerPlantSolarShip : CompProperties_Power
	{
		public float bonusPower;

		public CompProps_PowerPlantSolarShip()
		{
			this.compClass = typeof(CompPowerPlantSolarShip);
		}
	}
}
