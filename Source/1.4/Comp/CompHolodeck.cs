using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorld
{
	[StaticConstructorOnStartup]
	class CompHolodeck : ThingComp
	{
		public SkillDef CurSkill=null;
		static Dictionary<SkillDef, HolodeckProgram> programs = new Dictionary<SkillDef, HolodeckProgram>();
		Graphic[,] randomThings;
		bool[,] sameRoom;

		struct HolodeckProgram
		{
			public Graphic mainTerrain;
			public List<Graphic> randomObjects;
			public string programName;
			public float density;

			public HolodeckProgram(Graphic mainTerrain, List<Graphic> randomObjects, string programName, float density)
			{
				this.mainTerrain = mainTerrain;
				this.randomObjects = randomObjects;
				this.programName = programName;
				this.density = density;
			}
		}

		static CompHolodeck()
		{
			List<Graphic> tmp = new List<Graphic>();
			tmp.Add(GraphicDatabase.Get<Graphic_Single>("UI/Overlays/LaunchableMouseAttachment"));
			programs.Add(SkillDefOf.Shooting, new HolodeckProgram(GraphicDatabase.Get<Graphic_Single>("HolodeckStars"), tmp, "Space Battle", 0.2f));

			tmp = new List<Graphic>();
			tmp.Add(GraphicDatabase.Get<Graphic_Single>("ForPrisoners"));
			programs.Add(SkillDefOf.Melee, new HolodeckProgram(new Graphic_256(TerrainDefOf.PavedTile.graphic), tmp, "Fight Club", 0.25f));

			tmp = new List<Graphic>();
			tmp.Add(GraphicDatabase.Get<Graphic_Single>("CassandraIcon"));
			tmp.Add(GraphicDatabase.Get<Graphic_Single>("PhoebeIcon"));
			tmp.Add(GraphicDatabase.Get<Graphic_Single>("RandyIcon"));
			programs.Add(SkillDefOf.Social, new HolodeckProgram(new Graphic_256(TerrainDefOf.WaterShallow.graphic), tmp, "Hot Spring", 0.2f));

			tmp = new List<Graphic>();
			foreach(PawnKindDef def in DefDatabase<PawnKindDef>.AllDefs)
			{
				if(def.race.race.Animal && def.lifeStages.Count>0)
					tmp.Add(def.lifeStages.Last().bodyGraphicData.Graphic);
			}
			programs.Add(SkillDefOf.Animals, new HolodeckProgram(new Graphic_256(TerrainDefOf.Soil.graphic), tmp, "Exotic Menagerie", 0.25f));

			tmp = new List<Graphic>();
			tmp.Add(GraphicDatabase.Get<Graphic_Single>("Things/Item/Resource/Gold/Gold_c"));
			tmp.Add(ThingDef.Named("Kidney").graphic);
			programs.Add(SkillDefOf.Medicine, new HolodeckProgram(new Graphic_256(TerrainDefOf.MetalTile.graphic.GetColoredVersion(ShaderTypeDefOf.Cutout.Shader,Color.gray,Color.white)), tmp, "Organ Tycoon", 0.5f));

			tmp = new List<Graphic>();
			tmp.Add(GraphicDatabase.Get<Graphic_Single>("Things/Building/Misc/Campfire_MenuIcon"));
			programs.Add(SkillDefOf.Cooking, new HolodeckProgram(new Graphic_256(TerrainDefOf.Sand.graphic), tmp, "Beach Cookout", 0.2f));

			tmp = new List<Graphic>();
			tmp.Add(ThingDefOf.Column.graphic.GetColoredVersion(ShaderTypeDefOf.Cutout.Shader, ThingDefOf.Silver.stuffProps.color,Color.white));
			programs.Add(SkillDefOf.Construction, new HolodeckProgram(new Graphic_256(TerrainDefOf.MetalTile.graphic.GetColoredVersion(ShaderTypeDefOf.Cutout.Shader, ThingDefOf.Gold.stuffProps.color, Color.white)), tmp, "Golden Palace", 0.25f));

			tmp = new List<Graphic>();
			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
			{
				if (def.plant != null)
					tmp.Add(def.graphic);
			}
			programs.Add(SkillDefOf.Plants, new HolodeckProgram(new Graphic_256(TerrainDefOf.Soil.graphic), tmp, "Enchanted Garden", 1f));

			tmp = new List<Graphic>();
			foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
			{
				if (def.mineable)
					tmp.Add(def.graphic);
			}
			programs.Add(SkillDefOf.Mining, new HolodeckProgram(new Graphic_256(TerrainDefOf.FlagstoneSandstone.graphic), tmp, "Caveworld", 0.2f));

			tmp = new List<Graphic>();
			tmp.Add(ThingDef.Named("SculptureSmall").graphic);
			tmp.Add(ThingDef.Named("SculptureLarge").graphic);
			programs.Add(SkillDefOf.Artistic, new HolodeckProgram(new Graphic_256(TerrainDefOf.TileSandstone.graphic), tmp, "Art Museum", 0.25f));

			tmp = new List<Graphic>();
			foreach (PawnKindDef def in DefDatabase<PawnKindDef>.AllDefs)
			{
				if (def.race.race.IsMechanoid && !def.defName.Contains("Shuttle") && def.lifeStages.Count>0)
					tmp.Add(def.lifeStages.Last().bodyGraphicData.Graphic);
			}
			programs.Add(SkillDefOf.Crafting, new HolodeckProgram(new Graphic_256(TerrainDefOf.MetalTile.graphic.GetColoredVersion(ShaderTypeDefOf.Cutout.Shader,ThingDefOf.Steel.stuffProps.color,Color.white)), tmp, "Mechanoid Hive", 0.2f));

			tmp = new List<Graphic>();
			tmp.Add(GraphicDatabase.Get<Graphic_Single>("Things/Mote/SpeechSymbols/KindWords"));
			programs.Add(SkillDefOf.Intellectual, new HolodeckProgram(GraphicDatabase.Get<Graphic_Single>("RoughAlphaAdd"), tmp, "Psychedelia", 0.2f));
		}

		public void StartHolodeck(Pawn pawn)
		{
			List<SkillDef> burningSkills = new List<SkillDef>();
			List<SkillDef> passionSkills = new List<SkillDef>();
			List<SkillDef> otherSkills = new List<SkillDef>();
			foreach(SkillRecord rec in pawn.skills.skills)
			{
				if (rec.passion == Passion.Major)
					burningSkills.Add(rec.def);
				else if (rec.passion == Passion.Minor)
					passionSkills.Add(rec.def);
				else if (!rec.TotallyDisabled)
					otherSkills.Add(rec.def);
			}
			if(Rand.Chance(0.5f) && burningSkills.Count>0)
			{
				CurSkill = burningSkills.RandomElement();
			}
			else if(Rand.Chance(0.75f) && passionSkills.Count>0)
			{
				CurSkill = passionSkills.RandomElement();
			}
			else
			{
				CurSkill = otherSkills.RandomElement();
			}

			ShuffleGraphics();
		}

		void ShuffleGraphics()
		{
			if(!parent.Map.reservationManager.AllReservedThings().Contains(parent) || CurSkill==null)
			{
				StopHolodeck();
				return;
			}
			randomThings = new Graphic[parent.def.building.watchBuildingStandRectWidth, parent.def.building.watchBuildingStandRectWidth];
			sameRoom = new bool[parent.def.building.watchBuildingStandRectWidth, parent.def.building.watchBuildingStandRectWidth];
			for (int x = 0; x < parent.def.building.watchBuildingStandRectWidth; x++)
			{
				for (int z = 0; z < parent.def.building.watchBuildingStandRectWidth; z++)
				{
					if (parent.GetRoom() == RegionAndRoomQuery.RoomAt(parent.Position + new IntVec3(x - parent.def.building.watchBuildingStandRectWidth / 2, 0, z - parent.def.building.watchBuildingStandRectWidth / 2), parent.Map))
					{
						if (Rand.Chance(programs[CurSkill].density))
						{
							randomThings[x, z] = programs[CurSkill].randomObjects.RandomElement();
							if (randomThings[x, z] is Graphic_Random)
							{
								randomThings[x, z] = ((Graphic_Random)randomThings[x, z]).SubGraphicFor(parent.Map.listerThings.AllThings.RandomElement());
							}
						}
						sameRoom[x, z] = true;
					}
				}
			}
		}

		public void StopHolodeck()
		{
			CurSkill = null;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Defs.Look<SkillDef>(ref CurSkill, "CurrentSkill");
		}

		public override string CompInspectStringExtra()
		{
			if(CurSkill!=null)
				return "Current program: "+programs[CurSkill].programName;
			return "Offline";
		}

		public override void PostDraw()
		{
			base.PostDraw();
			if(CurSkill != null)
			{
				if (randomThings == null)
					ShuffleGraphics();
				HolodeckProgram program = programs[CurSkill];
				Graphic terrainGraphic = program.mainTerrain;
				for(int x=-1 * parent.def.building.watchBuildingStandRectWidth / 2; x <= parent.def.building.watchBuildingStandRectWidth / 2; x++)
				{
					for (int z = -1 * parent.def.building.watchBuildingStandRectWidth / 2; z <= parent.def.building.watchBuildingStandRectWidth / 2; z++)
					{
						if (sameRoom[x + parent.def.building.watchBuildingStandRectWidth / 2, z + parent.def.building.watchBuildingStandRectWidth / 2])
						{
							DrawInSquare(terrainGraphic, new Vector3(x, 0, z));
							if (randomThings[x + parent.def.building.watchBuildingStandRectWidth / 2, z + parent.def.building.watchBuildingStandRectWidth / 2] != null)
							{
								DrawInSquare(randomThings[x + parent.def.building.watchBuildingStandRectWidth / 2, z + parent.def.building.watchBuildingStandRectWidth / 2], new Vector3(x, 0.1f, z));
							}
						}
					}
				}
			}
		}

		void DrawInSquare(Graphic graphic, Vector3 offset)
		{
			Graphics.DrawMesh(material: graphic.MatSingleFor(parent), mesh: graphic.MeshAt(parent.Rotation), position: new UnityEngine.Vector3(parent.DrawPos.x+offset.x, offset.y, parent.DrawPos.z+offset.z), rotation: Quaternion.identity, layer: 0);
		}

		public override void CompTickRare()
		{
			base.CompTickRare();
			if(CurSkill != null && CurSkill!=SkillDefOf.Plants)
			{
				ShuffleGraphics();
			}
		}
	}
}
