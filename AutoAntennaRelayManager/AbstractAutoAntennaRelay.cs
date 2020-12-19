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
		public abstract class AbstractAutoAntennaRelay
		{
			private readonly string tagGroup;
			protected readonly MyGridProgram p;
			protected readonly MemberInfo MyInfo;
			private readonly List<IMyBroadcastListener> listGroupListener = new List<IMyBroadcastListener>();
			private IMyShipController controller;
			private IMyRadioAntenna antenna;
			protected Logger LOGGER { get; private set; }
			public AbstractAutoAntennaRelay(MyGridProgram p, String tagGroup)
			{
				this.p = p;
				IMyTextSurface lcd = (IMyTextSurface) p.GridTerminalSystem.GetBlockWithName("lcd");
				LOGGER = new Logger(lcd);
				this.tagGroup = tagGroup;
				MyInfo = new MemberInfo(LOGGER, p.IGC.Me);
				LoadBlocks();
				MyInfo.Update(new GPS(p.Me.GetPosition()), DateTime.UtcNow, new Vector3D(0, 0, 0), GetCurrentAntennaRange());
			}

			protected void AddListener(string subtag, bool withCallback = false)
			{
				string realTag = ToRealTag(subtag);
				IMyBroadcastListener l = p.IGC.RegisterBroadcastListener(realTag);
				if (withCallback)
				{
					l.SetMessageCallback(realTag);
				}
				listGroupListener.Add(l);
			}

			protected virtual bool LoadBlocks()
			{
				bool isSuccess = true;

				List<IMyShipController> lstController = new List<IMyShipController>();
				p.GridTerminalSystem.GetBlocksOfType(lstController);
				if (lstController.Count <= 0)
					LOGGER.Warn("No Controller ship available");
				else
					controller = lstController.First();

				List<IMyRadioAntenna> lstAntennas = new List<IMyRadioAntenna>();
				p.GridTerminalSystem.GetBlocksOfType(lstAntennas);
				if(lstAntennas.Count == 0)
				{
					// FATAL no antenna aviable
					LOGGER.Error("No Antenna available");
					isSuccess = false;
				}
				else
					antenna = lstAntennas.First();
				return isSuccess;
			}

			public abstract void Run(UpdateType updateSource);

			private string ToRealTag(string subtag)
			{
				return tagGroup + "|" + subtag;
			}
			private string GetSubTag(string tag)
			{
				return tag.Split('|')[1];
			}

			protected void WriteToChanel<TData>(string subtag, TData data)
			{
				LOGGER.Debug("Send " + data + " to " + subtag);
				p.IGC.SendBroadcastMessage(ToRealTag(subtag), data);
			}

			protected bool UpdateMyInfo()
			{
				GPS newPos = new GPS(p.Me.GetPosition());
				Vector3D speed;
				if (controller != null)
				{
					speed = controller.GetShipVelocities().LinearVelocity;
				}
				else
				{
					// Calcul manually
					double durationMs = MyInfo.MillisecondsSinceSend();
					speed = (newPos.ToVector3D() - MyInfo.Pos.ToVector3D()) / (durationMs / 1000D);
				}
				int antennaRange = GetCurrentAntennaRange();
				return MyInfo.Update(newPos, DateTime.UtcNow, speed, antennaRange);
			}

			protected void ReadChanelMsgs(Action<MyIGCMessage, string> actionOnMessage)
			{
				foreach(IMyBroadcastListener l in listGroupListener)
				{
					while (l.HasPendingMessage)
					{
						MyIGCMessage msg = l.AcceptMessage();
						if (p.IGC.Me != msg.Source)
						{
							actionOnMessage(msg, GetSubTag(msg.Tag));
						}
					}
				}
			}

			protected int GetCurrentAntennaRange()
			{
				return (int)antenna.Radius;
			}

			protected void SetAntennaRange(int v)
			{
				antenna.Radius = v;
				MyInfo.Update(v);
			}

		}
	}
}
