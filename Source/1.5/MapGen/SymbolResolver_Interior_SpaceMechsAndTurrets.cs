using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI.Group;

namespace RimWorld.BaseGen
{
	class SymbolResolver_Interior_SpaceMechsAndTurrets : SymbolResolver
	{

		public override void Resolve(ResolveParams rp)
		{
			Map map = BaseGen.globalSettings.map;
			ThingDef filth;
			
			filth = ThingDefOf.Filth_Blood;
			int num = Rand.Range(5, 8);
			Lord lord = rp.singlePawnLord;
			if (lord == null && num > 0)
			{
				IntVec3 point;
				LordJob lordJob;
				(from x in rp.rect.Cells where !x.Impassable(map) select x).TryRandomElement(out point);
				lordJob = new LordJob_DefendPoint(point);
				lord = LordMaker.MakeNewLord(rp.faction, lordJob, map, null);
			}
			for (int i = 0; i < num; i++)
			{
				PawnKindDef pawnKindDef = rp.singlePawnKindDef;
				if (pawnKindDef == null)
				{
					pawnKindDef = (from kind in DefDatabase<PawnKindDef>.AllDefsListForReading
								   where kind.RaceProps.IsMechanoid
								   select kind).RandomElementByWeight((PawnKindDef kind) => 1f / kind.combatPower);
				}
				ResolveParams resolveParams = rp;
				resolveParams.singlePawnKindDef = pawnKindDef;
				resolveParams.singlePawnLord = lord;
				resolveParams.faction = rp.faction;
				BaseGen.symbolStack.Push("pawn", resolveParams);
			}

			foreach (IntVec3 current in rp.rect)
			{
				if (Rand.Chance(0.3f))
				{
					Thing thing = ThingMaker.MakeThing(filth);
					GenSpawn.Spawn(thing, current, map);
				}
				if(Rand.Chance(0.025f))
				{
					Thing thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.AllDefs.Where(def=> (typeof(Building_Turret)).IsAssignableFrom(def.thingClass) && def.Size.x==1 && def.Size.z==1).RandomElement());
					thing.SetFaction(rp.faction);
					GenSpawn.Spawn(thing, current, map);
				}
				else if(Rand.Chance(0.025f))
				{
					Thing thing = ThingMaker.MakeThing(ThingDef.Named("TrapIED_HighExplosive"));
					thing.SetFaction(rp.faction);
					GenSpawn.Spawn(thing, current, map);
				}
			}
		}
	}
}