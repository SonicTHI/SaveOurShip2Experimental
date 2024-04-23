using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class ThingSetMaker_ArchotechGift : ThingSetMaker_MarketValue
	{
		protected override IEnumerable<ThingDef> AllowedThingDefs(ThingSetMakerParams parms)
		{
			List<ThingDef> defs = new List<ThingDef>();
			foreach(ArchotechGiftDef def in DefDatabase<ArchotechGiftDef>.AllDefs)
			{
				if(def.research.IsFinished)
				{
					defs.Add(def.thing);
				}
			}
			if(ModLister.BiotechInstalled && DefDatabase<ResearchProjectDef>.GetNamed("ArchotechArchites").IsFinished)
			{
				defs.Add(ThingDefOf.ArchiteCapsule);
				defs.Add(ThingDefOf.Genepack);
			}
			return defs;
		}

		protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
		{
			base.Generate(parms, outThings);
			if (ModLister.BiotechInstalled)
			{
				bool hasArchites = false;
				foreach (Thing t in outThings)
				{
					if (t.def == ThingDefOf.ArchiteCapsule)
						hasArchites = true;
				}
				if(hasArchites)
				{
					Genepack pack = new Genepack();
					pack.def = ThingDefOf.Genepack;
					pack.Initialize(new List<GeneDef> { DefDatabase<GeneDef>.AllDefs.Where(gene => gene.biostatArc > 0).RandomElement() });
					outThings.Add(pack);
				}	
			}
		}
	}
}
