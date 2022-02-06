using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
    class ScenPart_AfterlifeVault : ScenPart
    {
        public override bool CanCoexistWith(ScenPart other)
        {
            return !(other is ScenPart_StartInSpace);
        }

        public override void GenerateIntoMap(Map map)
        {
            if (WorldSwitchUtility.SelectiveWorldGenFlag)
                return;
            List<Pawn> startingPawns = Find.GameInitData.startingAndOptionalPawns;
            Building core = null;
            try
            {
                ShipInteriorMod2.GenerateShip(DefDatabase<EnemyShipDef>.GetNamed("AfterlifeVaultStart"), map, null, Faction.OfPlayer, null, out core, true);
            }
            catch(Exception e)
            {
                Log.Error(e.ToString());
            }
            foreach (Pawn p in startingPawns)
            {
                PutInCasket(p, map);
            }
            foreach (Letter letter in Find.LetterStack.LettersListForReading)
                Find.LetterStack.RemoveLetter(letter);
        }

        public void PutInCasket(Pawn p, Map map)
        {
            foreach(Thing thing in map.listerThings.ThingsOfDef(ThingDef.Named("Ship_AvatarCasket")))
            {
                Building building = thing as Building;
                if(building.TryGetComp<CompBuildingConsciousness>().Consciousness==null)
                {
                    List<Apparel> apparel = new List<Apparel>();
                    foreach (Apparel app in p.apparel.WornApparel)
                        apparel.Add(app);
                    p.equipment.DestroyAllEquipment();
                    p.apparel.DestroyAll();
                    p.Kill(null);
                    building.TryGetComp<CompBuildingConsciousness>().InstallConsciousness(p.Corpse, apparel, false);
                    break;
                }
            }
        }
    }
}
