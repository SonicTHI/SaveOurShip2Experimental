using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using System.Linq;

namespace RimWorld
{
	public class SpaceNavyDef : Def
	{
		public List<FactionDef> factionDefs = new List<FactionDef>();
		public List<EnemyShipDef> enemyShipDefs = new List<EnemyShipDef>();
		public Color colorPrimary;
		public Color colorSecondary;
		public bool canOperateAfterFactionDefeated = true;
		public bool bountyHunts;
		public bool pirates;

		public PawnKindDef crewDef;
		public PawnKindDef marineDef;
		public PawnKindDef marineHeavyDef;

		public string GetUniqueLoadID()
		{
			return "SpaceNavy_" + defName;
		}
	}
}