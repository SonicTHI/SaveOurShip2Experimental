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
using RimworldMod.VacuumIsNotFun;

namespace RimWorld
{
	public class Building_ShipBridge : Building
    {
        public string ShipName = "Unnamed Ship";
        public int ShipThreat = 0;
        public int ShipMass = 0;
        public float ShipMaxTakeoff = 0;
        public float ShipThrust = 0;
        public float ShipHeatCap = 0;
        public float ShipHeat = 0;
        public int shipIndex = -1;
        public bool TacCon = false;
        public SortedSet<ThingDef> nonRotatableObjects = new SortedSet<ThingDef>();

        bool selected = false;
        public List<Building> cachedShipParts;
        List<Building> cachedPods;
        List<string> fail;

        public ShipHeatMapComp mapComp;
        public CompShipHeat heatComp;
        public CompPowerTrader powerComp;
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
            if (!cachedShipParts.Any((Building pa) => pa.def == ThingDefOf.Ship_ComputerCore || pa.def == ResourceBank.ThingDefOf.ShipArchotechSpore))
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipReportMissingPart") + ": " + ThingDefOf.Ship_ComputerCore.label);
            if (!cachedShipParts.Any((Building pa) => pa.def == ThingDefOf.Ship_SensorCluster || pa.def ==ThingDef.Named("Ship_SensorClusterAdv")))
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipReportMissingPart") + ": " + ThingDefOf.Ship_SensorCluster.label);
            int playerJTPower = 0;
            if (cachedShipParts.Any((Building pa) => pa.TryGetComp<CompEngineTrail>() != null))
            {
                foreach (Building b in cachedShipParts.Where(b => b.TryGetComp<CompEngineTrail>() != null && b.GetComp<CompEngineTrail>().Props.energy))
                {
                    int mult = 10000;
                    if (b.def.size.x > 3)
                        mult = 30000;
                    playerJTPower += mult;
                }
            }
            float playerJTReq = this.Map.listerBuildings.allBuildingsColonist.Count * 2.5f;
            if (playerJTPower == 0)
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipReportMissingPart") + ": " + ThingDef.Named("Ship_Engine_Interplanetary").label);
            else if (playerJTPower < playerJTReq)
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipNeedsMoreJTEngines"));
            if (this.PowerComp.PowerNet?.CurrentStoredEnergy() < playerJTReq)
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipNeedsMorePower", playerJTReq));
            if (mapComp.ShipCombatMaster)
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipOnEnemyMap"));
            if (ShipCountdown.CountingDown)
                result.Add("ShipAlreadyCountingDown".Translate());
            return result;
        }

		[DebuggerHidden]
		public override IEnumerable<Gizmo> GetGizmos()
		{
            var heatNet = this.TryGetComp<CompShipHeat>().myNet;
            bool ckActive = Prefs.DevMode && ModLister.HasActiveModWithName("Save Our Ship Creation Kit");
            if (!TacCon)
            {
                if (this.Faction != Faction.OfPlayer)
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
                    if (!ckActive)
                        yield break;
                }
                if (!selected)
                {
                    Log.Message("recached: " + this);
                    cachedShipParts = ShipUtility.ShipBuildingsAttachedTo(this);
                    cachedPods = new List<Building>();
                    nonRotatableObjects.Clear();
                    foreach (Building b in cachedShipParts)
                    {
                        if (b.TryGetComp<CompCryptoLaunchable>() != null)
                        {
                            cachedPods.Add(b);
                        }
                        if (b.def.rotatable == false && b.def.size.x != b.def.size.z)
                        {
                            nonRotatableObjects.Add(b.def);
                        }
                    }
                    fail = InterstellarFailReasons();
                    selected = true;
                }
            }
			foreach (Gizmo c in base.GetGizmos())
			{
				yield return c;
            }
            if (TacCon || heatNet == null || this.Faction != Faction.OfPlayer || !powerComp.PowerOn)
                yield break;
            if (!mapComp.InCombat)
            {
                Command_Action renameShip = new Command_Action
                {
                    action = delegate
                    {
                        Find.WindowStack.Add(new Dialog_NameShip(this));
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
                        RecalcStats();
                        float capacity = 0;
                        foreach (CompPowerBattery bat in this.GetComp<CompPower>().PowerNet.batteryComps)
                            capacity += bat.Props.storedEnergyMax;
                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipName", ShipName));
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsNotoriety", Find.World.GetComponent<PastWorldUWO2>().PlayerFactionBounty));
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipMass", ShipMass));
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipMaxTakeoff", ShipMaxTakeoff));
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipEnergy", this.PowerComp.PowerNet.CurrentStoredEnergy(), capacity));
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipHeat", heatComp.myNet.StorageUsed, heatComp.myNet.StorageCapacity));
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipCombatRating", ShipThreat));
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipCombatThrust", ShipThrust.ToString("F3")));
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine();
                        if (this.Map.IsSpace())
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
            if (this.Map.IsSpace())
            {
                if (cachedPods.Any())
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
                                if (target.Map != null && target.Map.Parent != null && target.Map.Parent.def.defName.Equals("ShipOrbiting"))
                                {
                                    return null;
                                }
                                if (target.WorldObject != null && target.WorldObject.def.defName.Equals("ShipOrbiting"))
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
                    if (cachedPods.Where(pod => pod is Building_CryptosleepCasket c && c.GetDirectlyHeldThings().Any()).Count() == 0)
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
                    var originMapComp = mapComp.OriginMapComp;
                    var masterMapComp = mapComp.MasterMapComp;
                    Command_Action escape = new Command_Action
                    {
                        action = delegate
                        {
                            mapComp.EndBattle(this.Map, true);
                        },
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipCombatEscape"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipCombatEscapeDesc"),
                        icon = ContentFinder<Texture2D>.Get("UI/Escape_Icon")
                    };
                    if (masterMapComp.Range < 395)
                    {
                        escape.disabled = true;
                        escape.disabledReason = TranslatorFormattedStringExtensions.Translate("NotAtMaxShipRange");
                    }
                    yield return escape;
                    /*Command_Action withdraw = new Command_Action
                    {
                        action = delegate
                        {
                            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmWithdrawShipCombat", delegate
                            {
                                MapComp.RemoveShipFromBattle(shipIndex, this);
                            }));
                        },
                        icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_Withdraw"),
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("CommandWithdrawShip"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("CommandWithdrawShipDesc"),
                    };
                    if (MapComp.ShipsOnMap.Count <= 1)
                    {
                        withdraw.disabled = true;
                        withdraw.disabledReason = TranslatorFormattedStringExtensions.Translate("CommandWithdrawShipLast");
                    }
                    yield return withdraw;*/
                    if (masterMapComp.PlayerMaintain == true || originMapComp.Heading != -1)
                    {
                        Command_Action retreat = new Command_Action
                        {
                            action = delegate
                            {
                                originMapComp.Heading = -1;
                                masterMapComp.PlayerMaintain = false;
                                masterMapComp.callSlowTick = true;
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipCombatRetreat"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipCombatRetreatDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_Retreat")
                        };
                        yield return retreat;
                    }
                    if (masterMapComp.PlayerMaintain == false)
                    {
                        Command_Action maintain = new Command_Action
                        {
                            action = delegate
                            {
                                masterMapComp.PlayerMaintain = true;
                                masterMapComp.RangeToKeep = masterMapComp.Range;
                                masterMapComp.callSlowTick = true;
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipCombatMaintain"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipCombatMaintainDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_Maintain")
                        };
                        yield return maintain;
                    }
                    if (masterMapComp.PlayerMaintain == true || originMapComp.Heading != 0)
                    {
                        Command_Action stop = new Command_Action
                        {
                            action = delegate
                            {
                                originMapComp.Heading = 0;
                                masterMapComp.PlayerMaintain = false;
                                masterMapComp.callSlowTick = true;
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipCombatStop"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipCombatStopDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Ship_Icon_Stop")
                        };
                        yield return stop;
                    }
                    if (masterMapComp.PlayerMaintain == true || originMapComp.Heading != 1)
                    {
                        Command_Action advance = new Command_Action
                        {
                            action = delegate
                            {
                                originMapComp.Heading = 1;
                                masterMapComp.PlayerMaintain = false;
                                masterMapComp.callSlowTick = true;
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
                }
                //not incombat
                else
                {
                    //space - move, land
                    if (!mapComp.IsGraveyard || ckActive)
                    {
                        Command_Action gotoNewWorld = new Command_Action
                        {
                            action = delegate
                            {
                                WorldSwitchUtility.ColonyAbandonWarning(delegate { WorldSwitchUtility.SaveShip(this); });
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipPlanetLeave"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipPlanetLeaveDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", true)
                        };

                        if (fail.Any())
                            gotoNewWorld.Disable(fail.First());
                        yield return gotoNewWorld;
                        /*if (WorldSwitchUtility.PastWorldTracker != null && WorldSwitchUtility.PastWorldTracker.PastWorlds.Count > 0)
                        {
                            Command_Action returnToPrevWorld = new Command_Action
                            {
                                action = delegate
                                {
                                    WorldSwitchUtility.ColonyAbandonWarning(delegate { WorldSwitchUtility.ReturnToPreviousWorld(this.Map, this); });
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
                        else if (nonRotatableObjects.Any())
                        {
                            moveShipRot.Disable();
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipInsideMoveRotNo"));
                            // Limiting the list of non-rotatable objects.
                            int maxCount = 5;
                            int addedLines = 0;
                            bool devMode = Prefs.DevMode;
                            foreach (var bd in nonRotatableObjects)
                            {
                                // Printing more detailed info for modders.
                                if (devMode)
                                    sb.AppendFormat("{0} ({1})\n", bd.label, bd.defName);
                                else
                                    sb.AppendLine(bd.label);
                                addedLines++;
                                if (addedLines > maxCount )
                                    break;
                            }
                            if (addedLines < nonRotatableObjects.Count)
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
                        //New code for endgame
                        if (ResourceBank.ResearchProjectDefOf.ArchotechPillarB.IsFinished)
                        {
                            if (!WorldSwitchUtility.PastWorldTracker.Unlocks.Contains("ArchotechPillarA"))
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
                            if (!WorldSwitchUtility.PastWorldTracker.Unlocks.Contains("ArchotechPillarB") && !Find.WorldObjects.AllWorldObjects.Any(ob => ob is MoonBase))
                            {
                                Command_Action goGetThatPillarB = new Command_Action
                                {
                                    action = delegate
                                    {
                                        AttackableShip attacker = new AttackableShip
                                        {
                                            attackableShip = DefDatabase<EnemyShipDef>.GetNamed("MechSphereLarge"),
                                            spaceNavyDef = DefDatabase<SpaceNavyDef>.GetNamed("Mechanoid_SpaceNavy"),
                                            shipFaction = Faction.OfMechanoids
                                        };
                                        mapComp.StartShipEncounter(this, attacker);
                                        MapParent site = (MapParent)ShipInteriorMod2.GenerateArchotechPillarBSite();
                                    },
                                    icon = ContentFinder<Texture2D>.Get("UI/Moon_Icon_Quest"),
                                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipQuestPillarB"),
                                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipQuestPillarBDesc")
                                };
                                yield return goGetThatPillarB;
                            }
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
                    else if (!this.Map.Parent.def.defName.Equals("ShipOrbiting"))
                    {
                        Command_Action captureShip = new Command_Action
                        {
                            action = delegate
                            {
                                if (mapComp.ShipCombatOriginMap == null)
                                {
                                    Map m = ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def.defName.Equals("ShipOrbiting")).FirstOrDefault()).Map;
                                    if (m != null)
                                        mapComp.ShipCombatOriginMap = m;
                                    else
                                        ShipInteriorMod2.GeneratePlayerShipMap(this.Map.Size, this.Map);
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
                    action = new Action(this.TryLaunch),
                    hotKey = KeyBindingDefOf.Misc1,
                    defaultLabel = "CommandShipLaunch".Translate(),
                    defaultDesc = "CommandShipLaunchDesc".Translate(),
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", true)
                };
                if (!this.CanLaunchNow)
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
			if (this.CanLaunchNow)
			{
                if (Find.WorldObjects.AllWorldObjects.Any(ob => ob.def.defName.Equals("ShipOrbiting")))
                {
                    MapParent parent = ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def.defName.Equals("ShipOrbiting")).FirstOrDefault());
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
            foreach (Building pod in cachedPods)
            {
                pod.TryGetComp<CompCryptoLaunchable>().ChoseWorldTarget(target);
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
            text += "\nShip Name: " + ShipName;
            return text;
        }
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.heatComp = this.GetComp<CompShipHeat>();
            this.powerComp = this.GetComp<CompPowerTrader>();
            this.mapComp = this.Map.GetComponent<ShipHeatMapComp>();
            if (!mapComp.MapRootListAll.Contains(this))
                mapComp.MapRootListAll.Add(this);
            //Log.Message("Spawned: " + this + " to " + this.Map);
            if (this.def.defName.Equals("ShipConsoleTactical"))
            {
                TacCon = true;
            }
            var countdownComp = this.Map.Parent.GetComponent<TimedForcedExitShip>();
            if (countdownComp != null && !mapComp.IsGraveyard && countdownComp.ForceExitAndRemoveMapCountdownActive)
            {
                countdownComp.ResetForceExitAndRemoveMapCountdown();
                Messages.Message("ShipBurnupPlayerPrevented", this, MessageTypeDefOf.PositiveEvent);
            }
        }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mapComp.MapRootListAll.Contains(this))
                mapComp.MapRootListAll.Remove(this);
            base.DeSpawn(mode);
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (mapComp.MapRootListAll.Contains(this))
                mapComp.MapRootListAll.Remove(this);
            //Log.Message("Destroyed: " + this + " from " + this.Map);
            if (mapComp.InCombat) //force check on next tick
            {
                mapComp.BridgeDestroyed = true;
            }
            else if (this.Map.IsSpace() && mapComp.MapRootListAll.NullOrEmpty())
            {
                var countdownComp = this.Map.Parent.GetComponent<TimedForcedExitShip>();
                if (countdownComp != null && !countdownComp.ForceExitAndRemoveMapCountdownActive)
                {
                    countdownComp.StartForceExitAndRemoveMapCountdown();
                    Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("ShipBurnupPlayer"), TranslatorFormattedStringExtensions.Translate("ShipBurnupPlayerDesc", countdownComp.ForceExitAndRemoveMapCountdownTimeLeftString), LetterDefOf.ThreatBig);
                }
            }
            base.Destroy(mode);
        }
        public override void Tick()
        {
            base.Tick();
            if (selected && !Find.Selector.IsSelected(this))
                selected = false;
            //td rem this?
            int ticks = Find.TickManager.TicksGame;
            if (ticks % 250 == 0)
                TickRare();
        }
		
		public void RecalcStats()
        {
            ShipThreat = 0;
            ShipMass = 0;
            ShipMaxTakeoff = 0;
            ShipThrust = 0;
            foreach (Building b in cachedShipParts)
            {
                if (b.TryGetComp<CompSoShipPart>()?.Props.isPlating ?? false)
                    ShipMass += 1;
                else
                {
                    ShipMass += (b.def.size.x * b.def.size.z) * 3;
                    if (b.TryGetComp<CompShipHeat>() != null)
                        ShipThreat += b.TryGetComp<CompShipHeat>().Threat;
                    else if (b.def == ThingDef.Named("ShipSpinalAmplifier"))
                        ShipThreat += 5;
                    var engine = b.TryGetComp<CompEngineTrail>();
                    if (engine != null)
                    {
                        ShipThrust += engine.Thrust;
                        if (engine.Props.takeOff)
                        {
                            ShipMaxTakeoff += b.TryGetComp<CompRefuelable>().Props.fuelCapacity;
                            if (b.TryGetComp<CompRefuelable>().Props.fuelFilter.AllowedThingDefs.Contains(ThingDef.Named("ShuttleFuelPods")))
                            {
                                ShipMaxTakeoff += b.TryGetComp<CompRefuelable>().Props.fuelCapacity;
                            }
                        }
                    }
                }
            }
            ShipThrust *= 500f / Mathf.Pow(cachedShipParts.Count, 1.1f);
            ShipThreat += ShipMass / 100;
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
            if (this.ShipName == "Psychic Amplifier")
            {
                Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoSPsychicAmplifierCaptured"), TranslatorFormattedStringExtensions.Translate("SoSPsychicAmplifierCapturedDesc"), LetterDefOf.PositiveEvent);
                WorldSwitchUtility.PastWorldTracker.Unlocks.Add("ArchotechSpore");
            }
            if (pawn != null)
                pawn.skills.GetSkill(SkillDefOf.Intellectual).Learn(2000);
            if (mapComp.InCombat)
            {
                mapComp.RemoveShipFromBattle(shipIndex, this, Faction.OfPlayer);
            }
            else
            {
                cachedShipParts = ShipUtility.ShipBuildingsAttachedTo(this);
                foreach (Building b in cachedShipParts)
                {
                    if (b.def.CanHaveFaction)
                        b.SetFaction(Faction.OfPlayer);
                }
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
            if (this.AllComps != null)
            {
                for (int i = 0; i < AllComps.Count; i++)
                {
                    foreach (FloatMenuOption item in AllComps[i].CompFloatMenuOptions(selPawn))
                    {
                        options.Add(item);
                    }
                }
            }
            if (Faction != Faction.OfPlayer)
                options.Add(new FloatMenuOption("Hack", delegate { Job capture = new Job(DefDatabase<JobDef>.GetNamed("HackEnemyShip"), this); selPawn.jobs.TryTakeOrderedJob(capture); }));
            return options;
        }
    }
	public class Dialog_LoadShipDef : Dialog_Rename
    {
        private string ship = "shipdeftoload";
        private Map mapi;
        public Dialog_LoadShipDef(string ship, Map map)
        {
            curName = ship;
            mapi = map;
        }

        protected override void SetName(string name)
        {
            if (name == ship || string.IsNullOrEmpty(name))
                return;
            if (DefDatabase<EnemyShipDef>.GetNamedSilentFail(name) == null)
            {
                Log.Error("Ship not found in database: " + name);
                return;
            }
            AttackableShip shipa = new AttackableShip();
            shipa.attackableShip = DefDatabase<EnemyShipDef>.GetNamed(name);
            if (shipa.attackableShip.navyExclusive)
            {
                shipa.spaceNavyDef = DefDatabase<SpaceNavyDef>.AllDefs.Where(n => n.enemyShipDefs.Contains(shipa.attackableShip)).RandomElement();
                shipa.shipFaction = Find.FactionManager.AllFactions.Where(f => shipa.spaceNavyDef.factionDefs.Contains(f.def)).RandomElement();
            }
            mapi.passingShipManager.AddShip(shipa);
        }
    }
}