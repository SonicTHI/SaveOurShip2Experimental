using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
	class Graphic_256_Wreckage : Graphic
	{
		protected Graphic subGraphic;

		public override Material MatSingle
		{
			get
			{
				return MaterialAtlasPool256.SubMaterialFromAtlas(this.subGraphic.MatSingle, 0);
			}
		}

		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return new Graphic_256_Wreckage(this.subGraphic.GetColoredVersion(newShader, newColor, newColorTwo))
			{
				data = this.data
			};
		}

		public override void Print(SectionLayer layer, Thing thing, float extraRotation)
		{
			Material mat = this.LinkedDrawMatFrom(thing, thing.Position);
			Printer_Plane.PrintPlane(layer, thing.TrueCenter(), new Vector2(1f, 1f), mat, 0f, false, null, null, 0.01f, 0f);
		}

		public Material MatSingleFor(Thing thing, Vector3 loc)
		{
			return this.LinkedDrawMatFrom(thing, loc.ToIntVec3());
		}

		protected Material LinkedDrawMatFrom(Thing parent, IntVec3 cell)
		{
			DetachedShipPart part = (DetachedShipPart)parent;
			return MaterialAtlasPool256.SubMaterialFromAtlas(this.subGraphic.MatSingleFor(parent), ((cell.x - Mathf.RoundToInt(part.drawOffset.x)) % 16) + (16 * ((cell.z - Mathf.RoundToInt(part.drawOffset.z)) % 16)));
		}

		public Graphic_256_Wreckage(Graphic subGraphic)
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
			loc += this.DrawOffset(rot);
			Material material = this.MatSingleFor(thing, loc);
			Graphics.DrawMesh(arg_4D_0, loc, quaternion, material, 0);
			if (this.ShadowGraphic != null)
			{
				this.ShadowGraphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
			}
		}
	}

	public static class MaterialAtlasPool256
	{
		private class MaterialAtlas256
		{
			protected Material[] subMats = new Material[256];

			public MaterialAtlas256(Material newRootMat)
			{
				Vector2 mainTextureScale = new Vector2(0.0625f, 0.0625f);
				for (int i = 0; i < 256; i++)
				{
					float x = (float)(i % 16) * 0.0625f;
					float y = (float)(i / 16) * 0.0625f;
					Vector2 mainTextureOffset = new Vector2(x, y);
					Material material = new Material(newRootMat);//(Material)typeof(MaterialAtlasPool).Assembly.GetType("MaterialAllocator", true).GetMethod("Create", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { newRootMat });
					material.name = newRootMat.name + "_ASM" + i;
					material.mainTextureScale = mainTextureScale;
					material.mainTextureOffset = mainTextureOffset;
					this.subMats[i] = material;
				}
			}

			public Material SubMat(int which)
			{
				if (which >= this.subMats.Length)
				{
					Log.Warning("Cannot get submat of index " + which + ": out of range.");
					return BaseContent.BadMat;
				}
				return this.subMats[which];
			}
		}

		private static Dictionary<Material, MaterialAtlasPool256.MaterialAtlas256> atlasDict = new Dictionary<Material, MaterialAtlasPool256.MaterialAtlas256>();

		public static Material SubMaterialFromAtlas(Material mat, int which)
		{
			if (!MaterialAtlasPool256.atlasDict.ContainsKey(mat))
			{
				MaterialAtlasPool256.atlasDict.Add(mat, new MaterialAtlasPool256.MaterialAtlas256(mat));
			}
			return MaterialAtlasPool256.atlasDict[mat].SubMat(which);
		}
	}
}
