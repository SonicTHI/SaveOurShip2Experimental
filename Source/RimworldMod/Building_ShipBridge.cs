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

        bool selected = false;
        IntVec3 LowestCorner;
        List<Building> cachedShipParts;
        List<Building> cachedShields;
        List<Building> cachedCloaks;
        List<Building> cachedPods;
        bool anyCloakOn;
        List<Building> cachedWeapons;
        List<string> fail;

        bool ShouldStartCombat = false;

        public ShipHeatMapComp mapComp;
        public ShipHeatMapComp MapComp
        {
            get
            {
                if (this.mapComp == null)
                {
                    this.mapComp = this.Map.GetComponent<ShipHeatMapComp>();
                }
                return this.mapComp;
            }
        }
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
            if (!cachedShipParts.Any((Building pa) => pa.def == ThingDefOf.Ship_ComputerCore || pa.def == ShipInteriorMod2.ArchotechSpore))
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipReportMissingPart") + ": " + ThingDefOf.Ship_ComputerCore.label);
            if (!cachedShipParts.Any((Building pa) => pa.def == ThingDefOf.Ship_SensorCluster || pa.def ==ThingDef.Named("Ship_SensorClusterAdv")))
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipReportMissingPart") + ": " + ThingDefOf.Ship_SensorCluster.label);
            int playerJTPower = 0;
            if (cachedShipParts.Any((Building pa) => pa.TryGetComp<CompEngineTrailEnergy>() != null))
            {
                foreach (Building b in cachedShipParts.Where(b => b.TryGetComp<CompEngineTrailEnergy>() != null))
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
            if (MapComp.ShipCombatMaster)
                result.Add(TranslatorFormattedStringExtensions.Translate("ShipOnEnemyMap"));
            return result;
        }

		[DebuggerHidden]
		public override IEnumerable<Gizmo> GetGizmos()
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
                yield break;
            }
            if (!selected)
            {
                Log.Message("recached: " + this);
                cachedShipParts = ShipUtility.ShipBuildingsAttachedTo(this);
                cachedShields = new List<Building>();
                cachedWeapons = new List<Building>();
                cachedCloaks = new List<Building>();
                cachedPods = new List<Building>();
                LowestCorner = new IntVec3(int.MaxValue, 0, int.MaxValue);
                anyCloakOn = false;
                foreach (Building b in cachedShipParts)
                {
                    if (b.Position.x < LowestCorner.x)
                        LowestCorner.x = b.Position.x;
                    if (b.Position.z < LowestCorner.z)
                        LowestCorner.z = b.Position.z;
                    if (b is Building_ShipTurret)
                    {
                        cachedWeapons.Add(b);
                    }
                    else if (b is Building_ShipCloakingDevice)
                    {
                        cachedCloaks.Add(b);
                        if (b.TryGetComp<CompFlickable>().SwitchIsOn)
                            anyCloakOn = true;
                    }
                    else if (b.TryGetComp<CompShipCombatShield>() != null)
                        cachedShields.Add(b);
                    else if (b.TryGetComp<CompCryptoLaunchable>() != null)
                    {
                        cachedPods.Add(b);
                    }
                }
                fail = InterstellarFailReasons();
                selected = true;
            }
			foreach (Gizmo c in base.GetGizmos())
			{
				yield return c;
			}
            if (!MapComp.InCombat)
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
                        stringBuilder.AppendLine(TranslatorFormattedStringExtensions.Translate("ShipStatsShipHeat", ShipHeat, ShipHeatCap));
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
                if (cachedCloaks.Any())
                {
                    Command_Toggle toggleCloak = new Command_Toggle
                    {
                        toggleAction = delegate
                        {
                            foreach (Building b in cachedCloaks)
                            {
                                b.TryGetComp<CompFlickable>().SwitchIsOn = !anyCloakOn;
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
                bool anyShieldOn = false;
                foreach(Building b in cachedShields)
                {
                    if (b.TryGetComp<CompFlickable>().SwitchIsOn)
                        anyShieldOn = true;
                }
                if (cachedShields.Any())
                {
					Command_Toggle toggleShields = new Command_Toggle
					{
						toggleAction = delegate
						{
							foreach (Building b in cachedShields)
							{
								b.TryGetComp<CompFlickable>().SwitchIsOn = !anyShieldOn;
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
                if (MapComp.InCombat)
                {
                    var originMapComp = MapComp.OriginMapComp;
                    var masterMapComp = MapComp.MasterMapComp;
                    Command_Action escape = new Command_Action
                    {
                        action = delegate
                        {
                            MapComp.EndBattle(this.Map, true);
                        },
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipCombatEscape"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipCombatEscapeDesc"),
                        icon = ContentFinder<Texture2D>.Get("UI/Escape_Icon")
                    };
                    if (masterMapComp.Range < 400)
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
                    if (cachedWeapons.Count != 0)
                    {
                        Command_Action selectWeapons = new Command_Action
                        {
                            action = delegate
                            {
                                Find.Selector.Deselect(this);
                                foreach (Building b in cachedWeapons)
                                {
                                    Find.Selector.Select(b);
                                }
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideSelectWeapons"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideSelectWeaponsDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Select_All_Weapons_Icon")
                        };
                        yield return selectWeapons;
                    }
                    /*if (Prefs.DevMode) //nonfunc, disable AI
                    {
                        if (enemyShip.Heading != -1)
                        {
                            Command_Action retreat = new Command_Action
                            {
                                action = delegate
                                {
                                    enemyShip.Heading = -1;
                                    enemyShipComp.callSlowTick = true;
                                },
                                defaultLabel = "Dev: Enemy ship retreat",
                            };
                            yield return retreat;
                        }
                        if (enemyShip.Heading != 0)
                        {
                            Command_Action stop = new Command_Action
                            {
                                action = delegate
                                {
                                    enemyShipComp.Heading = 0;
                                    enemyShipComp.callSlowTick = true;
                                },
                                defaultLabel = "Dev: Enemy ship stop",
                            };
                            yield return stop;
                        }
                        if (enemyShip.Heading != 1)
                        {
                            Command_Action advance = new Command_Action
                            {
                                action = delegate
                                {
                                    enemyShip.Heading = 1;
                                    enemyShipComp.callSlowTick = true;
                                },
                                defaultLabel = "Dev: Enemy ship advance",
                            };
                            yield return advance;
                        }
                    }*/
                }
                //not incombat
                else
                {
                    //space - move, land
                    if (!MapComp.IsGraveyard)
                    {
                        Command_Action gotoNewWorld = new Command_Action
                        {
                            action = delegate
                            {
                                WorldSwitchUtility.ColonyAbandonWarning(delegate { WorldSwitchUtility.SwitchToNewWorld(this.Map, this); });
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipPlanetLeave"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipPlanetLeaveDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip", true)
                        };

                        if (fail.Any())
                            gotoNewWorld.Disable(fail.First());
                        yield return gotoNewWorld;
                        if (WorldSwitchUtility.PastWorldTracker != null && WorldSwitchUtility.PastWorldTracker.PastWorlds.Count > 0)
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
                        }
                        Command_Action moveShip = new Command_Action
                        {
                            action = delegate
                            {
                                MoveShipSketch(LowestCorner, this, this.Map);
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideMove"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Land_Ship")
                        };
                        if (ShipCountdown.CountingDown)
                        {
                            moveShip.Disable();
                        }
                        yield return moveShip;
                        //flip
                        Command_Action moveShipFlip = new Command_Action
                        {
                            action = delegate
                            {
                                IntVec3 lowestCornerAdj = new IntVec3();
                                lowestCornerAdj.x = this.Map.Size.x - LowestCorner.x;
                                lowestCornerAdj.z = this.Map.Size.z - LowestCorner.z;
                                MoveShipSketch(lowestCornerAdj, this, this.Map, 2);
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveFlip"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveFlipDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Flip_Ship")
                        };
                        if (ShipCountdown.CountingDown)
                        {
                            moveShipFlip.Disable();
                        }
                        yield return moveShipFlip;
                        if (Prefs.DevMode)
                        {
                            //CCW rot
                            Command_Action moveShipRot = new Command_Action
                            {
                                action = delegate
                                {
                                    IntVec3 lowestCornerAdj = new IntVec3();
                                    lowestCornerAdj.x = this.Map.Size.z - LowestCorner.z;
                                    lowestCornerAdj.z = LowestCorner.x;
                                    MoveShipSketch(lowestCornerAdj, this, this.Map, 1);
                                },
                                defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveRot"),
                                defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideMoveRotDesc"),
                                icon = ContentFinder<Texture2D>.Get("UI/Rotate_Ship")
                            };
                            if (ShipCountdown.CountingDown)
                            {
                                moveShipRot.Disable();
                            }
                            yield return moveShipRot;
                        }
                        //land - dev mode can "land" in space with CK enabled
                        List<Map> landableMaps = new List<Map>();
                        foreach (Map m in Find.Maps)
                        {
                            if ((!m.IsSpace() && !m.IsTempIncidentMap) || ((Prefs.DevMode && ModLister.HasActiveModWithName("Save Our Ship Creation Kit"))) && m != this.Map)
                                landableMaps.Add(m);
                        }
                        foreach (Map m in landableMaps)
                        {
                            Command_Action landShip = new Command_Action
                            {
                                action = delegate
                                {
                                    MoveShipSketch(LowestCorner, this, m, 0);
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
                        if (ShipInteriorMod2.PillarBProject.IsFinished)
                        {
                            if (!WorldSwitchUtility.PastWorldTracker.Unlocks.Contains("ArchotechPillarA"))
                            {
                                Command_Action goGetThatPillarA = new Command_Action
                                {
                                    action = delegate
                                    {
                                        AttackableShip station = new AttackableShip();
                                        station.enemyShip = DefDatabase<EnemyShipDef>.GetNamed("ArchotechGardenStation");
                                        MapComp.StartShipEncounter(this, station);
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
                                        AttackableShip station = new AttackableShip();
                                        station.enemyShip = DefDatabase<EnemyShipDef>.GetNamed("MechShipMegaRing");
                                        MapComp.StartShipEncounter(this, station);
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
                                    ShouldStartCombat = true;
                                },
                                defaultLabel = "Dev: Start ship battle",
                            };
                            startBattle.hotKey = KeyBindingDefOf.Misc9;
                            yield return startBattle;
                            Command_Action loadshipdef = new Command_Action
                            {
                                action = delegate
                                {
                                    Find.WindowStack.Add(new Dialog_LoadShipDef("shipdeftoload", this.Map));
                                },
                                defaultLabel = "Dev: load ship from database",
                            };
                            loadshipdef.hotKey = KeyBindingDefOf.Misc9;
                            yield return loadshipdef;
                            if (WorldSwitchUtility.PastWorldTracker.PastWorlds.Any())
                            {
                                Command_Action purgePlanets = new Command_Action
                                {
                                    action = delegate
                                    {
                                        List<Faction> toKeep = Find.FactionManager.AllFactions.ToList();
                                        WorldSwitchUtility.PastWorldTracker.PastWorlds.Clear();
                                        WorldSwitchUtility.PastWorldTracker.WorldFactions.Clear();
                                        List<Faction> toRemove = Find.FactionManager.AllFactions.Except(toKeep).ToList();
                                        foreach (Faction fac in toRemove)
                                            ((List<Faction>)typeof(FactionManager).GetField("allFactions", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Find.FactionManager)).Remove(fac);
                                    },
                                    defaultLabel = "Dev: Remove all previously visited worlds",
                                    defaultDesc = "WARNING: This action cannot be undone! It should only be used to fix bugs or reduce save file size."
                                };
                                yield return purgePlanets;
                            }
                        }
                        //attack passing ship
                        if (this.Map.passingShipManager.passingShips.Any())
                        {
                            foreach (PassingShip ship in this.Map.passingShipManager.passingShips)
                            {
                                if (ship is TradeShip)
                                {
                                    Command_Action attackTradeShip = new Command_Action
                                    {
                                        action = delegate
                                        {
                                            MapComp.StartShipEncounter(this, ship);
                                            if (ModLister.IdeologyInstalled)
                                                IdeoUtility.Notify_PlayerRaidedSomeone(this.Map.mapPawns.FreeColonists);
                                        },
                                        icon = ContentFinder<Texture2D>.Get("UI/IncomingShip_Icon_Trader"),
                                        defaultLabel = "Attack " + ship.FullTitle,
                                        defaultDesc = "Attempt an act of space piracy against " + ship.FullTitle
                                    };
                                    yield return attackTradeShip;
                                }
                                else if (ship is AttackableShip)
                                {
                                    Command_Action attackAttackableShip = new Command_Action
                                    {
                                        action = delegate
                                        {
                                            MapComp.StartShipEncounter(this, ship);
                                            this.Map.passingShipManager.RemoveShip(ship);
                                            if (ModLister.IdeologyInstalled)
                                                IdeoUtility.Notify_PlayerRaidedSomeone(this.Map.mapPawns.FreeColonists);
                                        },
                                        icon = ContentFinder<Texture2D>.Get("UI/IncomingShip_Icon_Pirate"),
                                        defaultLabel = "Attack " + ship.FullTitle,
                                        defaultDesc = "Attempt to engage " + ship.FullTitle
                                    };
                                    yield return attackAttackableShip;
                                }
                                else if (ship is DerelictShip)
                                {
                                    Command_Action approachDerelictShip = new Command_Action
                                    {
                                        action = delegate
                                        {
                                            MapComp.StartShipEncounter(this, ship);
                                            this.Map.passingShipManager.RemoveShip(ship);
                                        },
                                        icon = ContentFinder<Texture2D>.Get("UI/IncomingShip_Icon_Quest"),
                                        defaultLabel = "Approach " + ship.FullTitle,
                                        defaultDesc = "Approach to investigate " + ship.FullTitle
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
                                if (MapComp.ShipCombatOriginMap == null)
                                {
                                    Map m = ((MapParent)Find.WorldObjects.AllWorldObjects.Where(ob => ob.def.defName.Equals("ShipOrbiting")).FirstOrDefault()).Map;
                                    if (m != null)
                                        MapComp.ShipCombatOriginMap = m;
                                    else
                                        ShipInteriorMod2.GeneratePlayerShipMap(this.Map.Size, this.Map);
                                }
                                MoveShipSketch(LowestCorner, this, MapComp.ShipCombatOriginMap, 0);
                            },
                            defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideCaptureShip"),
                            defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideCaptureShipDesc"),
                            icon = ContentFinder<Texture2D>.Get("UI/Capture_Ship_Icon")
                        };
                        if (MapComp.ShipFaction == Faction.OfPlayer)
                        {
                            captureShip.defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipInsideReturnShip");
                            captureShip.defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipInsideReturnShipDesc");
                            captureShip.icon = ContentFinder<Texture2D>.Get("UI/Planet_Landing_Icon");
                        }
                        if (ShipCountdown.CountingDown || MapComp.OriginMapComp.InCombat)
                        {
                            captureShip.Disable();
                        }
                        yield return captureShip;
                    }
                }
            }
            else //launch - very slow
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

        public void MoveShipSketch(IntVec3 lowestCorner, Building b, Map m, Byte rotb = 0)
        {
            Sketch shipSketch = GenerateShipSketch(lowestCorner, rotb);
            MinifiedThingShipMove fakeMover = (MinifiedThingShipMove)new ShipMoveBlueprint(shipSketch).TryMakeMinified();
            fakeMover.shipRoot = b;
            fakeMover.shipRotNum = rotb;
            fakeMover.bottomLeftPos = lowestCorner;
            ShipInteriorMod2.shipOriginMap = b.Map;
            fakeMover.targetMap = m;
            fakeMover.Position = b.Position;
            fakeMover.SpawnSetup(m, false);
            List<object> selected = new List<object>();
            foreach (object ob in Find.Selector.SelectedObjects)
                selected.Add(ob);
            foreach (object ob in selected)
                Find.Selector.Deselect(ob);
            Current.Game.CurrentMap = m;
            Find.Selector.Select(fakeMover);
            InstallationDesignatorDatabase.DesignatorFor(ThingDef.Named("ShipMoveBlueprint")).ProcessInput(null);
        }
        private Sketch GenerateShipSketch(IntVec3 lowestCorner, byte rotb = 0)
        {
            Sketch sketch = new Sketch();
            List<IntVec3> positions = new List<IntVec3>();
            IntVec3 rot = new IntVec3(0, 0, 0);
            foreach (Building building in cachedShipParts)
            {
                foreach (IntVec3 pos in GenAdj.CellsOccupiedBy(building))
                {
                    if (!positions.Contains(pos))
                        positions.Add(pos);
                }
            }
            foreach (IntVec3 pos in positions)
            {
                if (rotb == 1)
                {
                    rot.x = this.Map.Size.x - pos.z;
                    rot.z = pos.x;
                    sketch.AddThing(ThingDef.Named("Ship_FakeBeam"), rot - lowestCorner, Rot4.North);
                }
                else if (rotb == 2)
                {
                    rot.x = this.Map.Size.x - pos.x;
                    rot.z = this.Map.Size.z - pos.z;
                    sketch.AddThing(ThingDef.Named("Ship_FakeBeam"), rot - lowestCorner, Rot4.North);
                }
                else
                    sketch.AddThing(ThingDef.Named("Ship_FakeBeam"), pos - lowestCorner, Rot4.North);
            }
            return sketch;
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
                    MoveShipSketch(LowestCorner, this, m, 0);
                }
                else
                    ShipCountdown.InitiateCountdown(this);
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
            if (!MapComp.MapRootListAll.Contains(this))
                MapComp.MapRootListAll.Add(this);
            //Log.Message("Spawned: " + this + " to " + this.Map);
            var countdownComp = this.Map.Parent.GetComponent<TimedForcedExitShip>();
            if (countdownComp != null && !MapComp.IsGraveyard && countdownComp.ForceExitAndRemoveMapCountdownActive)
            {
                countdownComp.ResetForceExitAndRemoveMapCountdown();
                Messages.Message("ShipBurnupPlayerPrevented", this, MessageTypeDefOf.PositiveEvent);
            }
            //td add name from other bridges
            //td cache reinit
            /*
            if (mapComp.InCombat) //redo cache - prob better to prevent bridge building IC
            {
                mapComp.StartBattleCache();
            }
            else
            if (!mapComp.InCombat && this.Map != mapComp.ShipCombatMasterMap && mapComp.ShipsOnMap.Count > 0)
            {
                foreach (Building b in ShipUtility.ShipBuildingsAttachedTo(this))
                {
                    if (b is Building_ShipBridge)
                    {
                        this.shipIndex = ((Building_ShipBridge)b).shipIndex;
                        Log.Message("Spawned bridge: " + this + " on ship: " + shipIndex);
                    }
                }
            }*/
        }
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (MapComp.MapRootListAll.Contains(this))
                MapComp.MapRootListAll.Remove(this);
            //Log.Message("Destroyed: " + this + " from " + this.Map);
            if (MapComp.InCombat) //force check on next tick
            {
                MapComp.BridgeDestroyed = true;
            }
            else if (this.Map.IsSpace() && MapComp.MapRootListAll.NullOrEmpty())
            {
                var countdownComp = this.Map.Parent.GetComponent<TimedForcedExitShip>();
                if (countdownComp != null)
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
            if (ShouldStartCombat)
            {
                MapComp.StartShipEncounter(this);
                ShouldStartCombat = false;
            }
            //td rem this?
            int ticks = Find.TickManager.TicksGame;
            if (ticks % 250 == 0)
                TickRare();
        }
		
		public void RecalcStats ()
        {
            ShipThreat = 0;
            ShipMass = 0;
            ShipMaxTakeoff = 0;
            ShipThrust = 0;
            foreach (Building b in cachedShipParts)
            {
                if (b.def != ShipInteriorMod2.hullPlateDef && b.def!= ShipInteriorMod2.archoHullPlateDef && b.def!= ShipInteriorMod2.mechHullPlateDef)
                {
                    ShipMass += (b.def.size.x * b.def.size.z) * 3;
                    if (b.TryGetComp<CompShipHeat>() != null)
                        ShipThreat += b.TryGetComp<CompShipHeat>().Props.threat;
                    else if (b.def == ThingDef.Named("ShipSpinalAmplifier"))
                        ShipThreat += 5;
                    else if (b.TryGetComp<CompEngineTrail>() != null)
                    {
                        ShipThrust += b.TryGetComp<CompEngineTrail>().Props.thrust;
                        ShipMaxTakeoff += b.TryGetComp<CompRefuelable>().Props.fuelCapacity;
                        //nuclear counts x2
                        if (b.TryGetComp<CompRefuelable>().Props.fuelFilter.AllowedThingDefs.Contains(ThingDef.Named("ShuttleFuelPods")))
                        {
                            ShipMaxTakeoff += b.TryGetComp<CompRefuelable>().Props.fuelCapacity;
                        }
                    }
                    else if (b.TryGetComp<CompEngineTrailEnergy>() != null)
                        ShipThrust += b.TryGetComp<CompEngineTrailEnergy>().Props.thrust;
                }
                else if (b.def == ShipInteriorMod2.hullPlateDef || b.def== ShipInteriorMod2.archoHullPlateDef || b.def== ShipInteriorMod2.mechHullPlateDef)
                    ShipMass += 1;
            }
            ShipThrust *= 500f / Mathf.Pow(cachedShipParts.Count, 1.1f);
            ShipThreat += ShipMass / 100;
            if(this.TryGetComp<CompShipHeat>()!=null)
            {
                ShipHeatNet net = this.TryGetComp<CompShipHeat>().myNet;
                ShipHeat = net.StorageUsed;
                ShipHeatCap = net.StorageCapacity;
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
            if (pawn != null)
                pawn.skills.GetSkill(SkillDefOf.Intellectual).Learn(2000);
            if (MapComp.InCombat)
            {
                MapComp.RemoveShipFromBattle(shipIndex, this, Faction.OfPlayer);
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
            if (this.ShipName == "Psychic Amplifier")
            {
                Find.LetterStack.ReceiveLetter(TranslatorFormattedStringExtensions.Translate("SoSPsychicAmplifierCaptured"), TranslatorFormattedStringExtensions.Translate("SoSPsychicAmplifierCapturedDesc"), LetterDefOf.PositiveEvent);
                WorldSwitchUtility.PastWorldTracker.Unlocks.Add("ArchotechSpore");
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
            if (name == ship || string.IsNullOrEmpty(name) || DefDatabase<EnemyShipDef>.GetNamed(name)==null)
                return;
            AttackableShip shipa = new AttackableShip();
            shipa.enemyShip = DefDatabase<EnemyShipDef>.GetNamed(name);
            mapi.passingShipManager.AddShip(shipa);
        }
    }
}