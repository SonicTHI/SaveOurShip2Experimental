using System.Linq;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class Dialog_LoadShipDef : Dialog_RenameShip
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
			if (DefDatabase<ShipDef>.GetNamedSilentFail(name) == null)
			{
				Log.Error("Ship not found in database: " + name);
				return;
			}
			AttackableShip shipa = new AttackableShip();
			shipa.attackableShip = DefDatabase<ShipDef>.GetNamed(name);
			if (shipa.attackableShip.navyExclusive)
			{
				shipa.spaceNavyDef = DefDatabase<NavyDef>.AllDefs.Where(n => n.spaceShipDefs.Contains(shipa.attackableShip)).RandomElement();
				shipa.shipFaction = Find.FactionManager.AllFactions.Where(f => shipa.spaceNavyDef.factionDefs.Contains(f.def)).RandomElement();
			}
			Map.passingShipManager.AddShip(shipa);
		}
	}
}