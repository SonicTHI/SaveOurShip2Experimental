using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_Overlay : CompProperties
	{
		public Vector3 size;
		[NoTranslate]
		public string graphicPath;
		[Unsaved]
		public Material overlayGraphic;

		public CompProps_Overlay()
		{
			this.compClass = typeof(CompOverlay);
		}

		public override void ResolveReferences(ThingDef parentDef)
		{
			base.ResolveReferences(parentDef);
			LongEventHandler.ExecuteWhenFinished((Action)(() => this.overlayGraphic = MaterialPool.MatFrom(this.graphicPath)));
		}
	}
}
