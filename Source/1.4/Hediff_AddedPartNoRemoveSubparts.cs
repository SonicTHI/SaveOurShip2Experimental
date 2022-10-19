using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimWorld
{
	public class Hediff_AddedPartNoRemoveSubparts : Hediff_Implant
	{
		public override string TipStringExtra
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(base.TipStringExtra);
				stringBuilder.AppendLine("Efficiency".Translate() + ": " + def.addedPartProps.partEfficiency.ToStringPercent());
				return stringBuilder.ToString();
			}
		}

		public override void PostAdd(DamageInfo? dinfo)
		{
			pawn.health.RestorePart(base.Part, this, checkStateChange: false);
			base.PostAdd(dinfo);
		}
	}
}
