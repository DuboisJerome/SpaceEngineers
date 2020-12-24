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
