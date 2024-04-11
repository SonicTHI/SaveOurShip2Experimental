using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
	public class CompRCSThruster : ThingComp
	{
		public CompProperties_RCSThruster Props
		{
			get { return props as CompProperties_RCSThruster; }
		}
		public bool active => powerComp != null && powerComp.PowerOn;
		public ShipHeatMapComp mapComp;
		public CompPowerTrader powerComp;
		public IntVec3 ventTo;
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			powerComp = parent.TryGetComp<CompPowerTrader>();
			mapComp = parent.Map.GetComponent<ShipHeatMapComp>();
			ventTo = (parent.Position + IntVec3.South.RotatedBy(parent.Rotation));//.ToVector3();
		}
		public override void PostDeSpawn(Map map)
		{
			mapComp = null;
			base.PostDeSpawn(map);
		}
	}
}
