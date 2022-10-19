using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace RimWorld
{
    public class CompProperties_ShipHeat : CompProperties
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
    }
}
