namespace SaveOurShip2
{
    using RimWorld;
    using Verse;
    using UnityEngine;

    public class CachedPawnSpaceModifiers
    {

        public CachedPawnSpaceModifiers(Pawn pawn)
        {
            DecompressionResistance = CalculateDecompressionResistance(pawn);
            HypoxiaResistance = CalculateHypoxiaResistance(pawn);
            VacuumSpeedMultiplier = pawn.GetStatValue(ResourceBank.StatDefOf.VacuumSpeedMultiplier);
        }

        public float DecompressionResistance { get; set; }

        public float HypoxiaResistance { get; set; }

        public float VacuumSpeedMultiplier { get; set; }

        public bool CanSurviveVacuum
        {
            get
            {
                return HypoxiaResistance >= 1f && DecompressionResistance >= 1f;
            }
        }

        private float CalculateDecompressionResistance(Pawn pawn)
        {
            float resistance = pawn.GetStatValue(ResourceBank.StatDefOf.DecompressionResistance);
            resistance += pawn.CurrentBed()?.GetStatValue(ResourceBank.StatDefOf.DecompressionResistanceOffset) ?? 0.0f;
            return Mathf.Clamp(resistance, 0.0f, 1.0f);
        }

        private float CalculateHypoxiaResistance(Pawn pawn)
        {
            float resistance = pawn.GetStatValue(ResourceBank.StatDefOf.HypoxiaResistance);
            resistance += pawn.CurrentBed()?.GetStatValue(ResourceBank.StatDefOf.HypoxiaResistanceOffset) ?? 0.0f;
            return Mathf.Clamp(resistance, 0.0f, 1.0f);
        }
    }
}
