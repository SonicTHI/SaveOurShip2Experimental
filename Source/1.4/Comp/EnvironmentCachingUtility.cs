using System.Collections.Generic;
using SaveOurShip2;
using Verse;

namespace RimworldMod
{
    public class EnvironmentCachingUtility : GameComponent
    {

        private Dictionary<int, bool> spaceMaps = new Dictionary<int, bool>();
        public HashSet<Thing> shuttleCache = new HashSet<Thing>();

        public EnvironmentCachingUtility(Game game)
        {
            AccessExtensions.Utility = this;
        }

        public bool this[Map map]
        {
            get
            {
                if (map == null) return false;
                if (spaceMaps.TryGetValue(map.uniqueID, out var space)) return space;

                var isSpace = map.Biome == ResourceBank.BiomeDefOf.OuterSpaceBiome;
                spaceMaps.Add(map.uniqueID, isSpace);
                return isSpace;
            }
        }

        public void RecacheSpaceMaps()
        {
            spaceMaps = new Dictionary<int, bool>();
        }

    }

    public static class AccessExtensions
    {

        public static EnvironmentCachingUtility Utility;

        public static bool IsSpace(this Map map)
        {
            return Utility[map];
        }
    }
}