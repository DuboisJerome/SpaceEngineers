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
		public class ShipFormationSlave : AbstractSlaveSystem
		{
			public ShipFormationSlave(MyGridProgram p) : base(p) {
			}

			public override void Run()
			{
				if (!HasMaster())
				{
					GetLight().SetValue("Color", new Color(255, 0, 0));
				} else
				{
					GetLight().SetValue("Color", new Color(0, 0, 255));
				}
				base.Run();
			}

			public override void DoOnDirectMasterMsg(MyIGCMessage msg)
			{
				p.Echo("Getting msg from master");
				if(msg.Tag == ShipFormationMaster.TAG_FORMATION)
				{
					GPS center = new GPS(msg.Data.ToString());
					p.Echo("Getting formation msg from master : ");

					IMyRemoteControl r = GetRemote();

					r.ClearWaypoints();
					
					int nbWP = 18;
					double angleTmp = 0;
					double angleDeg = 360 / nbWP;
					for(int i =0; i < nbWP; ++i)
					{
						GPS wp = center.ComputeCoordinate(ShipFormationMaster.DISTANCE, DegreeToRadian(angleTmp));
						MyWaypointInfo dest = new MyWaypointInfo("WP_"+i, wp.x, wp.y, wp.z);
						r.AddWaypoint(dest);
						angleTmp += angleDeg;
					}
					
					GetLight().SetValue("Color", new Color(0, 255, 0));

					r.FlightMode = FlightMode.Circle;
					r.SetCollisionAvoidance(false);
					r.SetAutoPilotEnabled(true);
				}
			}

			private double DegreeToRadian(double angle)
			{
				return Math.PI * angle / 180.0;
			}

			private IMyLightingBlock GetLight()
			{
				return (IMyLightingBlock) p.GridTerminalSystem.GetBlockWithName("L");
			}

			private IMyRemoteControl GetRemote()
			{
				return (IMyRemoteControl) p.GridTerminalSystem.GetBlockWithName("D");
			}

			private void CalcCoord(Vector3D centre, Vector3D pointDepart, double angleRad)
			{
				//
				Vector3D OA = pointDepart - centre;
				double rGrandAxe = OA.Length();

				// ZX
				Vector2D B = new Vector2D(pointDepart.Z, pointDepart.X);
				Vector2D centre2D = new Vector2D(centre.Z, centre.X);
				Vector2D centreToB = B - centre2D;
				double rPetitAxe = centreToB.Length();
				double teta = Math.Acos(rPetitAxe / rGrandAxe);
				Vector2D zx = Ellipse(centre2D, rGrandAxe, rPetitAxe, angleRad, teta);
				double z = zx.X;
				double x = zx.Y;
				
				// teta valable que pour ZX

			}

			private Vector2D Ellipse(Vector2D centre, double rGrandAxe, double rPetitAxe, double angleRad, double angleGrandAxe)
			{
				double x = centre.X + rGrandAxe * Math.Cos(angleGrandAxe) * Math.Cos(angleRad) - rPetitAxe * Math.Sin(angleGrandAxe) * Math.Sin(angleRad);
				double y = centre.Y + rGrandAxe * Math.Sin(angleGrandAxe) * Math.Cos(angleRad) + rPetitAxe * Math.Cos(angleGrandAxe) * Math.Sin(angleRad);
				return new Vector2D(x,y);
			}

			private Vector2D EllipsePetitAxe(Vector2D centre, double rGrandAxe, double rPetitAxe, double angleRad, double anglePetitAxe)
			{
				double x = centre.X + rGrandAxe * (-Math.Sin(anglePetitAxe)) * Math.Cos(angleRad) - rPetitAxe * Math.Cos(anglePetitAxe) * Math.Sin(angleRad);
				double y = centre.Y + rGrandAxe * Math.Cos(anglePetitAxe) * Math.Cos(angleRad) + rPetitAxe * (-Math.Sin(anglePetitAxe)) * Math.Sin(angleRad);
				return new Vector2D(x, y);
			}
		}
	}
}
