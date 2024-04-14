using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using RimWorld;

namespace SaveOurShip2
{
	public class Building_ShipForceField : Building
	{
		public override bool BlocksPawn(Pawn p)
		{
			return false;
		}
	}
}
