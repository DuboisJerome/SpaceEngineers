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
		public class Logger
		{
			public enum Level : ushort
			{
				DEBUG=0, INFO=1, WARN=2, ERROR =3
			}

			private readonly IMyTextSurface _surface;

			private readonly MyGridProgram _p;
			private string lastLine;
			private List<string> _sb = new List<string>();
			public Level MinLvl { get; set; } = Level.DEBUG;

			public Logger(MyGridProgram p)
			{
				this._p = p;
				this._surface = p.Me.GetSurface(0);
				if (this._surface == null)
				{
					p.Echo("No Logger found");
					return;
				}
				else
				{
					p.Echo("Logger surface found");
				}
				this._surface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
			}

			public Logger(IMyTextSurface s)
			{
				this._surface = s;
				this._surface.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
			}

			public void Debug(object o)
			{
				Log(Level.DEBUG, o);
			}

			public void Info(object o)
			{
				Log(Level.INFO, o);
			}

			public void Warn(object o)
			{
				Log(Level.WARN, o);
			}

			public void Error(object o)
			{
				Log(Level.ERROR, o);
			}

			public void Log(Level level, object o)
			{
				if (this._surface == null || (ushort)MinLvl > (ushort)level)
					return;
				string line = "[" + level.ToString()[0] + "] - "+DateTime.UtcNow.ToString("HH:mm:ss.fff")+" : " + o;
				if (line == lastLine)
					return;
				_sb.Insert(0,line);
				lastLine = line;
				int N = 2000;
				if(_sb.Count > N)
				{
					_sb.RemoveRange(N, _sb.Count-N);
				}
				this._surface.WriteText(String.Join(Environment.NewLine, _sb));
				if(_p != null)
				{
					_p.Echo(_sb.ToString());
				}
			}

			public void Clear()
			{
				if (this._surface == null)
					return;
				this._surface.WriteText(string.Empty);
				lastLine = string.Empty;
				_sb.Clear();
			}
		}
	}
}
