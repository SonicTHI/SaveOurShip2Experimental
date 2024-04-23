using System;
using Verse;

namespace SaveOurShip2
{
	public class CompProps_EngineTrail : CompProperties
	{
		//public GraphicData graphicData = new GraphicData();
		public int thrust = 0;
		public int fuelUse = 0;
		public int width = 0;
		public bool takeOff = false;
		public bool energy = false;
		public bool reactionless = false;
		public SoundDef soundWorking;
		public SoundDef soundStart;
		public SoundDef soundEnd;
		public CompProps_EngineTrail()
		{
			this.compClass = typeof(CompEngineTrail);
		}
	}
}

