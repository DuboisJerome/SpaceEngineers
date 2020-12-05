using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript
{
	partial class Program : MyGridProgram
	{
		/* 
	//Whip's Super Simple Missile Code for Rexxar v1 - 1/23/17

	/ //// /   SETUP    / //// /   
	You need at least:
	* A timer set to run this program and trigger now itself
	* A antenna
	* A thruster pointed forward
	* A remote
	* A program
	* A gyro

	/ //// /   FIRING INSTRUCTIONS  / //// /   
	Plug in a Vector3D or GPS coordinate into the argument and it will fly to that spot.
	*/

		//My magical PID constants
		const double proportionalConstant = 5;
		const double derivativeConstant = 2;

		bool driftCompensation = true;
		//set this to false if u want the missiles to drift like ass


		// No touchey below
		IMyRemoteControl shipReference = null;

		bool fireMissile = false;
		bool firstGuidance = true;
		Logger logger;

		Vector3D targetPos = new Vector3D();
		Vector3D missilePos = new Vector3D();
		Vector3D gravVec = new Vector3D();

		double lastYawAngle = 0;
		double lastPitchAngle = 0;
		double lastRollAngle = 0;

		List<IMyThrust> thrust = new List<IMyThrust>();
		List<IMyThrust> forwardThrust = new List<IMyThrust>();
		List<IMyThrust> otherThrust = new List<IMyThrust>();
		List<IMyGyro> gyros = new List<IMyGyro>();
		List<IMyRemoteControl> remotes = new List<IMyRemoteControl>();

		void Main(string arg)
		{
			Runtime.UpdateFrequency = UpdateFrequency.Update1;
			logger = new Logger(Me.GetSurface(0));
			bool isSetup = GetBlocks();

			if (isSetup)
			{
				GuideMissile();

			}
		}

		bool GetBlocks()
		{
			bool successfulSetup = true;

			forwardThrust.Clear();
			otherThrust.Clear();

			GridTerminalSystem.GetBlocksOfType(gyros);
			GridTerminalSystem.GetBlocksOfType(thrust);
			GridTerminalSystem.GetBlocksOfType(remotes);

			if (gyros.Count == 0)
			{
				Echo($"Error: No gyros");
				successfulSetup = false;
			}

			if (thrust.Count == 0)
			{
				Echo($"Error: No thrust");
				successfulSetup = false;
			}

			if (remotes.Count == 0)
			{
				Echo($"Error: No remotes");
				successfulSetup = false;
			}

			if (successfulSetup)
			{
				shipReference = remotes[0];
				GetThrusterOrientation(shipReference, thrust, out forwardThrust, out otherThrust);

				foreach (IMyThrust thisThrust in otherThrust)
				{
					//thisThrust.ApplyAction("OnOff_On");
				}

				foreach (IMyThrust thisThrust in forwardThrust)
				{
					thisThrust.ApplyAction("OnOff_On");
					//thisThrust.SetValue("Override", float.MaxValue);
				}

			}

			return successfulSetup;
		}

		void GetThrusterOrientation(IMyRemoteControl refBlock, List<IMyThrust> thrusters, out List<IMyThrust> _forwardThrust, out List<IMyThrust> _otherThrust)
		{
			var forwardDirn = refBlock.WorldMatrix.Forward;

			_forwardThrust = new List<IMyThrust>();
			_otherThrust = new List<IMyThrust>();

			foreach (IMyThrust thisThrust in thrusters)
			{
				var thrustDirn = thisThrust.WorldMatrix.Backward;
				bool sameDirn = thrustDirn == forwardDirn;

				if (sameDirn)
				{
					_forwardThrust.Add(thisThrust);
				}
				else
				{
					_otherThrust.Add(thisThrust);
				}
			}
		}

		void GuideMissile()
		{
			missilePos = shipReference.GetPosition();

			//---Get orientation vectors of our missile 
			Vector3D missileFrontVec = shipReference.WorldMatrix.Forward;
			Vector3D missileLeftVec = shipReference.WorldMatrix.Left;
			Vector3D missileUpVec = shipReference.WorldMatrix.Up;

			//---Check if we have gravity 
			double rollAngle = 0; double rollSpeed = 0;

			var remote = remotes[0] as IMyRemoteControl;

			gravVec = shipReference.GetNaturalGravity();
			double gravMagSquared = gravVec.LengthSquared();
			if (gravMagSquared != 0)
			{
				if (gravVec.Dot(missileUpVec) < 0)
				{
					//	rollAngle = Math.PI / 2 - Math.Acos(MathHelper.Clamp(gravVec.Dot(missileLeftVec) / gravVec.Length(), -1, 1));
				}
				else
				{
					//	rollAngle = Math.PI + Math.Acos(MathHelper.Clamp(gravVec.Dot(missileLeftVec) / gravVec.Length(), -1, 1));
				}
			}
			else
			{
				rollSpeed = 0;
			}


			//---Get travel vector 
			var missileVelocityVec = shipReference.GetShipVelocities().LinearVelocity;
			double speed = shipReference.GetShipSpeed();

			//---Find vector from missile to destinationVec   
			Vector3D missileToTargetVec;
			if (speed > 10 || )
			{
				missileToTargetVec = Vector3D.Negate(missileVelocityVec);
			}
			else
			{
				shipReference.TryGetPlanetPosition(out targetPos);
				missileToTargetVec = Vector3D.Negate(targetPos - missilePos);// targetPos - missilePos;
			}

			//---Calc our new heading based upon our travel vector    

			var headingVec = CalculateHeadingVector(missileToTargetVec, missileVelocityVec, driftCompensation);

			//---Get pitch and yaw angles 
			double yawAngle; double pitchAngle;
			GetRotationAngles(headingVec, missileFrontVec, missileLeftVec, missileUpVec, out yawAngle, out pitchAngle);

			logger.Info("mttv: " + missileToTargetVec);
			logger.Info("mvv: " + missileVelocityVec);
			logger.Info("hv: " + headingVec);
			if (firstGuidance)
			{
				lastPitchAngle = pitchAngle;
				lastYawAngle = yawAngle;
				firstGuidance = false;
			}

			//---Angle controller
			logger.Debug(yawAngle + "|" + lastYawAngle);
			double yawSpeed = Math.Round(proportionalConstant * yawAngle + derivativeConstant * (yawAngle - lastYawAngle) * 60, 3);
			double pitchSpeed = Math.Round(proportionalConstant * pitchAngle + derivativeConstant * (pitchAngle - lastPitchAngle) * 60, 3);

			//---Set appropriate gyro override 
			if (speed <= 1E-2)
			{
				logger.Info("Fin");
				ApplyGyroOverride(0, 0, 0, gyros, shipReference);
				Runtime.UpdateFrequency = UpdateFrequency.None;
			}
			else
			{
				logger.Info("V=" + missileVelocityVec.Length());
				ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, gyros, shipReference);
			}

			//---Store previous values 
			lastYawAngle = yawAngle;
			lastPitchAngle = pitchAngle;
			lastRollAngle = rollAngle;

		}

		Vector3D CalculateHeadingVector(Vector3D targetVec, Vector3D velocityVec, bool driftComp)
		{
			if (!driftComp)
			{
				return targetVec;
			}

			if (velocityVec.LengthSquared() < 100)
			{
				return targetVec;
			}

			if (targetVec.Dot(velocityVec) > 0)
			{
				return VectorReflection(velocityVec, targetVec);
			}
			else
			{
				return -velocityVec;
			}
		}

		Vector3D VectorReflection(Vector3D a, Vector3D b) //reflect a over b    
		{
			Vector3D project_a = VectorProjection(a, b);
			Vector3D reject_a = a - project_a;
			return project_a - reject_a;
		}

		/*
		/// Whip's Get Rotation Angles Method v9 - 12/1/17 ///
		* Fix to solve for zero cases when a vertical target vector is input
		* Fixed straight up case
		*/
		void GetRotationAngles(Vector3D v_target, Vector3D v_front, Vector3D v_left, Vector3D v_up, out double yaw, out double pitch)
		{
			//Dependencies: VectorProjection() | VectorAngleBetween()
			var projectTargetUp = VectorProjection(v_target, v_up);
			var projTargetFrontLeft = v_target - projectTargetUp;

			yaw = VectorAngleBetween(v_front, projTargetFrontLeft);

			if (Vector3D.IsZero(projTargetFrontLeft) && !Vector3D.IsZero(projectTargetUp)) //check for straight up case
			{
				pitch = MathHelper.PiOver2;
			}
			else
			{
				pitch = VectorAngleBetween(v_target, projTargetFrontLeft); //pitch should not exceed 90 degrees by nature of this definition
			}

			//---Check if yaw angle is left or right  
			//multiplied by -1 to convert from right hand rule to left hand rule
			yaw = -1 * Math.Sign(v_left.Dot(v_target)) * yaw;

			//---Check if pitch angle is up or down    
			pitch = Math.Sign(v_up.Dot(v_target)) * pitch;

			//---Check if target vector is pointing opposite the front vector
			if (Math.Abs(yaw) <= 1E-6 && v_target.Dot(v_front) < 0)
			{
				yaw = Math.PI;
			}
		}

		Vector3D VectorProjection(Vector3D a, Vector3D b)
		{
			if (Vector3D.IsZero(b))
			{
				return Vector3D.Zero;
			}

			return a.Dot(b) / b.LengthSquared() * b;
		}

		double VectorAngleBetween(Vector3D a, Vector3D b) //returns radians 
		{
			if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
			{
				return 0;
			}
			else
			{
				return Math.Acos(MathHelper.Clamp(a.Dot(b) / Math.Sqrt(a.LengthSquared() * b.LengthSquared()), -1, 1));
			}
		}

		//Whip's ApplyGyroOverride Method v9 - 8/19/17
		void ApplyGyroOverride(double pitch_speed, double yaw_speed, double roll_speed, List<IMyGyro> gyro_list, IMyTerminalBlock reference)
		{
			var rotationVec = new Vector3D(-pitch_speed, yaw_speed, roll_speed); //because keen does some weird stuff with signs 
			var shipMatrix = reference.WorldMatrix;
			var relativeRotationVec = Vector3D.TransformNormal(rotationVec, shipMatrix);

			foreach (var thisGyro in gyro_list)
			{
				var gyroMatrix = thisGyro.WorldMatrix;
				var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gyroMatrix));

				thisGyro.Pitch = (float)transformedRotationVec.X;
				thisGyro.Yaw = (float)transformedRotationVec.Y;
				thisGyro.Roll = (float)transformedRotationVec.Z;
				thisGyro.GyroOverride = pitch_speed != 0 || yaw_speed != 0 || roll_speed != 0;
			}
		}
	}
}
