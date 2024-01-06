
namespace SaveOurShip2
{
    using Verse;

    public class Comp_EVA : ThingComp
    {

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            ShipInteriorMod2.WorldComp.RemovePawnFromSpaceCache(pawn);
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            ShipInteriorMod2.WorldComp.RemovePawnFromSpaceCache(pawn);
        }

        public override string GetDescriptionPart()
        {
            return "SOS.EVACapable".Translate();
        }
    }
}
