using System;
using UnityEngine;
using Verse;

namespace RimWorld
{
	public class CompProperties_SpinalMount : CompProperties
	{
		public bool emits=false;
		public bool receives=false;
		public bool stackEnd = false;
		public float ampAmount;
		public ThingDef rootGun = null;
		public Color color = Color.white;
		public bool destroysHull = true;

		public CompProperties_SpinalMount()
		{
			this.compClass = typeof(CompSpinalMount);
		}
	}
}
