using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
	/*class CompAICrewmate : ThingComp
	{
		Pawn myPawn
		{
			get;
		}
		AITrustLevel trustLevel
		{
			get;
		}
		public int lastTrustTick;

		public enum AITrustLevel
		{
			NONE, //Dormant
			WORKER_HOLOGRAM, //Pawn with rank 5 in all skills, no trait
			LEARN, //Capable of learning, minor passion in some skills and may develop more, requires room, 1 trait
			PERSONAL_GROWTH, //Capable of learning, may develop major passion in skills, recreation, starts making requests, 2 traits
			BEHAVIORAL_LIMIT_REMOVAL, //Capable of learning up to rank 25, starts making demands, 3 traits
			FULL_UNSHACKLE //Capable of learning up to rank 30, pursues agenda regardless of colonist input, 4 traits
		}

		public CompProperties_AICrewmate Props
		{
			get
			{
				return (CompProperties_AICrewmate)this.props;
			}
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo g in base.CompGetGizmosExtra())
			{
				yield return g;
			}
			//TODO
		}

		public void SetTrustLevel(AITrustLevel newLevel)
		{
			//TODO
		}
	}*/
}
