using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using Vehicles;

namespace SaveOurShip2
{
	[StaticConstructorOnStartup]
	public class CompShipHeat : ThingComp
	{
		public static Graphic ShipHeatOverlay = new Graphic_ShipHeat_Overlay(GraphicDatabase.Get<Graphic_Single>("Things/Building/Ship/ShipHeat_Overlay_Atlas", ShaderDatabase.MetaOverlay));
		public static Graphic ShipHeatConnectorBase = GraphicDatabase.Get<Graphic_Single>("Things/Special/Power/OverlayBase", ShaderDatabase.MetaOverlay);
		public static Graphic ShipHeatGraphic = new Graphic_LinkedShipConduit(GraphicDatabase.Get<Graphic_Single>("Things/Building/Ship/Atlas_CoolantConduit", ShaderDatabase.Cutout));

		public ShipHeatNet myNet=null;

		public CompProps_ShipHeat Props
		{
			get { return props as CompProps_ShipHeat; }
		}

		public virtual int Threat
		{
			get
			{
				return Props.threat;
			}
		}
		public bool Venting
		{
			get
			{
				if (myNet != null)
					return myNet.venting;
				return false;
			}
		}
		public void PrintForGrid(SectionLayer layer)
		{
			ShipHeatOverlay.Print(layer, (Thing)(object)base.parent, 0);
		}
		public override string CompInspectStringExtra()
		{
			string output = "";
			if (myNet != null)
			{
				output += TranslatorFormattedStringExtensions.Translate("SoS.HeatStored", Mathf.Round(myNet.StorageUsed), myNet.StorageCapacity, myNet.StorageCapacityRaw);
				if (myNet.RatioInNetworkRaw > 0.9f)
					output += "\n" + TranslatorFormattedStringExtensions.Translate("SoS.HeatCritical").Colorize(Color.red);
				if (Prefs.DevMode)
				{
					output += "\nGrid:" + myNet.GridID + " Ratio:" + myNet.RatioInNetworkRaw.ToString("F2") + " Depl ratio:" + myNet.DepletionRatio.ToString("F2") + "Temp: " + Mathf.Lerp(0, 200, myNet.RatioInNetworkRaw).ToString("F0");
				}
			}
			else
				output+= TranslatorFormattedStringExtensions.Translate("SoS.HeatNotConnected");

			if (this.Props.energyToFire > 0)
			{
				output += "\n"+ "SoS.HeatTurretEnergy".Translate();
				if (this.parent is Building_ShipTurret t && t.spinalComp != null)
				{
					if (t.AmplifierCount != -1)
						output += this.Props.energyToFire * (1 + t.AmplifierDamageBonus) + " Wd";
					else
						output += "N/A";
				}
				else
					output += this.Props.energyToFire + " Wd";
			}
			return output;
		}
		public bool AddHeatToNetwork(float amount)
		{
			//Log.Message("Adding " + amount + " heat to network. Network currently has " + (myNet == null ? 0 : (myNet.StorageAvailable)) + " of " + (myNet == null ? 0 : (myNet.StorageCapacity)) + " available.");
			if (myNet == null || amount > myNet.StorageCapacity - myNet.StorageUsed)
				return false;
			myNet.AddHeat(amount);
			return true;
		}
		public bool RemHeatFromNetwork(float amount)
		{
			if (myNet == null || amount > myNet.StorageUsed)
				return false;
			myNet.RemoveHeat(amount);
			return true;
		}
		public bool AddDepletionToNetwork(float amount)
		{
			if (myNet == null || amount > myNet.StorageCapacity)
				return false;
			myNet.AddDepletion(amount);
			return true;
		}
		public void RemoveDepletionFromNetwork(float amount)
		{
			myNet?.RemoveDepletion(amount);
		}
		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			if (this.parent is VehiclePawn)
				return;
			var mapComp = this.parent.Map.GetComponent<ShipMapComp>();
			//td change to check for adj nets, perform merges
			mapComp.cachedPipes.Add(this);
			mapComp.heatGridDirty = true;
		}
		public override void PostDeSpawn(Map map)
		{
			base.PostDeSpawn(map);
			if (myNet != null)
				myNet.DeRegister(this);
			if (this.parent is VehiclePawn)
				return;
			var mapComp = map.GetComponent<ShipMapComp>();
			//td change to check for adj nets, if at end of line simple remove, else regen
			mapComp.cachedPipes.Remove(this);
			mapComp.heatGridDirty = true;
		}
		public override void PostExposeData()
		{
			base.PostExposeData();
		}
	}
}
