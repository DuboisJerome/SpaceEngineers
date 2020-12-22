using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
	partial class Program
	{
		public class VectorUtils
		{
			public static Vector3D Projection(Vector3D a, Vector3D b)
			{
				Vector3D projection = a.Dot(b) / b.LengthSquared() * b;
				return projection;
			}

			public static Vector3D Rejection(Vector3D a, Vector3D b)
			{
				return a - Projection(a, b);
			}

			public static Vector3D GetDirection(IMyEntity refBlock, string dirStr)
			{
				return GetDirection(refBlock.WorldMatrix, dirStr);
			}

			public static Vector3D GetDirection(MatrixD m, string dirStr)
			{
				string upperDir = dirStr == null ? "F" : dirStr.ToUpper();
				Vector3D r;
				switch (dirStr)
				{
					case "BACKWARD":
					case "B":
						r = m.Backward;
						break;
					case "UP":
					case "U":
						r = m.Up;
						break;
					case "DOWN":
					case "D":
						r = m.Down;
						break;
					case "LEFT":
					case "L":
						r = m.Left;
						break;
					case "RIGHT":
					case "R":
						r = m.Right;
						break;
					case "FORWARD":
					case "F":
					default:
						r = m.Forward;
						break;
				}
				return r;
			}
		}
	}
}
