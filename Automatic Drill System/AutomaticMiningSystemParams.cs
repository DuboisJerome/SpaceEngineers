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
		public class AutomaticMiningSystemParams : AbstractAMSParams
		{

			public AutomaticMiningSystemParams(Program program, string name = "A.M.S") : base(program, name)
			{
			}
			

			// beacon range during idle/working
			public float MinBeaconRadius { get; set; } = 200;

			// beacon range when work is finnish or alert
			public float MaxBeaconRadius { get; set; } = 50000;

			// Velocity of pistons when mining
			public float SlowPistonVelocity { get; set; } = 0.25F;

			// Velocity of pistons when not mining
			public float FastPistonVelocity { get; set; } = 0.6F;

			// Velocity of rotor
			public float RotorVelocity { get; set; } = 1;

			// Grp name of pistons in the mining direction
			public string GrpNamePistonMDir => Name + " Pistons MDir";

			// Name of pistons in mining opposite direction
			public string GrpNamePistonMODir => Name + " Pistons !MDir";

			// Grp name of lights
			public string GrpNameLight => Name + " Lights";

			// Grp name of cargos
			public string GrpNameCargo => Name + " Cargos";

			// Rotor name
			public string MainRotorName => Name + " Rotor";

			// Min angle of rotor where A.M.S start mining
			public float MinRotorAngle { get; set; } = 0;

			// Max angle of rotor where A.M.S stop mining
			public float MaxRotorAngle { get; set; } = 360;

			// Angle between 2 mining phases
			public float StepRotorAngle { get; set; } = 20;

			public void SetProperty(string propertyName, string propertyValue)
			{
				bool isOk = true;
				if (propertyName.Equals("MinBeaconRadius", StringComparison.OrdinalIgnoreCase))
				{
					float newValue = MinBeaconRadius;
					isOk = float.TryParse(propertyValue, out newValue);
					if (isOk) MinBeaconRadius = newValue;
				}
				if (propertyName.Equals("MaxBeaconRadius", StringComparison.OrdinalIgnoreCase))
				{
					float newValue = MaxBeaconRadius;
					isOk = float.TryParse(propertyValue, out newValue);
					if (isOk) MaxBeaconRadius = newValue;
				}
				if (propertyName.Equals("SlowPistonVelocity", StringComparison.OrdinalIgnoreCase))
				{
					float newValue = SlowPistonVelocity;
					isOk = float.TryParse(propertyValue, out newValue);
					if (isOk) SlowPistonVelocity = newValue;
				}
				if (propertyName.Equals("FastPistonVelocity", StringComparison.OrdinalIgnoreCase))
				{
					float newValue = FastPistonVelocity;
					isOk = float.TryParse(propertyValue, out newValue);
					if (isOk) FastPistonVelocity = newValue;
				}
				if (propertyName.Equals("RotorVelocity", StringComparison.OrdinalIgnoreCase))
				{
					float newValue = RotorVelocity;
					isOk = float.TryParse(propertyValue, out newValue);
					if (isOk) RotorVelocity = newValue;
				}
				if (propertyName.Equals("MinRotorAngle", StringComparison.OrdinalIgnoreCase))
				{
					float newValue = MinRotorAngle;
					isOk = float.TryParse(propertyValue, out newValue);
					if (isOk) MinRotorAngle = newValue;
				}
				if (propertyName.Equals("MaxRotorAngle", StringComparison.OrdinalIgnoreCase))
				{
					float newValue = MaxRotorAngle;
					isOk = float.TryParse(propertyValue, out newValue);
					if (isOk) MaxRotorAngle = newValue;
				}
				if (propertyName.Equals("StepRotorAngle", StringComparison.OrdinalIgnoreCase))
				{
					float newValue = StepRotorAngle;
					isOk = float.TryParse(propertyValue, out newValue);
					if (isOk) StepRotorAngle = newValue;
				}
				if (!isOk)
				{
					Program.Echo($"Error custom param name {propertyName}");
				}
			}
		}
	}
}
