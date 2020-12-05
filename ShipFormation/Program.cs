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
	partial class Program : MyGridProgram
	{
		public const string TYPE_MASTER = "MASTER";
		public const string TYPE_SLAVE = "SLAVE";
		// Args
		private AbstractSystem me = null;

		public Program()
		{ }

		public void Main(string arg)
		{
			if (me == null)
			{
				bool isMaster = TYPE_MASTER.Equals(arg, StringComparison.InvariantCultureIgnoreCase);
				if (isMaster)
				{
					me = new ShipFormationMaster(this);
				}
				else
				{
					me = new ShipFormationSlave(this);
				}
				Runtime.UpdateFrequency = UpdateFrequency.Update100;
			}
			else
			{
				me.Run();
			}
		}
	}
}
