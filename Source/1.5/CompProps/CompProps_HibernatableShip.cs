using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	public class CompProps_HibernatableShip : CompProperties
	{
		public float startupDays = 15f;

		public IncidentTargetTagDef incidentTargetWhileStarting;

		public CompProps_HibernatableShip()
		{
			this.compClass = typeof(CompHibernatableShip);
		}

		[DebuggerHidden]
		public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
		{
			foreach (string err in base.ConfigErrors(parentDef))
			{
				yield return err;
			}
			if (parentDef.tickerType != TickerType.Normal)
			{
				yield return string.Concat(new object[]
				{
					"CompHibernatable needs tickerType ",
					TickerType.Normal,
					", has ",
					parentDef.tickerType
				});
			}
		}
	}
}