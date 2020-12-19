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
		public class AutoAntennaRelaySlave : AbstractAutoAntennaRelay
		{

			public AutoAntennaRelaySlave(MyGridProgram p, String tagGroup) : base(p, tagGroup)
			{
				p.IGC.UnicastListener.SetMessageCallback(AutoAntennaRelayManager.TAG_ASK_CHANGE_RANGE);
				AddListener(AutoAntennaRelayManager.TAG_ASK_UPDATE_INFO, true);
				AddListener(AutoAntennaRelayManager.TAG_ASK_UPDATE_INFO_OPTIONNAL, true);
				SendMyInfo();
			}

			public override void Run(UpdateType updateSource)
			{
				if ((updateSource & UpdateType.Terminal) != 0)
				{
					LOGGER.Clear();
				}
				LOGGER.Debug("updateSource = " + updateSource);
				bool needSendInfo = false;
				// Reads all msgs
				bool isNeedReadMessage = (updateSource & UpdateType.IGC) != 0;
				if (isNeedReadMessage)
				{
					ReadDirectMsgs();
					ReadChanelMsgs((msg, tag) =>
					{
						bool isInfoAsk = tag.Equals(AutoAntennaRelayManager.TAG_ASK_UPDATE_INFO);
						bool isInfoAskOptionnal = tag.Equals(AutoAntennaRelayManager.TAG_ASK_UPDATE_INFO_OPTIONNAL);
						if(isInfoAsk || isInfoAskOptionnal)
						{
							bool isInfoChanged = UpdateMyInfo();
							needSendInfo |= isInfoAsk || (isInfoAskOptionnal && isInfoChanged);
						}
						
					});
				}
				if (needSendInfo)
				{
					SendMyInfo();
				}

			}

			private void ReadDirectMsgs()
			{
				// Listen direct messages
				IMyUnicastListener uniL = p.IGC.UnicastListener;
				int newRangeRequired = -1;
				while (uniL.HasPendingMessage)
				{
					MyIGCMessage msg = uniL.AcceptMessage();
					LOGGER.Debug("Got direct msg");
					if (msg.Tag == AutoAntennaRelayManager.TAG_ASK_CHANGE_RANGE)
					{
						newRangeRequired = Math.Max((int)msg.Data, newRangeRequired);
					}
				}

				if(newRangeRequired > -1)
				{
					SetAntennaRange(newRangeRequired);
					SendMyInfo();
				}
			}

			private void SendMyInfo()
			{
				WriteToChanel(AutoAntennaRelayManager.TAG_INFO_MEMBER, MyInfo.ToString());
			}

		}
	}
}
