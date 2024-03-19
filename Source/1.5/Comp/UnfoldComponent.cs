using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
	/// <summary>
	/// Currently only handles 1x1 with a 3 long extension. Should probably read from def, too. Will get there...
	/// </summary>
	public class UnfoldComponent : ThingComp
	{
		public float extension = 0.0f;
		private int timeTillRetract;
		private float target = 0.0f;
		Rot4 rot;

		public CompProperties_Unfold Props
		{
			get
			{
				return (CompProperties_Unfold)this.props;
			}
		}

		public override void PostExposeData()
		{
			Scribe_Values.Look<float>(ref this.target, "target");
			Scribe_Values.Look<float>(ref this.extension, "extension");
		}
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			rot = this.parent.Rotation;
		}
		public override void PostDraw()
		{
			base.PostDraw();
			if (extension == 0.0f)
				return;
			if (this.parent is Building_ShipAirlock airlock)
			{
				if (airlock.firstRot == -1)
					return;
				rot = new Rot4(airlock.firstRot);
			}	
			Matrix4x4 matrix = new Matrix4x4();
			matrix.SetTRS(this.parent.DrawPos + (Props.extendDirection.RotatedBy(rot).ToVector3() * Props.startOffset) + (Props.extendDirection.RotatedBy(rot).ToVector3() * (Props.length / 2) * extension) + Altitudes.AltIncVect, rot.AsQuat, new Vector3(Props.width, 1f, Props.length * extension));
			if (Props.width != 1 && (rot.AsByte == 0 || rot.AsByte == 2))
				Graphics.DrawMesh(MeshPool.plane10, matrix, Props.unfoldGraphicAlt, 0);
			else
				Graphics.DrawMesh(MeshPool.plane10, matrix, Props.unfoldGraphic, 0);
		}

		public override string CompInspectStringExtra()
		{
			if (parent is Building_ShipAirlock)
				return "";
			StringBuilder stringBuilder = new StringBuilder();
			//stringBuilder.Append(base.CompInspectStringExtra());
			stringBuilder.Append(TranslatorFormattedStringExtensions.Translate("UnfoldStatus"));
		   
			if(Mathf.Approximately(extension, Target))
			{
				if (Mathf.Approximately(extension, 0.0f))
				{
					stringBuilder.Append(TranslatorFormattedStringExtensions.Translate("UnfoldRetracted"));
				}
				else
				{
					stringBuilder.Append(TranslatorFormattedStringExtensions.Translate("UnfoldExtended"));
				}
			}
			else
			{
				if(extension < Target)
				{
					stringBuilder.Append(TranslatorFormattedStringExtensions.Translate("UnfoldExtending"));
				}
				else if(extension > Target)
				{
					if(Mathf.Approximately(timeTillRetract, 0.0f))
					{
						stringBuilder.Append(TranslatorFormattedStringExtensions.Translate("UnfoldRetracting"));
					}
					else
					{
						stringBuilder.Append(TranslatorFormattedStringExtensions.Translate("UnfoldExtended"));
					}
				}
			}
			return stringBuilder.ToString();
		}

		public override void CompTick()
		{
			base.CompTick();
			if (Target > extension)
			{
				extension += Props.extendRate;
				if (extension > Target)
					extension = Target;
				timeTillRetract = Props.retractTime;
			}
			else if (Target < extension)
			{
				timeTillRetract -= 1;
				if (timeTillRetract <= 0)
				{
					extension -= Props.retractRate;
					if (extension < Target)
						extension = Target;
					timeTillRetract = 0;
				}
			}
			else
			{
				timeTillRetract = Props.retractTime;
			}
		}

		public float Target {
			set
			{
				target = value;
				if (target > 1.0f) target = 1.0f;
				if (target < 0.0f) target = 0.0f;
			}
			get
			{
				return target;
			}
		}

		public bool IsAtTarget
		{
			get
			{
				return Mathf.Approximately(extension, Target);
			}
		}
	}
}
