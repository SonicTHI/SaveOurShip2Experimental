using RimWorld.Planet;
using RimWorld;
using System;
using System.Reflection;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using Verse.Sound;
using System.Diagnostics;
using System.Text;
using System.Linq;
using HarmonyLib;
using SaveOurShip2;

namespace RimWorld
{
    public class WorldObjectOrbitingShip : MapParent
    {
        public float theta;
        public float radius;
        public float phi;

        public float thetaset;
        public bool startMove = false;
        public bool preventMove = false;

        public static Vector3 orbitVec = new Vector3(0, 0, 1);
        public static Vector3 orbitVecPolar = new Vector3(0, 1, 0);

        static FieldInfo mapField = typeof(TravelingTransportPods).GetField("initialTile", BindingFlags.Instance | BindingFlags.NonPublic);

        public bool IsShip => this.def == ResourceBank.WorldObjectDefOf.ShipEnemy || this.def == ResourceBank.WorldObjectDefOf.WreckSpace;

        public override Vector3 DrawPos
        {
            get
            {
                if(radius==0)
                {
                    radius = 150f;
                    theta = -3;
                }
                return Vector3.SlerpUnclamped(orbitVec * radius, orbitVec * radius * -1, theta * -1); //TODO phi
            }
        }

        public override void Tick()
		{
			base.Tick();
            //move ship to next pos if player owned, on raretick, not in combat or encounter or durring shuttle use
            if (startMove && Find.TickManager.TicksGame % 60 == 0 && this.def.canBePlayerHome && !this.Map.GetComponent<ShipHeatMapComp>().InCombat)
            {
                preventMove = false;
                foreach (TravelingTransportPods obj in Find.WorldObjects.TravelingTransportPods)
                {
                    int initialTile = (int)mapField.GetValue(obj);
                    if (initialTile == this.Tile || obj.destinationTile == this.Tile)
                    {
                        preventMove = true;
                        break;
                    }
                }

                if (!preventMove)
                {
                    if (thetaset - 0.0005f > theta)
                        theta += 0.0005f;
                    else if (thetaset + 0.0005f < theta)
                        theta -= 0.0005f;
                    else if (startMove == true && theta > thetaset - 0.0005f && theta < thetaset + 0.0005f)//arrived deadzone
                    {
                        startMove = false;
                        Messages.Message(TranslatorFormattedStringExtensions.Translate("ShipInsideMoveComplete"), this, MessageTypeDefOf.PositiveEvent);
                    }
                }
            }
		}

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref theta, "theta", -3, false);
            Scribe_Values.Look<float>(ref phi, "phi", 0, false);
            Scribe_Values.Look<float>(ref radius, "radius", 0f, false);
            Scribe_Values.Look<float>(ref thetaset, "thetaset", -3, false);
            Scribe_Values.Look<bool>(ref startMove, "startMove", false, false);
        }

        public override void Print(LayerSubMesh subMesh)
        {
            float averageTileSize = Find.WorldGrid.averageTileSize;
            WorldRendererUtility.PrintQuadTangentialToPlanet(this.DrawPos, 1.7f * averageTileSize, 0.015f, subMesh, false, false, true);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }
            if (this.HasMap)
            {
                var mapComp = this.Map.GetComponent<ShipHeatMapComp>();
                yield return new Command_Action
                {
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandShowMap"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandShowMapDesc"),
                    icon = (Texture2D)typeof(MapParent).GetField("ShowMapCommand", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null),
                    hotKey = KeyBindingDefOf.Misc1,
                    action = delegate
                    {
                        Current.Game.CurrentMap = this.Map;
                        if (!CameraJumper.TryHideWorld())
                        {
                            SoundDefOf.TabClose.PlayOneShotOnCamera(null);
                        }
                    }
                };
                if (this.def.canBePlayerHome)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideAbandonHome"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideAbandonHomeDesc"),
                        icon = ContentFinder<Texture2D>.Get("UI/ShipAbandon_Icon", true),
                        action = delegate
                        {
                            Map map = this.Map;
                            if (map == null)
                            {
                                Abandon(this);
                                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                                return;
                            }

                            foreach (TravelingTransportPods obj in Find.WorldObjects.TravelingTransportPods)
                            {
                                int initialTile = (int)Traverse.Create(obj).Field("initialTile").GetValue();
                                if (initialTile == this.Tile || obj.destinationTile == this.Tile)
                                {
                                    Messages.Message(TranslatorFormattedStringExtensions.Translate("CommandScuttleShipPods"), this, MessageTypeDefOf.NeutralEvent);
                                    return;
                                }
                            }
                            StringBuilder stringBuilder = new StringBuilder();
                            IEnumerable<Pawn> source = map.mapPawns.PawnsInFaction(Faction.OfPlayer).Where(pawn => !pawn.InContainerEnclosed || (pawn.ParentHolder is Thing && ((Thing)pawn.ParentHolder).def != ResourceBank.ThingDefOf.Ship_CryptosleepCasket));
                            if (source.Any())
                            {
                                StringBuilder stringBuilder2 = new StringBuilder();
                                foreach (Pawn item in source.OrderByDescending((Pawn x) => x.IsColonist))
                                {
                                    if (stringBuilder2.Length > 0)
                                    {
                                        stringBuilder2.AppendLine();
                                    }
                                    stringBuilder2.Append("    " + item.LabelCap);
                                }
                                stringBuilder.Append("ConfirmAbandonHomeWithColonyPawns".Translate(stringBuilder2));
                            }
                            PawnDiedOrDownedThoughtsUtility.BuildMoodThoughtsListString(source, PawnDiedOrDownedThoughtsKind.Died, stringBuilder, null, "\n\n" + "ConfirmAbandonHomeNegativeThoughts_Everyone".Translate(), "ConfirmAbandonHomeNegativeThoughts");
                            if (stringBuilder.Length == 0)
                            {
                                Abandon(this);
                                SoundDefOf.Tick_High.PlayOneShotOnCamera();
                            }
                            else
                            {
                                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(stringBuilder.ToString(), delegate
                                {
                                    Abandon(this);
                                }));
                            }
                        }
                    };
                    if (!preventMove && !startMove && !mapComp.InCombat && !mapComp.BurnUpSet)
                    {
                        yield return new Command_Action
                        {
                            action = delegate ()
                            {
                                thetaset = theta + 0.2f;
                                startMove = true;
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveWestFar"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveWestFarDesc"),
                            hotKey = KeyBindingDefOf.Misc1,
                            icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_On_mid", true)
                        };
                        yield return new Command_Action
                        {
                            action = delegate ()
                            {
                                thetaset = theta + 0.05f;
                                startMove = true;
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveWest"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveWestDesc"),
                            hotKey = KeyBindingDefOf.Misc2,
                            icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_On_slow", true)
                        };
                        yield return new Command_Action
                        {
                            action = delegate ()
                            {
                                thetaset = theta - 0.05f;
                                startMove = true;
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveEast"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveEastDesc"),
                            hotKey = KeyBindingDefOf.Misc3,
                            icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_On_slow_rev", true)
                        };
                        yield return new Command_Action
                        {
                            action = delegate ()
                            {
                                thetaset = theta - 0.2f;
                                startMove = true;
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveEastFar"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveEastFarDesc"),
                            hotKey = KeyBindingDefOf.Misc4,
                            icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_On_mid_rev", true)
                        };
                    }
                    if (Prefs.DevMode)
                    {
                        yield return new Command_Action
                        {
                            action = delegate ()
                            {
                                thetaset = 0;
                                theta = 0;
                                startMove = false;
                            },
                            defaultLabel = "Dev: Reset position",
                            defaultDesc = "Reset ship location to default.",
                        };
                    }
                }
                if (mapComp.IsGraveyard && !mapComp.ShipCombatOriginMap.GetComponent<ShipHeatMapComp>().InCombat && !mapComp.BurnUpSet)
                {
                    yield return new Command_Action
                    {
                        action = delegate
                        {
                            mapComp.BurnUpSet = true;
                        },
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideLeaveGraveyard"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideLeaveGraveyardDesc"),
                        hotKey = KeyBindingDefOf.Misc5,
                        icon = ContentFinder<Texture2D>.Get("UI/ShipAbandon_Icon", true)
                    };
                }
                if (Prefs.DevMode && !mapComp.InCombat && IsShip && !mapComp.BurnUpSet)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = "Dev: Remove ship",
                        defaultDesc = "Delete a glitched ship and its map.",
                        action = delegate
                        {
                            mapComp.BurnUpSet = true;
                        }
                    };
                }
            }
        }

        void Abandon(WorldObjectOrbitingShip ship)
        {
            var mapComp = this.Map.GetComponent<ShipHeatMapComp>();
            if (mapComp.InCombat)
                mapComp.EndBattle(this.Map, false);
            if (this.Map.mapPawns.AnyColonistSpawned)
            {
                Find.GameEnder.CheckOrUpdateGameOver();
            }
            Current.Game.DeinitAndRemoveMap_NewTemp(this.Map, false);
            this.Destroy();
            //this.Map.GetComponent<ShipHeatMapComp>().BurnUpSet = true;
        }

        public override MapGeneratorDef MapGeneratorDef
        {
            get
            {
                return DefDatabase<MapGeneratorDef>.GetNamed("EmptySpaceMap");
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            return new List<FloatMenuOption>();
        }

        [DebuggerHidden]
        public override IEnumerable<FloatMenuOption> GetTransportPodsFloatMenuOptions(IEnumerable<IThingHolder> pods, CompLaunchable representative)
        {
            return new List<FloatMenuOption>();
        }

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            var mapcomp = Map.GetComponent<ShipHeatMapComp>();
            if (!mapcomp.InCombat && mapcomp.BurnUpSet)
            {
                foreach(TravelingTransportPods obj in Find.WorldObjects.TravelingTransportPods)
                {
                    int initialTile = (int)Traverse.Create(obj).Field("initialTile").GetValue();
                    if (initialTile == this.Tile) //dont remove if pods in flight from this WO
                    {
                        alsoRemoveWorldObject = false;
                        return false;
                    }
                    else if (obj.destinationTile == this.Tile) //divert from this WO to initial //td might not work
                    {
                        obj.destinationTile = initialTile;
                        alsoRemoveWorldObject = false;
                        return false;
                    }
                }
                /*foreach (Pawn p in Map.mapPawns.AllPawnsSpawned.Where(o => o.Faction == Faction.OfPlayer))
                {
                    p.Kill(null);
                }*/
                alsoRemoveWorldObject = true;
                return true;
            }
            alsoRemoveWorldObject = false;
            return false;
        }
    }
}

