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
		public class ConfigUtils
        {
            private IMyTerminalBlock refBlock;
            private Dictionary<string, string> configDict = new Dictionary<string, string>();

            public ConfigUtils(IMyTerminalBlock block)
			{
                this.refBlock = block;
            }

            public void Clear()
			{
                configDict.Clear();
			}

            public void Add(string name, string value)
			{
                configDict.Add(name, value);
			}

            public void LoadConfigFromCustomData()
			{
                string customData = refBlock.CustomData;
                var lines = customData.Split('\n');

                foreach (var thisLine in lines)
                {
                    var words = thisLine.Split('=');
                    if (words.Length == 2)
                    {
                        var variableName = words[0].Trim();
                        var variableValue = words[1].Trim();
                        configDict[variableName] = variableValue;
                    }
                }
            }

            public void WriteConfig()
            {
                StringBuilder configSB = new StringBuilder();
                configSB.Clear();
                foreach (var keyValue in configDict)
                {
                    configSB.AppendLine($"{keyValue.Key} = {keyValue.Value}");
                }

                refBlock.CustomData = configSB.ToString();
            }

            public void GetVariable(string name, ref bool variableToUpdate)
            {
                string valueStr;
                if (configDict.TryGetValue(name, out valueStr))
                {
                    bool thisValue;
                    if (bool.TryParse(valueStr, out thisValue))
                    {
                        variableToUpdate = thisValue;
                    }
                }
            }

            public void GetVariable(string name, ref int variableToUpdate)
            {
                string valueStr;
                if (configDict.TryGetValue(name, out valueStr))
                {
                    int thisValue;
                    if (int.TryParse(valueStr, out thisValue))
                    {
                        variableToUpdate = thisValue;
                    }
                }
            }

            public void GetVariable(string name, ref float variableToUpdate)
            {
                string valueStr;
                if (configDict.TryGetValue(name, out valueStr))
                {
                    float thisValue;
                    if (float.TryParse(valueStr, out thisValue))
                    {
                        variableToUpdate = thisValue;
                    }
                }
            }

            public void GetVariable(string name, ref double variableToUpdate)
            {
                string valueStr;
                if (configDict.TryGetValue(name, out valueStr))
                {
                    double thisValue;
                    if (double.TryParse(valueStr, out thisValue))
                    {
                        variableToUpdate = thisValue;
                    }
                }
            }

            public void GetVariable(string name, ref string variableToUpdate)
            {
                string valueStr;
                if (configDict.TryGetValue(name, out valueStr))
                {
                    variableToUpdate = valueStr;
                }
            }
        }
	}
}
