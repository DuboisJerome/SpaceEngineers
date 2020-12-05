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
		public abstract class AbstractSlaveSystem : AbstractSystem
		{
			public const string TAG_SEARCH_MASTER = "TAG_SEARCH_MASTER";
			public const string TAG_SLAVE_ACCEPTATION = "TAG_SLAVE_ACCEPTATION";
			public AbstractSlaveSystem(MyGridProgram p) : base(p)
			{ }

			public override void Run()
			{
				if (!HasMaster())
				{
					SearchMaster();
				}
				else
				{
					ListenToEveryDirectMsg(DoOnDirectMsg);
					CheckIfMasterReachable();
				}
			}

			protected void CheckIfMasterReachable()
			{
				RemoveNotReachableContact();
			}

			public bool HasMaster()
			{
				return HasContact();
			}

			private void SearchMaster()
			{
				ListenToOneDirectMsg(TryRegisterMaster);
				if (!HasMaster())
				{
					p.Echo("Sending search master");
					SendToEveryOne(TAG_SEARCH_MASTER, "");
				}
			}

			public void TryRegisterMaster(MyIGCMessage msg)
			{
				if (msg.Tag == AbstractMasterSystem.TAG_MASTER_RESERVATION)
				{
					p.Echo("Getting master reservation from " + msg.Source);
					// Don't change Master
					if (!HasMaster())
					{
						p.Echo("Adding master " + msg.Source);
						AddContact(msg.Source);
						SendToContact(msg.Source, TAG_SLAVE_ACCEPTATION, "");
					}
				}
			}

			protected bool IsFromMyMaster(MyIGCMessage msg)
			{
				return IsMyContact(msg.Source);
			}

			public void DoOnDirectMsg(MyIGCMessage msg)
			{
				if (IsFromMyMaster(msg))
				{
					DoOnDirectMasterMsg(msg);
				}
			}

			public abstract void DoOnDirectMasterMsg(MyIGCMessage msg);
		}
	}
}
