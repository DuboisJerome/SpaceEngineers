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
		public abstract class AbstractSEClass
		{
			protected readonly MyGridProgram p;
			protected readonly Logger LOGGER;
            protected readonly ConfigUtils config;

            public AbstractSEClass(MyGridProgram p)
			{
				this.p = p;
                this.LOGGER = new Logger(p.Me.GetSurface(0));
                this.config = new ConfigUtils(p.Me);
                BuildConfig();
			}

			protected abstract bool LoadBlocks();

            protected virtual void BuildConfig() { }

            protected double GetShipFarthestEdgeDistance(IMyShipController reference)
			{
                MatrixD m = reference.WorldMatrix;
                double d = GetEdgeDistance(reference, m.Forward);
                d = Math.Max(d, GetEdgeDistance(reference, m.Backward));
                d = Math.Max(d, GetEdgeDistance(reference, m.Left));
                d = Math.Max(d, GetEdgeDistance(reference, m.Right));
                d = Math.Max(d, GetEdgeDistance(reference, m.Up));
                return Math.Max(d, GetEdgeDistance(reference, m.Down));
            }

            private double GetEdgeDistance(IMyShipController reference, Vector3D direction)
			{
                Vector3D edgeDirection = GetShipEdgeVector(reference, direction);
                Vector3D edgePos = reference.GetPosition() + edgeDirection;
                return Vector3D.Distance(reference.CenterOfMass, edgePos);
            }

            protected Vector3D GetShipEdgeVector(IMyTerminalBlock reference, Vector3D direction)
            {
                //get grid relative max and min
                Vector3I gridMinimum = reference.CubeGrid.Min;
                Vector3I gridMaximum = reference.CubeGrid.Max;

                //get dimension of grid cubes
                float gridSize = reference.CubeGrid.GridSize;

                //get worldmatrix for the grid
                MatrixD gridMatrix = reference.CubeGrid.WorldMatrix;

                //convert grid coordinates to world coords
                Vector3D worldMinimum = Vector3D.Transform(gridMinimum * gridSize, gridMatrix);
                Vector3D worldMaximum = Vector3D.Transform(gridMaximum * gridSize, gridMatrix);

                //get reference position
                Vector3D origin = reference.GetPosition();

                //compute max and min relative vectors
                Vector3D minRelative = worldMinimum - origin;
                Vector3D maxRelative = worldMaximum - origin;

                //project relative vectors on desired direction
                Vector3D minProjected = Vector3D.Dot(minRelative, direction) / direction.LengthSquared() * direction;
                Vector3D maxProjected = Vector3D.Dot(maxRelative, direction) / direction.LengthSquared() * direction;

                //check direction of the projections to determine which is correct
                if (Vector3D.Dot(minProjected, direction) > 0)
                    return minProjected;
                else
                    return maxProjected;
            }

            //Whip's Get Closest Block of Type Method variant 2 - 5/26/17
            //Added optional ignore name variable
            protected T GetClosestBlockOfType<T>(string name = "", string ignoreName = "") where T : class, IMyTerminalBlock
            {
                var allBlocks = new List<T>();

                if (name == "")
                {
                    if (ignoreName == "")
                        p.GridTerminalSystem.GetBlocksOfType(allBlocks);
                    else
                        p.GridTerminalSystem.GetBlocksOfType(allBlocks, block => !block.CustomName.ToLower().Contains(ignoreName.ToLower()));
                }
                else
                {
                    if (ignoreName == "")
                        p.GridTerminalSystem.GetBlocksOfType(allBlocks, block => block.CustomName.ToLower().Contains(name.ToLower()));
                    else
                        p.GridTerminalSystem.GetBlocksOfType(allBlocks, block => block.CustomName.ToLower().Contains(name.ToLower()) && !block.CustomName.ToLower().Contains(ignoreName.ToLower()));
                }

                if (allBlocks.Count == 0)
                {
                    return null;
                }

                var closestBlock = allBlocks[0];
                var shortestDistance = Vector3D.DistanceSquared(p.Me.GetPosition(), closestBlock.GetPosition());
                allBlocks.Remove(closestBlock); //remove this block from the list

                foreach (T thisBlock in allBlocks)
                {
                    var thisDistance = Vector3D.DistanceSquared(p.Me.GetPosition(), thisBlock.GetPosition());

                    if (thisDistance < shortestDistance)
                    {
                        closestBlock = thisBlock;
                        shortestDistance = thisDistance;
                    }
                    //otherwise move to next one
                }

                return closestBlock;
            }

            protected void Debug(string name, Vector3D val)
			{
                LOGGER.Debug(name + " = " + (val == null ? "NULL VAL" : "Vecteur(O,("+val.X+","+val.Y+","+val.Z+"))"));
            }
            protected void Debug<T>(string name, T val)
			{
                LOGGER.Debug(name + " = " + (val == null ? "NULL VAL" : val.ToString()));
			}
        }
    }
}
