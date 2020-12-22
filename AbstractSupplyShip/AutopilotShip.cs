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
		public class AutopilotShip : AbstractSEClass
		{
			protected IMyShipController referenceBlock;
			protected readonly string controllerName;
			protected double timeSpentStationary = 0;
			protected double shipCenterToEdge = 0;
			protected GPS destination;
			protected bool isSetup = false;

			//Maximum in game speed. (change this if you use speed mods)
			private const double MAX_SPEED = 100;
			//Time (in seconds) that the ship must remain stationary before
			private const double shutdownTime = 3;
			//The speed (m/s) that the ship will move at once it has begun braking 
			private const double FROM_BRAKING_TO_END_SPEED = 3;
			//If the code should attempt to land the drop pod or stop slightly above the ground
			private const bool attemptToLand = false;

			private const double updatesPerSecond = 10;
			private const double timeMaxCycle = 1.0 / updatesPerSecond;
			private const double BURN_THRUST_PERCENTAGE = 0.80;
			private const double SafetyCushionConstant = 0.5;

			#region copier depuis drop ship
			double timeCurrentCycle = 100;

			double lastErr = 696969; //giggle
			#endregion

			List<IMyGyro> gyros = new List<IMyGyro>();
			List<IMyThrust> allThrusters = new List<IMyThrust>();
			List<IMyThrust> brakingThrusters = new List<IMyThrust>();
			List<IMyThrust> otherThrusters = new List<IMyThrust>();

			public AutopilotShip(MyGridProgram p, string controllerName = "") : base(p)
			{
				this.controllerName = controllerName;
				LoadBlocks();
			}

			public override bool LoadBlocks()
			{
				isSetup = true;

				UpdateConfig();

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
					}
				}
				else
				{
					referenceBlock = (IMyShipController)p.GridTerminalSystem.GetBlockGroupWithName(controllerName);
					isSetup = referenceBlock != null;
				}

				p.GridTerminalSystem.GetBlocksOfType(gyros);
				p.GridTerminalSystem.GetBlocksOfType(allThrusters);

				if (referenceBlock != null)
				{
					GetThrusterOrientation(referenceBlock);
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
					thisThrust.Enabled = false;
				}
				// Stop gyro override
				foreach (IMyGyro thisGyro in gyros)
				{
					thisGyro.GyroOverride = false;
				}
				// Stop autres thrusters
				foreach (IMyThrust thisThrust in otherThrusters)
				{
					thisThrust.ThrustOverridePercentage = 0f;
					thisThrust.Enabled = false;
				}
				// Reactive les dampeners
				referenceBlock.DampenersOverride = true;

				// Stop l'auto update
				p.Runtime.UpdateFrequency = UpdateFrequency.None;
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
				double safetyCushion = maxSpeed * timeMaxCycle * SafetyCushionConstant;

				//derived from: vf^2 = vi^2 + 2*a*d
				//added for safety :)
				double distanceToStop = shipVelocityVec.LengthSquared() / (2 * wantedDeceleration) + safetyCushion;

				return distanceToStop;
			}

			#region Thruster management
			protected void GetThrusterOrientation(IMyTerminalBlock refBlock)
			{
				brakingThrusters.Clear();
				var brakingDir = refBlock.WorldMatrix.Forward;

				foreach (IMyThrust thisThrust in allThrusters)
				{
					var thrustDir = thisThrust.WorldMatrix.Forward;
					bool sameDir = thrustDir == brakingDir;

					if (sameDir)
						brakingThrusters.Add(thisThrust);
					else
						otherThrusters.Add(thisThrust);
				}
			}

			protected void BrakingOn()
			{
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
					(err - lastErr) / timeMaxCycle;

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
				if(false)
				{
					foreach (IMyThrust thisThrust in otherThrusters)
					{
						thisThrust.Enabled = false;
					}
				}
			}

			#endregion
			public void Run()
			{
				if (!isSetup)
				{
					isSetup = LoadBlocks();
				}
				if(isSetup)
				{
					timeCurrentCycle += secondsPerTick;

					if (timeCurrentCycle >= timeMaxCycle)
					{
						StabilizePod();
						if (timeSpentStationary > shutdownTime)
							StopSystem();
						timeCurrentCycle = 0;
					}

					referenceBlock.DampenersOverride = false;
					ActivateBlocks();
				}
			}

			#region Gyro management

			private void StabilizePod()
			{
				//---Get speed
				double currentSpeed = referenceBlock.GetShipSpeed();


				Vector3D vectDistanceToEnd = destination.ToVector3D() - referenceBlock.GetPosition();

				Vector3D shipVelocityVec = referenceBlock.GetShipVelocities().LinearVelocity;
				
				double brakingSpeed = VectorUtils.Projection(shipVelocityVec, vectDistanceToEnd).Length() * Math.Sign(shipVelocityVec.Dot(vectDistanceToEnd));

				//---Determine if we should manually override brake controls
				double distanceToEnd = vectDistanceToEnd.Length();

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

				double distanceToStartBraking = GetBrakingDistanceThreshold(gravityVecMagnitude,  shipVelocityVec);
				//this gives us a good safety cushion for stabilization procedures
				double distanceToStartStabilize = distanceToStartBraking + 10 * currentSpeed;

				bool shouldBrake = false;
				bool shouldStabilize = false;
				if (distanceToEnd < 100 && currentSpeed < 1)
					timeSpentStationary += timeCurrentCycle;
				else
				{
					timeSpentStationary = 0;
					shouldStabilize = distanceToEnd <= distanceToStartStabilize;
					shouldBrake = distanceToEnd <= distanceToStartBraking;
					//kills dampeners to stop their interference with landing procedures
					if (shouldBrake)
						referenceBlock.DampenersOverride = false;
				}

				if (shouldBrake)
				{
					bool isBraking = brakingSpeed > FROM_BRAKING_TO_END_SPEED;
					if (isBraking)
						BrakingOn();
					else
					{
						if (attemptToLand)
							BrakingThrust(brakingSpeed, gravityVecMagnitude);
						else
							StopSystem(); // Fin
					}
				}
				else
					BrakingOff();
				

				if (shouldStabilize)
				{
					//---Get Roll and Pitch Angles 

					Vector3D alignmentVec = brakingSpeed > FROM_BRAKING_TO_END_SPEED || gravityVecMagnitude <= 0 ? shipVelocityVec : gravityVec;

					//---Dir'n vectors of the reference block 
					var referenceMatrix = referenceBlock.WorldMatrix;
					var referenceForward = referenceMatrix.Forward;
					var referenceLeft = referenceMatrix.Left;
					var referenceUp = referenceMatrix.Up;
					var referenceOrigin = referenceMatrix.Translation;

					// Roll = rotation around forward/backward axis => to roll
					// Pitch = rotation around left/right axis => to go up or down
					// Yaw = rotation around up/down axis => to go left or right

					double forAngleRadPitch = MathHelper.Clamp(alignmentVec.Dot(referenceForward) / alignmentVec.Length(), -1, 1);
					double anglePitch = Math.Acos(forAngleRadPitch); // TODO test mais on freine vers l'avant pas le bas : - Math.PI / 2;

					// ???
					Vector3D relativeLeftVec = referenceForward.Cross(alignmentVec);

					double forAngleRadRoll = MathHelper.Clamp(referenceLeft.Dot(relativeLeftVec) / relativeLeftVec.Length(), -1, 1);
					double angleRoll = Math.Acos(forAngleRadRoll);
					angleRoll *= Math.Sign(VectorUtils.Projection(referenceLeft, alignmentVec).Dot(alignmentVec)); //ccw is positive 

					anglePitch *= -1;
					angleRoll *= -1;

					double roll_deg = Math.Round(angleRoll / Math.PI * 180);
					double pitch_deg = Math.Round(anglePitch / Math.PI * 180);

					//---Angle controller    
					double rollSpeed = Math.Round(angleRoll, 2);
					double pitchSpeed = Math.Round(anglePitch, 2);

					//---Enforce rotation speed limit
					if (Math.Abs(rollSpeed) + Math.Abs(pitchSpeed) > 2 * Math.PI)
					{
						double scale = 2 * Math.PI / (Math.Abs(rollSpeed) + Math.Abs(pitchSpeed));
						rollSpeed *= scale;
						pitchSpeed *= scale;
					}

					ApplyGyroOverride(pitchSpeed, 0, -rollSpeed);
				}
				else
				{
					foreach (IMyGyro thisGyro in gyros)
						thisGyro.GyroOverride = false;
				}
			}
			public void ApplyGyroOverride(double pitch_speed, double yaw_speed, double roll_speed)
			{
				var rotationVec = new Vector3D(-pitch_speed, yaw_speed, roll_speed); //because keen does some weird stuff with signs
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
			#endregion
			#region VARIABLE CONFIG
			public override void InitConfig()
			{
				string destination = "";
				config.Clear();
				config.Add("GPS", destination);
				UpdateConfig();
			}

			private void UpdateConfig()
			{
				string gps = "0;0;0";
				config.LoadConfigFromCustomData();
				config.GetVariable("GPS", ref gps);
				config.WriteConfig();
				destination = new GPS(gps);
			}
			#endregion
		}

	}
}
