using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
	/*class HediffComp_GiveSubPart : HediffComp
	{
		private HediffCompProperties_GiveSubPart Props => (HediffCompProperties_GiveSubPart)props;

		public override void CompPostPostAdd(DamageInfo? dinfo)
		{
			base.CompPostPostAdd(dinfo);
			foreach(Hediff missing in parent.pawn.health.hediffSet.hediffs.Where(hediff=>hediff.def==HediffDefOf.MissingBodyPart))
			{
				if (missing.Part.def == Props.whereToInstall)
				{
					parent.pawn.health.RemoveHediff(missing);
				}
			}
			parent.pawn.health.AddHediff(Props.hediffDef, parent.pawn.RaceProps.body.GetPartsWithDef(Props.whereToInstall).FirstOrDefault());
		}
	}*/
}
