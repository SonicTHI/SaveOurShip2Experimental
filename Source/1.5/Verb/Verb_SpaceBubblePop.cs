using RimWorld;
using Verse;

namespace SaveOurShip2
{
    public class Verb_SpaceBubblePop : Verb
    {
        protected override bool TryCastShot()
        {
            Pop(ReloadableCompSource);
            return true;
        }

        public static void Pop(CompApparelReloadable comp)
        {
            if (comp == null || !comp.CanBeUsed(out string reason))
            {
                return;
            }

            Pawn wearer = comp.Wearer;
            GenExplosion.DoExplosion(wearer.Position, wearer.Map, 1, DamageDefOf.Smoke,  null, postExplosionSpawnChance: 1.0f, screenShakeFactor: 0.0f);
            comp.UsedOnce();
            wearer.health.AddHediff(ResourceBank.HediffDefOf.SpaceBeltBubbleHediff);
        }
    }
}
