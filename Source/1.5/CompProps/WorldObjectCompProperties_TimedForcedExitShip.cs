using System;
using System.Collections.Generic;
using RimWorld.Planet;

namespace RimWorld
{
	public class WorldObjectCompProperties_TimedForcedExitShip : WorldObjectCompProperties
	{
		public WorldObjectCompProperties_TimedForcedExitShip()
		{
			this.compClass = typeof(TimedForcedExitShip);
		}

		public override IEnumerable<string> ConfigErrors(WorldObjectDef parentDef)
		{
			foreach (string text in base.ConfigErrors(parentDef))
			{
				yield return text;
			}
			
			if (!typeof(MapParent).IsAssignableFrom(parentDef.worldObjectClass))
			{
				yield return parentDef.defName + " has WorldObjectCompProperties_TimedForcedExit but it's not MapParent.";
			}
			yield break;
		}
	}
}
