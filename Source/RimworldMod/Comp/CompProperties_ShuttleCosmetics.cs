using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorld
{
	public class CompProperties_ShuttleCosmetics : CompProperties
	{
		public List<GraphicData> graphics;
		public List<GraphicData> graphicsHover;
		public List<string> names;

		public CompProperties_ShuttleCosmetics()
		{
			this.compClass = typeof(CompShuttleCosmetics);
		}

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);

			if(!CompShuttleCosmetics.GraphicsToResolve.ContainsKey(parentDef))
				CompShuttleCosmetics.GraphicsToResolve.Add(parentDef,this);
		}
    }
}
