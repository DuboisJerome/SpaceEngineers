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
	partial class Program : MyGridProgram
	{
		private AbstractAutoAntennaRelay autoAntennaRelay;

		public Program()
		{
		}

		public void Save()
		{
		}

		public void Main(string argument, UpdateType updateSource)
		{
			if (autoAntennaRelay == null){
				if((updateSource & UpdateType.Terminal) != 0)
				{
					string tagGroup = Me.CustomData.Trim();
					if ("Master".Equals(argument, StringComparison.InvariantCultureIgnoreCase))
					{
						Me.GetSurface(0).WriteText("Master : " + IGC.Me);
						autoAntennaRelay = new AutoAntennaRelayManager(this, tagGroup);
					}
					else
					{
						Me.GetSurface(0).WriteText("Slave : " + IGC.Me);
						autoAntennaRelay = new AutoAntennaRelaySlave(this, tagGroup);
					}
				}
			} else {
				autoAntennaRelay.Run(updateSource);
			}
		}
	}
}
