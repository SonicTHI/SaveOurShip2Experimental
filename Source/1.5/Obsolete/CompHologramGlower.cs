using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace SaveOurShip2
{
	//DEPRECATED
	/*class CompHologramGlower : ThingComp
	{
		IntVec3 lastCell = IntVec3.Zero;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			parent.Map.mapDrawer.MapMeshDirty(parent.Position, MapMeshFlag.Things);
			parent.Map.glowGrid.RegisterGlower(this);
		}

		public override void PostDeSpawn(Map map)
		{
			if (map == null || map.glowGrid==null)
				return;
			map.glowGrid.DeRegisterGlower(this);
			map.mapDrawer.MapMeshDirty(parent==null ? lastCell : parent.Position, MapMeshFlag.Things);
		}

		public override void CompTick()
		{
			base.CompTick();
			if(parent.Position!=lastCell)
			{
				lastCell = parent.Position;
				parent.Map.glowGrid.MarkGlowGridDirty(parent.Position);
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<IntVec3>(ref lastCell, "lastCell");
		}
	}*/
}
