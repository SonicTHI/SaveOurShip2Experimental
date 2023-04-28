using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
	public class CompProperties_ArcholifeCosmetics : CompProperties
	{
		public List<GraphicData> graphics;
		public List<string> names;

		public CompProperties_ArcholifeCosmetics()
		{
			this.compClass = typeof(CompArcholifeCosmetics);
		}

		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);

			if (!CompArcholifeCosmetics.GraphicsToResolve.ContainsKey(parentDef))
				CompArcholifeCosmetics.GraphicsToResolve.Add(parentDef, this);
		}
	}
}
