
namespace SaveOurShip2
{
    using Verse;

    public class HediffComp_Space : HediffComp
    {
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            ShipInteriorMod2.WorldComp.RemovePawnFromSpaceCache(Pawn);
        }

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            ShipInteriorMod2.WorldComp.RemovePawnFromSpaceCache(Pawn);
        }
    }
}
