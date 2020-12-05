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
		public class AutomaticMiningSystem : AbstractAMS<AutomaticMiningSystemParams>
		{
			const string PHASE_EXTENDING_DRILL = "PHASE_EXTENDING_DRILL";
			const string PHASE_RETRACTING_LG = "PHASE_RETRACTING_LG";
			const string PHASE_MOVING_MOTOR = "PHASE_MOVING_MOTOR";

			// references to blocks
			private List<IMyShipDrill> listDrills = new List<IMyShipDrill>();
			// Pistons in mining direction
			private List<IMyPistonBase> listPistonMDir = new List<IMyPistonBase>();
			// Pistons in mining opposite direction
			private List<IMyPistonBase> listPistonMODir = new List<IMyPistonBase>();
			private List<IMyLightingBlock> listLight = new List<IMyLightingBlock>();
			private List<IMyCargoContainer> listCargo = new List<IMyCargoContainer>();
			private IMyBeacon beacon = null;
			private IMyMotorStator mainMotor = null;

			// var
			private double lastAngle;

			public AutomaticMiningSystem(AutomaticMiningSystemParams parameters) : base(parameters)
			{
				this.lastAngle = Params.MinRotorAngle;
				
				AMSPhase phase1 = this.AddPhase(PHASE_EXTENDING_DRILL, RunPhaseExtendingPistonMiningDirection);
				AMSPhase phase2 = this.AddPhase(PHASE_RETRACTING_LG, RunPhaseRetractingPistionOppositeMiningDirection);
				AMSPhase phase3 = this.AddPhase(PHASE_MOVING_MOTOR, RunPhaseRotating);

				// Set worflow
				phaseLaunch.NextPhase = phase1;
				phase1.NextPhase = phase2;
				phase2.NextPhase = phase3;
				phase3.NextPhase = phaseEnding;
			}

			protected override void Start()
			{
				base.Start();
				SetDrillsOnOff(true);
				SetBeaconAlert(false);
			}

			protected override void Stop()
			{
				base.Stop();
				// Turn off drills
				SetDrillsOnOff(false);
				// Stop pistons
				foreach (IMyPistonBase p in listPistonMDir)
				{
					p.SetValue("Velocity", 0F);
				}
				foreach (IMyPistonBase p in listPistonMODir)
				{
					p.SetValue("Velocity", 0F);
				}
				if (mainMotor != null)
				{
					mainMotor.SetValue("Velocity", 0F);
				}
			}

			protected override void Reset()
			{
				base.Reset();
				SetBeaconAlert(false);
			}

			protected override void AlertFull()
			{
				base.AlertFull();
				SetLightFull();
				SetBeaconAlert(true, Params.Name + " is Full");
			}

			private void SetLightFinnish()
			{
				foreach (IMyLightingBlock light in listLight)
				{
					light.SetValue("Color", Color.Green);
					light.SetValue("OnOff", true);
					light.SetValue("Blink Interval", 0F);
					light.SetValue("Blink Lenght", 50F);
				}
			}

			private void SetLightRunning()
			{
				foreach (IMyLightingBlock light in listLight)
				{
					light.SetValue("Color", Color.Blue);
					light.SetValue("OnOff", true);
					light.SetValue("Blink Interval", 1F);
					light.SetValue("Blink Lenght", 20F);
				}
			}

			private void SetLightFull()
			{
				foreach (IMyLightingBlock light in listLight)
				{
					light.SetValue("Color", Color.Orange);
					light.SetValue("OnOff", true);
					light.SetValue("Blink Interval", 1F);
					light.SetValue("Blink Lenght", 50F);
				}
			}

			private void SetBeaconAlert(bool isAlert, string newBeaconName = null)
			{
				if (beacon != null)
				{
					beacon.SetValue("Radius", isAlert ? Params.MaxBeaconRadius : Params.MinBeaconRadius);
					beacon.HudText = (isAlert && newBeaconName != null) ? newBeaconName : Params.Name;

				}
			}

			protected override bool IsFull()
			{
				bool isMustTestCargoCapacity = !IsPhase(PHASE_ENDING) && !IsPhase(PHASE_NONE);
				if (!isMustTestCargoCapacity)
				{
					return false;
				}

				foreach (IMyCargoContainer cargo in listCargo)
				{
					IMyInventory inventory = cargo.GetInventory();
					if (inventory.CurrentVolume != inventory.MaxVolume)
					{
						return false;
					}
				}
				return true;
			}

			protected override void LoadBlocks()
			{
				listDrills = new List<IMyShipDrill>();
				GetProgram().GridTerminalSystem.GetBlocksOfType(listDrills);

				IMyBlockGroup groupPistonsMDir = GetProgram().GridTerminalSystem.GetBlockGroupWithName(Params.GrpNamePistonMDir);
				listPistonMDir = new List<IMyPistonBase>();
				if (groupPistonsMDir == null)
				{
					logger.Error("Group \"" + Params.GrpNamePistonMDir + "\" not found");
				}
				else
				{
					groupPistonsMDir.GetBlocksOfType(listPistonMDir);
				}

				IMyBlockGroup groupPistonMODir = GetProgram().GridTerminalSystem.GetBlockGroupWithName(Params.GrpNamePistonMODir);
				listPistonMODir = new List<IMyPistonBase>();
				if (groupPistonMODir == null)
				{
					logger.Info("Group \"" + Params.GrpNamePistonMODir + "\" not found");
				}
				else
				{
					GetProgram().GridTerminalSystem.GetBlocksOfType(listPistonMODir);
				}

				List<IMyBeacon> listBeacon = new List<IMyBeacon>();
				GetProgram().GridTerminalSystem.GetBlocksOfType(listBeacon);
				if (listBeacon.Count > 0)
				{
					beacon = listBeacon.First();
					beacon.SetValue("Radius", Params.MinBeaconRadius);
				}
				else
				{
					logger.Info("No beacon found");
				}

				mainMotor = GetProgram().GridTerminalSystem.GetBlockWithName(Params.MainRotorName) as IMyMotorStator;
				if (mainMotor == null)
				{
					logger.Info("No rotor \"" + Params.MainRotorName + "\" found");
				}
				else
				{
					mainMotor.SetValue("LowerLimit", Params.MinRotorAngle);
					mainMotor.SetValue("UpperLimit", Params.MinRotorAngle);
				}

				IMyBlockGroup groupLights = GetProgram().GridTerminalSystem.GetBlockGroupWithName(Params.GrpNameLight);
				listLight = new List<IMyLightingBlock>();
				if (groupLights == null)
				{
					logger.Info("Group \"" + Params.GrpNameLight + "\" not found");
				}
				else
				{
					groupLights.GetBlocksOfType(listLight);
				}

				IMyBlockGroup groupCargo = GetProgram().GridTerminalSystem.GetBlockGroupWithName(Params.GrpNameCargo);
				listCargo = new List<IMyCargoContainer>();
				if (groupCargo == null)
				{
					logger.Info("Group \"" + Params.GrpNameCargo + "\" not found");
				}
				else
				{
					groupCargo.GetBlocksOfType(listCargo);
				}
			}

			private void SetDrillsOnOff(bool isOn)
			{
				foreach (IMyShipDrill d in listDrills)
				{
					d.SetValue("OnOff", isOn);
				}
			}

			// Retract pistons
			protected override bool ResetPosition()
			{
				SetLightRunning();
				bool isInitEnd = true;
				isInitEnd &= InitPistonMiningDirection();
				isInitEnd &= InitPistonMiningOppositeDirection();
				return isInitEnd;
			}

			private bool InitPistonMiningDirection()
			{
				bool isInitEnd = true;
				foreach (IMyPistonBase p in listPistonMDir)
				{
					if (p.Status != PistonStatus.Retracted)
					{
						isInitEnd = false;
						// retracts
						p.SetValue("Velocity", -Params.FastPistonVelocity);
						// share inertia tensor
						p.SetValue("ShareInertiaTensor", true);
					}
				}

				SetDrillsOnOff(!isInitEnd);

				return isInitEnd;
			}

			private bool InitPistonMiningOppositeDirection()
			{
				bool isInitEnd = true;
				foreach (IMyPistonBase p in listPistonMODir)
				{
					if (p.Status != PistonStatus.Extended)
					{
						isInitEnd = false;
						// extends
						p.SetValue("Velocity", Params.FastPistonVelocity);
						// share inertia tensor
						p.SetValue("ShareInertiaTensor", true);
					}
				}
				return isInitEnd;
			}

			private bool RunPhaseExtendingPistonMiningDirection()
			{
				SetLightRunning();
				if (listPistonMDir == null || listPistonMDir.Count <= 0)
					return true;
				SetDrillsOnOff(true);
				// looking for extending piston
				IMyPistonBase currentPistonExtending = listPistonMDir.Find(p => p.Status == PistonStatus.Extending);

				if (currentPistonExtending != null)
				{
					// piston mining already extending
					// do nothing
					return false;
				}

				foreach (IMyPistonBase p in listPistonMDir)
				{
					if (p.Status != PistonStatus.Extended)
					{
						// Extend one of the pistons not already extended
						p.SetValue("Velocity", Params.SlowPistonVelocity);
						return false;
					}
				}

				return true;
			}

			private bool RunPhaseRetractingPistionOppositeMiningDirection()
			{
				SetLightRunning();
				if (listPistonMODir == null || listPistonMODir.Count <= 0)
					return true;
				SetDrillsOnOff(true);
				bool isAllPistonRetracted = true;

				foreach (IMyPistonBase p in listPistonMODir)
				{
					if (p.Status != PistonStatus.Retracting)
					{
						p.SetValue("Velocity", -Params.SlowPistonVelocity);
					}
					if (p.Status != PistonStatus.Retracted)
					{
						isAllPistonRetracted = false;
					}
				}

				return isAllPistonRetracted;
			}

			private bool RunPhaseRotating()
			{
				SetLightRunning();
				if (mainMotor == null)
					return true;
				bool isMiningEnd = false;
				bool isInitEnd = ResetPosition();
				if (isInitEnd)
				{
					double newUpperLimit = lastAngle + Params.StepRotorAngle;
					if (newUpperLimit > Params.MaxRotorAngle)
					{
						isMiningEnd = true;
					}
					else
					{
						double currentAngle = Math.Round(mainMotor.Angle * 180.0 / Math.PI);
						bool isEndRotating = currentAngle == newUpperLimit;
						if (isEndRotating)
						{
							// End rotation
							mainMotor.SetValue("Velocity", 0F);
							currentPhase = GetPhase(PHASE_EXTENDING_DRILL);
							lastAngle = currentAngle;
						}
						else
						{
							// Start rotation
							mainMotor.SetValue("UpperLimit", (float)newUpperLimit);
							mainMotor.SetValue("Velocity", Params.RotorVelocity);
						}
					}
				}

				return isMiningEnd;
			}

			protected override void OnEnd()
			{
				if (mainMotor != null)
				{
					this.lastAngle = Params.MinRotorAngle;
					mainMotor.SetValue("UpperLimit", Params.MinRotorAngle);
					mainMotor.SetValue("Velocity", -Params.RotorVelocity);
				}
				SetLightFinnish();
				SetBeaconAlert(true, Params.Name + " has Finnished");
				SetDrillsOnOff(false);
			}
		}

	}
}
