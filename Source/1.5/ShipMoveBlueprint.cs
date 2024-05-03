using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using static RimWorld.Sketch;

namespace SaveOurShip2
{
	[StaticConstructorOnStartup]
	class ShipMoveBlueprint : Thing
	{
		public static readonly Color GhostColor = new Color(0.7f, 0.7f, 0.7f, 0.35f);
		public static readonly Color ConflictColor = new Color(0.7f, 0.7f, 0.2f, 0.35f);
		public static readonly Color ExtenderColor = new Color(0.3f, 0.7f, 0.8f, 0.65f);
		public static readonly Color BlockedColor = new Color(0.8f, 0.2f, 0.2f, 0.35f);
		public Sketch shipSketch;
		public Sketch conflictSketch;
		public Sketch extenderSketch;

		ShipMoveBlueprint() {}

		public ShipMoveBlueprint(Sketch sketchShip, Sketch sketchConflict, Sketch sketchExtender)
		{
			shipSketch = sketchShip;
			if (sketchConflict != null)
				conflictSketch = sketchConflict;
			if (sketchExtender != null)
				extenderSketch = sketchExtender;
			def = ResourceBank.ThingDefOf.ShipMoveBlueprint;
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			shipSketch.DrawGhost(drawLoc.ToIntVec3(), Sketch.SpawnPosType.Unchanged, false, null);
			if (conflictSketch != null)
				DrawGhost(conflictSketch, ConflictColor, drawLoc.ToIntVec3(), Sketch.SpawnPosType.Unchanged, false, null);
			if (extenderSketch != null)
				DrawGhost(extenderSketch, ExtenderColor, drawLoc.ToIntVec3(), Sketch.SpawnPosType.Unchanged, false, null);
		}

		public void DrawGhost(IntVec3 drawLoc, bool flip = false)
		{
			shipSketch.DrawGhost(drawLoc, Sketch.SpawnPosType.Unchanged, false, null);
			if (conflictSketch != null)
				DrawGhost(conflictSketch, ConflictColor, drawLoc, Sketch.SpawnPosType.Unchanged, false, null);
			if (extenderSketch != null)
				DrawGhost(extenderSketch, ExtenderColor, drawLoc, Sketch.SpawnPosType.Unchanged, false, null);
		}
		public void DrawGhost(Sketch sketch, Color ghostColor, IntVec3 pos, SpawnPosType posType = SpawnPosType.Unchanged, bool placingMode = false, Thing thingToIgnore = null, Func<SketchEntity, IntVec3, List<Thing>, Map, bool> validator = null)
		{
			List<Thing> tmpSketchThings = new List<Thing>();
			IntVec3 offset = sketch.GetOffset(pos, posType);
			Map currentMap = Find.CurrentMap;
			bool flag = false;
			foreach (SketchEntity entity in sketch.Entities)
			{
				if (!entity.OccupiedRect.MovedBy(offset).InBounds(currentMap))
				{
					flag = true;
					break;
				}
			}

			foreach (SketchBuildable buildable in sketch.Buildables)
			{
				Thing spawnedBlueprintOrFrame = buildable.GetSpawnedBlueprintOrFrame(buildable.pos + offset, currentMap);
				SketchThing sketchThing;
				if (spawnedBlueprintOrFrame != null)
				{
					tmpSketchThings.Add(spawnedBlueprintOrFrame);
				}
				else if ((sketchThing = buildable as SketchThing) != null)
				{
					Thing sameSpawned = sketchThing.GetSameSpawned(sketchThing.pos + offset, currentMap);
					if (sameSpawned != null)
					{
						tmpSketchThings.Add(sameSpawned);
					}
				}
			}

			CellRect cellRect = Find.CameraDriver.CurrentViewRect.ExpandedBy(1).ClipInsideMap(Find.CurrentMap);
			foreach (SketchEntity entity2 in sketch.Entities)
			{
				if ((placingMode || !entity2.IsSameSpawnedOrBlueprintOrFrame(entity2.pos + offset, currentMap)) && entity2.OccupiedRect.MovedBy(offset).InBounds(currentMap))
				{
					Color color = ((flag || (entity2.IsSpawningBlocked(entity2.pos + offset, currentMap, thingToIgnore) && !entity2.IsSameSpawnedOrBlueprintOrFrame(entity2.pos + offset, currentMap)) || (validator != null && !validator(entity2, offset, tmpSketchThings, Find.CurrentMap))) ? BlockedColor : ghostColor);
					if (cellRect.Contains(entity2.pos + offset))
					{
						entity2.DrawGhost(entity2.pos + offset, color);
					}
				}
			}
			tmpSketchThings.Clear();
		}
	}
}
