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
	partial class Program : MyGridProgram
	{

		public void Main(string argument, UpdateType updateSource)
		{
			List<Vector3D> lst = getListWaypointMinimalOrbit(new Vector3D(2,2,4), new Vector3D(1,3,1), Vector3D.UnitY, 6);
			Me.CustomData = "";
			int i = 0;
			foreach (Vector3D v in lst)
			{
				Me.CustomData += "v" + i + " = Point({" + v.X + "," + v.Y + "," + v.Z + "})" + "\n";
				i++;
			}
		}

		/** 
		 * @return list of waypoint nearest from equator going througth define point which is min/max latitude
		 */
		private List<Vector3D> getListWaypointMinimalOrbit(Vector3D O, Vector3D A, Vector3D rotationAxis, int nbPoint)
		{
			List<Vector3D> lst = new List<Vector3D>();
			Vector3D vOA = A - O;
			double r = vOA.Length();
			double deltaAngle = 360 / nbPoint;
			double angle = 0;
			for (int i= 0; i < nbPoint; ++i)
			{
				double angleRad = DegreeToRadian(angle);
				lst.Add(new Vector3D(r * Math.Cos(angleRad), r * Math.Sin(angleRad), 0));
				angle += deltaAngle;
			}
			return getListWaypointMinimalOrbit(O,A, rotationAxis, lst);
		}

		private double DegreeToRadian(double angle)
		{
			return Math.PI * angle / 180.0;
		}

		/**
		 * Given a point with local and move it from depending on O, A et rotation Axis parameter
		 * point = Point in local coordinate
		 * ex 1. point = {0, 0, 0} => output = O
		 * ex 2. point = {oaLength, 0, 0} => output = A
		 * ex 3. point = {0,oaLength,0} => output = looking from rotationAxis, angle OA OPoint = Pi/2 (90°)
		 * O = center of rotation (point in world coordinate)
		 * A = Point for min/max latitude on rotationAxis (point in world coordinate)
		 * rotationAxis = vector for rotation axis
		 */
		private List<Vector3D> getListWaypointMinimalOrbit(Vector3D O, Vector3D A, Vector3D rotationAxis, List<Vector3D> points)
		{
			Vector3D vOA = A - O;

			// Rot 1 - Rotate on Z to Align X axis with OA vector
			Vector3D vAonXY = new Vector3D(A.X - O.X, A.Y - O.Y, 0);
			double signeAngle1 = Math.Sign(Vector3D.Dot(Vector3D.UnitY, vAonXY));
			double angle1 = signeAngle1 * GetAngle(Vector3D.UnitX, vAonXY);
			Vector3D axeRot1 = Vector3D.UnitZ;
			Vector3D x1 = Rotate(Vector3D.UnitX, angle1, axeRot1);
			Vector3D y1 = Rotate(Vector3D.UnitY, angle1, axeRot1);
			Vector3D z1 = Rotate(Vector3D.UnitZ, angle1, axeRot1);


			// Rot 2 - Rotate on Y1 to match X1 with OA
			double angle2 = GetAngle(Vector3D.UnitZ, vOA) - Math.PI / 2;
			Vector3D axeRot2 = y1;
			/// x2 == vOA
			Vector3D x2 = Rotate(x1, angle2, axeRot2);
			Vector3D y2 = Rotate(y1, angle2, axeRot2);
			Vector3D z2 = Rotate(z1, angle2, axeRot2);


			// Rot 3 - Rotate depending on rotation Axis given (wanted)
			Vector3D vRotFinalOnEquator = Vector3D.ProjectOnPlane(ref rotationAxis, ref vOA);
			double signeAngle3 = Math.Sign(Vector3D.Dot(-y2, vRotFinalOnEquator));
			double angle3 = signeAngle3 * GetAngle(z2, vRotFinalOnEquator);
			Vector3D axeRot3 = x2;

			List<Vector3D> output = new List<Vector3D>();
			foreach(Vector3D point in points)
			{
				Vector3D rotatedPoint = point;
				rotatedPoint = Rotate(rotatedPoint, angle1, axeRot1);
				rotatedPoint = Rotate(rotatedPoint, angle2, axeRot2);
				rotatedPoint = Rotate(rotatedPoint, angle3, axeRot3);
				rotatedPoint = Translate(rotatedPoint, O);
				output.Add(rotatedPoint);
			}
			return output;
		}

		private Vector3D Translate(Vector3D v, Vector3D translation)
		{
			return new Vector3D(v.X + translation.X, v.Y + translation.Y, v.Z + translation.Z);
		}

		private Vector3D getWaypointMinimalOrbit(Vector3D O, Vector3D A, Vector3D rotationAxis, Vector3D point)
		{
			List<Vector3D> lst = new List<Vector3D>();
			lst.Add(point); 
			lst = getListWaypointMinimalOrbit(O, A, rotationAxis, lst);
			return lst[0];
		}

		/** Return angle in radian between 2 vectors */
		private double GetAngle(Vector3D a, Vector3D b)
		{
			if (Vector3D.IsZero(a) || Vector3D.IsZero(b))
			{
				return 0;
			}
			else
			{
				return Math.Acos(MathHelper.Clamp(a.Dot(b) / (a.Length() * b.Length()), -1, 1));
			}
		}

		private Vector3D Rotate(Vector3D v, double angle, Vector3D rotAxis)
		{
			if(v == rotAxis)
			{
				return v;
			}
			Vector3D result = new Vector3D();
			MatrixD matrix = Matrix.CreateFromAxisAngle(rotAxis, (float)angle);
			Vector3D.Rotate(ref v, ref matrix, out result);
			return result;
		}
	}
}
