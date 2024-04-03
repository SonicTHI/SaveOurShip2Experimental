using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using SaveOurShip2;
using RimWorld.Planet;
using System.Security.Policy;
using Verse.Noise;

namespace RimWorld
{
	class MinifiedThingShipMove : MinifiedThing
	{
		public Building shipRoot;
		public IntVec3 bottomLeftPos;
		public byte shipRotNum;
		public bool includeRock = false;
		public Map originMap = null;
		public Map targetMap = null;
		public bool atmospheric = false;
		public Faction fac = null;

		public override void Tick()
		{
			base.Tick();
			if (Find.Selector.SelectedObjects.Count > 1 || !Find.Selector.SelectedObjects.Contains(this))
			{
				if (InstallBlueprintUtility.ExistingBlueprintFor(this) != null)
				{
					if (atmospheric) //transit from/to space, pick landing site and set vars instead of moving
					{
						//ShipInteriorMod2.LaunchShip counterpart
						bool originIsSpace = originMap.IsSpace();
						var mapComp = originMap.GetComponent<ShipHeatMapComp>();
						var ship = mapComp.ShipsOnMapNew[((Building_ShipBridge)shipRoot).ShipIndex];
						IntVec3 adj = IntVec3.Zero;
						WorldObjectOrbitingShip mapPar; //origin might not be WOS
						
						if (!originIsSpace || (originIsSpace && (mapComp.ShipsOnMapNew.Count > 1 || originMap.mapPawns.AllPawns.Any(p => !mapComp.MapShipCells.ContainsKey(p.Position))))) //to either with temp map
						{
							//spawn new WO and map
							WorldObjectOrbitingShip transit = (WorldObjectOrbitingShip)WorldObjectMaker.MakeWorldObject(ResourceBank.WorldObjectDefOf.WreckSpace);
							transit.drawPos = originMap.Parent.DrawPos;
							transit.SetFaction(Faction.OfPlayer);
							transit.Tile = ShipInteriorMod2.FindWorldTile();
							Find.WorldObjects.Add(transit);
							Map newMap = MapGenerator.GenerateMap(originMap.Size, transit, transit.MapGeneratorDef);
							newMap.fogGrid.ClearAllFog();
							mapComp = newMap.GetComponent<ShipHeatMapComp>();
							mapPar = transit;

							//set vecs //td
							//adj = ship.CenterShipOnMap();
							//mapComp.MoveToVec = adj.Inverse();

							//move
							ShipInteriorMod2.MoveShip(shipRoot, newMap, adj, fac, shipRotNum, includeRock);
							if (!originIsSpace)
								newMap.weatherManager.TransitionTo(ResourceBank.WeatherDefOf.OuterSpaceWeather);
						}
						else //to ground with originMap - spacehome
						{
							mapPar = (WorldObjectOrbitingShip)originMap.Parent;
						}
						mapComp.MoveToVec = InstallBlueprintUtility.ExistingBlueprintFor(this).Position - bottomLeftPos;
						mapComp.MoveToMap = targetMap;
						mapComp.MoveToTile = targetMap.Tile;

						//vars1
						mapPar.originDrawPos = originMap.Parent.DrawPos;
						if (originIsSpace) //to ground either
						{
							mapPar.targetDrawPos = targetMap.Parent.DrawPos;
							mapComp.Heading = -1;
							mapComp.Altitude = mapComp.Altitude - 1;
							mapComp.Takeoff = false;
						}
						else //to space with temp map
						{
							mapPar.targetDrawPos = ShipInteriorMod2.FindPlayerShipMap().Parent.DrawPos;
							mapComp.Heading = 1;
							mapComp.Altitude = ShipInteriorMod2.altitudeLand; //startup altitude
							mapComp.Takeoff = true;
						}

						//vars2
						mapComp.BurnTimer = Find.TickManager.TicksGame;
						mapComp.PrevMap = originMap;
						mapComp.PrevTile = originMap.Tile;
						mapComp.EnginesOn = true;
						mapComp.ShipMapState = ShipMapState.inTransit;
						CameraJumper.TryJump(mapComp.MapRootListAll.FirstOrDefault().Position, originMap);
					}
					else //normal move to target map
					{
						ShipInteriorMod2.MoveShip(shipRoot, targetMap, InstallBlueprintUtility.ExistingBlueprintFor(this).Position - bottomLeftPos, fac, shipRotNum, includeRock);
					}
				}
				if (!Destroyed)
					Destroy(DestroyMode.Vanish);
			}
		}

		protected override void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			if (Graphic is Graphic_Single)
			{
				Graphic.Draw(drawLoc, Rot4.North, this, 0f);
				return;
			}
			Graphic.Draw(drawLoc, Rot4.South, this, 0f);
		}
	}
}
