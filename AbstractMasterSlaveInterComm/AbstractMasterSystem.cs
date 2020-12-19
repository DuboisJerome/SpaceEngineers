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
		public abstract class AbstractMasterSystem : AbstractSystem
		{
			public const string TAG_MASTER_RESERVATION = "TAG_MASTER_RESERVATION";
			public AbstractMasterSystem(MyGridProgram p) : base(p)
			{
			}

			public override void Run()
			{
				ListenToSlaveSearchingMaster();
				ListenToEveryDirectMsg(DoOnDirectMsg);
				CheckIfSlaveReachable();
			}

			protected virtual void CheckIfSlaveReachable()
			{
				RemoveNotReachableContact();
			}

			protected void ListenToSlaveSearchingMaster()
			{
				ListenToEveryIndirectMsg(AbstractSlaveSystem.TAG_SEARCH_MASTER, ReserveSlave);
			}

			private void ReserveSlave(MyIGCMessage msg)
			{
				p.Echo("Sending Reservation to " + msg.Source);
				SendToContact(msg.Source, TAG_MASTER_RESERVATION, "");
			}

			protected bool IsFromMySlave(MyIGCMessage msg)
			{
				return IsMyContact(msg.Source);
			}

			public void DoOnDirectMsg(MyIGCMessage msg)
			{
				if (msg.Tag == AbstractSlaveSystem.TAG_SLAVE_ACCEPTATION)
				{
					p.Echo("Adding slave " + msg.Source);
					AddContact(msg.Source);
				} else if(IsFromMySlave(msg))
				{
					DoOnDirectSlaveMsg(msg);
				}
			}

			public abstract void DoOnDirectSlaveMsg(MyIGCMessage msg);
		}
	}
}
