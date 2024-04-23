using RimWorld.Planet;
using RimWorld;
using System;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using Verse.Sound;
using System.Diagnostics;
using System.Text;
using System.Linq;
using HarmonyLib;

namespace SaveOurShip2
{
	public class WorldObjectOrbitingShip : MapParent
	{
		private string nameInt;
		public string Name
		{
			get
			{
				return nameInt;
			}
			set
			{
				nameInt = value;
			}
		}
		public override string Label
		{
			get
			{
				if (nameInt == null)
				{
					return base.Label;
				}
				return nameInt;
			}
		}

		ShipMapComp mapComp => Map.GetComponent<ShipMapComp>();
		//used for orbit transition only
		public override Vector3 DrawPos
		{
			get
			{
				return drawPos;
			}
		}
		public Vector3 drawPos;
		public Vector3 originDrawPos = Vector3.zero;
		public Vector3 targetDrawPos = Vector3.zero;
		public Vector3 NominalPos => Vector3.SlerpUnclamped(vecEquator * 150, vecEquator * -150, 3);
		public void SetNominalPos()
		{
			radius = 150;
			Theta = -3;
		}
		//used in orbit
		public static Vector3 vecEquator = new Vector3(0, 0, 1);
		public static Vector3 vecPolar = new Vector3(0, 1, 0);
		public int orbitalMove = 0;
		public bool preventMove = false;
		private float radius = 150; //altitude ~95-150
		private float phi = 0; //up/down on radius //td change to N/S orbital
		private float theta = -3; //E/W orbital on radius
		public float Radius
		{
			get { return radius; }
			set
			{
				radius = value;
				OrbitSet();
			}
		}
		public float Phi
		{
			get { return phi; }
			set
			{
				phi = value;
				OrbitSet();
			}
		}
		public float Theta
		{
			get { return theta; }
			set
			{
				theta = value;
				OrbitSet();
			}
		}
		void OrbitSet() //recalc on change only
		{
			Vector3 v = Vector3.SlerpUnclamped(vecEquator * radius, vecEquator * radius * -1, theta * -1);
			drawPos = new Vector3(v.x, phi, v.z); //td not correct
		}
		public override void SpawnSetup()
		{
			if (drawPos == Vector3.zero)
				OrbitSet();

			base.SpawnSetup();
		}

		public override void Tick()
		{
			base.Tick();
			//move ship to next pos if player owned, on raretick, if nominal, not durring shuttle use
			if (orbitalMove == 0)
				return;

			if (orbitalMove > 0)
			{
				Theta = theta - 0.0001f;
			}
			else if (orbitalMove < 0)
			{
				Theta = theta + 0.0001f;
			}

			if (Find.TickManager.TicksGame % 60 == 0)
			{
				if (mapComp.ShipMapState != ShipMapState.nominal)
				{
					orbitalMove = 0;
					return;
				}
				foreach (TravelingTransportPods obj in Find.WorldObjects.TravelingTransportPods)
				{
					int initialTile = obj.initialTile;
					if (initialTile == Tile || obj.destinationTile == Tile)
					{
						orbitalMove = 0;
						return;
					}
				}
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look<float>(ref theta, "theta", -3, false);
			Scribe_Values.Look<float>(ref phi, "phi", 0, false);
			Scribe_Values.Look<float>(ref radius, "radius", 150f, false);
			Scribe_Values.Look<int>(ref orbitalMove, "orbitalMove", 0, false);
			Scribe_Values.Look<string>(ref nameInt, "nameInt", null, false);
			Scribe_Values.Look<Vector3>(ref drawPos, "drawPos", Vector3.zero, false);
			Scribe_Values.Look<Vector3>(ref originDrawPos, "originDrawPos", Vector3.zero, false);
			Scribe_Values.Look<Vector3>(ref targetDrawPos, "targetDrawPos", Vector3.zero, false);
		}

		public override void Print(LayerSubMesh subMesh)
		{
			float averageTileSize = Find.WorldGrid.averageTileSize;
			WorldRendererUtility.PrintQuadTangentialToPlanet(DrawPos, 1.7f * averageTileSize, 0.015f, subMesh, false, false, true);
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo g in base.GetGizmos())
			{
				yield return g;
			}
			if (HasMap)
			{
				yield return new Command_Action
				{
					defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandShowMap"),
					defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandShowMapDesc"),
					icon = ShowMapCommand,
					hotKey = KeyBindingDefOf.Misc1,
					action = delegate
					{
						Current.Game.CurrentMap = Map;
						if (!CameraJumper.TryHideWorld())
						{
							SoundDefOf.TabClose.PlayOneShotOnCamera(null);
						}
					}
				};
				if (def.canBePlayerHome)
				{
					yield return new Command_Action
					{
						defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.AbandonHome"),
						defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.AbandonHomeDesc"),
						icon = ContentFinder<Texture2D>.Get("UI/ShipAbandon_Icon", true),
						action = delegate
						{
							Map map = this.Map;
							if (map == null)
							{
								Abandon();
								SoundDefOf.Tick_High.PlayOneShotOnCamera();
								return;
							}

							foreach (TravelingTransportPods obj in Find.WorldObjects.TravelingTransportPods)
							{
								int initialTile = (int)Traverse.Create(obj).Field("initialTile").GetValue();
								if (initialTile == this.Tile || obj.destinationTile == this.Tile)
								{
									Messages.Message(TranslatorFormattedStringExtensions.Translate("SoS.ScuttleShipPods"), this, MessageTypeDefOf.NeutralEvent);
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
									stringBuilder2.Append("	" + item.LabelCap);
								}
								stringBuilder.Append("ConfirmAbandonHomeWithColonyPawns".Translate(stringBuilder2));
							}
							PawnDiedOrDownedThoughtsUtility.BuildMoodThoughtsListString(source, PawnDiedOrDownedThoughtsKind.Died, stringBuilder, null, "\n\n" + "ConfirmAbandonHomeNegativeThoughts_Everyone".Translate(), "ConfirmAbandonHomeNegativeThoughts");
							if (stringBuilder.Length == 0)
							{
								Abandon();
								SoundDefOf.Tick_High.PlayOneShotOnCamera();
							}
							else
							{
								Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(stringBuilder.ToString(), delegate
								{
									Abandon();
								}));
							}
						}
					};
					if (!preventMove && mapComp.ShipMapState == ShipMapState.nominal && mapComp.ShipMapState != ShipMapState.burnUpSet)
					{
						Command_Action burnWest = new Command_Action
						{
							action = delegate ()
							{
								orbitalMove = -1;
							},
							defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.MoveWest"),
							defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.MoveWestDesc"),
							hotKey = KeyBindingDefOf.Misc2,
							icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_On_slow", true)
						};
						Command_Action burnStop = new Command_Action
						{
							action = delegate ()
							{
								orbitalMove = 0;
							},
							defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.MoveStop"),
							defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.MoveStopDesc"),
							hotKey = KeyBindingDefOf.Misc1,
							icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_Stop", true)
						};
						Command_Action burnEast = new Command_Action
						{
							action = delegate ()
							{
								orbitalMove = 1;
							},
							defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.MoveEast"),
							defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.MoveEastDesc"),
							hotKey = KeyBindingDefOf.Misc3,
							icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_On_slow_rev", true)
						};
						if (preventMove)
						{
							burnWest.disabled = true;
							burnStop.disabled = true;
							burnEast.disabled = true;
						}
						else if (orbitalMove == 0)
						{
							burnStop.disabled = true;
						}
						else
						{
							burnWest.disabled = true;
							burnEast.disabled = true;
						}
						yield return burnWest;
						yield return burnStop;
						yield return burnEast;
					}
					if (Prefs.DevMode)
					{
						yield return new Command_Action
						{
							action = delegate ()
							{
								orbitalMove = 0;
								drawPos = NominalPos;
							},
							defaultLabel = "Dev: Reset position",
							defaultDesc = "Reset ship location to default.",
						};
					}
				}
				if (mapComp.ShipMapState == ShipMapState.isGraveyard && !mapComp.IsGraveOriginInCombat && mapComp.ShipMapState != ShipMapState.burnUpSet)
				{
					yield return new Command_Action
					{
						action = delegate
						{
							mapComp.ShipMapState = ShipMapState.burnUpSet;
						},
						defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.LeaveGraveyard"),
						defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.LeaveGraveyardDesc"),
						hotKey = KeyBindingDefOf.Misc5,
						icon = ContentFinder<Texture2D>.Get("UI/ShipAbandon_Icon", true)
					};
				}
				if (Prefs.DevMode && mapComp.ShipMapState != ShipMapState.burnUpSet)
				{
					yield return new Command_Action
					{
						defaultLabel = "Dev: Remove ship",
						defaultDesc = "Delete a glitched ship and its map.",
						action = delegate
						{
							mapComp.ShipMapState = ShipMapState.burnUpSet;
						}
					};
				}
			}
		}
		public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string inspectString = base.GetInspectString();
			if (!inspectString.NullOrEmpty())
			{
				stringBuilder.AppendLine(inspectString);
			}
			if (Prefs.DevMode)
			{
				stringBuilder.AppendLine("State: " + mapComp.ShipMapState + "  Altitude: " + mapComp.Altitude);
				stringBuilder.AppendLine("Radius: " + radius + "  Theta: " + theta + "  Phi: " + phi);
				stringBuilder.AppendLine("DrawPos: " + DrawPos);
				stringBuilder.AppendLine("originDrawPos: " + originDrawPos);
				stringBuilder.AppendLine("targetDrawPos: " + targetDrawPos);
			}
			return stringBuilder.ToString().TrimEndNewlines();
		}
		public override void Abandon()
		{
			if (mapComp.ShipMapState == ShipMapState.inCombat)
				mapComp.EndBattle(Map, false);
			if (Map.mapPawns.AnyColonistSpawned)
			{
				Find.GameEnder.CheckOrUpdateGameOver();
			}
			Current.Game.DeinitAndRemoveMap(Map, false);
			Destroy();
			//base.Abandon();
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

		public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject) //on tick check to remove
		{
			if (mapComp.ShipMapState == ShipMapState.burnUpSet)
			{
				//td recheck all of this after VF, generally pods need origin to exist till they land
				foreach (TravelingTransportPods obj in Find.WorldObjects.TravelingTransportPods)
				{
					int initialTile = obj.initialTile;
					if (initialTile == Tile) //dont remove if pods in flight from this WO
					{
						alsoRemoveWorldObject = false;
						return false;
					}
					else if (obj.destinationTile == Tile) //divert from this WO to initial //td might not work
					{
						obj.destinationTile = initialTile;
						alsoRemoveWorldObject = false;
						return false;
					}
				}

				//kill off pawns to prevent reappearance, tell player
				List<Pawn> toKill = new List<Pawn>();
				foreach (Thing t in Map.spawnedThings)
				{
					if (t is Pawn p)
						toKill.Add(p);
				}
				foreach (Pawn p in toKill)
				{
					p.Kill(new DamageInfo(DamageDefOf.Bomb, 99999));
				}
				if (toKill.Any(p => p.Faction == Faction.OfPlayer))
				{
					string letterString = TranslatorFormattedStringExtensions.Translate("SoS.PawnsLostReEntry") + "\n\n";
					foreach (Pawn deadPawn in toKill.Where(p => p.Faction == Faction.OfPlayer))
						letterString += deadPawn.LabelShort + "\n";
					Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoS.PawnsLostReEntryDesc"), letterString,
						LetterDefOf.NegativeEvent);
				}

				alsoRemoveWorldObject = true;
				return true;
			}
			alsoRemoveWorldObject = false;
			return false;
		}
	}
}

