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
		public class ShipAutoPilot : AbstractSEClass
		{

			#region Const fields
			public const UpdateType UPDATE_TYPE = UpdateType.Update1;
			public const UpdateFrequency UPDATE_FREQUENCY = UpdateFrequency.Update1;

			//Maximum in game speed. (change this if you use speed mods)
			private const double MAX_SPEED = 100;
			//Time (in cycle) that the ship must remain stationary before
			private const double SHUTDOWN_TIME = 15;
			//The speed (m/s) that the ship will move at once it has begun braking 
			private const double FROM_BRAKING_TO_END_SPEED = 3;
			//If the code should attempt to land the drop pod or stop slightly above the ground
			private const bool ATTEMPT_TO_LAND = false;

			private const double TIME_MAX_CYCLE = 1.0 / 10;
			private const double BURN_THRUST_PERCENTAGE = 0.80;
			private const double SAFETY_CUSHION = 0.5;

			#endregion

			#region Variable fields
			protected IMyShipController referenceBlock;
			protected List<IMyGyro> gyros = new List<IMyGyro>();
			protected List<IMyThrust> allThrusters = new List<IMyThrust>();
			protected List<IMyThrust> brakingThrusters = new List<IMyThrust>();
			protected List<IMyThrust> otherThrusters = new List<IMyThrust>();
			protected readonly string controllerName;
			protected double timeSpentStationary = 0;
			protected double shipCenterToEdge = 0;
			protected bool isSetup = false;
			protected double lastErr = 696969; //giggle

			//--- Configurables
			protected GPS destination = new GPS(0, 0, 0);
			protected double distanceToStop = 200;
			protected string brakingDir = "F";
			protected string accelerationDir = "F";
			protected bool isControlPitch = true;
			protected bool isControlYaw = true;
			protected bool isControlRoll = true;

			#endregion

			public ShipAutoPilot(MyGridProgram p, string controllerName = "") : base(p)
			{
				this.controllerName = controllerName;
				LoadBlocks();
			}

			protected override bool LoadBlocks()
			{
				isSetup = true;

				gyros.Clear();
				allThrusters.Clear();
				brakingThrusters.Clear();
				otherThrusters.Clear();

				if (controllerName == "")
				{
					List<IMyShipController> lstController = new List<IMyShipController>();
					p.GridTerminalSystem.GetBlocksOfType(lstController);
					if (lstController.Count == 0)
					{
						LOGGER.Error("No ship controller on this ship");
						isSetup = false;
					}
					else
					{
						// Récupère le premier controller trouvé
						referenceBlock = lstController[0];
						LOGGER.Info("Controller found");
					}
				}
				else
				{
					referenceBlock = (IMyShipController)p.GridTerminalSystem.GetBlockGroupWithName(controllerName);
					LOGGER.Info("Controller with name " + controllerName + " found");
					isSetup = referenceBlock != null;
				}

				p.GridTerminalSystem.GetBlocksOfType(gyros);
				p.GridTerminalSystem.GetBlocksOfType(allThrusters);

				if (referenceBlock != null)
				{
					GetThrusterOrientation();
				}

				if (brakingThrusters.Count == 0)
				{
					isSetup = false;
					p.Echo("CRITICAL: No braking thrusters were found");
				}

				if (gyros.Count == 0)
				{
					isSetup = false;
					p.Echo($"CRITICAL: No gyroscopes were found");
				}

				if (!isSetup)
				{
					p.Echo("Setup Failed!");
				}
				else
				{
					p.Echo("Setup Successful!");
					shipCenterToEdge = GetShipFarthestEdgeDistance(referenceBlock);
					p.Runtime.UpdateFrequency = UPDATE_FREQUENCY;
				}
				return isSetup;
			}
			private void ActivateBlocks()
			{
				foreach (IMyGyro thisGyro in gyros)
				{
					thisGyro.Enabled = true;
				}

				foreach (IMyThrust thisThruster in otherThrusters)
				{
					thisThruster.Enabled = true;
				}
			}

			private void StopSystem()
			{
				// Stop thrusters de freinage
				foreach (IMyThrust thisThrust in brakingThrusters)
				{
					thisThrust.ThrustOverridePercentage = 0f;
					//thisThrust.Enabled = false;
				}
				// Stop gyro override
				StopGyros();
				// Stop autres thrusters
				foreach (IMyThrust thisThrust in otherThrusters)
				{
					thisThrust.ThrustOverridePercentage = 0f;
					//thisThrust.Enabled = false;
				}
				// Reactive les dampeners
				referenceBlock.DampenersOverride = true;

				// Stop l'auto update
				LOGGER.Debug("STOP");
				p.Runtime.UpdateFrequency = UpdateFrequency.None;
			}

			public void Reset()
			{
				LOGGER.Info("Reset");
				StopSystem();
				UpdateConfig();
			}

			public void Run()
			{
				if (!isSetup)
				{
					isSetup = LoadBlocks();
				}
				if (isSetup)
				{
					// FOR rerun after reset
					p.Runtime.UpdateFrequency = UPDATE_FREQUENCY;
					bool shouldBrake;
					bool shouldStabilize;
					CheckStabilizeAndBrake(out shouldBrake, out shouldStabilize);
					Debug("ShouldBrake ", shouldBrake);
					//Debug("shouldStabilize ", shouldStabilize);
					shouldStabilize = true;

					double shipAcceleration = referenceBlock.GetNaturalGravity().Length();
					//Debug("shipAcceleration", shipAcceleration);

					double brakingSpeed = GetBrakingSpeed();
					//Debug("brakingSpeed ", brakingSpeed);
					ManageGyro(shouldStabilize, brakingSpeed);
					ManageBrake(shouldBrake, brakingSpeed, shipAcceleration);
					if (timeSpentStationary > SHUTDOWN_TIME)
					{

						Debug("timeSpentStationary", timeSpentStationary);
						LOGGER.Debug("immobile => stop");
						StopSystem();
					}

					referenceBlock.DampenersOverride = false;
					ActivateBlocks();
				}
			}

			#region Thruster management
			protected void GetThrusterOrientation()
			{
				brakingThrusters.Clear();
				Vector3D vectBrakingDir = VectorUtils.GetDirection(referenceBlock, brakingDir);

				foreach (IMyThrust thisThrust in allThrusters)
				{
					var thrustDir = thisThrust.WorldMatrix.Forward;
					bool sameDir = thrustDir == vectBrakingDir;

					if (sameDir)
						brakingThrusters.Add(thisThrust);
					else
						otherThrusters.Add(thisThrust);
				}
			}

			private double GetBrakingDistanceThreshold(double shipAcceleration, Vector3D shipVelocityVec)
			{
				double forceSum = GetBrakingForce();
				//some arbitrary number that will stop NaN cases
				if (forceSum == 0)
					return 1000d;

				double mass = referenceBlock.CalculateShipMass().PhysicalMass;

				//Echo($"Mass: {mass.ToString()}");
				double maxDeceleration = forceSum / mass;
				double effectiveDeceleration = maxDeceleration - shipAcceleration;
				double wantedDeceleration = effectiveDeceleration * BURN_THRUST_PERCENTAGE;
				//Echo($"Decel: {deceleration.ToString()}");

				// If speed mod
				double maxSpeed = MAX_SPEED;
				if (shipVelocityVec.LengthSquared() > MAX_SPEED * MAX_SPEED)
					maxSpeed = shipVelocityVec.Length();

				//cushion to account for discrete time errors
				double safetyCushion = maxSpeed * TIME_MAX_CYCLE * SAFETY_CUSHION;

				//derived from: vf^2 = vi^2 + 2*a*d
				//added for safety :)
				double distanceToStop = shipVelocityVec.LengthSquared() / (2 * wantedDeceleration) + safetyCushion;

				return distanceToStop;
			}

			protected void BrakingOn()
			{
				LOGGER.Debug("BrakingOn");
				foreach (IMyThrust thisThrust in brakingThrusters)
				{
					thisThrust.Enabled = true;
					thisThrust.ThrustOverridePercentage = 1f;
				}

				foreach (IMyThrust thisThrust in otherThrusters)
				{
					thisThrust.ThrustOverridePercentage = 0.00001f;
				}
			}

			protected void BrakingOff()
			{
				foreach (IMyThrust thisThrust in brakingThrusters)
				{
					thisThrust.ThrustOverridePercentage = 0.00001f;
				}

				foreach (IMyThrust thisThrust in otherThrusters)
				{
					thisThrust.ThrustOverridePercentage = 0f;
				}
			}

			protected double GetBrakingForce()
			{
				return brakingThrusters.Select(t => t.MaxEffectiveThrust).Sum();
			}
			protected void BrakingThrust(double brakingSpeed, double shipAcceleration)
			{
				double forceSum = GetBrakingForce();

				//Calculate equillibrium thrust ratio
				var mass = referenceBlock.CalculateShipMass().PhysicalMass;
				var equillibriumThrustPercentage = mass * shipAcceleration / forceSum * 100;

				//PD controller
				var err = brakingSpeed - FROM_BRAKING_TO_END_SPEED;
				double errDerivative = (lastErr == 696969) ?
					0 :
					(err - lastErr) / TIME_MAX_CYCLE;

				//This is the thing we will add to correct our speed
				double kP = 5;
				double kD = 2;
				var deltaThrustPercentage = kP * err + kD * errDerivative;
				lastErr = err;

				float percentNeedToEnd = (float)(equillibriumThrustPercentage + deltaThrustPercentage) / 100f;
				foreach (IMyThrust thisThrust in brakingThrusters)
				{
					thisThrust.ThrustOverridePercentage = percentNeedToEnd;
					thisThrust.Enabled = true;
				}

				// TODO revoir fonctionnement planete vs espace, 
				// si on fait ça dans l'espace on va dériver, 
				// si on fait pas ça sur une planete on va peut etre se crasher
				if (false)
				{
					foreach (IMyThrust thisThrust in otherThrusters)
					{
						thisThrust.Enabled = false;
					}
				}
			}

			#endregion

			private double GetBrakingSpeed()
			{
				Vector3D acceleration = referenceBlock.GetNaturalGravity();
				Vector3D shipVelocityVec = referenceBlock.GetShipVelocities().LinearVelocity;
				return VectorUtils.Projection(shipVelocityVec, acceleration).Length() * Math.Sign(shipVelocityVec.Dot(acceleration));
			}
			private void CheckStabilizeAndBrake(out bool shouldBrake, out bool shouldStabilize)
			{
				//---Get speed
				double currentSpeed = referenceBlock.GetShipSpeed();
				Vector3D vectDest = destination.ToVector3D();
				Vector3D pos = referenceBlock.GetPosition();
				Vector3D vectDistanceToEnd = vectDest - pos;
				double distanceToEnd = vectDistanceToEnd.Length();
				Vector3D shipVelocityVec = referenceBlock.GetShipVelocities().LinearVelocity;

				Debug("currentSpeed", currentSpeed);
				Debug("distanceToEnd", distanceToEnd);
				Debug("shipVelocityVec", shipVelocityVec);

				//--- Manage gravity
				Vector3D gravityVec = referenceBlock.GetNaturalGravity();
				double gravityVecMagnitude = gravityVec.Length();
				if (gravityVecMagnitude > 0.0001)
				{
					double distanceToSurface;
					referenceBlock.TryGetPlanetElevation(MyPlanetElevation.Surface, out distanceToSurface);
					// TODO take into account this distance if we fly near the surface
				}
				distanceToEnd -= shipCenterToEdge;

				double distanceToStartBraking = GetBrakingDistanceThreshold(gravityVecMagnitude, shipVelocityVec);
				//this gives us a good safety cushion for stabilization procedures
				double distanceToStartStabilize = distanceToStartBraking + 10 * currentSpeed;
				Debug("distanceToStartBraking", distanceToStartBraking);
				//Debug("distanceToStartStabilize", distanceToStartStabilize);

				shouldBrake = false;
				shouldStabilize = false;
				if (distanceToEnd < 100 && currentSpeed < 1)
					timeSpentStationary += 1;
				else
				{
					timeSpentStationary = 0;
					shouldStabilize = distanceToEnd <= distanceToStartStabilize;
					shouldBrake = distanceToEnd <= distanceToStartBraking;
				}
			}

			private void ManageBrake(bool shouldBrake, double brakingSpeed, double shipAcceleration)
			{
				if (shouldBrake)
				{
					//kills dampeners to stop their interference with landing procedures
					referenceBlock.DampenersOverride = false;
					bool isBraking = brakingSpeed > FROM_BRAKING_TO_END_SPEED;
					if (isBraking)
						BrakingOn();
					else
					{
						Debug("brakingSpeed", brakingSpeed);
						LOGGER.Debug("!isBraking => Stop");
						if (ATTEMPT_TO_LAND)
							BrakingThrust(brakingSpeed, shipAcceleration);
						else
							StopSystem(); // Fin
					}
				}
				else
					BrakingOff();
			}

			#region Gyro management

			private void ManageGyro(bool shouldStabilize, double brakingSpeed)
			{

				if (shouldStabilize)
				{
					Vector3D shipVelocityVec = referenceBlock.GetShipVelocities().LinearVelocity;
					// V = Alignment vector
					Vector3D v;
					if (brakingSpeed > FROM_BRAKING_TO_END_SPEED)
					{
						v = shipVelocityVec;
					}
					else
					{
						// TODO tenir compte du vecteur gravité
						Vector3D gravityVec = referenceBlock.GetNaturalGravity();
						Vector3D toDestVec = destination.ToVector3D() - referenceBlock.GetPosition();
						v = toDestVec;
					}

					//---Get Roll, Yaw and Pitch Angles 
					var m = referenceBlock.WorldMatrix;

					// Roll = rotation around forward/backward axis => to roll
					// Pitch = rotation around left/right axis => to go up or down
					// Yaw = rotation around up/down axis => to go left or right
					// Pitch > 0 = pique du nez 
					// | < 0 = lève le nez
					// Yaw > 0 = tourne direction droite comme une voiture 
					// | < 0 = gauche 
					// Roll > 0 = tourne sur l'axe vers la gauche (eq touche A) 
					// | < 0 = vers la droite (eq touche E)

					var refF = VectorUtils.GetDirection(referenceBlock, brakingDir);
					double pitchSpeed = 0;
					double yawSpeed = 0;
					double rollSpeed = 0;
					GetPitchYawRoll(v, refF, ref pitchSpeed, ref yawSpeed, ref rollSpeed);

					double pitch_deg = Math.Round(pitchSpeed / Math.PI * 180);
					double yaw_deg = Math.Round(yawSpeed / Math.PI * 180);
					double roll_deg = Math.Round(rollSpeed / Math.PI * 180);
					Debug("pitch_deg", pitch_deg);
					Debug("yaw_deg", yaw_deg);
					Debug("roll_deg", roll_deg);

					//---Enforce rotation speed limit
					double sumSpeed = Math.Abs(rollSpeed) + Math.Abs(yawSpeed) + Math.Abs(pitchSpeed);
					if (sumSpeed > 2 * Math.PI)
					{
						double scale = 2 * Math.PI / sumSpeed;
						rollSpeed *= scale;
						yawSpeed *= scale;
						pitchSpeed *= scale;
					}

					if (!isControlPitch)
						pitchSpeed = 0;
					if (!isControlYaw)
						yawSpeed = 0;
					if (!isControlRoll)
						rollSpeed = 0;
					
					ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed);
				}
				else
				{
					StopGyros();
				}
			}

			private void GetPitchYawRoll(Vector3D v, Vector3D refF, ref double pitchSpeed, ref double yawSpeed, ref double rollSpeed)
			{
				MatrixD m = referenceBlock.WorldMatrix;
				var refU = GetUpDir(m, refF);
				var refR = GetRightDir(m, refF);
				// if down > refDir = forward

				Vector3D projVToRefF = VectorUtils.Projection(v, refF);
				Vector3D projVToRefU = VectorUtils.Projection(v, refU);
				Vector3D projVToRefR = VectorUtils.Projection(v, refR);

				//-- Angle 1 : Pitch for F
				Vector3D projVToFU = projVToRefF + projVToRefU;
				double clamp1 = MathHelper.Clamp(refF.Dot(projVToFU) / (refF.Length() * projVToFU.Length()), -1, 1);
				double angleProjVToFU_F = Math.Acos(clamp1);
				int signe1 = Math.Sign(refR.Dot(projVToFU.Cross(refF)));
				angleProjVToFU_F *= signe1;

				//-- Angle 2 : Yaw for F
				Vector3D projVToFR = projVToRefF + projVToRefR;
				double clamp2 = MathHelper.Clamp(refF.Dot(projVToFR) / (refF.Length() * projVToFR.Length()), -1, 1);
				double angleProjVToFR_F = Math.Acos(clamp2);
				int signe2 = Math.Sign(refU.Dot(projVToFR.Cross(refF)));
				angleProjVToFR_F *= signe2;

				angleProjVToFU_F = Math.Round(angleProjVToFU_F, 3);
				angleProjVToFR_F = Math.Round(angleProjVToFR_F, 3);

				if (refF == m.Forward)
				{//OK
					pitchSpeed = angleProjVToFU_F;
					yawSpeed = angleProjVToFR_F;
				}
				else if (refF == m.Backward)
				{//OK
					pitchSpeed = -angleProjVToFU_F;
					yawSpeed = angleProjVToFR_F;
				}
				else if (refF == m.Up)
				{//OK
					pitchSpeed = angleProjVToFU_F;
					rollSpeed = angleProjVToFR_F;
				}
				else if (refF == m.Down)
				{//OK
					pitchSpeed = angleProjVToFU_F;
					rollSpeed = -angleProjVToFR_F;
				}
				else if (refF == m.Left)
				{//OK
					rollSpeed = -angleProjVToFU_F;
					yawSpeed = angleProjVToFR_F;
				}
				else if (refF == m.Right)
				{//OK
					rollSpeed = angleProjVToFU_F;
					yawSpeed = angleProjVToFR_F;
				}
			}

			private void StopGyros()
			{
				foreach (IMyGyro thisGyro in gyros)
				{
					thisGyro.GyroOverride = false;
					thisGyro.Pitch = 0;
					thisGyro.Yaw = 0;
					thisGyro.Roll = 0;
				}
			}
			public void ApplyGyroOverride(double pitch_speed, double yaw_speed, double roll_speed)
			{
				var rotationVec = new Vector3D(pitch_speed, yaw_speed, roll_speed);
				var shipMatrix = referenceBlock.WorldMatrix;
				var relativeRotationVec = Vector3D.TransformNormal(rotationVec, shipMatrix);
				foreach (var thisGyro in gyros)
				{
					var gyroMatrix = thisGyro.WorldMatrix;
					var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gyroMatrix));
					thisGyro.Pitch = (float)transformedRotationVec.X;
					thisGyro.Yaw = (float)transformedRotationVec.Y;
					thisGyro.Roll = (float)transformedRotationVec.Z;
					thisGyro.GyroOverride = true;
				}
			}

			private Vector3D GetRightDir(MatrixD m, Vector3D refDir)
			{
				Vector3D result = m.Right;
				if (refDir == m.Backward)
					result = m.Left;
				else if (refDir == m.Up)
					result = m.Right;
				else if (refDir == m.Down)
					result = m.Right;
				else if (refDir == m.Left)
					result = m.Forward;
				else if (refDir == m.Right)
					result = m.Backward;
				return result;
			}
			private Vector3D GetUpDir(MatrixD m, Vector3D refDir)
			{
				Vector3D result = m.Up;
				if (refDir == m.Backward)
					result = m.Up;
				else if (refDir == m.Up)
					result = m.Backward;
				else if (refDir == m.Down)
					result = m.Forward;
				else if (refDir == m.Left)
					result = m.Up;
				else if (refDir == m.Right)
					result = m.Up;
				return result;
			}

			#endregion
			#region VARIABLE CONFIG

			protected override void BuildConfig()
			{
				config.Clear();
				config.Add("GPS", destination.ToString());
				config.Add("distanceToStop", distanceToStop.ToString());
				config.Add("brakingDir", brakingDir.ToString());
				config.Add("accelerationDir", accelerationDir.ToString());
				config.Add("isControlPitch", isControlPitch.ToString());
				config.Add("isControlYaw", isControlYaw.ToString());
				config.Add("isControlRoll", isControlRoll.ToString());
				UpdateConfig();
			}
			protected void UpdateConfig()
			{
				string gps = "0;0;0";
				config.LoadConfigFromCustomData();
				config.GetVariable("GPS", ref gps);
				config.GetVariable("distanceToStop", ref distanceToStop);
				config.GetVariable("brakingDir", ref brakingDir);
				config.GetVariable("accelerationDir", ref accelerationDir);
				config.GetVariable("isControlPitch", ref isControlPitch);
				config.GetVariable("isControlYaw", ref isControlYaw);
				config.GetVariable("isControlRoll", ref isControlRoll);
				destination = new GPS(gps);
				config.WriteConfig();
			}
			#endregion

		}

	}
}
