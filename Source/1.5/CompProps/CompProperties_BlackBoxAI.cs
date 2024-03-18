using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
	public class CompProperties_BlackBoxAI : CompProperties
	{

		public CompProperties_BlackBoxAI()
		{
			this.compClass = typeof(CompBlackBoxAI);
		}
	}
}