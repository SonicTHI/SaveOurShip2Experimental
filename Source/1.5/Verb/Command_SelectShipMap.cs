using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld.Planet;
using SaveOurShip2;

namespace RimWorld
{
	//mode
	[Flags]
	public enum SelectShipMapMode
	{
		scoop = 1,
		stabilize = 2,
		target = 4
	}

	public class Command_SelectShipMap : Command
	{
		public Building salvageBay;
		public int salvageBayNum;
		public Map sourceMap;
		public Map targetMap;
		public SelectShipMapMode mode = 0;

		public override void ProcessInput(Event ev)
		{
			base.ProcessInput(ev);
			SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();

			var mapComp = sourceMap.GetComponent<ShipHeatMapComp>();
			var tgtMapComp = targetMap.GetComponent<ShipHeatMapComp>();
			if (mode == SelectShipMapMode.scoop) //scoop up any thing that is not a building and not on ship
			{
				bool space = false;
				foreach (IntVec3 vec in salvageBay.OccupiedRect())
				{
					if (vec.GetThingList(sourceMap).Count < 3)
					{
						space = true;
						break;
					}
				}
				if (!space)
				{
					Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipSalvageNoSpace"), MessageTypeDefOf.NeutralEvent);
					return;
				}
				List<Thing> things = new List<Thing>();
				float mass = 0;
				int count = 0;
				foreach (IntVec3 vec in targetMap.AllCells.Where(v => !tgtMapComp.MapShipCells.ContainsKey(v)))
				{
					foreach (Thing t in targetMap.thingGrid.ThingsAt(vec))
					{
						if (mass > 500)
							break;
						if (count > 25)
							break;

						if (!t.Destroyed && !(t is Building) && t.def != ResourceBank.ThingDefOf.DetachedShipPart && !things.Contains(t) && t.def.BaseMass < 500)
						{
							if (t.def.stackLimit == 1)
								count++;

							things.Add(t);
							mass += t.GetStatValue(StatDefOf.Mass);
						}
					}
				}
				foreach (Thing t in things)
				{
					if (t.Spawned)
						t.DeSpawn();
					GenSpawn.Spawn(t, salvageBay.Position, sourceMap);
					if (t is Pawn p && !p.Dead)
						ShipInteriorMod2.AddPawnToLord(sourceMap, p);
				}
			}
			else if (mode == SelectShipMapMode.stabilize)
			{
				int bCount = targetMap.listerBuildings.allBuildingsNonColonist.Count + targetMap.listerBuildings.allBuildingsColonist.Count;
				int bMax = mapComp.MaxSalvageWeightOnMap();
				if (bCount > bMax)
				{
					Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipSalvageCount", bCount, bMax), MessageTypeDefOf.NeutralEvent);
				}
				else
				{
					float req = bCount;// * 2.5f;
					float fuel = 0f;
					List<CompEngineTrail> engines = new List<CompEngineTrail>();
					foreach (SoShipCache ship in mapComp.ShipsOnMapNew.Values)
					{
						foreach (CompEngineTrail engine in ship.Engines.Where(e => e.Props.fuelUse > 0))
						{
							engines.Add(engine);
							fuel += engine.refuelComp.Fuel;
							if (engine.PodFueled)
								fuel += engine.refuelComp.Fuel;
						}
					}
					Log.Message("SOS2: ".Colorize(Color.cyan) + " fuel/req: " + fuel +"/"+req);
					if (fuel > req)
					{
						Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(TranslatorFormattedStringExtensions.Translate("ShipSalvageStablizeConfirm", targetMap.Parent.Label, req), delegate
						{
							foreach (CompEngineTrail engine in engines)
							{
								float consume = req * engine.refuelComp.Fuel / fuel;
								if (engine.PodFueled)
									consume *= 0.5f;
								engine.refuelComp.ConsumeFuel(Mathf.Min(consume, engine.refuelComp.Fuel));
							}
							targetMap.Parent.GetComponent<TimedForcedExitShip>().ticksLeftToForceExitAndRemoveMap += 60000;
							float adj = Rand.Range(0.025f, 0.075f);
							((WorldObjectOrbitingShip)targetMap.Parent).Theta = ((WorldObjectOrbitingShip)sourceMap.Parent).Theta + adj;
						}));
					}
					else
					{
						Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipSalvageStablizeFuel", req), MessageTypeDefOf.NeutralEvent);
					}
				}
			}
		}
	}
}
