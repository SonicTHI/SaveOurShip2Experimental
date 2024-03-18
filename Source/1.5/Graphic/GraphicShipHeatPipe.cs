using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
	class GraphicShipHeatPipe : Graphic_Linked
	{
		public override bool ShouldLinkWith(IntVec3 c, Thing parent)
		{
			if(GenGrid.InBounds(c, parent.Map))
			{
				return parent.Map.GetComponent<ShipHeatMapComp>().grid[parent.Map.cellIndices.CellToIndex(c)] != -1;
			}
			return false;
		}
	}
}
