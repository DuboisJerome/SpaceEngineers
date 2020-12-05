using Sandbox.Game.EntityComponents;
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
		public class PlateformeForageParams : AbstractAMSParams
		{
			public PlateformeForageParams(Program program, string name = "A.M.S") : base(program, name)
			{
			}

			public float PistonVitesseDescente { get; set; } = 0.12F;
			public float PistonVitesseMonte { get; set; } = -2F;
			public string MainRotorName { get; set; } = "MainRotor";
			public float MainRotorVitesse { get; set; } = 6;
			public string MainHingeName { get; set; } = "MainHinge";
			public float MainHingeStepAngle { get; set; } = 6;
			public float MainHingeMaxAngle { get; set; } = 75F;
		}
	}
}
