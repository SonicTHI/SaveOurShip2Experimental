using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEngine;
using Verse;

namespace SaveOurShip2
{
	public struct ShipShape : IExposable
	{
		public string shapeOrDef; //Looks for an SpaceShipPartDef. If none, it looks for a ThingDef.
		public string stuff;
		public int x;
		public int z;
		public int width;
		public int height;
		public Rot4 rot;
		public bool alt; //alternate mode, for sun lights, etc.
		public float radius;
		public string colorDef;
		public string faction; //faction override

		public override int GetHashCode()
		{
			return (shapeOrDef +","+ stuff + "," + width+","+height+","+alt+","+radius+"," + colorDef).GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (!(obj is ShipShape))
				return false;
			ShipShape otherShape = (ShipShape)obj;
			return otherShape.shapeOrDef == shapeOrDef && otherShape.stuff == stuff && otherShape.width == width && otherShape.height == height && otherShape.alt == alt && otherShape.radius == radius && otherShape.colorDef == colorDef;

		}

		public void ExposeData()
		{
			Scribe_Values.Look<string>(ref shapeOrDef, "shapeOrDef");
			Scribe_Values.Look<string>(ref stuff, "stuff");
			Scribe_Values.Look<string>(ref faction, "faction");
			//Scribe_Values.Look<int>(ref x, "x");
			//Scribe_Values.Look<int>(ref z, "z");
			Scribe_Values.Look<int>(ref width, "width");
			Scribe_Values.Look<int>(ref height, "height");
			//Scribe_Values.Look<Rot4>(ref rot, "rot");
			Scribe_Values.Look<bool>(ref alt, "alt");
			Scribe_Values.Look<float>(ref radius, "radius");
			Scribe_Values.Look<string>(ref colorDef, "colorDef");
		}
	}
	public struct OffsetShip : IExposable
	{
		public string ship;
		public int offsetX;
		public int offsetZ;

		public override int GetHashCode()
		{
			return (ship + "," + offsetX + "," + offsetZ).GetHashCode();
		}
		public override bool Equals(object obj)
		{
			if (!(obj is OffsetShip))
				return false;
			OffsetShip otherShip = (OffsetShip)obj;
			return otherShip.ship == ship && otherShip.offsetX == offsetX && otherShip.offsetZ == offsetZ;

		}
		public void ExposeData()
		{
			Scribe_Values.Look<string>(ref ship, "ship");
			Scribe_Values.Look<int>(ref offsetX, "offsetX");
			Scribe_Values.Look<int>(ref offsetZ, "offsetZ");
		}

	}

	/*public struct ShipPosRotShape
	{
		public int x;
		public int z;
		public Rot4 rot;
		public string shape;

		public override int GetHashCode()
		{
			return (x+","+z+","+rot).GetHashCode();
		}
	}*/
	public class ShipDef : Def, ILoadReferenceable
	{
		public Dictionary<string, ShipShape> symbolTable;
		/*[Unsaved(false)]
		public Dictionary<ShipShape, string> symbolTableBackwards;
		[Unsaved(false)]
		public List<ShipPosRotShape> ShipStructure;*/
		public int saveSysVer = 1;
		public List<OffsetShip> ships;
		public int offsetX = 0;
		public int offsetZ = 0;
		public int sizeX = 0;
		public int sizeZ = 0;
		public int rarityLevel = 1; //1 - common, 2 - rare, 3 - gacha rare (not used ATM), 0 - ignore rarity checks in code
		public int combatPoints;
		public int randomTurretPoints;
		public int cargoValue;
		public List<ShipShape> parts;
		public ShipShape core;
		public bool neverRandom = false; //true for specially spawned ships, starts
		public bool neverWreck = false; //prevent random wrecks from this shipdef
		public bool neverFleet = false; //will not appear in random gen fleets
		public bool neverAttacks = false; //will not attack player
		public bool startingShip = false;
		public bool startingDungeon = false;
		public bool spaceSite = false; //custom space site
		public bool tradeShip = false; //trades, requires neverAttacks
		public bool navyExclusive = false; //only navies can use this
		public bool customPaintjob = false; //prevents simple navy paint scheme

		public string bigString = "";

		public string GetUniqueLoadID()
		{
			return "SpaceShip_" + defName;
		}

		/*public void ConvertToSymbolTable()
		{
			char charPointer = '?';
			symbolTable = new Dictionary<char, ShipShape>();
			symbolTableBackwards = new Dictionary<ShipShape, char>();
			ShipStructure = new List<ShipPosRotShape>();
			foreach(ShipShape shape in parts)
			{
				if (!symbolTableBackwards.ContainsKey(shape))
				{
					symbolTable.Add(charPointer, shape);
					symbolTableBackwards.Add(shape, charPointer);
					charPointer = (char)(((int)charPointer)+1);
					if (charPointer == '|')
						charPointer = (char)(((int)charPointer) + 1);
				}
				ShipPosRotShape posrot = new ShipPosRotShape();
				posrot.x = shape.x;
				posrot.z = shape.z;
				posrot.rot = shape.rot;
				posrot.shape = symbolTableBackwards[shape];
				ShipStructure.Add(posrot);
			}
		}

		public void ConvertToBigString()
		{
			string bigString = "";
			bool isFirst = true;
			foreach (ShipPosRotShape shape in ShipStructure)
			{
				if (isFirst)
					isFirst = false;
				else
					bigString += "|";
				bigString += shape.x + "," + shape.z + "," + shape.rot.AsInt + "," + shape.shape;
			}
			this.bigString = bigString;
			this.ShipStructure = null;
		}*/

		public override void PostLoad()
		{
			base.PostLoad();
			if (parts == null)
			{
				ConvertFromBigString();
				//ConvertFromSymbolTable();
			}
		}

		public void ConvertFromBigString()
		{
			parts = new List<ShipShape>();
			string[] strings = bigString.Split('|');
			foreach(string obj in strings)
			{
				string[] parms = obj.Split(',');
				ShipShape shape = new ShipShape();
				shape.x = int.Parse(parms[0]);
				shape.z = int.Parse(parms[1]);
				shape.rot = new Rot4(int.Parse(parms[2]));
				ShipShape symbol = symbolTable[parms[3]];
				shape.alt = symbol.alt;
				shape.height = symbol.height;
				shape.radius = symbol.radius;
				shape.shapeOrDef = symbol.shapeOrDef;
				shape.stuff = symbol.stuff;
				shape.width = symbol.width;
				shape.colorDef = symbol.colorDef;
				parts.Add(shape);
			}
		}

		/*public void ConvertFromSymbolTable()
		{
			parts = new List<ShipShape>();
			foreach(ShipPosRotShape shape in ShipStructure)
			{
				ShipShape instance = new ShipShape();
				ShipShape symbol = symbolTable[shape.shape];
				instance.captain = symbol.captain;
				instance.height = symbol.height;
				instance.width = symbol.width;
				instance.radius = symbol.radius;
				instance.shapeOrDef = symbol.shapeOrDef;
				instance.stuff = symbol.stuff;
				instance.x = shape.x;
				instance.z = shape.z;
				instance.rot = shape.rot;
				parts.Add(instance);
			}
		}

		public void ConvertFromBigString()
		{
			ShipStructure = new List<ShipPosRotShape>();
			string[] strings = bigString.Split('|');
			foreach(string obj in strings)
			{
				string[] parms = obj.Split(',');
				ShipPosRotShape shape = new ShipPosRotShape();
				shape.x = int.Parse(parms[0]);
				shape.z = int.Parse(parms[1]);
				shape.rot = new Rot4(int.Parse(parms[2]));
				shape.shape = char.Parse(parms[3]);
				ShipStructure.Add(shape);
			}
		}*/
	}
}
