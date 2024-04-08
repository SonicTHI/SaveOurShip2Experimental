using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimWorld
{
	[StaticConstructorOnStartup]
	class CompShuttleCosmetics : ThingComp
	{
		public int whichVersion = 0;

		public static Dictionary<string, Graphic[]> graphics = new Dictionary<string, Graphic[]>();
		public static Dictionary<string, Graphic[]> graphicsHover = new Dictionary<string, Graphic[]>();

		public static Dictionary<ThingDef, CompProperties_ShuttleCosmetics> GraphicsToResolve = new Dictionary<ThingDef, CompProperties_ShuttleCosmetics>();

		public CompProperties_ShuttleCosmetics Props
		{
			get
			{
				return (CompProperties_ShuttleCosmetics)this.props;
			}
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look<int>(ref whichVersion, "version");
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			Command_Action setVersion = new Command_Action
			{
				action = delegate
				{
					List<FloatMenuOption> list = new List<FloatMenuOption>();
					for(int index=0;index<Props.names.Count;index++)
					{
						list.Add(new FloatMenuOption(Props.names[index], delegate { ChangeShipGraphics(parent, Props, true); }));
					}
					Find.WindowStack.Add(new FloatMenuWithCallback(list));
				},
				icon = (Texture2D)Props.graphics[whichVersion].Graphic.MatSouth.mainTexture,
				defaultLabel = TranslatorFormattedStringExtensions.Translate("ShuttleChangeColor"),
				defaultDesc = TranslatorFormattedStringExtensions.Translate("ShuttleChangeColorDesc")
			};
			List<Gizmo> toReturn = new List<Gizmo>();
			toReturn.Add(setVersion);
			return toReturn;
		}

		public static void ChangeShipGraphics(ThingWithComps parent, CompProperties_ShuttleCosmetics Props, bool triggeredByChange = false)
		{
			if(triggeredByChange)
				parent.GetComp<CompShuttleCosmetics>().whichVersion = FloatMenuWithCallback.whichOptionWasChosen;
			int whichVersion = parent.GetComp<CompShuttleCosmetics>().whichVersion;
			if (parent is Pawn pawn)
			{
				pawn.Drawer.renderer.graphics.nakedGraphic = graphicsHover[parent.def.defName][whichVersion];
				pawn.Drawer.renderer.graphics.ClearCache();
			}
			else
			{
				parent.graphicInt=graphics[parent.def.defName][whichVersion];
				parent.DirtyMapMesh(parent.Map);
			}
		}
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
