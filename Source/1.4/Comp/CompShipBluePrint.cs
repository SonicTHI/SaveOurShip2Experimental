using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class CompShipBlueprint : ThingComp
    {
        public CompProperties_ShipBlueprint Props
        {
            get { return props as CompProperties_ShipBlueprint; }
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            Command_Action place = new Command_Action
            {

                action = delegate
                {
                    Sketch shipSketch = GenerateBlueprintSketch(this.parent.Position, Props.shipDef);
                    MinifiedThingShipBlueprint fakeMover = (MinifiedThingShipBlueprint)new ShipSpawnBlueprint(shipSketch).TryMakeMinified();
                    fakeMover.parent = this.parent;
                    fakeMover.ShipDef = Props.shipDef;
                    fakeMover.bottomLeftPos = this.parent.Position;
                    ShipInteriorMod2.shipOriginMap = this.parent.Map;
                    fakeMover.targetMap = this.parent.Map;
                    fakeMover.Position = this.parent.Position;
                    fakeMover.SpawnSetup(this.parent.Map, false);
                    List<object> selected = new List<object>();
                    foreach (object ob in Find.Selector.SelectedObjects)
                        selected.Add(ob);
                    foreach (object ob in selected)
                        Find.Selector.Deselect(ob);
                    Find.Selector.Select(fakeMover);
                    InstallationDesignatorDatabase.DesignatorFor(ThingDef.Named("ShipSpawnBlueprint")).ProcessInput(null);
                },
                defaultLabel = "Place blueprint",
                defaultDesc = "Place ship blueprint",
                icon = ContentFinder<Texture2D>.Get("UI/SalvageMove")
            };
            if (!ResearchProjectDef.Named("ShipBasics").IsFinished)
            {
                place.Disable(TranslatorFormattedStringExtensions.Translate("ShipBlueprintDisabled"));
            }
            yield return place;
        }
        private Sketch GenerateBlueprintSketch(IntVec3 lowestCorner, EnemyShipDef shipDef)
        {
            Sketch sketch = new Sketch();
            /*foreach (ShipShape shape in shipDef.parts)
            {
                if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    ThingDef def = ThingDef.Named(shape.shapeOrDef);
                    if (def.building != null)
                    {
                        sketch.AddThing(DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef), new IntVec3(shape.x, 0, shape.z), shape.rot);
                    }
                }
            }*/
            List<IntVec3> positions = new List<IntVec3>();
            foreach (ShipShape shape in shipDef.parts)
            {
                if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef) != null)
                {
                    ThingDef def = ThingDef.Named(shape.shapeOrDef);
                    if (def.building != null && def.building.shipPart && def.IsResearchFinished)
                    {
                        IntVec3 pos =  new IntVec3(shape.x, 0, shape.z);
                        if (def.Size.x > 1 || def.Size.z > 1)
                        {
                            if (shape.rot == Rot4.North || shape.rot == Rot4.South)
                            {
                                pos.x -= (def.Size.x - 1) / 2;
                                pos.z -= (def.Size.z - 1) / 2;
                            }
                            else
                            {
                                pos.x -= (def.Size.z - 1) / 2;
                                pos.z -= (def.Size.x - 1) / 2;
                            }
                            if (def.size.z % 2 == 0 && def.size.x % 2 != 0)
                            {
                                if (shape.rot == Rot4.South)
                                    pos.z -= 1;
                                else if (shape.rot == Rot4.West)
                                    pos.x -= 1;
                            }
                            for (int i = 0; i < def.Size.x; i++)
                            {
                                for (int j = 0; j < def.Size.z; j++)
                                {
                                    IntVec3 adjPos;
                                    if (shape.rot == Rot4.North || shape.rot == Rot4.South)
                                        adjPos = new IntVec3(pos.x + i, 0, pos.z + j);
                                    else
                                        adjPos = new IntVec3(pos.x + j, 0, pos.z + i);
                                    if (!positions.Contains(adjPos))
                                    {
                                        positions.Add(adjPos);
                                    }
                                }
                            }
                        }
                        else if (!positions.Contains(pos))
                        {
                            positions.Add(pos);
                        }
                    }
                }
            }
            foreach (IntVec3 pos in positions)
            {
                sketch.AddThing(ThingDef.Named("Ship_FakeBeam"), pos, Rot4.North);
            }
            return sketch;
        }
    }
    class ShipSpawnBlueprint : Thing
    {
        public Sketch shipSketch;

        public ShipSpawnBlueprint(Sketch sketch)
        {
            shipSketch = sketch;
            this.def = DefDatabase<ThingDef>.GetNamed("ShipSpawnBlueprint");
        }
        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            this.shipSketch.DrawGhost(drawLoc.ToIntVec3(), Sketch.SpawnPosType.Unchanged, false, null);
        }
        public void DrawGhost(IntVec3 drawLoc, bool flip = false)
        {
            this.shipSketch.DrawGhost(drawLoc, Sketch.SpawnPosType.Unchanged, false, null);
        }
    }
    class MinifiedThingShipBlueprint : MinifiedThing
    {
        public Thing parent;
        public EnemyShipDef ShipDef;
        public IntVec3 bottomLeftPos;
        public Map targetMap = null;
        bool done = false;
        public override void Tick()
        {
            base.Tick();
            if (Find.Selector.SelectedObjects.Count > 1 || !Find.Selector.SelectedObjects.Contains(this))
            {
                if (done)
                {
                    this.Destroy(DestroyMode.Vanish);
                    //parent.Destroy(DestroyMode.Vanish);
                }
                else if (InstallBlueprintUtility.ExistingBlueprintFor(this) != null)
                {
                    done = true;
                    try
                    {
                        SpawnShipDefBlueprint(ShipDef, InstallBlueprintUtility.ExistingBlueprintFor(this).Position, targetMap);
                    }
                    catch (Exception e)
                    {
                        //fuck this error, somehow this shit recurses
                    }
                }
            }
        }
        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            if (this.Graphic is Graphic_Single)
            {
                this.Graphic.Draw(drawLoc, Rot4.North, this, 0f);
                return;
            }
            this.Graphic.Draw(drawLoc, Rot4.South, this, 0f);
        }
        public override string GetInspectString()
        {
            return TranslatorFormattedStringExtensions.Translate("ShipMoveDesc");
        }
        public void SpawnShipDefBlueprint(EnemyShipDef shipdef, IntVec3 pos, Map map)
        {
            foreach (ShipShape shape in shipdef.parts)
            {
                if (DefDatabase<ThingDef>.GetNamedSilentFail(shape.shapeOrDef).building != null)
                {
                    ThingDef def = ThingDef.Named(shape.shapeOrDef);
                    if (def.IsResearchFinished)
                    {
                        ThingDef stuff = GenStuff.DefaultStuffFor(def);
                        if (def.MadeFromStuff)
                        {
                            if (shape.stuff != null)
                                stuff = ThingDef.Named(shape.stuff);
                        }
                        GenConstruct.PlaceBlueprintForBuild(def, new IntVec3(pos.x + shape.x, 0, pos.z + shape.z), map, shape.rot, Faction.OfPlayer, stuff);
                    }
                }
            }
        }
    }
}