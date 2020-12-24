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
		public class AMSPhase
		{

			private readonly Func<bool> RunImpl;
			public AMSPhase(string name, Func<bool> fcntRun)
			{
				Name = name;
				RunImpl = fcntRun;
				NextPhase = null;
			}

			public string Name { get; }


			public bool Run()
			{
				bool isEnd = false;
				if (FirstCall)
				{
					isEnd = DoIfNotEnd(BeforeFirstRun, isEnd);
				}
				isEnd = DoIfNotEnd(RunImpl, isEnd);
				FirstCall = false;
				return isEnd;
			}

			private bool DoIfNotEnd(Func<bool> a, bool isEnd)
			{
				if (a != null && !isEnd)
				{
					return a();
				}
				return isEnd;
			}

			public Func<bool> BeforeFirstRun { get; set; }

			public AMSPhase NextPhase { get; set; }
			private bool FirstCall { get; set; } = false;

			public AMSPhase Init()
			{
				FirstCall = true;
				return this;
			}


		}
	}
}
