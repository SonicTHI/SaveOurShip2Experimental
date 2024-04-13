using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	class CompUseEffect_WeatherCancel : CompUseEffect
	{
		public override void DoEffect(Pawn usedBy)
		{
			base.DoEffect(usedBy);
			if(this.parent.Map != null)
			{
				this.parent.Map.weatherManager.TransitionTo(WeatherDefOf.Clear);
				List<GameCondition> toEnd = new List<GameCondition>();
				foreach(GameCondition cond in this.parent.Map.GameConditionManager.ActiveConditions)
				{
					if (cond.def == GameConditionDefOf.ColdSnap || cond.def == GameConditionDefOf.ToxicFallout || cond.def == GameConditionDefOf.HeatWave || cond.def == GameConditionDefOf.VolcanicWinter || cond.def == GameConditionDefOf.Flashstorm)
						toEnd.Add(cond);
				}
				foreach(GameCondition cond in toEnd)
					cond.End();
				List<Tornado> tornadoes = new List<Tornado>();
				foreach(Thing t in this.parent.Map.listerThings.AllThings)
				{
					if (t is Tornado)
						tornadoes.Add((Tornado)t);
				}
				foreach (Tornado t in tornadoes)
					t.ticksLeftToDisappear = 1;
			}
			this.parent.Destroy();
		}
	}
}
