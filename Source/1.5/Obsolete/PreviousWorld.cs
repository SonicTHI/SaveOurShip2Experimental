using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
	//dep planet travel save
	/*public class PreviousWorld : IExposable //Light(er)weight version of World, with no processing time wasted on it
	{
		public WorldInfo info = new WorldInfo();
		public List<WorldComponent> components = new List<WorldComponent>();
		public WorldObjectsHolder worldObjects;
		public WorldFeatures features;
		public WorldGrid grid;
		public List<Faction> myFactions = new List<Faction>();
		public List<ScenPart> scenario = new List<ScenPart>();
		public string scenarioName;
		public string scenarioSummary;
		public string scenarioDescription;
		public Faction donatedFaction=null;
		public float donatedAmount = 0f;

		public void ExposeData()
		{
			if(grid!=null && (grid.tileIDToNeighbors_offsets==null || grid.tileIDToNeighbors_offsets.Count==0)) //To fix the "grid saving" issue
			{
				PlanetShapeGenerator.Generate(10, out grid.verts, out grid.tileIDToVerts_offsets, out grid.tileIDToNeighbors_offsets, out grid.tileIDToNeighbors_values, 100f, grid.viewCenter, grid.viewAngle);
			}
			Scribe_Deep.Look<WorldInfo>(ref this.info, "info", new object[0]);
			Scribe_Deep.Look<WorldGrid>(ref this.grid, "grid", new object[0]);

			//Saakra's code to fix loading issues
			if(Scribe.mode == LoadSaveMode.LoadingVars)
			{
				if(this.grid.tiles.Count==0)
				{
					for(int i=0; i<this.grid.TilesCount;i++)
					{
						this.grid.tiles.Add(new Tile());
					}
				}
			}
			if(Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				typeof(WorldGrid).GetMethod("RawDataToTiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(this.grid, new object[] { });
			}

			Scribe_Collections.Look<Faction>(ref this.myFactions, "factions",LookMode.Deep);
			Scribe_Deep.Look<WorldObjectsHolder>(ref this.worldObjects, "worldObjects", new object[0]);
			Scribe_Deep.Look<WorldFeatures>(ref this.features, "features", new object[0]);
			Scribe_Collections.Look<WorldComponent>(ref this.components, "components", LookMode.Deep, new object[]
			{
				null
			});
			Scribe.EnterNode("scenario");
			Scribe_Values.Look<string>(ref this.scenarioName, "name");
			Scribe_Values.Look<string>(ref this.scenarioSummary, "summary");
			Scribe_Values.Look<string>(ref this.scenarioDescription, "description");
			Scribe_Collections.Look<ScenPart>(ref this.scenario, "parts", LookMode.Deep, new object[0]);
			Scribe.ExitNode();
			Scribe_References.Look<Faction>(ref this.donatedFaction, "donatedFaction");
			Scribe_Values.Look<float>(ref this.donatedAmount, "donatedAmount");
		}
	}*/
}