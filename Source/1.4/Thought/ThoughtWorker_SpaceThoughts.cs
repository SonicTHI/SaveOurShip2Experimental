using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
	public class ThoughtWorker_SpaceThoughts : ThoughtWorker
	{
		protected override ThoughtState CurrentStateInternal(Pawn p)
		{
			if(p.Map.terrainGrid.TerrainAt(p.Position) == ResourceBank.TerrainDefOf.EmptySpace)
			{
				if(p.story.traits.HasTrait(TraitDefOf.Undergrounder) || p.story.traits.HasTrait(TraitDef.Named("Wimp"))) {
					return ThoughtState.ActiveAtStage(3);
				} else
				{
					return ThoughtState.ActiveAtStage(2);
				}
			} else if(p.Map.terrainGrid.TerrainAt(IntVec3.Zero) == ResourceBank.TerrainDefOf.EmptySpace)
			{
				if (p.story.traits.HasTrait(TraitDefOf.Undergrounder) || p.story.traits.HasTrait(TraitDef.Named("Wimp"))) {
					return ThoughtState.ActiveAtStage(1);
				}
				else
				{
					return ThoughtState.ActiveAtStage(0);
				}
			}

			return ThoughtState.Inactive;
		}
	}
}
