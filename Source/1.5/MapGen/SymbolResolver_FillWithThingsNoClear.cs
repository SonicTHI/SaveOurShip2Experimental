using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld.BaseGen
{
	public class SymbolResolver_FillWithThingsNoClear : SymbolResolver
	{
		public override bool CanResolve(ResolveParams rp)
		{
			if (!base.CanResolve(rp))
			{
				return false;
			}
			if (rp.singleThingToSpawn != null)
			{
				return false;
			}
			if (rp.singleThingDef != null)
			{
				Rot4? thingRot = rp.thingRot;
				Rot4 rot = (!thingRot.HasValue) ? Rot4.North : thingRot.Value;
				IntVec3 zero = IntVec3.Zero;
				IntVec2 size = rp.singleThingDef.size;
				GenAdj.AdjustForRotation(ref zero, ref size, rot);
				if (rp.rect.Width < size.x || rp.rect.Height < size.z)
				{
					return false;
				}
			}
			return true;
		}

		public override void Resolve(ResolveParams rp)
		{
			ThingDef arg_3A_0;
			if ((arg_3A_0 = rp.singleThingDef) == null)
			{
				arg_3A_0 = (from x in ThingSetMakerUtility.allGeneratableItems
							where x.IsWeapon || x.IsMedicine || x.IsDrug
							select x).RandomElement<ThingDef>();
			}
			ThingDef thingDef = arg_3A_0;
			Rot4? thingRot = rp.thingRot;
			Rot4 rot = (!thingRot.HasValue) ? Rot4.North : thingRot.Value;
			IntVec3 zero = IntVec3.Zero;
			IntVec2 size = thingDef.size;
			int? fillWithThingsPadding = rp.fillWithThingsPadding;
			int num = (!fillWithThingsPadding.HasValue) ? 0 : fillWithThingsPadding.Value;
			if (num < 0)
			{
				num = 0;
			}
			GenAdj.AdjustForRotation(ref zero, ref size, rot);
			if (size.x <= 0 || size.z <= 0)
			{
				Log.Error("Thing has 0 size.");
				return;
			}
			for (int i = rp.rect.minX; i <= rp.rect.maxX - size.x + 1; i += size.x + num)
			{
				for (int j = rp.rect.minZ; j <= rp.rect.maxZ - size.z + 1; j += size.z + num)
				{
					ResolveParams resolveParams = rp;
					resolveParams.rect = new CellRect(i, j, size.x, size.z);
					resolveParams.singleThingDef = thingDef;
					resolveParams.thingRot = new Rot4?(rot);
					BaseGen.symbolStack.Push("thing", resolveParams);
				}
			}
		}
	}
}