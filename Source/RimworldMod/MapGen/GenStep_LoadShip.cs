using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using System.IO;

namespace RimWorld
{
	public class GenStep_LoadShip : GenStep
	{
        public override int SeedPart => 42069;

        public override void Generate(Map map, GenStepParams parms)
		{
			string shipFolder = Path.Combine (GenFilePaths.SaveDataFolderPath, "Ships");
			string shipFile = Path.Combine (shipFolder, Faction.OfPlayer.Name + ".rwship");
			Scribe.loader.InitLoading (shipFile);
			List<Thing> theThings = new List<Thing> ();
			Scribe_Collections.Look<Thing> (ref theThings, "things", LookMode.Deep, new object[0]);
			foreach (Thing theThing in theThings) {
				theThing.SpawnSetup (map, false);
			}
			Scribe.loader.FinalizeLoading ();
		}
	}
}