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
		public class PlateformeForage : AbstractAMS<PlateformeForageParams>
		{
			const string PHASE_EXTENDING_DRILL = "PHASE_EXTENDING_DRILL";
			const string PHASE_RETRACTING_DRILL = "PHASE_RETRACTING_DRILL";
			const string PHASE_MOVING_HINGE = "PHASE_MOVING_HINGE";

			private List<IMyPistonBase> listPistonVertical = new List<IMyPistonBase>();
			private IMyMotorStator mainRotor = null;
			private IMyMotorStator mainHinge = null;
			private List<IMyShipDrill> listDrills = new List<IMyShipDrill>();

			public PlateformeForage(PlateformeForageParams parameters) : base(parameters)
			{
				// find cockpit
				IMyCockpit cockpit = GetProgram().GridTerminalSystem.GetBlockWithName("Cockpit") as IMyCockpit;
				if(cockpit != null)
				{					
					this.logger = new Logger(cockpit.GetSurface(0));
					this.logger.MinLvl = Logger.Level.INFO;
				} else
				{
					this.logger.Warn("No cockpit name Cockpit found");
				}

				AMSPhase phase1 = this.AddPhase(PHASE_EXTENDING_DRILL, RunPhaseGoDown);
				AMSPhase phase2 = this.AddPhase(PHASE_RETRACTING_DRILL, RunPhaseGoUp);
				AMSPhase phase3 = this.AddPhase(PHASE_MOVING_HINGE, RunPhaseInclinate);

				// set worflow
				phaseLaunch.NextPhase = phase1;
				phase1.NextPhase = phase2;
				phase2.NextPhase = phase3;
				phase3.NextPhase = phase1;
			}

			protected override void LoadBlocks()
			{
				listDrills = new List<IMyShipDrill>();
				GetProgram().GridTerminalSystem.GetBlocksOfType(listDrills);

				listPistonVertical = new List<IMyPistonBase>();
				GetProgram().GridTerminalSystem.GetBlocksOfType(listPistonVertical);

				mainRotor = GetProgram().GridTerminalSystem.GetBlockWithName(Params.MainRotorName) as IMyMotorStator;
				if (mainRotor == null)
				{
					logger.Warn("No rotor \"" + Params.MainRotorName + "\" found");
				}

				mainHinge = GetProgram().GridTerminalSystem.GetBlockWithName(Params.MainHingeName) as IMyMotorStator;
				if (mainHinge == null)
				{
					logger.Warn("No hinge \"" + Params.MainHingeName + "\" found");
				}
			}

			protected override void Start()
			{
				base.Start();
				SetDrillsOnOff(true);
				if (mainRotor != null)
				{
					mainRotor.TargetVelocityRPM = Params.MainRotorVitesse;
				}
			}

			protected override void Stop()
			{
				base.Stop();
				foreach (IMyPistonBase p in listPistonVertical)
				{
					p.Velocity = 0F;
				}
				if (mainRotor != null)
				{
					mainRotor.TargetVelocityRPM = 0F;
				}
				if (mainHinge != null)
				{
					mainHinge.TargetVelocityRPM = 0F;
				}
				SetDrillsOnOff(false);
			}

			protected override bool ResetPosition()
			{
				return RunPhaseGoUp();
			}

			protected override void OnEnd()
			{
				if (mainHinge != null)
				{
					mainHinge.LowerLimitDeg = 0F;
					mainHinge.UpperLimitDeg = 0F;
					mainHinge.TargetVelocityRPM = 0F;
				}
				if(mainRotor != null)
				{
					mainRotor.TargetVelocityRPM = 0F;
				}
			}

			private bool RunPhaseGoDown()
			{
				if (listPistonVertical == null || listPistonVertical.Count <= 0)
					return true;
				SetDrillsOnOff(true);

				return ExtendingPistons();
			}

			private bool RunPhaseGoUp()
			{
				bool isEnd = RetractingHinge();
				logger.Debug("Is hinge retracted = " + isEnd);
				if (isEnd)
				{
					isEnd = RetractingPistons();
					logger.Debug("Is piston retracted = " + isEnd);
				}
				return isEnd;
			}

			private bool RunPhaseInclinate()
			{
				if (mainHinge == null)
					return true;
				bool isEnd = false;
				double currentAngle = Math.Round(mainHinge.Angle * 180.0 / Math.PI);
				logger.Debug("ll = " + mainHinge.LowerLimitDeg);
				logger.Debug("ul = " + mainHinge.UpperLimitDeg);
				logger.Debug("a = " + currentAngle);
				logger.Debug("tv = " + mainHinge.TargetVelocityRPM);
				if (currentAngle <= mainHinge.LowerLimitDeg)
				{
					// May need to move
					if (mainHinge.UpperLimitDeg <= Params.MainHingeMaxAngle)
					{
						// Next step
						mainHinge.TargetVelocityRPM = 1F;
						mainHinge.UpperLimitDeg += Params.MainHingeStepAngle;
					}
					else
					{
						isEnd = true;
						currentPhase = GetPhase(PHASE_ENDING);
					}
				} else
				{
					// is moving	
					if (mainHinge.TargetVelocityRPM <= 0.005)
					{
						mainHinge.TargetVelocityRPM = 1F;
					}
					if(currentAngle >= mainHinge.UpperLimitDeg - 0.005)
					{
						isEnd = true;
						mainHinge.TargetVelocityRPM = 0F;
						mainHinge.LowerLimitDeg += Params.MainHingeStepAngle;
					}
				}

				return isEnd;
			}

			private bool RetractingPistons()
			{
				bool isAllRetracted = true;
				foreach (IMyPistonBase p in listPistonVertical)
				{
					if (p.Status != PistonStatus.Retracted)
					{
						isAllRetracted = false;
						// retracts
						p.SetValue("Velocity", Params.PistonVitesseMonte);
						// share inertia tensor
						p.SetValue("ShareInertiaTensor", true);
					}
				}

				return isAllRetracted;
			}

			private bool ExtendingPistons()
			{
				// Extends piston one by one
				IMyPistonBase currentPistonExtending = listPistonVertical.Find(p => p.Status == PistonStatus.Extending);

				if (currentPistonExtending != null)
				{
					// piston mining already extending
					// do nothing
					return false;
				}

				foreach (IMyPistonBase p in listPistonVertical)
				{
					if (p.Status != PistonStatus.Extended)
					{
						// Extend one of the pistons not already extended
						p.SetValue("Velocity", Params.PistonVitesseDescente);
						return false;
					}
				}

				// Tous les pistons sont étendues
				return true;
			}

			private bool RetractingHinge()
			{
				if (mainHinge == null)
					return true;
				bool isEnd = false;
				double currentAngle = Math.Round(mainHinge.Angle * 180.0 / Math.PI);
				logger.Debug("ll = " + mainHinge.LowerLimitDeg);
				logger.Debug("a = " + currentAngle);
				logger.Debug("tv = " + mainHinge.TargetVelocityRPM);
				if (currentAngle > mainHinge.LowerLimitDeg)
				{
					if (mainHinge.TargetVelocityRPM >= -0.005)
					{
						mainHinge.TargetVelocityRPM = -1F;
					}
					else
					{
						// retracting
					}
				}
				else
				{
					isEnd = true;
				}

				return isEnd;
			}

			private void SetDrillsOnOff(bool isOn)
			{
				foreach (IMyShipDrill d in listDrills)
				{
					d.SetValue("OnOff", isOn);
				}
			}

			protected override bool IsFull()
			{
				foreach(IMyShipDrill d in listDrills)
				{
					IMyInventory inventory = d.GetInventory();
					double pourcentFull = (100D* inventory.CurrentVolume.RawValue) / inventory.MaxVolume.RawValue;
					// Si une foreuse est rempli à plus de 25%
					if (pourcentFull > 25)
					{
						logger.Warn("Drill inventory = "+ pourcentFull+"%");
					}
					if (pourcentFull >= 99)
					{
						return true;
					}
				}
				return base.IsFull();
			}
		}
	}
}
