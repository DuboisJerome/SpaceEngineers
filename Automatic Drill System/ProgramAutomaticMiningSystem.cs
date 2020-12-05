using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
	partial class Program : MyGridProgram
	{
		// arg
		AutomaticMiningSystem ads = null;

		public Program()
		{
			Runtime.UpdateFrequency = UpdateFrequency.Update100;			
			AutomaticMiningSystemParams parameters = new AutomaticMiningSystemParams(this);
			if (Me.CustomData != string.Empty)
			{
				string[] customParams = Me.CustomData.Split('\n');
				if(customParams != null && customParams.Length > 0)
				{
					foreach (string customParam in customParams)
					{
						string[] splitParam = customParam.Split('=');
						if (splitParam == null || splitParam.Length < 2)
							continue;
						string propertyName = splitParam[0];
						string propertyValue = splitParam[1];
						parameters.SetProperty(propertyName, propertyValue);
					}
				}
			}
			ads = new AutomaticMiningSystem(parameters);
		}

		public void Main(string arg, UpdateType updateSource)
		{
			ads.Run(arg, updateSource);
		}
	}
}