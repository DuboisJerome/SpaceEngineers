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
		public class PhysicsUtils
		{
			// if a0 >= 0 => NegativeInfinity, you will never be able to stop you are not decelerating
			// a_0 and v_0 must be colinear
			public static double TimeToStop(double a_0, double v_0 = 0)
			{
				// v_t = a_0*t + v_0 = 0
				//t = (v_t - v_0) / a_0
				if(a_0 >= 0)
					return Double.NegativeInfinity;
				return -v_0 / a_0;
			}

			public static double Position(double a_0, double v_0, double x_0, double t)
			{
				double acc = (a_0 / 2) * Math.Pow(t, 2);
				double speed = v_0 * t;
				return acc + speed + x_0;
			}

			public static double Speed(double a_0, double v_0, double t)
			{
				return a_0 * t + v_0;
			}

			// x_t = distance to stop
			// a_0 and v_0 must be colinear
			public static double MaxInitialSpeedToStop(double x_t, double a_0)
			{
				// x_t = 1/2*a_0*t*t + v_0*t + x_0 --> where x_0 = 0
				// v_t = a_0*t + v_0 = 0 --> v_0 = -a_0*t
				// x_t = 1/2*a_0*t*t - a_0*t*t = -1/2 * a_0*t*t
				// t*t = -2*x_t/a_0 --> t = sqrt(-2*x_t/a_0)
				if (a_0 >= 0)
					return Double.PositiveInfinity;
				return Math.Sqrt(-2 * x_t / a_0);
			}
		}
	}
}
