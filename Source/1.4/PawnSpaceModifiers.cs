namespace SaveOurShip2
{
    using RimWorld;
    using Verse;

    public class PawnSpaceModifiers
    {

        public PawnSpaceModifiers(Pawn pawn)
        {
            this.VacuumSpeedMultiplier = pawn.GetStatValue(ResourceBank.StatDefOf.VacuumSpeedMultiplier);
            this.HypoxiaResistance = pawn.GetStatValue(ResourceBank.StatDefOf.HypoxiaResistance);
            this.DecompressionResistance = pawn.GetStatValue(ResourceBank.StatDefOf.DecompressionResistance);
        }

        public float DecompressionResistance { get; set; }

        public float HypoxiaResistance { get; set; }

        public float VacuumSpeedMultiplier { get; set; }

        public bool CanSurviveVacuum
        {
            get
            {
                return this.HypoxiaResistance >= 1f && this.DecompressionResistance >= 1f;
            }
        }
    }
}
