using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorld
{
    [StaticConstructorOnStartup]
    public class CompPowerTraderOverdrivable : CompPowerPlant
    {
        public int overdriveSetting = 0;
        public float instability = 0;

        private Texture2D cachedCommandTex1;
        private Texture2D cachedCommandTex2;
        private Texture2D cachedCommandTex3;

        private static Graphic turbineGraphic = GraphicDatabase.Get(typeof(Graphic_Single), "Things/Building/Ship/Reactor_Turbine", ShaderDatabase.Cutout, new Vector2(3, 3), Color.white, Color.white);
        private static Graphic turbineGraphicOverdrive = GraphicDatabase.Get(typeof(Graphic_Single), "Things/Building/Ship/Reactor_Turbine_Overdrive", ShaderDatabase.Cutout, new Vector2(3, 3), Color.white, Color.white);
        private static Graphic turbineGraphicSuperOverdrive = GraphicDatabase.Get(typeof(Graphic_Single), "Things/Building/Ship/Reactor_Turbine_Super_Overdrive", ShaderDatabase.Cutout, new Vector2(3, 3), Color.white, Color.white);

        Sustainer reactorSustainer;

        private Texture2D CommandTex1
        {
            get
            {
                if (cachedCommandTex1 == null)
                {
                    cachedCommandTex1 = ContentFinder<Texture2D>.Get("UI/Overdrive1Icon");
                }
                return cachedCommandTex1;
            }
        }

        private Texture2D CommandTex2
        {
            get
            {
                if (cachedCommandTex2 == null)
                {
                    cachedCommandTex2 = ContentFinder<Texture2D>.Get("UI/Overdrive2Icon");
                }
                return cachedCommandTex2;
            }
        }

        private Texture2D CommandTex3
        {
            get
            {
                if (cachedCommandTex3 == null)
                {
                    cachedCommandTex3 = ContentFinder<Texture2D>.Get("UI/Overdrive3Icon");
                }
                return cachedCommandTex3;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref overdriveSetting, "overdrive");
            Scribe_Values.Look(ref instability, "instability");
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if ((refuelableComp != null && !refuelableComp.HasFuel) || (flickableComp != null && !flickableComp.SwitchIsOn))
                turbineGraphic.Draw(parent.DrawPos + new Vector3(0, 0, 0.33f), parent.Rotation, parent);
            else if (overdriveSetting < 2)
                turbineGraphic.Draw(parent.DrawPos + new Vector3(0, 0, 0.33f), parent.Rotation, parent, Find.TickManager.TicksGame * 0.25f * (1 + overdriveSetting));
            else if (overdriveSetting == 2)
                turbineGraphicOverdrive.Draw(parent.DrawPos + new Vector3(0, 0, 0.33f), parent.Rotation, parent, Find.TickManager.TicksGame * 0.75f);
            else
                turbineGraphicSuperOverdrive.Draw(parent.DrawPos + new Vector3(0, 0, 0.33f), parent.Rotation, parent, Find.TickManager.TicksGame);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }
            if (parent.Faction == Faction.OfPlayer)
            {
                Command_Toggle command_Toggle = new Command_Toggle();
                command_Toggle.icon = CommandTex1;
                command_Toggle.defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipReactorOverdrive");
                command_Toggle.defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipReactorOverdriveDesc");
                command_Toggle.isActive = (() => overdriveSetting == 1);
                command_Toggle.toggleAction = delegate
                {
                    FlickOverdrive(1);
                };
                yield return command_Toggle;
                Command_Toggle command_Toggle2 = new Command_Toggle();
                command_Toggle2.icon = CommandTex2;
                command_Toggle2.defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipReactorOverdrive2");
                command_Toggle2.defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipReactorOverdrive2Desc");
                command_Toggle2.isActive = (() => overdriveSetting == 2);
                command_Toggle2.toggleAction = delegate
                {
                    FlickOverdrive(2);
                };
                yield return command_Toggle2;
                Command_Toggle command_Toggle3 = new Command_Toggle();
                command_Toggle3.icon = CommandTex3;
                command_Toggle3.defaultLabel = TranslatorFormattedStringExtensions.Translate("ShipReactorOverdrive3");
                command_Toggle3.defaultDesc = TranslatorFormattedStringExtensions.Translate("ShipReactorOverdrive3Desc");
                command_Toggle3.isActive = (() => overdriveSetting == 3);
                command_Toggle3.toggleAction = delegate
                {
                    FlickOverdrive(3);
                };
                yield return command_Toggle3;
            }
        }

        private void FlickOverdrive(int level)
        {
            if (overdriveSetting == level)
            {
                overdriveSetting = 0;
            }
            else
            {
                overdriveSetting = level;
            }
            StartSustainer();
        }

        protected override float DesiredPowerOutput => base.DesiredPowerOutput * (1 + (overdriveSetting * 2));

        public void StartSustainer()
        {
            if (reactorSustainer != null)
                reactorSustainer.End();
            SoundInfo sound = SoundInfo.InMap(parent);
            if (overdriveSetting == 0)
                reactorSustainer = SoundStarter.TrySpawnSustainer(SoundDef.Named("ShipReactor_Ambience"), sound);
            else if (overdriveSetting == 1)
                reactorSustainer = SoundStarter.TrySpawnSustainer(SoundDef.Named("ShipReactor_Ambience_Medium"), sound);
            else if (overdriveSetting == 2)
                reactorSustainer = SoundStarter.TrySpawnSustainer(SoundDef.Named("ShipReactor_Ambience_High"), sound);
            else if (overdriveSetting == 3)
                reactorSustainer = SoundStarter.TrySpawnSustainer(SoundDef.Named("ShipReactor_Ambience_Unstable"), sound);
        }

        public override void CompTick()
        {
            base.CompTick();

            if ((refuelableComp == null || refuelableComp.HasFuel) && (flickableComp == null || flickableComp.SwitchIsOn))
            {
                if (reactorSustainer == null || reactorSustainer.Ended)
                {
                    StartSustainer();
                }
                reactorSustainer.Maintain();
            }

            if (!parent.IsHashIntervalTick(240)) //4 seconds
                return;
            if (overdriveSetting == 0 || !flickableComp.SwitchIsOn)
            {
                instability -= 0.1f;
                if (instability < 0)
                    instability = 0;
            }
            else if (overdriveSetting > 1)
                instability += ((overdriveSetting * 2) - 1);
            if (instability > 0 && overdriveSetting > 0 && Rand.Chance(instability/420)) //Disaster strikes!
            {
                SoundDef.Named("ShipReactor_Radiation").PlayOneShot(SoundInfo.InMap(parent));
                instability -= 5;
                if (instability < 0)
                    instability = 0;
                if (Rand.Chance(0.2f))
                {
                    GenExplosion.DoExplosion(parent.Position, parent.Map, 1.9f, DamageDefOf.Flame, parent);
                    int numAshPiles = Rand.RangeInclusive(3, 5);
                    for (int i = 0; i < numAshPiles; i++)
                        FilthMaker.TryMakeFilth(parent.RandomAdjacentCell8Way(), parent.Map, ThingDef.Named("Filth_SpaceReactorAsh"));
                }
                List<Pawn> pawnsToIrradiate = new List<Pawn>();
                foreach (Pawn p in this.parent.Map.mapPawns.AllPawnsSpawned)
                {
                    if (p.RaceProps.IsFlesh && p.GetRoom() != null && p.GetRoom() == RegionAndRoomQuery.RoomAt(new IntVec3(this.parent.Position.x, 0, this.parent.Position.z), this.parent.Map))
                    {
                        pawnsToIrradiate.Add(p);
                    }
                }
                foreach (Pawn p in pawnsToIrradiate)
                {
                    int damage = Rand.RangeInclusive(4, 7);
                    p.TakeDamage(new DamageInfo(DamageDefOf.Burn, damage));
                    float num = 0.05f;
                    num *= (1 - p.GetStatValue(StatDefOf.ToxicResistance, true));
                    if (num != 0f)
                    {
                        HealthUtility.AdjustSeverity(p, HediffDefOf.ToxicBuildup, num);
                    }
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            return base.CompInspectStringExtra() + "\nInstability: " + instability;
        }
    }
}
