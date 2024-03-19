using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

using SaveOurShip2;

namespace RimWorld.Planet
{
	/// <summary>
	/// Adds a countdown to set ShipMapState.burnUpSet. Once set shipWO removes map when possibe.
	/// </summary>
	public class TimedForcedExitShip : WorldObjectComp
	{
		public int ticksLeftToForceExitAndRemoveMap = -1;
		public bool ForceExitAndRemoveMapCountdownActive
		{
			get
			{
				return ticksLeftToForceExitAndRemoveMap >= 0;
			}
		}
		public string ForceExitAndRemoveMapCountdownTimeLeftString
		{
			get
			{
				if (!ForceExitAndRemoveMapCountdownActive)
				{
					return "";
				}
				return GetForceExitAndRemoveMapCountdownTimeLeftString(ticksLeftToForceExitAndRemoveMap);
			}
		}
		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref ticksLeftToForceExitAndRemoveMap, "ticksLeftToForceExitAndRemoveMapShip", -1, false);
		}
		public void ResetForceExitAndRemoveMapCountdown()
		{
			ticksLeftToForceExitAndRemoveMap = -1;
			if (parent.Biome != null && parent.Biome == ResourceBank.BiomeDefOf.OuterSpaceBiome && parent.GetComponent<TimeoutComp>() != null)
				ticksLeftToForceExitAndRemoveMap = parent.GetComponent<TimeoutComp>().TicksLeft;
		}
		public void StartForceExitAndRemoveMapCountdown()
		{
			StartForceExitAndRemoveMapCountdown(60000);
		}
		public void StartForceExitAndRemoveMapCountdown(int duration)
		{
			ticksLeftToForceExitAndRemoveMap = duration;
		}
		public override string CompInspectStringExtra()
		{
			if (ForceExitAndRemoveMapCountdownActive)
			{
				return "ShipForceExitAndRemoveMapCountdown".Translate(ForceExitAndRemoveMapCountdownTimeLeftString) + ".";
			}
			return null;
		}
		public override void CompTick()
		{
			MapParent mapParent = (MapParent)parent;
			if (ForceExitAndRemoveMapCountdownActive)
			{
				if (mapParent.HasMap)
				{
					ticksLeftToForceExitAndRemoveMap--;
					if (ticksLeftToForceExitAndRemoveMap <= 0)
					{
						ForceReform(mapParent);
						return;
					}
				}
				else
				{
					ticksLeftToForceExitAndRemoveMap = -1;
				}
			}
		}
		public static string GetForceExitAndRemoveMapCountdownTimeLeftString(int ticksLeft)
		{
			if (ticksLeft < 0)
			{
				return "";
			}
			return ticksLeft.ToStringTicksToPeriod(true, false, true, true);
		}
		public static void ForceReform(MapParent mapParent)
		{
			if (mapParent.Map.IsSpace())
			{
				mapParent.Map.GetComponent<ShipHeatMapComp>().ShipMapState = ShipMapState.burnUpSet;
			}
		}
	}
}
