using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using SaveOurShip2;

namespace RimWorld
{
	public class IncidentWorker_SpaceDebris : IncidentWorker
	{
		protected override bool CanFireNowSub(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			if (map.gameConditionManager.ConditionIsActive(ResourceBank.GameConditionDefOf.SpaceDebris) || map.GetComponent<ShipHeatMapComp>().ShipMapState != ShipMapState.nominal)
				return false;
			return true;
		}

		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			int duration = Mathf.RoundToInt(def.durationDays.RandomInRange * 60000f);
			GameCondition_SpaceDebris gameCondition_SpaceDebris = (GameCondition_SpaceDebris)GameConditionMaker.MakeCondition(ResourceBank.GameConditionDefOf.SpaceDebris, duration);
			int angle = Rand.RangeInclusive(0, 3);
			gameCondition_SpaceDebris.angle = angle;
			gameCondition_SpaceDebris.asteroids = Rand.Chance(0.3f);
			IntVec3 spawnCell;
			if (angle == 0)
			{
				spawnCell = new IntVec3(map.Size.x / 2, 0, map.Size.z - 1);
			}
			else if (angle == 1)
			{
				spawnCell = new IntVec3(map.Size.x - 1, 0, map.Size.z / 2);
			}
			else if (angle == 2)
			{
				spawnCell = new IntVec3(map.Size.x / 2, 0, 0);
			}
			else
			{
				spawnCell = new IntVec3(0, 0, map.Size.z / 2);
			}
			map.gameConditionManager.RegisterCondition(gameCondition_SpaceDebris);
			if (gameCondition_SpaceDebris.asteroids)
				base.SendStandardLetter(TranslatorFormattedStringExtensions.Translate("SoS.SpaceAsteroids"), TranslatorFormattedStringExtensions.Translate("SoS.SpaceAsteroidsDesc"), def.letterDef, parms, new TargetInfo(spawnCell, map, false), Array.Empty<NamedArgument>());
			else
				base.SendStandardLetter(def.letterLabel, def.letterText, def.letterDef, parms, new TargetInfo(spawnCell, map, false), Array.Empty<NamedArgument>());
			//ResourceBank.GameConditionDefOf.SpaceDebris.letterText
			/*
			int num = map.listerBuildings.allBuildingsColonist.Count / 10;
			ChoiceLetter_SpaceDebris choiceLetter_SpaceDebris = (ChoiceLetter_SpaceDebris)LetterMaker.MakeLetter(this.def.letterLabel, "LetterSpaceDebris".Translate("1", "2", num), this.def.letterDef, null, null);
			choiceLetter_SpaceDebris.title = "LetterLabelSpaceDebris".Translate(map.Parent.Label);
			choiceLetter_SpaceDebris.radioMode = true;
			choiceLetter_SpaceDebris.map = map;
			choiceLetter_SpaceDebris.fee = num;
			//choiceLetter_RansomDemand.quest = parms.quest;
			choiceLetter_SpaceDebris.StartTimeout(60000);
			Find.LetterStack.ReceiveLetter(choiceLetter_SpaceDebris, null);*/
			return true;
		}
	}
}
