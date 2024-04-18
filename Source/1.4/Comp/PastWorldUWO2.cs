using Verse;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;

namespace SaveOurShip2
{
	//SOS2 world comp, any global vars go here, legacy name is kept for backward compatibility
	public class PastWorldUWO2 : WorldComponent
	{
		//private int ShipsHaveInsidesVersion;
		public int PlayerFactionBounty;
		public int LastSporeGiftTick;
		public List<string> Unlocks = new List<string>();
		public bool startedEndgame;
		public bool SoSWin = false;
		public bool renderedThatAlready = false;
		public List<Building_ShipAdvSensor> Sensors = new List<Building_ShipAdvSensor>();
		public bool MoveShipFlag = false;

		public PastWorldUWO2(World world) : base(world)
		{

		}

		private int nextShipId = 0;
		private int newShipId
		{
			get
			{
				nextShipId++;
				return nextShipId;
			}
		}
		public int AddNewShip(Dictionary<int, SoShipCache> ShipsOnMap, Building core)
		{
			int mergeToIndex = ShipInteriorMod2.WorldComp.newShipId;
			ShipsOnMap.Add(mergeToIndex, new SoShipCache());
			ShipsOnMap[mergeToIndex].RebuildCache(core, mergeToIndex);
			return mergeToIndex;
		}
		public override void FinalizeInit()
		{
			base.FinalizeInit();
			/*foreach (Faction f in Find.FactionManager.AllFactions)
			{
				Log.Message("fac: " + f + " defName: " + f.def.defName);
			}*/
			if (!Find.FactionManager.AllFactions.Any(f => f.def == FactionDefOf.Mechanoid))
				Log.Error("SOS2: Mechanoid faction not found! Parts of SOS2 will likely fail to function properly!");
			if (!Find.FactionManager.AllFactions.Any(f => f.def == FactionDefOf.Pirate || f.def == FactionDefOf.PirateWaster || f.def.defName.Equals("PirateYttakin")))
				Log.Warning("SOS2: Pirate faction not found! SOS2 gameplay experience will be affected.");
			if (!Find.FactionManager.AllFactions.Any(f => f.def == FactionDefOf.Insect))
				Log.Warning("SOS2: Insect faction not found! SOS2 gameplay experience will be affected.");
		}

		public override void ExposeData()
		{
			base.ExposeData();
			//Scribe_Values.Look<int>(ref ShipsHaveInsidesVersion,"SoSVersion",0);
			Scribe_Collections.Look<string>(ref Unlocks, "Unlocks", LookMode.Value);
			Scribe_Values.Look<int>(ref PlayerFactionBounty, "PlayerFactionBounty", 0);
			Scribe_Values.Look<int>(ref LastSporeGiftTick, "LastSporeGiftTick", 0);
			Scribe_Values.Look<bool>(ref startedEndgame, "StartedEndgame");

			if (Scribe.mode != LoadSaveMode.PostLoadInit)
			{
				ShipInteriorMod2.PurgeWorldComp();
			}
			/*if (Scribe.mode!=LoadSaveMode.Saving)
			{
				if(Unlocks.Contains("JTDrive")) //Back-compatibility: unlock JT drive research project if you got it before techprints were a thing
				{
					Find.ResearchManager.FinishProject(ResearchProjectDef.Named("SoSJTDrive"));
					Unlocks.Remove("JTDrive");
				}
				if (!Unlocks.Contains("JTDriveToo")) //Legacy compatibility for back when policies were different and a certain developer's head was still outside his own ass
				{
					if (!Unlocks.Contains("JTDriveResearchChecked") && Find.ResearchManager.GetProgress(ResearchProjectDef.Named("SoSJTDrive")) >= 4000) //Hey, if you've already finished this research, you deserve special commemmoration!
					{
						Unlocks.Add("JTDriveToo");
						GiveMeEntanglementManifold();
					}
					else //Let's check another way!
					{
						foreach (FieldInfo field in typeof(ResearchManager).GetFields()) //Let's randomly look at fields inside the research manager!
						{
							if (field.FieldType == typeof(Dictionary<ResearchProjectDef, int>)) //Hmm, any sort of dictionary of research projects and integers must be important!
							{
								if (((Dictionary<ResearchProjectDef, int>)field.GetValue(Find.ResearchManager)).ContainsKey(ResearchProjectDef.Named("SoSJTDrive"))) //Hey, if the JT drive gets mentioned in such an important place, maybe it means you already found one!
								{
									Unlocks.Add("JTDriveToo");
									GiveMeEntanglementManifold();
									((Dictionary<ResearchProjectDef, int>)field.GetValue(Find.ResearchManager)).Remove(ResearchProjectDef.Named("SoSJTDrive")); //Remove the NASTY EVIL FORBIDDEN DATA!
								}
							}
						}
					}
					if(!Unlocks.Contains("JTDriveResearchChecked"))
						Unlocks.Add("JTDriveResearchChecked");
				}
			}
			//recover from incorrect savestates
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (!ShipCombatManager.InCombat && !ShipCombatManager.InEncounter)
				{
					if (ShipCombatManager.EnemyShip == null
						&& (ShipCombatManager.CanSalvageEnemyShip || ShipCombatManager.ShouldSalvageEnemyShip))
					{
						Log.Error("Recovering from incorrect state regarding enemy ship in save file. If there was an enemy ship, it is now lost and cannot be salvaged.");
						ShipCombatManager.CanSalvageEnemyShip = false;
						ShipCombatManager.ShouldSalvageEnemyShip = false;
						ShipCombatManager.ShouldSkipSalvagingEnemyShip = false;
					}
				}
			}*/
		}

		/*private void GiveMeEntanglementManifold()
		{
			IncidentParms parms = new IncidentParms();
			parms.target = Find.World;
			parms.forced = true;
			QueuedIncident qi = new QueuedIncident(new FiringIncident(IncidentDef.Named("SoSFreeEntanglement"), null, parms),Find.TickManager.TicksGame, Find.TickManager.TicksGame+99999999);
			Find.Storyteller.incidentQueue.Add(qi);
		}*/
	}
}
