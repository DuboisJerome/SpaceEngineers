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
		public class ShipFormationMaster : AbstractMasterSystem
		{
			public const string TAG_FORMATION = "TAG_FORMATION";
			public const int DISTANCE = 15;
			private GPS myLastPos = null;
			private int lastNbSlaves = 0;
			public ShipFormationMaster(MyGridProgram p) : base(p) { }

			public override void Run()
			{
				base.Run();
				MoveSlaves();
			}

			private void MoveSlaves()
			{

				GPS myNewPos = new GPS(p.Me.GetPosition());
				int nbSlaves = this.listeContactId.Count;
				if (nbSlaves > 0 && (nbSlaves != lastNbSlaves ||  HasMove()))
				{
					foreach (long idSlave in this.listeContactId)
					{
						SendToContact(idSlave, TAG_FORMATION, myNewPos.ToString());
					}
				}
				myLastPos = myNewPos;
				lastNbSlaves = nbSlaves;
			}

			private bool HasMove()
			{
				GPS myNewPos = new GPS(p.Me.GetPosition());
				return !myNewPos.Equals(myLastPos);
			}

			public override void DoOnDirectSlaveMsg(MyIGCMessage msg)
			{
				// Nothing
				p.Echo("Getting msg from slave " + msg.Source);
			}
		}
	}
}
