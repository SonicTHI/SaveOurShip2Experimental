using System.Linq;
using Verse;

namespace RimWorld
{
    public class Dialog_LoadShipDef : Dialog_Rename
    {
        private string ship = "shipdeftoload";
        private Map Map;
        public Dialog_LoadShipDef(string ship, Map map)
        {
            curName = ship;
            Map = map;
        }
        protected override void SetName(string name)
        {
            if (name == ship || string.IsNullOrEmpty(name))
                return;
            if (DefDatabase<EnemyShipDef>.GetNamedSilentFail(name) == null)
            {
                Log.Error("Ship not found in database: " + name);
                return;
            }
            AttackableShip shipa = new AttackableShip();
            shipa.attackableShip = DefDatabase<EnemyShipDef>.GetNamed(name);
            if (shipa.attackableShip.navyExclusive)
            {
                shipa.spaceNavyDef = DefDatabase<SpaceNavyDef>.AllDefs.Where(n => n.enemyShipDefs.Contains(shipa.attackableShip)).RandomElement();
                shipa.shipFaction = Find.FactionManager.AllFactions.Where(f => shipa.spaceNavyDef.factionDefs.Contains(f.def)).RandomElement();
            }
            Map.passingShipManager.AddShip(shipa);
        }
    }
}