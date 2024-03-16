using RimWorld.Planet;
using SaveOurShip2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
	class QuestPart_ArchotechSpawn : QuestPart_DropPods
	{
		public override void Notify_QuestSignalReceived(Signal signal)
		{
			if (!(signal.tag == inSignal))
			{
				return;
			}
			((List<Thing>)typeof(QuestPart_DropPods).GetField("items",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(this)).RemoveAll((Thing x) => x.Destroyed);
			Thing thing = Things.Where((Thing x) => x is Pawn).MaxByWithFallback((Thing x) => x.MarketValue);
			Thing thing2 = Things.MaxByWithFallback((Thing x) => x.MarketValue * (float)x.stackCount);
			if (mapParent != null && mapParent.HasMap && Things.Any())
			{
				Map map = mapParent.Map;
				IntVec3 intVec = map.spawnedThings.Where(t=>t.def == ResourceBank.ThingDefOf.ShipArchotechSpore).FirstOrDefault().Position;

				foreach(Thing t in Things)
                {
					Thing thingy = t;
					if (t.def.Minifiable)
						thingy = t.TryMakeMinified();
					GenPlace.TryPlaceThing(thingy, intVec, map, ThingPlaceMode.Near);
                }
				((List<Thing>)typeof(QuestPart_DropPods).GetField("items", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(this)).Clear();
			}
			if (!outSignalResult.NullOrEmpty())
			{
				if (thing != null)
				{
					Find.SignalManager.SendSignal(new Signal(outSignalResult, thing.Named("SUBJECT")));
				}
				else if (thing2 != null)
				{
					Find.SignalManager.SendSignal(new Signal(outSignalResult, thing2.Named("SUBJECT")));
				}
				else
				{
					Find.SignalManager.SendSignal(new Signal(outSignalResult));
				}
			}
			this.quest.End(QuestEndOutcome.Success);
			Find.QuestManager.Remove(this.quest);
		}
	}
}
