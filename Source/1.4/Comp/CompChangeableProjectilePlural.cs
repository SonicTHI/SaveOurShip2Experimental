using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    public class CompChangeableProjectilePlural : ThingComp, IStoreSettingsParent
    {
        private List<ThingDef> loadedShells=new List<ThingDef>();
        public int selectedTorp = 0;
        public StorageSettings allowedShellsSettings;

        public CompProperties_ChangeableProjectilePlural Props => (CompProperties_ChangeableProjectilePlural)props;

        public List<ThingDef> LoadedShells
        {
            get
            {
                return loadedShells;
            }
        }
        public ThingDef Projectile
        {
            get
            {
                if (!Loaded)
                {
                    return null;
                }
                return LoadedShells[selectedTorp].projectileWhenLoaded;
            }
        }

        public bool Loaded => LoadedShells.Any();
        public bool FullyLoaded => LoadedShells.Count >= Props.maxTorpedoes;

        public bool StorageTabVisible => true;

        public override void PostExposeData()
        {
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
            loadedShells.RemoveAt(selectedTorp);
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
                    CompChangeableProjectilePlural comp = c as CompChangeableProjectilePlural;
                    if (comp.allowedShellsSettings.filter.Allows(t)) return 1;
                    return 0;
                },
                (Action<object, Thing>)delegate (object c, Thing t)
                {
                    (c as CompChangeableProjectilePlural).LoadShell(t.def, 1);
                    t.Destroy();
                }});
        }
        static object FindCompNeedsShells(Building b)
        {
            var changeableProjectileCompPlural = (b as Building_ShipTurretTorpedo).gun?.TryGetComp<CompChangeableProjectilePlural>();
            if (changeableProjectileCompPlural?.FullyLoaded == false) return changeableProjectileCompPlural;
            return null;
        }
    }
}
