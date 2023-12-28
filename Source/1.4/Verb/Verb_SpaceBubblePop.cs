namespace RimWorld
{
    using SaveOurShip2;
    using Verse;

    public class Verb_SpaceBubblePop : Verb
    {
        public static void Pop(CompReloadable comp)
        {
            if (comp == null || !comp.CanBeUsed)
            {
                return;
            }

            Pawn wearer = comp.Wearer;
            GenExplosion.DoExplosion(wearer.Position, wearer.Map, 1, DamageDefOf.Smoke, null, postExplosionSpawnChance: 1.0f);
            comp.UsedOnce();
            wearer.health.AddHediff(ResourceBank.HediffDefOf.SpaceBeltBubbleHediff);
            ShipInteriorMod2.WorldComp.AddPawnToSpaceCache(wearer);
        }

        protected override bool TryCastShot()
        {
            Verb_SpaceBubblePop.Pop(this.ReloadableCompSource);
            return true;
        }
    }
}
