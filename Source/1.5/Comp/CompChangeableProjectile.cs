using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class CompChangeableProjectile : ThingComp, IStoreSettingsParent
	{
		public CompProps_ChangeableProjectile Props => (CompProps_ChangeableProjectile)props;
		public StorageSettings allowedShellsSettings;
		private List<ThingDef> preventShells = new List<ThingDef>();
		public List<ThingDef> PreventShells
		{
			get
			{
				if (preventShells == null)
				{
					preventShells = new List<ThingDef>();
				}
				return preventShells;
			}
		}
		private List<ThingDef> loadedShells = new List<ThingDef>();
		public List<ThingDef> LoadedShells
		{
			get
			{
				return loadedShells;
			}
		}
		private int selectedTorp = 0;
		public int SelectedTorp
		{
			get
			{
				if (!preventShells.NullOrEmpty())
				{
					int i = loadedShells.FindIndex(s => !PreventShells.Contains(s));
					if (i > -1)
						return i;
				}
				return selectedTorp;
			}
		}
		public ThingDef Projectile
		{
			get
			{
				return LoadedShells[SelectedTorp].projectileWhenLoaded;
			}
		}
		public bool LoadedNotPrevent => LoadedShells.Any(s => !PreventShells.Contains(s));
		public bool Loaded => LoadedShells.Any();
		public bool FullyLoaded => LoadedShells.Count >= Props.maxTorpedoes;
		public bool StorageTabVisible => true;

		/*none of this works, how? no idea, calling in turret
		public override void PostDeSpawn(Map map)
		{
			Log.Message("3");
			if (ShipInteriorMod2.AirlockBugFlag)
			{
				foreach (ThingDef def in loadedShells)
				{
					Log.Message("1");
					Thing thing = ThingMaker.MakeThing(def);
					GenPlace.TryPlaceThing(thing, parent.Position, map, ThingPlaceMode.Near, null, null, default);
				}
			}
			base.PostDeSpawn(map);
		}
		public override void PostDestroy(DestroyMode mode, Map previousMap)
		{
			Log.Message("2");
			if (previousMap != null)
			{
				foreach (ThingDef def in loadedShells)
				{
					Log.Message("1");
					Thing thing = ThingMaker.MakeThing(def);
					GenPlace.TryPlaceThing(thing, parent.Position, previousMap, ThingPlaceMode.Near, null, null, default);
				}
			}
			base.PostDestroy(mode, previousMap);
		}*/
		public override void PostExposeData()
		{
			Scribe_Collections.Look<ThingDef>(ref preventShells, "preventShells");
			Scribe_Collections.Look<ThingDef>(ref loadedShells, "loadedShells");
			Scribe_Deep.Look(ref allowedShellsSettings, "allowedShellsSettings");
		}
		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);
			allowedShellsSettings = new StorageSettings(this);
			if (parent.def.building.defaultStorageSettings != null)
			{
				allowedShellsSettings.CopyFrom(parent.def.building.defaultStorageSettings);
			}
		}
		public virtual void Notify_ProjectileLaunched()
		{
			loadedShells.RemoveAt(SelectedTorp);
		}
		public void LoadShell(ThingDef shell, int count)
		{
			loadedShells.Add(shell);
		}
		public List<Thing> RemoveShells()
		{
			List<Thing> output = new List<Thing>();
			foreach(ThingDef t in loadedShells)
			{
				if (t == null)
					continue;
				Thing thing = ThingMaker.MakeThing(t);
				thing.stackCount = 1;
				output.Add(thing);
			}
			foreach (Thing t in output)
				loadedShells.Remove(t.def);
			return output;
		}

		public StorageSettings GetStoreSettings()
		{
			return allowedShellsSettings;
		}

		public StorageSettings GetParentStoreSettings()
		{
			return parent.def.building.fixedStorageSettings;
		}

		public void Notify_SettingsChanged()
		{
		}
	}

	//Compatibility
	[StaticConstructorOnStartup]
	static class RegisterTorpedoTubesAsRefuelable
	{
		static RegisterTorpedoTubesAsRefuelable()
		{
			if (!ModLister.HasActiveModWithName("Project RimFactory Revived")) return;
			Type refueler = Type.GetType("ProjectRimFactory.Industry.Building_FuelingMachine, ProjectRimFactory", false);
			if (refueler == null)
			{
				Log.Warning("SoS2 failed to load compatibility for PRF; auto loading torpedo tubes won't work");
				return;
			}
			refueler.GetMethod("RegisterRefuelable", System.Reflection.BindingFlags.Static |
													 System.Reflection.BindingFlags.Public).Invoke(null,
				new object[] {
				typeof(Building_ShipTurretTorpedo),
				(Func<Building, object>)FindCompNeedsShells,
				(Func<object, Thing, int>)delegate (object c, Thing t)
				{
					CompChangeableProjectile comp = c as CompChangeableProjectile;
					if (comp.allowedShellsSettings.filter.Allows(t)) return 1;
					return 0;
				},
				(Action<object, Thing>)delegate (object c, Thing t)
				{
					(c as CompChangeableProjectile).LoadShell(t.def, 1);
					t.Destroy();
				}});
		}
		static object FindCompNeedsShells(Building b)
		{
			var changeableProjectileCompPlural = (b as Building_ShipTurretTorpedo).gun?.TryGetComp<CompChangeableProjectile>();
			if (changeableProjectileCompPlural?.FullyLoaded == false) return changeableProjectileCompPlural;
			return null;
		}
	}
}
