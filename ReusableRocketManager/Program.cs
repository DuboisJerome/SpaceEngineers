using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript
{
	partial class Program : MyGridProgram
	{
		// No touchey below
		IMyRemoteControl shipReference = null;
		
		bool firstGuidance = true;
		Logger logger;

		double lastYawAngle = 0;
		double lastPitchAngle = 0;
		double lastRollAngle = 0;

		List<IMyThrust> thrust = new List<IMyThrust>();
		List<IMyThrust> forwardThrust = new List<IMyThrust>();
		List<IMyThrust> otherThrust = new List<IMyThrust>();
		List<IMyGyro> gyros = new List<IMyGyro>();
		List<IMyRemoteControl> remotes = new List<IMyRemoteControl>();

		public void Main(string arg)
		{
			Runtime.UpdateFrequency = UpdateFrequency.Update10;
			logger = new Logger(Me.GetSurface(0));
			bool isSetup = GetBlocks();

			if (isSetup)
			{
				GuideShip();
			}
		}

		private bool GetBlocks()
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

		private void GetThrusterOrientation(IMyRemoteControl refBlock, List<IMyThrust> thrusters, out List<IMyThrust> _forwardThrust, out List<IMyThrust> _otherThrust)
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

		private void GuideShip()
		{
			Vector3D shipPos = shipReference.GetPosition();

			//---Check if we have gravity 
			double rollAngle = 0; double rollSpeed = 0;

			var remote = remotes[0] as IMyRemoteControl;

			//---Get travel vector 
			var shipVelocityVec = shipReference.GetShipVelocities().LinearVelocity;

			//---Find vector from ship to destinationVec   
			Vector3D shipToTargetVec;
			if (Vector3D.IsZero(shipReference.GetTotalGravity()))
			{
				logger.Debug("No gravity");
				// Align to speed direction
				shipToTargetVec = Vector3D.Negate(shipVelocityVec);
			}
			else
			{
				logger.Debug("Gravity");
				// align to center of planet
				Vector3D targetPos = new Vector3D();
				shipReference.TryGetPlanetPosition(out targetPos);
				shipToTargetVec = Vector3D.Negate(targetPos - shipPos);// targetPos - shipPos;
			}

			//---Calc our new heading based upon our travel vector    
			var headingVec = CalculateHeadingVector(shipToTargetVec, shipVelocityVec);

			//---Get pitch and yaw angles 
			double yawAngle; double pitchAngle;
			GetRotationAngles(headingVec, shipReference, out yawAngle, out pitchAngle);

			logger.Info("mttv: " + shipToTargetVec);
			logger.Info("mvv: " + shipVelocityVec);
			logger.Info("hv: " + headingVec);
			if (firstGuidance)
			{
				lastPitchAngle = pitchAngle;
				lastYawAngle = yawAngle;
				firstGuidance = false;
			}

			//---Angle controller
			logger.Debug(yawAngle + "|" + lastYawAngle);
			double yawSpeed = Math.Round(yawAngle + (yawAngle - lastYawAngle) * 60, 3);
			double pitchSpeed = Math.Round(pitchAngle + (pitchAngle - lastPitchAngle) * 60, 3);

			//---Set appropriate gyro override 
			double speed = shipReference.GetShipSpeed();
			if (speed <= 1E-2)
			{
				logger.Info("No more speed");
				ApplyGyroOverride(0, 0, 0, gyros, shipReference);
				Runtime.UpdateFrequency = UpdateFrequency.None;
			}
			else
			{
				logger.Info("V=" + shipVelocityVec.Length());
				ApplyGyroOverride(pitchSpeed, yawSpeed, rollSpeed, gyros, shipReference);
			}

			//---Store previous values 
			lastYawAngle = yawAngle;
			lastPitchAngle = pitchAngle;
			lastRollAngle = rollAngle;

		}

		private Vector3D CalculateHeadingVector(Vector3D targetVec, Vector3D velocityVec)
		{
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

		//reflect a over b
		private Vector3D VectorReflection(Vector3D a, Vector3D b)     
		{
			Vector3D project_a = VectorProjection(a, b);
			Vector3D reject_a = a - project_a;
			return project_a - reject_a;
		}

		private void GetRotationAngles(Vector3D v_target, IMyRemoteControl shipReference, out double yaw, out double pitch)
		{
			//---Get orientation vectors of our ship 
			Vector3D v_front = shipReference.WorldMatrix.Forward;
			Vector3D v_left = shipReference.WorldMatrix.Left;
			Vector3D v_up = shipReference.WorldMatrix.Up;

			// Dependencies: VectorProjection() | VectorAngleBetween()
			var projectTargetUp = VectorProjection(v_target, v_up);
			var projTargetFrontLeft = v_target - projectTargetUp;

			yaw = VectorAngleBetween(v_front, projTargetFrontLeft);

			//check for straight up case
			if (Vector3D.IsZero(projTargetFrontLeft) && !Vector3D.IsZero(projectTargetUp)) 
			{
				pitch = MathHelper.PiOver2;
			}
			else
			{
				//pitch should not exceed 90 degrees by nature of this definition
				pitch = VectorAngleBetween(v_target, projTargetFrontLeft); 
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

		private Vector3D VectorProjection(Vector3D a, Vector3D b)
		{
			if (Vector3D.IsZero(b))
			{
				return Vector3D.Zero;
			}
			return a.Dot(b) / b.LengthSquared() * b;
		}

		private double VectorAngleBetween(Vector3D a, Vector3D b) //returns radians 
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
		
		private void ApplyGyroOverride(double pitch_speed, double yaw_speed, double roll_speed, List<IMyGyro> gyro_list, IMyTerminalBlock reference)
		{
			//because keen does some weird stuff with signs 
			var rotationVec = new Vector3D(-pitch_speed, yaw_speed, roll_speed); 
			var relativeRotationVec = Vector3D.TransformNormal(rotationVec, reference.WorldMatrix);

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
