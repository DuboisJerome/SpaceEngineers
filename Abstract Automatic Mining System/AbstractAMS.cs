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
		public abstract class AbstractAMS<P> where P : AbstractAMSParams
		{
			// arg
			protected const string ARG_START = "Start";
			protected const string ARG_NEXT_STEP = "Next";
			protected const string ARG_STOP = "Stop";
			protected const string ARG_RESET = "Reset";

			protected const string PHASE_NONE = "PHASE_NONE";
			protected const string PHASE_LAUNCH = "PHASE_LAUNCH";
			protected const string PHASE_ENDING = "PHASE_ENDING";

			protected Logger logger;
			protected bool isRunning;
			protected AMSPhase currentPhase;
			protected AMSPhase phaseNone;
			protected AMSPhase phaseLaunch;
			protected AMSPhase phaseEnding;

			protected List<AMSPhase> listPhase = new List<AMSPhase>();

			public AbstractAMS(P parameters)
			{
				this.Params = parameters;
				logger = new Logger(GetProgram());
				this.isRunning = false;
				phaseNone = AddPhase(PHASE_NONE, RunPhaseNone);
				phaseLaunch = AddPhase(PHASE_LAUNCH, RunPhaseLaunch);
				phaseEnding = AddPhase(PHASE_ENDING, RunPhaseEnd);
				this.currentPhase = phaseNone;
			}
			
			protected AMSPhase AddPhase(string name, AMSPhase.AMSPhaseRun fcntRun)
			{
				AMSPhase phase = new AMSPhase(name, fcntRun);
				this.listPhase.Add(phase);
				return phase;
			}

			protected AMSPhase GetPhase(string name)
			{
				return listPhase.Find(p => p.Name == name);
			}

			protected bool IsPhase(string name)
			{
				return currentPhase.Name == name;
			}

			public P Params { get; }
			/** Go to start position
			 * @return is position reset 
			 */
			protected abstract bool ResetPosition();
			protected virtual void OnEnd()
			{

			}
			protected bool RunPhaseNone()
			{
				isRunning = false;
				return false;
			}

			protected bool RunPhaseLaunch()
			{
				return ResetPosition();
			}

			protected bool RunPhaseEnd()
			{
				bool isPhaseEnded = ResetPosition();
				if (isPhaseEnded)
				{
					OnEnd();
				}
				return isPhaseEnded;
			}

			public void Run(string arg, UpdateType updateSource)
			{
				if (arg == ARG_START)
				{
					this.Start();
				}
				else if (arg == ARG_STOP)
				{
					this.Stop();
				}
				else if (arg == ARG_RESET)
				{
					this.Reset();
				}
				else if (arg == ARG_NEXT_STEP)
				{
					// Force next step
					this.NextStep();
				}
				else if ((updateSource & (UpdateType.Update100)) != 0 && isRunning)
				{
					this.Update();
				}
			}

			/** Load blocks for A.M.S */
			protected abstract void LoadBlocks();
			/** Manual Commande Start */
			protected virtual void Start()
			{
				logger.Info("==> Start");
				isRunning = true;
				this.LoadBlocks();
				// If off, start from the beginning = launch
				// else continue the currentPhase
				if (currentPhase.Name == PHASE_NONE)
				{
					currentPhase = phaseLaunch;
				}
			}
			/** Manual Commande Stop */
			protected virtual void Stop()
			{
				logger.Info("==> Stop");
				isRunning = false;
				this.LoadBlocks();
			}
			/** Manual  Commande Reset */
			protected virtual void Reset()
			{
				logger.Info("==> Reset");
				isRunning = true;
				currentPhase = phaseEnding;
				this.logger.Clear();
				this.LoadBlocks();
			}

			protected virtual void NextStep()
			{
				logger.Info("==> Force Next Step");
				currentPhase = currentPhase.NextPhase;
				if (currentPhase == null)
				{
					currentPhase = phaseNone;
				}
			}

			/** Run every ticks */
			protected void Update()
			{
				logger.Info(currentPhase.Name);
				if (IsFull())
				{
					AlertFull();
					return;
				}
				bool isPhaseEnded = currentPhase.Run();
				if (isPhaseEnded)
				{
					NextStep();
				}
			}

			protected virtual bool IsFull()
			{
				return false;
			}

			protected virtual void AlertFull()
			{
				logger.Warn("Alert cargo full");
				Stop();
			}

			public Program GetProgram()
			{
				return this.Params.Program;
			}
		}
	}
}
