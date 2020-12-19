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
			protected const string PHASE_END = "PHASE_END";

			protected Logger logger;
			protected bool isRunning;
			private AMSPhase currentPhase;
			private AMSPhase phaseNone;
			private AMSPhase phaseLaunch;
			private AMSPhase phaseEnding;
			private AMSPhase phaseEnd;

			protected List<AMSPhase> listPhase = new List<AMSPhase>();

			public AbstractAMS(P parameters)
			{
				this.Params = parameters;
				logger = new Logger(GetProgram());
				this.isRunning = false;
				phaseNone = AddPhase(PHASE_NONE, RunPhaseNone);
				phaseLaunch = AddPhase(PHASE_LAUNCH, ResetPosition, BeforeFirstRunResetPosition);
				phaseEnding = AddPhase(PHASE_ENDING, ResetPosition, BeforeFirstRunResetPosition);
				phaseEnd = AddPhase(PHASE_END, RunPhaseEnd);
				phaseEnding.NextPhase = phaseEnd;
				this.currentPhase = phaseNone;
			}

			protected AMSPhase AddPhase(string name, Func<bool> fcntRun,
				Func<bool> beforeFirstRun = null)
			{
				AMSPhase phase = new AMSPhase(name, fcntRun)
				{
					BeforeFirstRun = beforeFirstRun
				};
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
			protected virtual bool BeforeFirstRunResetPosition()
			{
				return false;
			}
			protected abstract bool ResetPosition();
			protected bool RunPhaseNone()
			{
				isRunning = false;
				return false;
			}

			protected virtual void OnEnd()
			{
			}

			protected bool RunPhaseEnd()
			{
				OnEnd();
				return true;
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
				bool wasRunning = isRunning;
				isRunning = true;
				this.LoadBlocks();
				// If off, start from the beginning = launch
				// else continue the currentPhase
				if (currentPhase.Name == PHASE_NONE)
				{
					currentPhase = phaseLaunch;
				}
				if (!wasRunning)
				{
					currentPhase.Init();
				}
			}
			/** Manual Commande Stop */
			protected virtual void Stop()
			{
				logger.Info("==> Stop");
				isRunning = false;
				this.LoadBlocks();
			}
			/** Manual Commande Reset */
			protected virtual void Reset()
			{
				logger.Info("==> Reset");
				isRunning = true;
				ToPhaseEnd();
				this.logger.Clear();
				this.LoadBlocks();
			}

			protected virtual void NextStep()
			{
				ChangePhase(currentPhase.NextPhase);
			}

			protected void AfterLaunch(AMSPhase phase)
			{
				this.phaseLaunch.NextPhase = phase;
			}

			protected void ToPhaseEnd()
			{
				ChangePhase(phaseEnding);
			}

			protected void ChangePhase(AMSPhase newPhase)
			{
				currentPhase = newPhase;
				if (currentPhase == null)
				{
					currentPhase = phaseNone;
				}
				currentPhase.Init();
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
					logger.Debug("End: " + currentPhase.Name);
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
