using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
	public class CompProperties_Overlay : CompProperties
	{
		public Vector3 size;
		[NoTranslate]
		public string graphicPath;
		[Unsaved]
		public Material overlayGraphic;

		public CompProperties_Overlay()
		{
			this.compClass = typeof(OverlayComponent);
		}

		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);
			LongEventHandler.ExecuteWhenFinished((Action)(() => this.overlayGraphic = MaterialPool.MatFrom(this.graphicPath)));
		}
	}
}
