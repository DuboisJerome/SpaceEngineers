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
			const string PHASE_EXTENDING_PISTON = "PHASE_EXTENDING_PISTON";
			const string PHASE_RETRACTING_HINGE = "PHASE_RETRACTING_HINGE";
			const string PHASE_RETRACTING_PISTON = "PHASE_RETRACTING_PISTON";
			const string PHASE_MOVING_HINGE = "PHASE_MOVING_HINGE";
			const float deltaAngle = 0.005F;

			private List<IMyPistonBase> listPiston = new List<IMyPistonBase>();
			private IMyMotorStator mainRotor = null;
			private IMyMotorStator mainHinge = null;
			private List<IMyShipDrill> listDrill = new List<IMyShipDrill>();
			private List<IMyLightingBlock> listLight = new List<IMyLightingBlock>();

			public PlateformeForage(PlateformeForageParams parameters) : base(parameters)
			{
				AMSPhase phase1 = this.AddPhase(PHASE_EXTENDING_PISTON, RunPhaseExtendingPistons);
				AMSPhase phase2 = this.AddPhase(PHASE_RETRACTING_HINGE, RunPhaseRetractingHinge, BeforeFirstRunPhaseRetractingHinge);
				AMSPhase phase3 = this.AddPhase(PHASE_RETRACTING_PISTON, RunPhaseRetractingPistons);
				AMSPhase phase4 = this.AddPhase(PHASE_MOVING_HINGE, RunPhaseInclinate, BeforeFirstRunPhaseInclinate);

				// set worflow
				AfterLaunch(phase1);
				phase1.NextPhase = phase2;
				phase2.NextPhase = phase3;
				phase3.NextPhase = phase4;
				phase4.NextPhase = phase1;
			}

			protected override void LoadBlocks()
			{
				listDrill = new List<IMyShipDrill>();
				GetProgram().GridTerminalSystem.GetBlocksOfType(listDrill);

				listPiston = new List<IMyPistonBase>();
				GetProgram().GridTerminalSystem.GetBlocksOfType(listPiston);
				foreach (IMyPistonBase p in listPiston)
				{
					p.SetValue("ShareInertiaTensor", true);
				}

				listLight = new List<IMyLightingBlock>();
				GetProgram().GridTerminalSystem.GetBlocksOfType(listLight);


				mainRotor = GetProgram().GridTerminalSystem.GetBlockWithName(Params.MainRotorName) as IMyMotorStator;
				if (mainRotor == null)
				{
					logger.Warn("No rotor \"" + Params.MainRotorName + "\" found");
				}
				else
				{
					mainRotor.SetValue("ShareInertiaTensor", true);
				}

				mainHinge = GetProgram().GridTerminalSystem.GetBlockWithName(Params.MainHingeName) as IMyMotorStator;
				if (mainHinge == null)
				{
					logger.Warn("No hinge \"" + Params.MainHingeName + "\" found");
				}
				else
				{
					mainHinge.SetValue("ShareInertiaTensor", true);
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

				foreach (IMyLightingBlock l in listLight)
				{
					l.Color = Color.White;
				}
			}

			protected override void Stop()
			{
				base.Stop();
				foreach (IMyPistonBase p in listPiston)
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

			protected override bool BeforeFirstRunResetPosition()
			{
				GetPhase(PHASE_RETRACTING_HINGE).Init();
				GetPhase(PHASE_RETRACTING_PISTON).Init();
				return false;
			}
			protected override bool ResetPosition()
			{
				bool isPhaseEnded = GetPhase(PHASE_RETRACTING_HINGE).Run();
				if (isPhaseEnded)
				{
					isPhaseEnded = GetPhase(PHASE_RETRACTING_PISTON).Run();
				}
				return isPhaseEnded;
			}

			protected override void OnEnd()
			{
				if (mainHinge != null)
				{
					mainHinge.LowerLimitDeg = 0F;
					mainHinge.UpperLimitDeg = 0F;
					mainHinge.TargetVelocityRPM = 0F;
				}
				if (mainRotor != null)
				{
					mainRotor.TargetVelocityRPM = 0F;
				}
				SetDrillsOnOff(false);
			}

			private bool RunPhaseExtendingPistons()
			{
				if (listPiston == null || listPiston.Count <= 0)
					return true;
				// Extends piston one by one
				IMyPistonBase currentPistonExtending = listPiston.Find(p => p.Status == PistonStatus.Extending);

				if (currentPistonExtending != null)
				{
					// piston mining already extending
					// do nothing
					return false;
				}

				foreach (IMyPistonBase p in listPiston)
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

			private bool BeforeFirstRunPhaseRetractingHinge()
			{
				if (mainHinge == null)
					return true;
				mainHinge.LowerLimitDeg = 0F;
				foreach (IMyPistonBase p in listPiston)
				{
					p.SetValue("Velocity", 0F);
				}
				return false;
			}


			/**
			 * Retracting hinge to 0 angle
			 */
			private bool RunPhaseRetractingHinge()
			{
				return RunPhaseHingeToAngle(mainHinge.LowerLimitDeg, 3F);
			}

			private bool BeforeFirstRunPhaseInclinate()
			{
				if (mainHinge == null)
					return true;
				// May need to move
				float nextAngle = mainHinge.UpperLimitDeg + Params.MainHingeStepAngle;
				bool isFinalEnd = nextAngle > Params.MainHingeMaxAngle;
				if (isFinalEnd)
				{
					logger.Debug("Final:" + nextAngle);
					ToPhaseEnd();
				}
				else
				{
					// Next step
					mainHinge.UpperLimitDeg = nextAngle;
				}
				return isFinalEnd;
			}

			private bool RunPhaseInclinate()
			{
				return RunPhaseHingeToAngle(mainHinge.UpperLimitDeg, 1.5F);
			}

			private bool RunPhaseHingeToAngle(float angle, float maxAbsSpeed = 1F)
			{
				float speed = GetHingeSpeedNeedToAngle(angle, maxAbsSpeed);
				mainHinge.TargetVelocityRPM = speed;
				return speed == 0;
			}

			private bool RunPhaseRetractingPistons()
			{
				bool isAllRetracted = true;
				foreach (IMyPistonBase p in listPiston)
				{
					if (p.Status != PistonStatus.Retracted)
					{
						isAllRetracted = false;
						// retracts
						p.SetValue("Velocity", Params.PistonVitesseMonte);
					}
				}

				return isAllRetracted;
			}

			private double GetHingeAngle()
			{
				double currentAngle = Math.Round(mainHinge.Angle * 180.0 / Math.PI);
				logger.Debug("Hinge => " + mainHinge.LowerLimitDeg + " < " + currentAngle + " < " + mainHinge.UpperLimitDeg);
				return currentAngle;
			}

			private float GetHingeSpeedNeedToAngle(float limit, float speed = 1F)
			{
				double angle = GetHingeAngle();
				double max = limit + deltaAngle;
				double min = limit - deltaAngle;
				return angle > max ? -speed : (angle < min ? speed : 0F);
			}

			private void SetDrillsOnOff(bool isOn)
			{
				foreach (IMyShipDrill d in listDrill)
				{
					d.SetValue("OnOff", isOn);
				}
			}

			protected override bool IsFull()
			{
				foreach (IMyShipDrill d in listDrill)
				{
					IMyInventory inventory = d.GetInventory();
					double pourcentFull = (100D * inventory.CurrentVolume.RawValue) / inventory.MaxVolume.RawValue;
					if (pourcentFull >= 80)
					{
						return true;
					}
				}
				return base.IsFull();
			}

			protected override void AlertFull()
			{
				foreach (IMyLightingBlock l in listLight)
				{
					l.Color = Color.OrangeRed;
				}
				base.AlertFull();
			}
		}
	}
}
