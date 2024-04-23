using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class CompProps_Unfold : CompProperties
	{
		public float extendRate;
		public float retractRate;
		public int retractTime;
		public IntVec3 extendDirection;
		public float startOffset;
		public float length;
		public float width=1;
		[NoTranslate]
		public string graphicPath;
		public string graphicPathAlt;
		[Unsaved]
		public Material unfoldGraphic;
		public Material unfoldGraphicAlt;

		public CompProps_Unfold()
		{
			this.compClass = typeof(CompUnfold);
		}

		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);
			LongEventHandler.ExecuteWhenFinished((Action)(() => this.unfoldGraphic = MaterialPool.MatFrom(this.graphicPath)));
			LongEventHandler.ExecuteWhenFinished((Action)(() => this.unfoldGraphicAlt = MaterialPool.MatFrom(this.graphicPathAlt)));
		}

		public override void DrawGhost(IntVec3 center, Rot4 rot, ThingDef thingDef, Color ghostCol, AltitudeLayer drawAltitude, Thing thing)
		{
			base.DrawGhost(center, rot, thingDef, ghostCol, drawAltitude);
			if (!thingDef.defName.StartsWith("ShipAirlock"))
				GraphicDatabase.Get<Graphic_Single>(graphicPath, ShaderTypeDefOf.EdgeDetect.Shader, new Vector2(1, 3), ghostCol)
				.DrawFromDef(GenThing.TrueCenter(center, rot, thingDef.Size, drawAltitude.AltitudeFor()) + (IntVec3.South * 2).RotatedBy(rot).ToVector3(), rot, thingDef);

			
		}
	}
}
