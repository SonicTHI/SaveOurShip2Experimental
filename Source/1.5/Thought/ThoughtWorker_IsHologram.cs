using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
	class ThoughtWorker_IsHologram : ThoughtWorker
	{
		protected override ThoughtState CurrentStateInternal(Pawn p)
		{
			if (ShipInteriorMod2.IsHologram(p))
				return ThoughtState.ActiveDefault;
			return false;
		}
	}
}
