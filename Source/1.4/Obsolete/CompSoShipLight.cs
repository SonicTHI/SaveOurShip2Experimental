using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using SaveOurShip2;

namespace RimWorld
{
    /*[StaticConstructorOnStartup]
    public class CompSoShipLight : ThingComp
    {
        //props light: def of the wall light to be attached
        //icon
        public static Texture2D ShipWallLightIcon;
        public static Texture2D ShipSunLightIcon;
        public static Texture2D DiscoModeIcon;
        static CompSoShipLight()
        {
            ShipWallLightIcon = (Texture2D)GraphicDatabase.Get<Graphic_Single>("Things/Building/Ship/ShipWallLightIcon").MatSingle.mainTexture;
            ShipSunLightIcon = (Texture2D)GraphicDatabase.Get<Graphic_Single>("Things/Building/Ship/ShipSunLightIcon").MatSingle.mainTexture;
            DiscoModeIcon = (Texture2D)GraphicDatabase.Get<Graphic_Single>("Things/Building/Ship/DiscoModeIcon").MatSingle.mainTexture;
        }

        public bool hasLight = false;
        public bool sunLight = false;
        public int lightRot = -1;
        List<bool> rotCanLight;
        public ColorInt lightColor = new ColorInt(Color.white);
        public Building myLight = null;
        public bool discoMode = false;

        Map map;
        public ShipHeatMapComp mapComp;
        public CompProperties_SoShipLight Props
        {
            get
            {
                return (CompProperties_SoShipLight)props;
            }
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            map = parent.Map;
            mapComp = map.GetComponent<ShipHeatMapComp>();
            if (ShipInteriorMod2.AirlockBugFlag)
            {
                if (hasLight) //Despawned light in MoveShip - regenerate manually so we don't get power bugs
                    SpawnLight(lightRot, lightColor, sunLight);
                return;
            }
        }
        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (myLight != null && myLight.Spawned)
                myLight.DeSpawn();
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo giz in base.CompGetGizmosExtra())
                yield return giz;
            if (hasLight && ((parent.Faction==Faction.OfPlayer && ResearchProjectDefOf.ColoredLights.IsFinished) || DebugSettings.godMode))
            {
                rotCanLight = CanLightVecs();
                Command_Action toggleLight = new Command_Action
                {
                    action = delegate
                    {
                        if (hasLight)
                        {
                            hasLight = false;
                            if (myLight != null)
                                myLight.DeSpawn();
                            else
                                Log.Error("Tried to disable ship lighting at position " + parent.Position + " when no light exists. Please report this bug to the SoS2 team.");
                        }
                        else
                        {
                            if (lightRot == -1)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    if (rotCanLight[i])
                                    {
                                        lightRot = i;
                                        break;
                                    }
                                }
                            }
                            SpawnLight(lightRot, lightColor, sunLight);
                        }
                    },
                    icon = ShipWallLightIcon,
                    defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipWallLight"),
                    defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipWallLightDesc"),
                    disabled = rotCanLight.All(b => b == false),
                    disabledReason = TranslatorFormattedStringExtensions.Translate("ShipWallLightAdjacency")
                };
                yield return toggleLight;
                if (hasLight)
                {
                    Command_Action rotateLight = new Command_Action
                    {
                        action = delegate
                        {
                            for (int i = 1; i < 4; i++) //check other 3 rots, swap to first valid CW
                            {
                                int rot = (lightRot + i) % 4;
                                if (rotCanLight[rot])
                                {
                                    myLight.DeSpawn();
                                    SpawnLight(rot, lightColor, sunLight);
                                    break;
                                }
                            }
                        },
                        icon = ShipWallLightIcon,
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipWallLightRotate"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipWallLightRotateDesc"),
                        disabled = rotCanLight.Count(b => b == false) > 2,
                        disabledReason = TranslatorFormattedStringExtensions.Translate("ShipWallLightAdjacency")
                    };
                    yield return rotateLight;
                    Command_Toggle toggleSun = new Command_Toggle
                    {
                        toggleAction = delegate
                        {
                            sunLight = !sunLight;
                            if (myLight != null)
                                myLight.DeSpawn();
                            else
                                Log.Error("Tried to enable sunlight mode at position " + parent.Position + " when no light exists. Please report this bug to the SoS2 team.");
                            SpawnLight(lightRot, lightColor, sunLight);
                        },
                        isActive = delegate { return sunLight; },
                        icon = ShipSunLightIcon,
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipWallLightSun"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipWallLightSunDesc")
                    };
                    yield return toggleSun;
                    Command_Toggle toggleDisco = new Command_Toggle
                    {
                        toggleAction = delegate
                        {
                            discoMode = !discoMode;
                        },
                        isActive = delegate { return discoMode; },
                        icon = DiscoModeIcon,
                        defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipWallLightDisco"),
                        defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipWallLightDiscoDesc")
                    };
                    yield return toggleDisco;
                }
                if (hasLight)
                {
                    foreach (Gizmo giz in myLight.GetGizmos())
                        yield return giz;
                }
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look<bool>(ref hasLight, "hasLight", false);
            Scribe_Values.Look<bool>(ref sunLight, "sunLight", false);
            Scribe_Values.Look<int>(ref lightRot, "lightRot", -1);
            Scribe_Values.Look<bool>(ref discoMode, "discoMode", false);
            if (hasLight)
            {
                Scribe_Values.Look<ColorInt>(ref lightColor, "lightColor", new ColorInt(Color.white));
                Scribe_References.Look<Building>(ref myLight, "myLight");
            }
        }
        List<bool> CanLightVecs()
        {
            List<bool> rotCanLight = new List<bool>() { false, false, false, false };
            if (CanLight(parent.Position + new IntVec3(0, 0, 1), map))
                rotCanLight[0] = true;
            if (CanLight(parent.Position + new IntVec3(1, 0, 0), map))
                rotCanLight[1] = true;
            if (CanLight(parent.Position + new IntVec3(0, 0, -1), map))
                rotCanLight[2] = true;
            if (CanLight(parent.Position + new IntVec3(-1, 0, 0), map))
                rotCanLight[3] = true;
            if (parent is Building_ShipVent)
            {
                rotCanLight[parent.Rotation.AsInt] = false;
            }
            return rotCanLight;
        }
        bool CanLight(IntVec3 pos, Map map)
        {
            Building edifice = pos.GetEdifice(map);
            return (edifice == null || (!(edifice is Building_Door) && edifice.def.passability != Traversability.Impassable));
        }
        public void SpawnLight(int rot, ColorInt? color = null, bool sun = false)
        {
            lightRot = rot;
            hasLight = true;
            sunLight = sun;
            myLight = (Building)GenSpawn.Spawn(Props.light, parent.Position, parent.Map);
            CompPowerTrader trader = myLight.TryGetComp<CompPowerTrader>();
            if (trader != null)
            {
                trader.ConnectToTransmitter(parent.TryGetComp<CompPower>());
                if (sunLight)
                    trader.Props.basePowerConsumption = Props.sunLightPower;
                else
                    trader.Props.basePowerConsumption = Props.lightPower;
                trader.PowerOn = true;
            }
            CompShipLight lightComp = myLight.TryGetComp<CompShipLight>();
            if (lightComp != null)
            {
                if (color.HasValue)
                    lightColor = color.Value;
                else
                    lightColor = ColorIntUtility.AsColorInt(Color.white);
                if (rot == -1)
                {
                    rotCanLight = CanLightVecs();
                    for (int i = 0; i < 4; i++)
                    {
                        if (rotCanLight[i])
                        {
                            lightRot = i;
                            break;
                        }
                    }
                }
                lightComp.SetupLighting(this, sun, rot);
            }
            else
                Log.Error("Failed to initialize ship lighting at position " + parent.Position + " - please report this bug to the SoS2 team.");
        }
    }*/
}