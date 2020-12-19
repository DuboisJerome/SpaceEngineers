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
		public class DelimitedString
		{
			private DelimitedString() { }
			public static string[] ReadDelimited(string s, char sep)
			{
				return s.Split(sep);
			}
			public static T[] ReadDelimited<T>(string s, char sep, Func<string, T> converter)
			{
				string[] arr = ReadDelimited(s, sep);
				return arr.Select(s2 => converter(s2)).ToArray();
			}

			public static string WriteDelimited<T>(T[] list, char sep, Func<T,string> converter)
			{
				return String.Join(sep.ToString(),
					list.Select(t => converter(t)));
			}
			public static string WriteDelimited<T>(T[] list, char sep)
			{
				return String.Join(sep.ToString(),
					list.Select(t => t.ToString()));
			}
		}
	}
}
