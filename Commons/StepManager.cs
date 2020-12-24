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
		public class StepManager
		{
			private Step FirstStep { get; set; }
			private Step CurrentStep { get; set; }
			private bool FirstCallCurrentStep { get; set; } = true;

			public void Init(params Step[] arrOrderedStep)
			{
				if (arrOrderedStep == null || arrOrderedStep.Length == 0)
					return;

				Init(arrOrderedStep[0]);
				if (arrOrderedStep.Length > 1)
				{
					Step lastStep = FirstStep;
					foreach (Step r in arrOrderedStep.Skip(1))
					{
						if (lastStep != null)
							lastStep.NextStep = r;
						lastStep = r;
					}
				}				
			}

			public void Init(Step s)
			{
				if (s == null)
					return;
				FirstStep = s;
				SetCurrentStep(FirstStep);
			}

			public void SetCurrentStep(Step s)
			{
				FirstCallCurrentStep = true;
				CurrentStep = s;
			}

			// return true if there is no more step
			public bool Run()
			{
				return Run(new List<string>());
			}
			private bool Run(List<String> stepNameAlreadyCalled)
			{
				if (CurrentStep == null)
					return true;
				if (stepNameAlreadyCalled.Contains(CurrentStep.Name))
					return false;
				bool isStepEnd = CurrentStep.Run(FirstCallCurrentStep);
				if (isStepEnd)
				{
					// To avoid forever loop
					stepNameAlreadyCalled.Add(CurrentStep.Name);
					FirstCallCurrentStep = true;
					CurrentStep = CurrentStep.NextStep;
					if (CurrentStep != null && CurrentStep.IsImmediateRun)
						return Run(stepNameAlreadyCalled);
				}
				else
					FirstCallCurrentStep = false;
				return false;
			}
		}
		public class Step
		{
			private readonly Func<bool> RunImpl;
	
			public Step(string name, Func<bool> fcntRun)
			{
				Name = name;
				RunImpl = fcntRun;
			}

			#region Properties
			// true to start this next step immediatly after this Run if it return true
			public bool IsImmediateRun { get; set; } = false;
			// Step Name as Identifier
			public string Name { get; }
			public Step NextStep { get; set; }
			// If this func is set, it's called the first time the Run method is called
			// Called only one per phase, usefull to do a check before run
			public Func<bool> BeforeFirstRun { get; set; }
			#endregion

			public bool Run(bool isFirstCall)
			{
				bool isEnd = false;
				if (isFirstCall)
				{
					if (BeforeFirstRun != null)
						isEnd = BeforeFirstRun();
				}
				if (!isEnd && RunImpl != null)
					isEnd = RunImpl();
				return isEnd;
			}
		}
	}
}
