using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	class Projectile_ShipFake : Bullet
	{
		//taken from Projectile
		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			float num = this.ArcHeightFactor * GenMath.InverseParabola(this.DistanceCoveredFractionArc);
			Vector3 vector = drawLoc + new Vector3(0f, 20f, 1f) * num; //td upped to 20f, might need adjustment
			if (this.def.projectile.shadowSize > 0f)
			{
				this.DrawShadow(drawLoc, num);
			}
			Quaternion rotation = this.ExactRotation;
			if (this.def.projectile.spinRate != 0f)
			{
				float num2 = 60f / this.def.projectile.spinRate;
				rotation = Quaternion.AngleAxis((float)Find.TickManager.TicksGame % num2 / num2 * 360f, Vector3.up);
			}
			if (this.def.projectile.useGraphicClass)
			{
				this.Graphic.Draw(vector, base.Rotation, this, rotation.eulerAngles.y);
			}
			else
			{
				Graphics.DrawMesh(MeshPool.GridPlane(this.def.graphicData.drawSize), vector, rotation, this.DrawMat, 0);
			}
			base.Comps_PostDraw();
		}
	}
}
