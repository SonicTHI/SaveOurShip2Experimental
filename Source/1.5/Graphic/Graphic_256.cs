using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
	class Graphic_256 : Graphic
	{
		protected Graphic subGraphic;

		public override Material MatSingle
		{
			get
			{
				return MaterialAtlasPool256.SubMaterialFromAtlas(this.subGraphic.MatSingle, 255);
			}
		}

		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return new Graphic_256(this.subGraphic.GetColoredVersion(newShader, newColor, newColorTwo))
			{
				data = this.data
			};
		}

		public override void Print(SectionLayer layer, Thing thing, float extraRotation)
		{
			Material mat = this.MatSingleFor(thing);
			Printer_Plane.PrintPlane(layer, thing.TrueCenter(), new Vector2(1f, 1f), mat, 0f, false, null, null, 0.01f, 0f);
		}

		public override Material MatSingleFor(Thing thing)
		{
			if(thing.GetRoom()!=null)
				return MaterialAtlasPool256.SubMaterialFromAtlas(this.subGraphic.MatSingleFor(thing), (thing.GetRoom().ID * 16 + (thing.Position.x % 16) + (16 * (thing.Position.z % 16))) % 256);
			return MaterialAtlasPool256.SubMaterialFromAtlas(this.subGraphic.MatSingleFor(thing), (thing.Position.x % 16) + (16 * (thing.Position.z % 16)));
		}

		public Graphic_256(Graphic subGraphic)
		{
			this.subGraphic = subGraphic;
		}

		public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
		{
			Mesh arg_4D_0 = this.MeshAt(rot);
			Quaternion quaternion = this.QuatFromRot(rot);
			if (extraRotation != 0f)
			{
				quaternion *= Quaternion.Euler(Vector3.up * extraRotation);
			}
			Material material = this.MatSingleFor(thing);
			Graphics.DrawMesh(arg_4D_0, loc, quaternion, material, 0);
			if (this.ShadowGraphic != null)
			{
				this.ShadowGraphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
			}
		}
	}
}
