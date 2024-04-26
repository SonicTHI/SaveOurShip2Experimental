using Verse;
using RimWorld;
using UnityEngine;
using Verse.AI;
using Vehicles;

namespace SaveOurShip2
{
	[StaticConstructorOnStartup]
	public static class ResourceBank
	{
		static ResourceBank()
		{
			shipZeroEnemy = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Enemy_Icon_Off",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.red, Color.red);
			shipOneEnemy = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Enemy_Icon_On_slow",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.red, Color.red);
			shipTwoEnemy = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Enemy_Icon_On_mid",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.red, Color.red);
			projectileEnemy = GraphicDatabase.Get(typeof(Graphic_Single), "UI/EnemyProjectile",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.red, Color.red);
			shipZero = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Ship_Icon_Off",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
			shipOne = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Ship_Icon_On_slow",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
			shipTwo = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Ship_Icon_On_mid",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
			shipThree = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Ship_Icon_On_fast",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
			shuttlePlayer = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Shuttle_Icon_Player",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
			shuttleEnemy = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Shuttle_Icon_Enemy",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.red, Color.red);
			ruler = GraphicDatabase.Get(typeof(Graphic_Single), "UI/ShipRangeRuler",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
			projectile = GraphicDatabase.Get(typeof(Graphic_Single), "UI/ShipProjectile",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
			shipBarEnemy = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Map_Icon_Enemy",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
			shipBarPlayer = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Map_Icon_Player",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
			shipBarNeutral = GraphicDatabase.Get(typeof(Graphic_Single), "UI/Map_Icon_Neutral",
			ShaderDatabase.Cutout, new Vector2(1, 1), Color.white, Color.white);
		}
		public static Graphic shipZeroEnemy;
		public static Graphic shipOneEnemy;
		public static Graphic shipTwoEnemy;
		public static Graphic projectileEnemy;
		public static Graphic shipZero;
		public static Graphic shipOne;
		public static Graphic shipTwo;
		public static Graphic shipThree;
		public static Graphic shuttlePlayer;
		public static Graphic shuttleEnemy;
		public static Graphic ruler;
		public static Graphic projectile;
		public static Graphic shipBarEnemy;
		public static Graphic shipBarPlayer;
		public static Graphic shipBarNeutral;

		public static Texture2D PowerTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.45f, 0.425f, 0.1f));
		public static Texture2D HeatTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.5f, 0.1f, 0.1f));
		public static Texture2D DepletionTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.37f, 0.37f, 0.37f));
		public static Texture2D Splash = ContentFinder<Texture2D>.Get("SplashScreen");
		public static Texture2D virtualPhoto = new Texture2D(2048, 2048, TextureFormat.RGB24, false);
		public static RenderTexture target = new RenderTexture(2048, 2048, 16);
		public static Material PlanetMaterial = MaterialPool.MatFrom(virtualPhoto);


		[DefOf]
		public static class ThingDefOf
		{
			public static ThingDef Apparel_SpaceSurvivalBelt;
			public static ThingDef MechaniteFire;
			public static ThingDef ShipArchotechSpore;
			public static ThingDef ShipArchotechPillarB;
			public static ThingDef ShipArchotechPillarC;
			public static ThingDef ShipArchotechPillarD;
			public static ThingDef Ship_Beam;
			public static ThingDef Ship_Beam_Wrecked;
			public static ThingDef Ship_BeamMech;
			public static ThingDef Ship_BeamArchotech;
			public static ThingDef HullFoamWall;
			public static ThingDef ShipAirlockWrecked;
			public static ThingDef ShipHullTileWrecked;
			public static ThingDef ShipHullTile;
			public static ThingDef ShipHullTileMech;
			public static ThingDef ShipHullTileArchotech;
			public static ThingDef ShipHullfoamTile;
			public static ThingDef ShipAirlock;
			public static ThingDef ShipAirlockBeamWall;
			public static ThingDef ShipAirlockBeamWallInert;
			public static ThingDef ShipAirlockBeamTile;
			public static ThingDef ShipInside_SolarGenerator;
			public static ThingDef Ship_SensorClusterAdv;
			public static ThingDef ShipSpinalAmplifier;
			public static ThingDef ShipSpinalBarrelPsychic;
			public static ThingDef ShipTorpedo_HighExplosive;
			public static ThingDef ShipTorpedo_EMP;
			public static ThingDef ShipTorpedo_Antimatter;
			public static ThingDef Ship_Engine;
			public static ThingDef Ship_Engine_Large;
			public static ThingDef Ship_Engine_Interplanetary;
			public static ThingDef Ship_Engine_Interplanetary_Large;
			public static ThingDef Ship_FakeBeam;
			public static ThingDef ShuttleFuelPods;
			public static ThingDef ShipConsoleScience;
			public static ThingDef Cryptonest;
			public static ThingDef CrittersleepCasket;
			public static ThingDef CrittersleepCasketLarge;
			public static ThingDef Ship_CryptosleepCasket;
			public static ThingDef SoS2_Shuttle_Personal;
			public static ThingDef SoS2DummyObject;
			public static ThingDef Mote_HeatsinkPurge;
			public static ThingDef Proj_ShipDebrisA;
			public static ThingDef Proj_ShipDebrisB;
			public static ThingDef Proj_ShipDebrisC;
			public static ThingDef Proj_ShipDebrisD;
			public static ThingDef Proj_ShipRockA;
			public static ThingDef Proj_ShipRockB;
			public static ThingDef Proj_ShipRockC;
			public static ThingDef Bullet_Fake_Laser;
			public static ThingDef Bullet_Ground_Laser;
			public static ThingDef Bullet_Fake_Psychic;
			public static ThingDef ShipCombatLaserMote;
			public static ThingDef ArchotechExoticParticles;
			public static ThingDef DetachedShipPart;
			public static ThingDef ShipMoveBlueprint;
			public static ThingDef SoS2CryptosleepCocoon;
			public static ThingDef Shuttle_Laser;
			public static ThingDef Shuttle_Laser_Space;
			public static ThingDef Shuttle_Plasma;
			//vanilla defs
			public static ThingDef Turret_Autocannon;
			public static ThingDef Turret_Sniper;
			public static ThingDef MechSerumResurrector;
			public static ThingDef Mote_Bubble;
		}

		[DefOf]
		public static class TerrainDefOf
		{
			public static TerrainDef EmptySpace;
			public static TerrainDef FakeFloorInsideShip;
			public static TerrainDef FakeFloorInsideShipMech;
			public static TerrainDef FakeFloorInsideShipArchotech;
			public static TerrainDef ShipWreckageTerrain;
			public static TerrainDef FakeFloorInsideShipFoam;
		}

		[DefOf]
		public static class RoofDefOf
		{
			public static RoofDef RoofShip;
		}

		[DefOf]
		public static class HediffDefOf
		{
			public static HediffDef SpaceHypoxia;
			public static HediffDef SoSArchotechLung;
			public static HediffDef SoSArchotechSkin;
			public static HediffDef SpaceBeltBubbleHediff;
			public static HediffDef SoSHologramMachine;
			public static HediffDef SoSHologramArchotech;
		}

		[DefOf]
		public static class MemeDefOf
		{
			[MayRequireIdeology]
			public static MemeDef Structure_Archist;
		}

		[DefOf]
		public static class JobDefOf
		{
			public static JobDef FleeVacuum;
			public static JobDef HackEnemyShip;
			public static JobDef HackAirlock;
			public static JobDef BreachAirlock;
			public static JobDef DefendBreacher;
		}

		[DefOf]
		public static class DutyDefOf
		{
			public static DutyDef SoSDefendShip;
			public static DutyDef SoSAssaultShip;
		}

		[DefOf]
		public static class BiomeDefOf
		{
			public static BiomeDef OuterSpaceBiome;
		}

		[DefOf]
		public static class StorytellerDefOf
		{
			public static StorytellerDef Sara;
			public static StorytellerDef Kim;
		}

		[DefOf]
		public static class ResearchProjectDefOf
		{
			public static ResearchProjectDef ArchotechPillarA;
			public static ResearchProjectDef ArchotechPillarB;
			public static ResearchProjectDef ArchotechPillarC;
			public static ResearchProjectDef ArchotechPillarD;
			public static ResearchProjectDef ArchotechPsychicField;
			public static ResearchProjectDef ArchotechPsychicManipulation;
			public static ResearchProjectDef ArchotechPsylink;
			public static ResearchProjectDef ArchotechBroadManipulation;
			public static ResearchProjectDef ArchotechExotics;
		}

		[DefOf]
		public static class BackstoryDefOf
		{
			public static BackstoryDef SoSHologram;
		}

		[DefOf]
		public static class WeatherDefOf
		{
			public static WeatherDef OuterSpaceWeather;
		}

		[DefOf]
		public static class GameConditionDefOf
		{
			public static GameConditionDef SpaceDebris;
		}

		[DefOf]
		public static class SoundDefOf
		{
			public static SoundDef ShipPurgeHiss;
			public static SoundDef ShipEngineFuel;
		}

		[DefOf]
		public static class WorldObjectDefOf
		{
			public static WorldObjectDef ShipOrbiting; //player ship map
			public static WorldObjectDef ShipEnemy;
			public static WorldObjectDef WreckSpace;
			public static WorldObjectDef SiteSpace;
			public static WorldObjectDef MoonPillarSite;
		}

		[DefOf]
		public static class StatDefOf
		{
			public static StatDef DecompressionResistance;
			public static StatDef DecompressionResistanceOffset;
			public static StatDef HypoxiaResistance;
			public static StatDef HypoxiaResistanceOffset;
			public static StatDef VacuumSpeedMultiplier;
		}

		[DefOf]
		public static class VehicleStatDefOf
        {
			public static VehicleStatDef Hardpoints;
			public static VehicleStatDef SoS2CombatDodgeChance;
		}

		[DefOf]
		public static class ThingCategoryDefOf
        {
			public static ThingCategoryDef SpaceTorpedoes;
		}

		[DefOf]
		public static class VehicleTurretDefOf
        {
			public static VehicleTurretDef SoS2ShuttleLaser;
			public static VehicleTurretDef SoS2ShuttlePlasma;
			public static VehicleTurretDef SoS2ShuttleTorpedo;
		}

		[DefOf]
		public static class UpgradeTreeDefOf
        {
			public static UpgradeTreeDef SoS2ShuttleUpgradeTree;
		}
	}
}
