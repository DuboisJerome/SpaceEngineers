﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
	partial class Program
	{
		public class GPS
		{
			public int x;
			public int y;
			public int z;
			public GPS(int x, int y, int z)
			{
				this.x = x;
				this.y = y;
				this.z = z;
			}

			public GPS(Vector3D v)
			{
				this.x = (int)v.X;
				this.y = (int)v.Y;
				this.z = (int)v.Z;
			}

			public GPS(string str)
			{
				string[] s = str.Split(';');
				this.x=int.Parse(s[0]);
				this.y=int.Parse(s[1]);
				this.z=int.Parse(s[2]);
			}

			public bool Equals(GPS gps)
			{
				if (gps == null)
				{
					return false;
				}
				else
				{
					return this.x == gps.x && this.y == gps.y && this.z == gps.z;
				}
			}

			public override string ToString()
			{
				return x + ";" + y + ";" + z;
			}

			public GPS ComputeCoordinate(long distance, double angle)
			{
				int deltaZ = (int)(Math.Cos(angle) * distance);
				int deltaX = (int)(Math.Sin(angle) * distance);
				int deltaY = 0;
				int x = (int)this.x + deltaX;
				int y = (int)this.y + deltaY;
				int z = (int)this.z + deltaZ;
				return new GPS(x, y, z);
			}

			public double Distance(GPS o)
			{
				double x = Math.Pow(o.x - this.x, 2);
				double y = Math.Pow(o.y - this.y, 2);
				double z = Math.Pow(o.z - this.z, 2);
				return Math.Sqrt(x+y+z);
			}
		}
	}
}