using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
	public class Graphic_LinkedShipConduit : Graphic_Linked
	{
		public Graphic_LinkedShipConduit(Graphic subGraphic)
			: base(subGraphic)
		{
		}

		public override bool ShouldLinkWith(IntVec3 c, Thing parent)
		{
			if (!c.InBounds(parent.Map))
			{
				return false;
			}
			if (base.ShouldLinkWith(c, parent) || c.GetFirstThingWithComp<CompShipHeat>(parent.Map)!=null)
			{
				return true;
			}
			return false;
		}

		public override void Print(SectionLayer layer, Thing thing, float extraRotation)
		{
			base.Print(layer, thing, extraRotation);
			for (int i = 0; i < 4; i++)
			{
				IntVec3 intVec = thing.Position + GenAdj.CardinalDirections[i];
				if (intVec.InBounds(thing.Map))
				{
					ThingWithComps transmitter = intVec.GetFirstThingWithComp<CompShipHeat>(thing.Map);
					if (transmitter != null && !transmitter.def.graphicData.Linked)
					{
						Material mat = LinkedDrawMatFrom(thing, intVec);
						Printer_Plane.PrintPlane(layer, intVec.ToVector3ShiftedWithAltitude(thing.def.Altitude), Vector2.one, mat);
					}
				}
			}
		}
	}
}
