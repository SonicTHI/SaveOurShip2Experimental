using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld.Planet;
using SaveOurShip2;
using System.Reflection;
using System.Text;
using Verse.AI;
using RimworldMod;
using Verse.Noise;

namespace RimWorld
{
    public class Building_ShipBridge : Building
    {
        public string ShipName = "Unnamed Ship"; //for saving
        private int shipIndex = -1; //shipindex in mapcomp cache
        public int ShipIndex
        {
            get { return shipIndex; }
            set
            {
                shipIndex = value;
                if (shipIndex == -1)
                {
                    Ship = null;
                    return;
                }
                if (mapComp == null)
                    return;
                if (mapComp.ShipsOnMapNew.ContainsKey(shipIndex))
                {
                    Ship = mapComp.ShipsOnMapNew[shipIndex];
                }
                else
                    Log.Error("SOS2: ship index not found: " + shipIndex);
            }
        }
        public SoShipCache Ship
        {
            private set; get;
        }

        public bool TacCon = false;
        bool selected = false;
        List<string> fail;

        public ShipHeatMapComp mapComp;
        public CompShipHeat heatComp;
        public CompPowerTrader powerComp;
        public CompMannable mannableComp;
        public int heat = 0;
        public int heatCap = 0;
        public float heatRat = 0;
        public float heatRatDep = 0;
        public int power = 0;
        public int powerCap = 0;
        public float powerRat = 0;
        private bool CanLaunchNow
		{
			get
			{
				return !ShipUtility.LaunchFailReasons(this).Any<string>();
			}
		}

        private List<string> InterstellarFailReasons()
        {
            List<string> result = new List<string>();
            if (!Ship.Bridges.Any(b => b.def == ThingDefOf.Ship_ComputerCore || b.def == ResourceBank.ThingDefOf.ShipArchotechSpore))
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipReportMissingPart") + ": " + ThingDefOf.Ship_ComputerCore.label);
            if (!Ship.Sensors.Any(b => b.def == ThingDefOf.Ship_SensorCluster || b.def == ResourceBank.ThingDefOf.Ship_SensorClusterAdv))
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipReportMissingPart") + ": " + ThingDefOf.Ship_SensorCluster.label);
            int playerJTPower = 0;
            if (Ship.Engines.Any(e => e.Props.energy))
            {
                foreach (CompEngineTrail e in Ship.Engines.Where(e => e.Props.energy))
                {
                    int mult = 10000;
                    if (e.parent.def.size.x > 3)
                        mult = 30000;
                    playerJTPower += mult;
                }
            }
            if (playerJTPower == 0)
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipReportMissingPart") + ": " + ResourceBank.ThingDefOf.Ship_Engine_Interplanetary.label);
            else if (playerJTPower < Ship.Mass)
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipNeedsMoreJTEngines"));
            if (PowerComp.PowerNet?.CurrentStoredEnergy() < Ship.Mass)
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipNeedsMorePower", Ship.Mass));
            if (Map.Parent.def != ResourceBank.WorldObjectDefOf.ShipOrbiting)
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipOnEnemyMap")); //td desc from non stable map
            if (ShipCountdown.CountingDown)
                result.Add("ShipAlreadyCountingDown".Translate());
            return result;
        }

		[DebuggerHidden]
		public override IEnumerable<Gizmo> GetGizmos()
		{
            var heatNet = heatComp.myNet;
            bool ckActive = Prefs.DevMode && ShipInteriorMod2.HasSoS2CK;
            if (!TacCon && Faction != Faction.OfPlayer)
            {
                if (Prefs.DevMode)
                {
                    Command_Action hackMe = new Command_Action
                    {
                        action = delegate
                        {
                            Success(null);
                        },
                        defaultLabel = "Dev: Hack bridge",
                        defaultDesc = "Instantly take control of this ship"
                    };
                    yield return hackMe;
                }
                if (!ckActive) //allows other gizmos in CK mode
                    yield break;
            }
			foreach (Gizmo c in base.GetGizmos())
			{
				yield return c;
            }
            if (TacCon || heatNet == null || Faction != Faction.OfPlayer || !powerComp.PowerOn || Ship == null)
                yield break;
            if (!selected)
            {
                fail = InterstellarFailReasons();
                selected = true;
            }
            //if (!mapComp.InCombat)
            {
                Command_Action renameShip = new Command_Action
                {
                    action = delegate
                    {
                        Find.WindowStack.Add(new Dialog_NameShip(Ship));
                    },
                    hotKey = KeyBindingDefOf.Misc1,
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/RenameZone"),
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideRename"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideRenameDesc")
                };
                if (ShipCountdown.CountingDown)
                {
                    renameShip.Disable();
                }
                yield return renameShip;
                Command_Action showReport = new Command_Action
                {
                    action = delegate
                    {
                        float capacity = 0;
                        foreach (CompPowerBattery bat in GetComp<CompPower>().PowerNet.batteryComps)
                            capacity += bat.Props.storedEnergyMax;
                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipName", Ship.Name));
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsNotoriety", ShipInteriorMod2.WorldComp.PlayerFactionBounty));
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipMass", Ship.Mass));
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipMaxTakeoff", Ship.MaxTakeoff));
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipEnergy", PowerComp.PowerNet.CurrentStoredEnergy(), capacity));
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipHeat", heatComp.myNet.StorageUsed, heatComp.myNet.StorageCapacity, (heatComp.myNet.Depletion > 0) ? (" ("+ heatComp.myNet.StorageCapacityRaw + " maximum)") : ""));
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipCombatRating", Ship.Threat));
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipCombatThrust", Ship.ThrustRatio.ToString("F3")));
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine();
                        if (Map.IsSpace())
                        {
                            if (!fail.Any<string>())
                            {
                                stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipReportInterReady"));
                            }
                            else
                            {
                                stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipReportInterNotReady"));
                                foreach (string current in fail)
                                {
                                    stringBuilder.AppendLine();
                                    stringBuilder.AppendLine(current);
                                }
                            }
                        }
                        Dialog_MessageBox window = new Dialog_MessageBox(stringBuilder.ToString(), null, null, null, null, null, false, null, null);
                        Find.WindowStack.Add(window);
                    },
                    hotKey = KeyBindingDefOf.Misc4,
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchReport", true),
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideShipInfo"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideShipInfoDesc")
                };
                yield return showReport;
            }
            if (Map.IsSpace())
            {
                if (Ship.Pods.Any())
                {
                    Command_Action abandon = new Command_Action
                    {
                        action = delegate
                        {
                            CameraJumper.TryJump(CameraJumper.GetWorldTarget(this.Map.Parent));
                            Find.WorldSelector.ClearSelection();
                            Find.WorldTargeter.BeginTargeting(new Func<GlobalTargetInfo, bool>(this.ChoseWorldTarget), true, CompShuttleLaunchable.TargeterMouseAttachment, true, delegate
                            {
                            }, delegate (GlobalTargetInfo target)
                            {
                                if (!target.IsValid)
                                {
                                    return null;
                                }
                                if (target.Map != null && target.Map.Parent != null && target.Map.Parent.def == ResourceBank.WorldObjectDefOf.ShipOrbiting)
                                {
                                    return null;
                                }
                                if (target.WorldObject != null && target.WorldObject.def == ResourceBank.WorldObjectDefOf.ShipOrbiting)
                                    return null;
                                if (target.WorldObject != null && (target.WorldObject is SpaceSite || target.WorldObject is MoonBase))
                                    return TranslatorFormattedStringExtensions.Translate("MustLaunchFromOrbit");
                                return null;
                            });
                        },
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandScuttleShip"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandScuttleShipDesc"),
                        icon = ContentFinder<Texture2D>.Get("UI/Scuttle_Icon")
                    };
                    if (Ship.Pods.Where(pod => pod.parent is Building_CryptosleepCasket c && c.GetDirectlyHeldThings().Any()).Count() == 0)
                    {
                        abandon.disabled = true;
                        abandon.disabledReason = TranslatorFormattedStringExtensions.Translate("ShipInsideNoLoadedPods");
                    }
                    yield return abandon;
                }
                if (heatNet.Cloaks.Any())
                {
					bool anyCloakOn = heatNet.AnyCloakOn();
                    Command_Toggle toggleCloak = new Command_Toggle
                    {
                        toggleAction = delegate
                        {
                            foreach (CompShipHeatSource h in heatNet.Cloaks)
                            {
                                ((Building_ShipCloakingDevice)h.parent).flickComp.SwitchIsOn = !anyCloakOn;
                            }
                        },
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideToggleCloak"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideToggleCloakDesc"),
                        isActive = () => anyCloakOn
                    };
                    if (anyCloakOn)
                        toggleCloak.icon = ContentFinder<Texture2D>.Get("UI/CloakingDeviceOn");
                    else
                        toggleCloak.icon = ContentFinder<Texture2D>.Get("UI/CloakingDeviceOff");
                    yield return toggleCloak;
                }
                if (heatNet.Shields.Any())
                {
					bool anyShieldOn = heatNet.AnyShieldOn();
					Command_Toggle toggleShields = new Command_Toggle
					{
						toggleAction = delegate
						{
							foreach (var b in heatNet.Shields)
							{
								b.flickComp.SwitchIsOn = !anyShieldOn;
							}
						},
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideToggleShields"),
						defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideToggleShieldsDesc"),
                        icon = ContentFinder<Texture2D>.Get("UI/Shield_On"),
                        isActive = () => anyShieldOn
					};
					yield return toggleShields;
                }
                //incombat
                if (mapComp.InCombat)
                {
                    Command_Action escape = new Command_Action
                    {
                        action = delegate
                        {
                            if (mapComp.ShipCombatTargetMap.mapPawns.AnyColonistSpawned)
                                PawnsAbandonWarning();
                            else
                                mapComp.EndBattle(Map, true);
                        },
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipCombatEscape"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipCombatEscapeDesc"),
                        icon = ContentFinder<Texture2D>.Get("UI/Escape_Icon")
                    };
                    if (mapComp.Range < 395)
                    {
                        escape.disabled = true;
                        escape.disabledReason = TranslatorFormattedStringExtensions.Translate("NotAtMaxShipRange");
                    }
                    yield return escape;
                    Command_Action withdraw = new Command_Action
                    {
                        action = delegate
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmWithdrawShipCombat", delegate
                            {
                                mapComp.RemoveShipFromBattle(shipIndex);
                            }));
                        },
                        icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_Withdraw"),
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandWithdrawShip"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandWithdrawShipDesc"),
                    };
                    if (mapComp.IsGraveyard || mapComp.ShipsOnMapNew.Count(s => !s.Value.IsWreck) <= 1)
                    {
                        withdraw.disabled = true;
                        withdraw.disabledReason = TranslatorFormattedStringExtensions.Translate("CommandWithdrawShipLast");
                    }
                    yield return withdraw;
                    if (mapComp.Maintain == true || mapComp.Heading != -1)
                    {
                        Command_Action retreat = new Command_Action
                        {
                            action = delegate
                            {
                                mapComp.Heading = -1;
                                mapComp.Maintain = false;
                                mapComp.callSlowTick = true;
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipCombatRetreat"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipCombatRetreatDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_Retreat")
                        };
                        yield return retreat;
                    }
                    if (mapComp.Maintain == false)
                    {
                        Command_Action maintain = new Command_Action
                        {
                            action = delegate
                            {
                                mapComp.Maintain = true;
                                mapComp.RangeToKeep = mapComp.OriginMapComp.Range;
                                mapComp.callSlowTick = true;
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipCombatMaintain"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipCombatMaintainDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_Maintain")
                        };
                        yield return maintain;
                    }
                    if (mapComp.Maintain == true || mapComp.Heading != 0)
                    {
                        Command_Action stop = new Command_Action
                        {
                            action = delegate
                            {
                                mapComp.Heading = 0;
                                mapComp.Maintain = false;
                                mapComp.callSlowTick = true;
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipCombatStop"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipCombatStopDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_Stop")
                        };
                        yield return stop;
                    }
                    if (mapComp.Maintain == true || mapComp.Heading != 1)
                    {
                        Command_Action advance = new Command_Action
                        {
                            action = delegate
                            {
                                mapComp.Heading = 1;
                                mapComp.Maintain = false;
                                mapComp.callSlowTick = true;
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipCombatAdvance"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipCombatAdvanceDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_Advance")
                        };
                        yield return advance;
                    }
                    if (heatNet.Turrets.Any())
                    {
                        Command_Action selectWeapons = new Command_Action
                        {
                            action = delegate
                            {
                                Find.Selector.Deselect(this);
                                foreach (CompShipHeat h in heatNet.Turrets)
                                {
                                    Find.Selector.Deselect(this);
                                    Find.Selector.Select(h.parent);
                                }
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideSelectWeapons"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideSelectWeaponsDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Select_All_Weapons_Icon")
                        };
                        yield return selectWeapons;
                    }
                    if (heatNet.Sinks.Any())
                    {
                        Command_Action vent = new Command_Action
                        {
                            action = delegate
                            {
                                heatNet.StartVent();
                            },
                            icon = ContentFinder<UnityEngine.Texture2D>.Get("UI/ActiveVent"),
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipHeatPurge"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipHeatPurgeDesc"),
                            disabled = heatNet.venting || heatNet.RatioInNetworkRaw < 0.1f,
                            disabledReason = heatNet.venting ? TranslatorFormattedStringExtensions.Translate("ShipHeatPurgeVenting") : TranslatorFormattedStringExtensions.Translate("ShipHeatPurgeNotEnough")
                        };
                        yield return vent;
                    }
                }
                //engine burn
                else if (Map.gameConditionManager.ConditionIsActive(ResourceBank.GameConditionDefOf.SpaceDebris))
                {
                    List<SoShipCache> ships = mapComp.ShipsOnMapNew.Values.Where(s => s.CanMove).ToList();
                    bool anyEngineOn = ships.Any(s => s.Engines.Any(e => e.active));
                    Command_Toggle toggleEngines = new Command_Toggle
                    {
                        toggleAction = delegate
                        {
                            foreach (SoShipCache ship in ships)
                            {
                                if (anyEngineOn)
                                    ship.EnginesOff();
                                else
                                    ship.EnginesOn();
                            }
                        },
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideToggleEngines"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideToggleEnginesDesc"),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip"),
                        isActive = () => anyEngineOn
                    };
                    yield return toggleEngines;
                }
                //not incombat or in event
                else
                {
                    //space - move, land
                    if (!mapComp.IsGraveyard || ckActive)
                    {
                        Command_Action gotoNewWorld = new Command_Action
                        {
                            action = delegate
                            {
                                ShipInteriorMod2.SpaceTravelWarning(delegate { ShipInteriorMod2.SaveShipFlag = true; ShipCountdown.InitiateCountdown(this); });
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipPlanetLeave"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipPlanetLeaveDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Glitterworld_end_icon", true)
                        };

                        if (fail.Any())
                            gotoNewWorld.Disable(fail.First());
                        yield return gotoNewWorld;
                        /*if (ShipInteriorMod2.PastWorldTracker != null && ShipInteriorMod2.PastWorldTracker.PastWorlds.Count > 0)
                        {
                            Command_Action returnToPrevWorld = new Command_Action
                            {
                                action = delegate
                                {
                                    ShipInteriorMod2.ColonyAbandonWarning(delegate { ShipInteriorMod2.ReturnToPreviousWorld(this.Map, this); });
                                },
                                defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipPlanetReturn"),
                                defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipPlanetReturnDesc"),
                                icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", true)
                            };
                            if (fail.Any())
                                returnToPrevWorld.Disable(fail.First());
                            yield return returnToPrevWorld;
                        }*/
                        Command_Action moveShip = new Command_Action
                        {
                            action = delegate
                            {
                                if (Ship.HasPilotRCSAndFuel(0.01f, true))
                                    ShipInteriorMod2.MoveShipSketch(this, this.Map);
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideMove"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Land_Ship")
                        };
                        //flip
                        Command_Action moveShipFlip = new Command_Action
                        {
                            action = delegate
                            {
                                if (Ship.HasPilotRCSAndFuel(0.01f, true))
                                    ShipInteriorMod2.MoveShipSketch(this, this.Map, 2);
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveFlip"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveFlipDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Flip_Ship")
                        };
                        //CCW rot
                        Command_Action moveShipRot = new Command_Action
                        {
                            action = delegate
                            {
                                if (Ship.HasPilotRCSAndFuel(0.01f, true))
                                    ShipInteriorMod2.MoveShipSketch(this, this.Map, 1);
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveRot"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveRotDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Rotate_Ship")
                        };
                        if (ShipCountdown.CountingDown)
                        {
                            moveShip.Disable();
                            moveShipFlip.Disable();
                            moveShipRot.Disable();
                        }
                        else if (Ship.BuildingsNonRot.Any())
                        {
                            moveShipRot.Disable();
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipInsideMoveRotNo"));
                            // Limiting the list of non-rotatable objects.
                            int maxCount = 5;
                            int addedLines = 0;
                            bool devMode = Prefs.DevMode;
                            foreach (Building bd in Ship.BuildingsNonRot)
                            {
                                // Printing more detailed info for modders.
                                if (devMode)
                                    sb.AppendFormat("{0} ({1})\n", bd.def.label, bd.def.defName);
                                else
                                    sb.AppendLine(bd.def.label);
                                addedLines++;
                                if (addedLines > maxCount)
                                    break;
                            }
                            if (addedLines < Ship.BuildingsNonRot.Count)
                                sb.AppendLine("...");
                            moveShipRot.disabledReason = sb.ToString();
                        }
                        yield return moveShip;
                        yield return moveShipFlip;
                        yield return moveShipRot;

                        //land - dev mode can "land" in space with CK enabled
                        List<Map> landableMaps = new List<Map>();
                        foreach (Map m in Find.Maps)
                        {
                            if ((!m.IsSpace() && !m.IsTempIncidentMap) || (ckActive && m != this.Map))
                                landableMaps.Add(m);
                        }
                        foreach (Map m in landableMaps)
                        {
                            Command_Action landShip = new Command_Action
                            {
                                action = delegate
                                {
                                    if (Ship.HasPilotRCSAndFuel(0.1f))
                                        ShipInteriorMod2.MoveShipSketch(this, m, 0);
                                },
                                defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideLand") + " (" + m.Parent.Label + ")",
                                defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideLandDesc") + m.Parent.Label,
                                icon = ContentFinder<Texture2D>.Get("UI/Planet_Landing_Icon")
                            };
                            if (ShipCountdown.CountingDown)
                            {
                                landShip.Disable();
                            }
                            yield return landShip;
                        }
                        if (Ship.AICores.Any() && Ship.BuildingsDestroyed.Any())
                        {
                            Command_Action rebuildShip = new Command_Action
                            {
                                action = delegate
                                {
                                    foreach (var bp in Ship.BuildingsDestroyed)
                                    {
                                        GenConstruct.PlaceBlueprintForBuild(bp.Item1, bp.Item2, Map, bp.Item3, Faction, null);
                                    }
                                    Ship.BuildingsDestroyed.Clear();
                                },
                                defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipRebuild"),
                                defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipRebuildDesc"),
                                icon = ContentFinder<Texture2D>.Get("UI/Glitterworld_end_icon", true)
                            };
                            yield return rebuildShip;
                        }
                        //endgame missions
                        if (ResourceBank.ResearchProjectDefOf.ArchotechPillarA.IsFinished && !ShipInteriorMod2.WorldComp.Unlocks.Contains("ArchotechPillarA"))
                        {
                            Command_Action goGetThatPillarA = new Command_Action
                            {
                                action = delegate
                                {
                                    AttackableShip station = new AttackableShip
                                    {
                                        attackableShip = DefDatabase<EnemyShipDef>.GetNamed("StationArchotechGarden"),
                                        spaceNavyDef = DefDatabase<SpaceNavyDef>.GetNamed("Mechanoid_SpaceNavy"),
                                        shipFaction = Faction.OfMechanoids
                                    };
                                    mapComp.StartShipEncounter(this, station);
                                },
                                icon = ContentFinder<Texture2D>.Get("UI/ArchotechStation_Icon_Quest"),
                                defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipQuestPillarA"),
                                defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipQuestPillarADesc")
                            };
                            yield return goGetThatPillarA;
                        }
                        if (ResourceBank.ResearchProjectDefOf.ArchotechPillarB.IsFinished && !ShipInteriorMod2.WorldComp.Unlocks.Contains("ArchotechPillarB") && !Find.WorldObjects.AllWorldObjects.Any(ob => ob is MoonBase))
                        {
                            Command_Action goGetThatPillarB = new Command_Action
                            {
                                action = delegate
                                {
                                    float CR = Mathf.Max(mapComp.MapThreat() * 0.9f, 1500);
                                    SpaceNavyDef mechNavyDef = DefDatabase<SpaceNavyDef>.GetNamed("Mechanoid_SpaceNavy");
                                    AttackableShip attacker = new AttackableShip
                                    {
                                        spaceNavyDef = mechNavyDef,
                                        attackableShip = ShipInteriorMod2.RandomValidShipFrom(mechNavyDef.enemyShipDefs, CR, false, true),
                                        shipFaction = Faction.OfMechanoids
                                    };
                                    mapComp.StartShipEncounter(this, attacker);
                                    MapParent site = (MapParent)ShipInteriorMod2.GenerateSite("MoonPillarSite");
                                },
                                icon = ContentFinder<Texture2D>.Get("UI/Moon_Icon_Quest"),
                                defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipQuestPillarB"),
                                defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipQuestPillarBDesc")
                            };
                            yield return goGetThatPillarB;
                        }
                        //dev stuff
                        if (Prefs.DevMode)
                        {
                            Command_Action startBattle = new Command_Action
                            {
                                action = delegate
                                {
                                    if (Find.TickManager.Paused)
                                        Find.TickManager.TogglePaused();
                                    mapComp.StartShipEncounter(this);
                                },
                                defaultLabel = "Dev: Start ship battle",
                            };
                            startBattle.hotKey = KeyBindingDefOf.Misc9;
                            yield return startBattle;
                            Command_Action startFleetBattle = new Command_Action
                            {
                                action = delegate
                                {
                                    if (Find.TickManager.Paused)
                                        Find.TickManager.TogglePaused();
                                    mapComp.StartShipEncounter(this, fleet: true);
                                },
                                defaultLabel = "Dev: Start random fleet battle",
                            };
                            startFleetBattle.hotKey = KeyBindingDefOf.Misc10;
                            yield return startFleetBattle;
                            Command_Action loadshipdef = new Command_Action
                            {
                                action = delegate
                                {
                                    Find.WindowStack.Add(new Dialog_LoadShipDef("shipdeftoload", this.Map));
                                },
                                defaultLabel = "Dev: load ship from database",
                            };
                            loadshipdef.hotKey = KeyBindingDefOf.Misc11;
                            yield return loadshipdef;
                        }
                        //attack passing ship
                        if (this.Map.passingShipManager.passingShips.Any())
                        {
                            foreach (PassingShip passingShip in this.Map.passingShipManager.passingShips)
                            {
                                if (passingShip is TradeShip)
                                {
                                    Command_Action attackTradeShip = new Command_Action
                                    {
                                        action = delegate
                                        {
                                            mapComp.StartShipEncounter(this, passingShip);
                                            if (ModsConfig.IdeologyActive)
                                                IdeoUtility.Notify_PlayerRaidedSomeone(this.Map.mapPawns.FreeColonists);
                                        },
                                        icon = ContentFinder<Texture2D>.Get("UI/IncomingShip_Icon_Trader"),
                                        defaultLabel = "Attack " + passingShip.FullTitle,
                                        defaultDesc = "Attempt an act of space piracy against " + passingShip.FullTitle
                                    };
                                    yield return attackTradeShip;
                                }
                                else if (passingShip is AttackableShip)
                                {
                                    Command_Action attackAttackableShip = new Command_Action
                                    {
                                        action = delegate
                                        {
                                            mapComp.StartShipEncounter(this, passingShip);
                                            if (ModsConfig.IdeologyActive)
                                                IdeoUtility.Notify_PlayerRaidedSomeone(this.Map.mapPawns.FreeColonists);
                                        },
                                        icon = ContentFinder<Texture2D>.Get("UI/IncomingShip_Icon_Pirate"),
                                        defaultLabel = "Attack " + passingShip.FullTitle,
                                        defaultDesc = "Attempt to engage " + passingShip.FullTitle
                                    };
                                    yield return attackAttackableShip;
                                }
                                else if (passingShip is DerelictShip)
                                {
                                    Command_Action approachDerelictShip = new Command_Action
                                    {
                                        action = delegate
                                        {
                                            mapComp.StartShipEncounter(this, passingShip);
                                        },
                                        icon = ContentFinder<Texture2D>.Get("UI/IncomingShip_Icon_Quest"),
                                        defaultLabel = "Approach " + passingShip.FullTitle,
                                        defaultDesc = "Approach to investigate " + passingShip.FullTitle
                                    };
                                    yield return approachDerelictShip;
                                }
                            }
                        }
                    }
                    //in graveyard, not player map - capture/retrieve
                    else if (this.Map.Parent.def != ResourceBank.WorldObjectDefOf.ShipOrbiting)
                    {
                        Command_Action captureShip = new Command_Action
                        {
                            action = delegate
                            {
                                if (mapComp.ShipCombatOriginMap == null)
                                {
                                    Map m = ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def == ResourceBank.WorldObjectDefOf.ShipOrbiting).FirstOrDefault()).Map;
                                    if (m == null)
                                        m = ShipInteriorMod2.GeneratePlayerShipMap(this.Map.Size);
                                    mapComp.ShipCombatOriginMap = m;
                                }
                                ShipInteriorMod2.MoveShipSketch(this, mapComp.ShipCombatOriginMap, 0);
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideCaptureShip"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideCaptureShipDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Capture_Ship_Icon")
                        };
                        if (mapComp.ShipFaction == Faction.OfPlayer)
                        {
                            captureShip.defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideReturnShip");
                            captureShip.defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideReturnShipDesc");
                            captureShip.icon = ContentFinder<Texture2D>.Get("UI/Planet_Landing_Icon");
                        }
                        if (ShipCountdown.CountingDown || mapComp.OriginMapComp.InCombat)
                        {
                            captureShip.Disable();
                        }
                        yield return captureShip;
                    }
                }
            }
            else //launch
            {
                Command_Action launch = new Command_Action()
                {
                    action = new Action(TryLaunch),
                    hotKey = KeyBindingDefOf.Misc1,
                    defaultLabel = "CommandShipLaunch".Translate(),
                    defaultDesc = "CommandShipLaunchDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", true)
                };
                if (!CanLaunchNow)
                {
                    launch.Disable(ShipUtility.LaunchFailReasons(this).First<string>());
                }
                else if (ShipCountdown.CountingDown)
                {
                    launch.Disable(null);
                }
                yield return launch;
            }
            /*if (Prefs.DevMode)
            {
                Command_Action spawnAIPawn = new Command_Action
                {
                    action = delegate
                    {
                        PawnGenerationRequest req = new PawnGenerationRequest(PawnKindDef.Named("SoSHologram"), Faction.OfPlayer, PawnGenerationContext.NonPlayer, 0, true, true, false, false, false, false, 0);
                        Pawn p = PawnGenerator.GeneratePawn(req);
                        p.story.childhood = ShipInteriorMod2.hologramBackstory;
                        p.story.hairColor = new Color(0, 0.5f, 1, 0.6f);
                        p.Name = new NameTriple("Charlon", "Charlon", "Whitestone");
                        p.Position = this.Position;
                        p.relations = new Pawn_RelationsTracker(p);
                        p.interactions = new Pawn_InteractionsTracker(p);
                        while (p.story.traits.allTraits.Count() > 0)
                        {
                            p.story.traits.allTraits.RemoveLast();
                        }
                        while (p.story.traits.allTraits.Count() < 3)
                        {
                            p.story.traits.GainTrait(new Trait(DefDatabase<TraitDef>.AllDefs.Where(t => t.exclusionTags.Contains("AITrait")).RandomElement()));
                        }
                        p.SpawnSetup(this.Map, false);
                    },
                    defaultLabel = "Dev: Spawn AI pawn"
                };
                yield return spawnAIPawn;
            }*/
            //TODO add "solar system" option
        }
		private void TryLaunch()
		{
			if (CanLaunchNow)
			{
                if (Find.WorldObjects.AllWorldObjects.Any(ob => ob.def == ResourceBank.WorldObjectDefOf.ShipOrbiting))
                {
                    MapParent parent = ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def == ResourceBank.WorldObjectDefOf.ShipOrbiting).FirstOrDefault());
                    Map m = parent.Map;
                    if(m==null)
                    {
                        parent.Destroy();
                        ShipCountdown.InitiateCountdown(this);
                        return;
                    }
                    ShipInteriorMod2.MoveShipSketch(this, m, 0);
                }
                else
                {
                    ShipCountdown.InitiateCountdown(this);
                }
			}
		}
        private bool ChoseWorldTarget(GlobalTargetInfo target)
        {
            foreach (CompCryptoLaunchable pod in Ship.Pods)
            {
                pod.ChoseWorldTarget(target);
            }
            return true;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref ShipName, "ShipName");
        }
        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            text += "\nShip Name: " + Ship.Name;
            return text;
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            heatComp = GetComp<CompShipHeat>();
            powerComp = GetComp<CompPowerTrader>();
            mannableComp = GetComp<CompMannable>();
            mapComp = Map.GetComponent<ShipHeatMapComp>();
            if (!mapComp.MapRootListAll.Contains(this))
                mapComp.MapRootListAll.Add(this);
            //Log.Message("Spawned: " + this + " to " + this.Map);
            if (this.TryGetComp<CompShipHeatTacCon>() != null)
            {
                TacCon = true;
            }
            ShipIndex = shipIndex;
            if (!Map.IsSpace())
                return;
            var countdownComp = Map.Parent.GetComponent<TimedForcedExitShip>();
            if (countdownComp != null && !mapComp.IsGraveyard && countdownComp.ForceExitAndRemoveMapCountdownActive)
            {
                countdownComp.ResetForceExitAndRemoveMapCountdown();
                Messages.Message("ShipBurnupPlayerPrevented", this, MessageTypeDefOf.PositiveEvent);
            }
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
        }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mapComp.MapRootListAll.Contains(this))
                mapComp.MapRootListAll.Remove(this);
            if (Map.IsSpace() && mapComp.MapRootListAll.NullOrEmpty()) //last bridge on player map - deorbit
            {
                var countdownComp = Map.Parent.GetComponent<TimedForcedExitShip>();
                if (countdownComp != null && !countdownComp.ForceExitAndRemoveMapCountdownActive)
                {
                    countdownComp.StartForceExitAndRemoveMapCountdown();
                    Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("ShipBurnupPlayer"), TranslatorFormattedStringExtensions.Translate("ShipBurnupPlayerDesc", countdownComp.ForceExitAndRemoveMapCountdownTimeLeftString), LetterDefOf.ThreatBig);
                }
            }
            base.DeSpawn(mode);
        }
        public override void Tick()
        {
            base.Tick();
            if (selected && !Find.Selector.IsSelected(this))
                selected = false;
            //td rem this?
            int ticks = Find.TickManager.TicksGame;
            if (ticks % 8 == 0)
                UpdateUI();

            if (ticks % 250 == 0)
                TickRare();
        }
        private void UpdateUI() //update UI on primary
        {
            if (Ship.Core != this)
                return;
            PowerNet powerNet = powerComp.PowerNet;
            powerCap = 0;
            powerRat = 0;
            if (powerNet != null)
            {
                power = Mathf.FloorToInt(powerNet.CurrentStoredEnergy());
                float cap = 0;
                foreach (CompPowerBattery bat in powerNet.batteryComps)
                    cap += bat.Props.storedEnergyMax;
                powerCap = Mathf.CeilToInt(cap);
                if (cap > 0)
                    powerRat = power / cap;
            }
            else
            {
                power = 0;
            }
            ShipHeatNet heatNet = heatComp.myNet;
            if (heatNet != null)
            {
                heat = Mathf.FloorToInt(heatComp.myNet.StorageUsed);
                heatCap = Mathf.CeilToInt(heatComp.myNet.StorageCapacity);
                heatRat = heatComp.myNet.RatioInNetworkRaw;
                heatRatDep = heatComp.myNet.DepletionRatio;
            }
            else
            {
                heat = 0;
                heatCap = 0;
                heatRat = 0;
                heatRatDep = 0;
            }
        }

        public void HackMe(Pawn pawn)
        {
            if (Rand.Chance(0.05f * pawn.skills.GetSkill(SkillDefOf.Intellectual).levelInt))
            {
                Success(pawn);
            }
            else if (Rand.Chance(0.05f * (20 - pawn.skills.GetSkill(SkillDefOf.Intellectual).levelInt)))
            {
                CriticalFailure(pawn);
            }
            else
            {
                Failure(pawn);
            }
        }
        private void Success(Pawn pawn)
        {
            if (ShipName == "Psychic Amplifier")
            {
                Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoSPsychicAmplifierCaptured"), TranslatorFormattedStringExtensions.Translate("SoSPsychicAmplifierCapturedDesc"), LetterDefOf.PositiveEvent);
                ShipInteriorMod2.WorldComp.Unlocks.Add("ArchotechSpore");
            }
            if (pawn != null)
                pawn.skills.GetSkill(SkillDefOf.Intellectual).Learn(2000);
            if (mapComp.InCombat)
            {
                mapComp.RemoveShipFromBattle(ShipIndex, this, Faction.OfPlayer);
            }
            else
            {
				Ship.Capture(Faction.OfPlayer);
            }
        }
        private void Failure(Pawn pawn)
        {
            Messages.Message("Hack failed", null, MessageTypeDefOf.CautionInput);
        }
        private void CriticalFailure(Pawn pawn)
        {
            Messages.Message("Hack failed", null, MessageTypeDefOf.CautionInput);
        }
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            if (Faction != Faction.OfPlayer)
                options.Add(new FloatMenuOption("Hack", delegate { Job capture = new Job(DefDatabase<JobDef>.GetNamed("HackEnemyShip"), this); selPawn.jobs.TryTakeOrderedJob(capture); }));
            else if (this.AllComps != null)
            {
                for (int i = 0; i < AllComps.Count; i++)
                {
                    foreach (FloatMenuOption item in AllComps[i].CompFloatMenuOptions(selPawn))
                    {
                        options.Add(item);
                    }
                }
            }
            return options;
        }
        public void PawnsAbandonWarning()
        {
            DiaNode theNode = new DiaNode(TranslatorFormattedStringExtensions.Translate("ShipCombatAbandonPawns"));
            DiaOption accept = new DiaOption("Accept");
            accept.resolveTree = true;
            accept.action = delegate { mapComp.EndBattle(this.Map, true); };
            theNode.options.Add(accept);

            DiaOption cancel = new DiaOption("Cancel");
            cancel.resolveTree = true;
            theNode.options.Add(cancel);

            Dialog_NodeTree dialog_NodeTree = new Dialog_NodeTree(theNode, true, false, null);
            dialog_NodeTree.silenceAmbientSound = false;
            dialog_NodeTree.closeOnCancel = true;
            Find.WindowStack.Add(dialog_NodeTree);
        }
    }
}