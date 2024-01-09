namespace SaveOurShip2
{
    using RimWorld;
    using UnityEngine;
    using Verse;

    public static class PawnExtensions
    {
        public static float SOS_DecompressionResistance(this Pawn pawn)
        {
            float resistance = pawn.GetStatValue(ResourceBank.StatDefOf.DecompressionResistance);
            resistance += pawn.CurrentBed()?.GetStatValue(ResourceBank.StatDefOf.DecompressionResistanceOffset) ?? 0.0f;
            return Mathf.Clamp(resistance, 0.0f, 1.0f);
        }

        public static float SOS_HypoxiaResistance(this Pawn pawn)
        {
            float resistance = pawn.GetStatValue(ResourceBank.StatDefOf.HypoxiaResistance);
            resistance += pawn.CurrentBed()?.GetStatValue(ResourceBank.StatDefOf.HypoxiaResistanceOffset) ?? 0.0f;
            return Mathf.Clamp(resistance, 0.0f, 1.0f);
        }

        public static bool SOS_CanSurviveVacuum(this Pawn pawn)
        {
            return pawn.SOS_DecompressionResistance() >= 1.0f && pawn.SOS_HypoxiaResistance() >= 1.0f;
        }
    }
}

