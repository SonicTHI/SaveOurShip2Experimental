using System;
using System.Collections.Generic;
using System.Diagnostics;
using Verse;

namespace RimWorld
{
	public class CompProperties_HibernatableSoS : CompProperties
	{
		public float startupDays = 15f;

		public IncidentTargetTagDef incidentTargetWhileStarting;

		public CompProperties_HibernatableSoS()
		{
			this.compClass = typeof(CompHibernatableSoS);
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