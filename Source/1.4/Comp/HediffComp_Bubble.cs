
namespace SaveOurShip2
{
    using Verse;
    using UnityEngine;
    using RimWorld;

    public class HediffComp_Bubble : HediffComp
    {
        public HediffCompProperties_Bubble Props => (HediffCompProperties_Bubble)props;
        public Mote mote;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            Draw();

        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            mote?.Destroy();
        }

        private void Draw()
        {
            ThingDef moteDef = Props.customMote ?? ResourceBank.ThingDefOf.Mote_SurvivalBubble;
            if (mote is null || mote.Destroyed)
            {
                mote = MoteMaker.MakeAttachedOverlay(Pawn, moteDef, Vector3.zero, Props.scale);
            }
            else
            {
                mote.Maintain();
            }
        }
    }
}
