using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_ArcholifeCosmetics : CompProperties
	{
		public List<GraphicData> graphics;
		public List<string> names;

		public CompProps_ArcholifeCosmetics()
		{
			compClass = typeof(CompArcholifeCosmetics);
		}

		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);

			if (!CompArcholifeCosmetics.GraphicsToResolve.ContainsKey(parentDef))
				CompArcholifeCosmetics.GraphicsToResolve.Add(parentDef, this);
		}
	}
}
