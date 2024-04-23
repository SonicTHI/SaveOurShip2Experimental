using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

using RimWorld.BaseGen;

namespace SaveOurShip2
{
	public class SymbolResolver_Interior_SpaceLab : SymbolResolver
	{
		public override void Resolve(ResolveParams rp)
		{
			ResolveParams resolveParams = rp;
			resolveParams.singleThingDef = ThingDef.Named("ShipChunkSalvage");
			resolveParams.thingRot = new Rot4?((!Rand.Bool) ? Rot4.East : Rot4.North);
			int? fillWithThingsPadding = rp.fillWithThingsPadding;
			resolveParams.fillWithThingsPadding = new int?((!fillWithThingsPadding.HasValue) ? 1 : fillWithThingsPadding.Value);
			BaseGen.symbolStack.Push("fillWithThingsNoClear", resolveParams);
		}
	}
}