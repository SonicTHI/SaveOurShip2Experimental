using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
    class Designator_RestoreHull : Designator_Cells
    {
		public override int DraggableDimensions => 2;

		public override bool DragDrawMeasurements => true;

		public  Designator_RestoreHull()
		{
			useMouseIcon = true;
			soundDragSustain = SoundDefOf.Designate_DragStandard;
			soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			soundSucceeded = SoundDefOf.Designate_SmoothSurface; 
			defaultLabel = "SoSRestoreHull".Translate();
			defaultDesc = "SoSRestoreHullDesc".Translate();
			icon = ContentFinder<Texture2D>.Get("UI/RestoreHull");
		}

		public override void SelectedUpdate()
		{
			GenUI.RenderMouseoverBracket();
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            List<Thing> things = loc.GetThingList(Map);
			if (things.Any(thing => thing.def == ResourceBank.ThingDefOf.ShipHullTileWrecked || thing.def == ResourceBank.ThingDefOf.Ship_Beam_Wrecked || thing.def == ResourceBank.ThingDefOf.ShipAirlockWrecked || thing.def == ResourceBank.ThingDefOf.ShipHullfoamTile || thing.def == ResourceBank.ThingDefOf.HullFoamWall))
			{
				if (things.Any(thing => thing.def.IsFrame || thing is Blueprint))
					return "SoSAlreadyRestoring".Translate();
				return AcceptanceReport.WasAccepted;
			}
            return "SoSMustDesignateWreckage".Translate();
        }

        public override void DesignateThing(Thing t)
        {

		}

        public override void DesignateSingleCell(IntVec3 c)
        {
			List<Thing> things = c.GetThingList(Map).ToList();
			bool replaced=false;
			foreach(Thing thing in things)
            {
				if (thing.def == ResourceBank.ThingDefOf.ShipAirlockWrecked)
                {
					ReplaceWithBlueprint(thing, ResourceBank.ThingDefOf.ShipAirlock);
					replaced = true;
					break;
                }
			}
			if (!replaced)
			{
				foreach (Thing thing in things)
				{
					if (thing.def == ResourceBank.ThingDefOf.Ship_Beam_Wrecked || thing.def == ResourceBank.ThingDefOf.HullFoamWall)
					{
						ReplaceWithBlueprint(thing, ResourceBank.ThingDefOf.Ship_Beam);
						replaced = true;
						break;
					}
				}
			}
			if (!replaced)
			{
				foreach (Thing thing in things)
				{
					if (thing.def == ResourceBank.ThingDefOf.ShipHullTileWrecked || thing.def == ResourceBank.ThingDefOf.ShipHullfoamTile)
					{
						ReplaceWithBlueprint(thing, ResourceBank.ThingDefOf.ShipHullTile);
						replaced = true;
						break;
					}
				}
			}
		}

		void ReplaceWithBlueprint(Thing t, ThingDef replacer)
        {
			if(DebugSettings.godMode)
            {
				Thing replacement = ThingMaker.MakeThing(replacer);
				replacement.SetFactionDirect(Faction.OfPlayer);
				replacement = GenSpawn.Spawn(replacement, t.Position, Map);
            }
			else
				GenConstruct.PlaceBlueprintForBuild(replacer, t.Position, Map, Rot4.North, Faction.OfPlayer, null);
			FleckMaker.ThrowMetaPuffs(GenAdj.OccupiedRect(t.Position, Rot4.North, replacer.Size), base.Map);
		}
    }
}