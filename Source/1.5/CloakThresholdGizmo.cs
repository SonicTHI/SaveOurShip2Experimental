using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SaveOurShip2
{
	[StaticConstructorOnStartup]
    class CloakThresholdGizmo : Gizmo
    {
		Building_ShipCloakingDevice cloak;

		float breakCloakAtHeat = 1f;

		bool draggingBar;

		Vector2 topLeft;

		private static readonly Texture2D CloakTex = SolidColorMaterials.NewSolidColorTexture(ColorLibrary.BrickRed);

		private static readonly Texture2D CloakHighlightTex = SolidColorMaterials.NewSolidColorTexture(ColorLibrary.Red);

		private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));

		private static readonly Texture2D CloakTargetTex = SolidColorMaterials.NewSolidColorTexture(ColorLibrary.DarkRed);

		float BreakCloakAtHeat
		{
			get
			{
				if (!draggingBar)
				{
					return cloak.breakCloakAtHeat;
				}
				return breakCloakAtHeat;
			}
		}

		public CloakThresholdGizmo(Building_ShipCloakingDevice cloak)
        {
			this.cloak = cloak;
        }

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			this.topLeft = topLeft;
			Rect rect = new Rect(topLeft.x, topLeft.y, 212, 75);
			Rect rect2 = rect.ContractedBy(6f);
			Widgets.DrawWindowBackground(rect);
			Rect rect3 = rect2;
			float curY = rect3.yMin;
			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(rect3.x, ref curY, rect3.width, "SoS.BreakCloakAtHeat".Translate() + ": " + BreakCloakAtHeat.ToStringPercent());
			Text.Font = GameFont.Small;
			if (Mouse.IsOver(rect2) && !draggingBar)
			{
				Widgets.DrawHighlight(rect2);
				TooltipHandler.TipRegion(rect2, () => "SoS.BreakCloakAtHeatDesc".Translate(), 9493937);
			}
			DrawBar(rect2, curY);
			return new GizmoResult(GizmoState.Clear);
		}

		private void DrawBar(Rect inRect, float curY)
		{
			Rect rect = inRect;
			rect.xMin += 10f;
			rect.xMax -= 10f;
			rect.yMax = inRect.yMax - 4f;
			rect.yMin = curY + 10f;
			bool flag = Mouse.IsOver(rect);
			Widgets.FillableBar(rect, BreakCloakAtHeat, flag ? CloakHighlightTex : CloakTex, EmptyBarTex, doBorder: true);
			float num = Mathf.Clamp(Mathf.Round((Event.current.mousePosition.x - (rect.x + 3f)) / (rect.width - 8f) * 20f) / 20f, 0f, 1f);
			Event current2 = Event.current;
			if (current2.type == EventType.MouseDown && current2.button == 0 && flag)
			{
				breakCloakAtHeat = num;
				foreach (Building_ShipCloakingDevice cloak in this.cloak.mapComp.Cloaks)
					cloak.breakCloakAtHeat = breakCloakAtHeat;
				draggingBar = true;
				SoundDefOf.DragSlider.PlayOneShotOnCamera();
				current2.Use();
			}
			if (current2.type == EventType.MouseDrag && current2.button == 0 && draggingBar && flag)
			{
				if (Mathf.Abs(num - breakCloakAtHeat) > float.Epsilon)
				{
					SoundDefOf.DragSlider.PlayOneShotOnCamera();
				}
				breakCloakAtHeat = num;
				foreach (Building_ShipCloakingDevice cloak in this.cloak.mapComp.Cloaks)
					cloak.breakCloakAtHeat = breakCloakAtHeat;
				current2.Use();
			}
			DrawHealthTarget(rect, BreakCloakAtHeat);
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect, cloak.breakCloakAtHeat.ToStringPercent());
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
		}

		private void DrawHealthTarget(Rect rect, float percent)
		{
			float num = Mathf.Round((rect.width - 8f) * percent);
			GUI.DrawTexture(new Rect(rect.x + 3f + num, rect.y, 2f, rect.height), CloakTargetTex);
			float num2 = UIScaling.AdjustCoordToUIScalingFloor(rect.x + 2f + num);
			float xMax = UIScaling.AdjustCoordToUIScalingCeil(num2 + 4f);
			Rect rect2 = default(Rect);
			rect2.y = rect.y - 3f;
			rect2.height = 5f;
			rect2.xMin = num2;
			rect2.xMax = xMax;
			Rect rect3 = rect2;
			GUI.DrawTexture(rect3, CloakTargetTex);
			Rect position = rect3;
			position.y = rect.yMax - 2f;
			GUI.DrawTexture(position, CloakTargetTex);
		}

        public override float GetWidth(float maxWidth)
        {
            return 212;
        }

        public override bool GroupsWith(Gizmo other)
        {
			return other is CloakThresholdGizmo;
        }
    }
}
