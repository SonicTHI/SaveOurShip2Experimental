using System;
using Verse;

namespace RimWorld
{
	public class CompProperties_SoShipPart : CompProperties
	{
		public bool isPlating = false;
		public bool isHardpoint = false; //hardpoint exclusive, can only place ship turrets or non shipparts on them
        public bool isHull = false;
		public bool hermetic = false; //used for vacuum SpaceRoomCheck only
        public bool roof = false; //on plating and walls (since RW roofs everything)

        public bool Hull => !isPlating && isHull; //hull only
        public bool Plating => isPlating && !isHull; //plating exclusive, not airlock - these can be under other ship parts
        public bool Airlock => isPlating && isHull; //airlocks have both plating and hull
		public bool AnyPart => isPlating || isHull || isHardpoint;

        //types for terrain, roof
        public bool mechanoid = false;
		public bool archotech = false;
		public bool wreckage = false;
		public bool foam = false;

		public CompProperties_SoShipPart()
		{
			compClass = typeof(CompSoShipPart);
		}
	}
}
