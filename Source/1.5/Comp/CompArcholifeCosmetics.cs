using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace SaveOurShip2
{
	[StaticConstructorOnStartup]
	class CompArcholifeCosmetics : ThingComp
	{
		public static Dictionary<string, Graphic[]> graphics = new Dictionary<string, Graphic[]>();
		public static Dictionary<ThingDef, CompProps_ArcholifeCosmetics> GraphicsToResolve = new Dictionary<ThingDef, CompProps_ArcholifeCosmetics>();

		public int whichVersion = 0;

		public CompProps_ArcholifeCosmetics Props
		{
			get
			{
				return (CompProps_ArcholifeCosmetics)props;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref whichVersion, "version");
		}
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			if (parent.Faction != Faction.OfPlayer)
				yield break;

			Command_Action setVersion = new Command_Action
			{
				action = delegate
				{
					List<FloatMenuOption> list = new List<FloatMenuOption>();
					for (int index = 0; index < Props.names.Count; index++)
					{
						list.Add(new FloatMenuOption(Props.names[index], delegate { ChangeAnimalGraphics(parent, Props, this, true); }));
					}
					Find.WindowStack.Add(new FloatMenuWithCallback(list));
				},
				icon = (Texture2D)graphics[parent.def.defName][whichVersion].MatSouth.mainTexture,
				defaultLabel = TranslatorFormattedStringExtensions.Translate("SoS.ArcholifeChangeSkin"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("SoS.ArcholifeChangeSkinDesc")
			};
			yield return setVersion;
		}

		public static void ChangeAnimalGraphics(ThingWithComps parent, CompProps_ArcholifeCosmetics Props, CompArcholifeCosmetics cosmetics, bool triggeredByChange = false)
		{
			if (triggeredByChange)
				cosmetics.whichVersion = FloatMenuWithCallback.whichOptionWasChosen;
			Pawn pawn = (Pawn)parent;
			Vector2 drawSize;
			/*15disabled
			if (pawn.Drawer.renderer.graphics.nakedGraphic==null)
				drawSize = pawn.ageTracker.CurKindLifeStage.bodyGraphicData.Graphic.drawSize;
			else
				drawSize = pawn.Drawer.renderer.graphics.nakedGraphic.drawSize;
			pawn.Drawer.renderer.graphics.nakedGraphic = graphics[parent.def.defName][cosmetics.whichVersion];
			pawn.Drawer.renderer.graphics.nakedGraphic.drawSize = drawSize;
			*/
		}

		public class FloatMenuWithCallback : FloatMenu
		{
			public static int whichOptionWasChosen;

			public FloatMenuWithCallback(List<FloatMenuOption> options) : base(options)
			{
			}

			public override void PreOptionChosen(FloatMenuOption opt)
			{
				whichOptionWasChosen = this.options.IndexOf(opt);
			}
		}
	}
}
