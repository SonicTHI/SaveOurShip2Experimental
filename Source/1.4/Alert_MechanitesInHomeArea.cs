using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
    class Alert_MechanitesInHomeArea : Alert_Critical
	{
		private Fire FireInHomeArea
		{
			get
			{
				List<Map> maps = Find.Maps;
				for (int i = 0; i < maps.Count; i++)
				{
					List<Thing> list = maps[i].listerThings.ThingsOfDef(ResourceBank.ThingDefOf.MechaniteFire);
					for (int j = 0; j < list.Count; j++)
					{
						Thing thing = list[j];
						if (maps[i].areaManager.Home[thing.Position] && !thing.Position.Fogged(thing.Map))
						{
							return (Fire)thing;
						}
					}
				}
				return null;
			}
		}

		public Alert_MechanitesInHomeArea()
		{
			defaultLabel = "MechanitesInHomeArea".Translate();
			defaultExplanation = "MechanitesInHomeAreaDesc".Translate();
		}

		public override AlertReport GetReport()
		{
			return FireInHomeArea;
		}
	}
}
