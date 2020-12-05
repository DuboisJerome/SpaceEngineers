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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
	partial class Program : MyGridProgram
	{
		const string DOCKS_GRP_NAME = "Docks";
		List<IMyShipConnector> listDock;

		public Program()
		{
			IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(DOCKS_GRP_NAME);
			listDock = new List<IMyShipConnector>();
			group.GetBlocksOfType(listDock);
			Runtime.UpdateFrequency = UpdateFrequency.Update100;
		}

		public void Save()
		{

		}

		public void Main(string argument, UpdateType updateSource)
		{
			if ((updateSource & (UpdateType.Update100)) != 0)
			{
				foreach(IMyShipConnector dock in listDock)
				{
					ManageDock(dock);
				}
			}

		}

		private void ManageDock(IMyShipConnector dock)
		{
			if(dock == null)
			{
				return;
			}

			Echo($"Docking {dock.CustomName}");

			IMyShipConnector otherConnector = dock.OtherConnector;
			if(otherConnector == null || dock.Status == MyShipConnectorStatus.Unconnected)
			{
				Echo("not connected");
				return;
			} else if(dock.Status == MyShipConnectorStatus.Connectable)
			{
				Echo($"to connector {otherConnector.CustomName}");
				UnDock(otherConnector);
			} else if(dock.Status == MyShipConnectorStatus.Connected)
			{
				Echo($"to connector {otherConnector.CustomName}");
				Dock(otherConnector);
			}
		}


		private void Dock(IMyShipConnector otherConnector)
		{
			// Tanks 
			List<IMyGasTank> listGasTanks = new List<IMyGasTank>();
			GridTerminalSystem.GetBlocksOfType(listGasTanks, t => t.IsSameConstructAs(otherConnector));
			listGasTanks.ForEach(t => t.SetValue("Stockpile", true));

			// Batteries
			List<IMyBatteryBlock> listBatteries = new List<IMyBatteryBlock>();
			GridTerminalSystem.GetBlocksOfType(listBatteries, b => b.IsSameConstructAs(otherConnector));
			listBatteries.ForEach(b => b.SetValue("ChargeMode", (long)ChargeMode.Recharge));

			// 
			List<IMyTerminalBlock> powerUsers = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyThrust>(powerUsers, p => p.IsSameConstructAs(otherConnector));
			GridTerminalSystem.GetBlocksOfType<IMyAirVent>(powerUsers, p => p.IsSameConstructAs(otherConnector));
			GridTerminalSystem.GetBlocksOfType<IMyBeacon>(powerUsers, p => p.IsSameConstructAs(otherConnector));
			GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(powerUsers, p => p.IsSameConstructAs(otherConnector));
			GridTerminalSystem.GetBlocksOfType<IMyOreDetector>(powerUsers, p => p.IsSameConstructAs(otherConnector));
			GridTerminalSystem.GetBlocksOfType<IMyLaserAntenna>(powerUsers, p => p.IsSameConstructAs(otherConnector));
			GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(powerUsers, p => p.IsSameConstructAs(otherConnector));

			foreach (IMyTerminalBlock tb in powerUsers)
			{
				tb.SetValue("OnOff", false);
				Echo($"Turn {tb.CustomName} Off");
			}
		}

		private void UnDock(IMyShipConnector otherConnector)
		{
			// Tanks 
			List<IMyGasTank> listGasTanks = new List<IMyGasTank>();
			GridTerminalSystem.GetBlocksOfType(listGasTanks, t => t.IsSameConstructAs(otherConnector));
			listGasTanks.ForEach(t => t.SetValue("Stockpile", false));

			// Batteries
			List<IMyBatteryBlock> listBatteries = new List<IMyBatteryBlock>();
			GridTerminalSystem.GetBlocksOfType(listBatteries, b => b.IsSameConstructAs(otherConnector));
			listBatteries.ForEach(b => b.SetValue("ChargeMode", (long)ChargeMode.Auto));

			// 
			List<IMyTerminalBlock> powerUsers = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocksOfType<IMyThrust>(powerUsers, p => p.IsSameConstructAs(otherConnector));
			GridTerminalSystem.GetBlocksOfType<IMyAirVent>(powerUsers, p => p.IsSameConstructAs(otherConnector));
			GridTerminalSystem.GetBlocksOfType<IMyBeacon>(powerUsers, p => p.IsSameConstructAs(otherConnector));
			GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(powerUsers, p => p.IsSameConstructAs(otherConnector));
			GridTerminalSystem.GetBlocksOfType<IMyOreDetector>(powerUsers, p => p.IsSameConstructAs(otherConnector));
			GridTerminalSystem.GetBlocksOfType<IMyLaserAntenna>(powerUsers, p => p.IsSameConstructAs(otherConnector));
			GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(powerUsers, p => p.IsSameConstructAs(otherConnector));

			foreach (IMyTerminalBlock tb in powerUsers)
			{
				tb.SetValue("OnOff", true);
				Echo($"Turn {tb.CustomName} On");
			}
		}
	}
}