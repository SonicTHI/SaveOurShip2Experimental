using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class CompRCSThruster : ThingComp
	{
		public CompProps_RCSThruster Props
		{
			get { return props as CompProps_RCSThruster; }
		}
		public bool active => powerComp != null && powerComp.PowerOn;
		public ShipMapComp mapComp;
		public CompPowerTrader powerComp;
		public IntVec3 ventTo;
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			powerComp = parent.TryGetComp<CompPowerTrader>();
			mapComp = parent.Map.GetComponent<ShipMapComp>();
			ventTo = (parent.Position + IntVec3.South.RotatedBy(parent.Rotation));//.ToVector3();
		}
		public override void PostDeSpawn(Map map)
		{
			mapComp = null;
			base.PostDeSpawn(map);
		}
	}
}
