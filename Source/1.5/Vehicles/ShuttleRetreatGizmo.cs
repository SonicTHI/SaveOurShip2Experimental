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

namespace SaveOurShip2.Vehicles
{
	[StaticConstructorOnStartup]
    class ShuttleRetreatGizmo : Gizmo
    {
        CompShuttleLauncher shuttle;

		float selectedHealthTarget = -1f;

		bool draggingBar;

		private static readonly Texture2D HealthTex = SolidColorMaterials.NewSolidColorTexture(ColorLibrary.BlueGreen);

		private static readonly Texture2D HealthHighlightTex = SolidColorMaterials.NewSolidColorTexture(ColorLibrary.BrightBlue);

		private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));

		private static readonly Texture2D HealthTargetTex = SolidColorMaterials.NewSolidColorTexture(ColorLibrary.Blue);

		float RetreatAtHealth
		{
			get
			{
				if (!draggingBar)
				{
					return shuttle.retreatAtHealth;
				}
				return selectedHealthTarget;
			}
		}

		public ShuttleRetreatGizmo(CompShuttleLauncher launcher)
        {
            shuttle = launcher;
        }

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
		{
			Rect rect = new Rect(topLeft.x, topLeft.y, 212, 75);
			Rect rect2 = rect.ContractedBy(6f);
			Widgets.DrawWindowBackground(rect);
			Rect rect3 = rect2;
			float curY = rect3.yMin;
			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(rect3.x, ref curY, rect3.width, "SoS.RetreatAtHealth".Translate() + ": " + RetreatAtHealth.ToStringPercent());
			Text.Font = GameFont.Small;
			if (Mouse.IsOver(rect2) && !draggingBar)
			{
				Widgets.DrawHighlight(rect2);
				TooltipHandler.TipRegion(rect2, () => "SoS.RetreatAtHealthDesc".Translate(), 9493937);
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
			float health = shuttle.Vehicle.HitPoints / shuttle.Vehicle.MaxHitPoints;
			Widgets.FillableBar(rect, health, flag ? HealthHighlightTex : HealthTex, EmptyBarTex, doBorder: true);
			float num = Mathf.Clamp(Mathf.Round((Event.current.mousePosition.x - (rect.x + 3f)) / (rect.width - 8f) * 20f) / 20f, 0f, 1f);
			Event current2 = Event.current;
			if (current2.type == EventType.MouseDown && current2.button == 0 && flag)
			{
				selectedHealthTarget = num;
				shuttle.retreatAtHealth = selectedHealthTarget;
				draggingBar = true;
				SoundDefOf.DragSlider.PlayOneShotOnCamera();
				current2.Use();
			}
			if (current2.type == EventType.MouseDrag && current2.button == 0 && draggingBar && flag)
			{
				if (Mathf.Abs(num - selectedHealthTarget) > float.Epsilon)
				{
					SoundDefOf.DragSlider.PlayOneShotOnCamera();
				}
				selectedHealthTarget = num;
				shuttle.retreatAtHealth = selectedHealthTarget;
				current2.Use();
			}
			DrawHealthTarget(rect, RetreatAtHealth);
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.Label(rect, shuttle.retreatAtHealth.ToStringPercent());
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
		}

		private void DrawHealthTarget(Rect rect, float percent)
		{
			float num = Mathf.Round((rect.width - 8f) * percent);
			GUI.DrawTexture(new Rect(rect.x + 3f + num, rect.y, 2f, rect.height), HealthTargetTex);
			float num2 = UIScaling.AdjustCoordToUIScalingFloor(rect.x + 2f + num);
			float xMax = UIScaling.AdjustCoordToUIScalingCeil(num2 + 4f);
			Rect rect2 = default(Rect);
			rect2.y = rect.y - 3f;
			rect2.height = 5f;
			rect2.xMin = num2;
			rect2.xMax = xMax;
			Rect rect3 = rect2;
			GUI.DrawTexture(rect3, HealthTargetTex);
			Rect position = rect3;
			position.y = rect.yMax - 2f;
			GUI.DrawTexture(position, HealthTargetTex);
		}

        public override float GetWidth(float maxWidth)
        {
            return 212;
        }
    }
}
