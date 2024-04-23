using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace SaveOurShip2
{
	public class CompProps_ShipHeat : CompProperties
	{
		public float heatCapacity;
		public float heatPerPulse;
		public int energyToFire;
		public int threat = 0;
		public float optRange = 0;
		public float maxRange = 400;
		public float projectileSpeed = 1;
		public bool pointDefense = false;
		public bool groundDefense = false;
		public bool showOnRoof = false;
		public ThingDef groundProjectile;
		public float groundMissRadius = 0;
		public float heatPerSecond = 0;
		public SoundDef singleFireSound=null;
		public bool antiEntropic = false;
		public int heatVent = 0;
		public int heatLoss = 0;
		public float heatPurge = 0;
		public float heatMultiplier = 1.0f;
		public Color color = Color.white;
		public Color laserColor = Color.red;
		public int shieldMin = 20;
		public int shieldMax = 60;
		public int shieldDefault = 40;
	}
}
