using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
	class Alert_ArchotechSporeMoodLow : Alert_Critical
	{
		private Building_ArchotechSpore SadSpore
		{
			get
			{
				List<Map> maps = Find.Maps;
				for (int i = 0; i < maps.Count; i++)
				{
					List<Thing> list = maps[i].listerThings.ThingsOfDef(ResourceBank.ThingDefOf.ShipArchotechSpore);
					for (int j = 0; j < list.Count; j++)
					{
						Building_ArchotechSpore spore = list[j] as Building_ArchotechSpore;
						if(spore!=null && spore.Mood < 0.5f)
						{
							return spore;
						}
					}
				}
				return null;
			}
		}

		public Alert_ArchotechSporeMoodLow()
		{
			defaultLabel = "ArchotechSporeMoodLow".Translate();
			defaultExplanation = ModsConfig.RoyaltyActive ? "ArchotechSporeMoodLowDesc".Translate() : "ArchotechSporeMoodLowDescNoRoyalty".Translate();
		}

		public override AlertReport GetReport()
		{
			return SadSpore;
		}
	}
}
