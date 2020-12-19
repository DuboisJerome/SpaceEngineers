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
	partial class Program : MyGridProgram
	{
		public class MemberInfo : Comparer<MemberInfo>
		{ 
			private const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss.fff";

			private readonly Logger LOGGER;
			public MemberInfo(Logger l, long id)
			{
				this.LOGGER = l;
				this.Id = id;
			}

			public long Id { get; }
			public GPS Pos { get; private set; } = new GPS(0, 0, 0);
			public Vector3D Speed { get; private set; } = new Vector3D(0, 0, 0);
			public DateTime Date { get; private set; } = DateTime.UtcNow;
			public GPS NextPos { get; private set; } = new GPS(0, 0, 0);
			public int AntennaRange { get; private set; } = 0;
			public double MillisecondsSinceSend()
			{
				return (DateTime.UtcNow - Date).TotalMilliseconds;
			}
			public double CalculDistance(MemberInfo me)
			{
				return me == this ? 0 : this.ComputeNextPos().Distance(me.ComputeNextPos());
			}
			public bool UpdateFromData(string data)
			{
				string[] dataArray = data.Split('|');
				return Update(new GPS(dataArray[0]),
					DateTime.ParseExact(dataArray[1], DATE_FORMAT, System.Globalization.CultureInfo.InvariantCulture),
					ReadSpeed(dataArray[2]),
					int.Parse(dataArray[3]));
			}
			public bool Update(GPS pos, DateTime date, Vector3D speed, int antennaRange)
			{
				bool posChanged = HasChanged(this.Pos, pos);
				bool speedChanged = HasChanged(this.Speed, speed);
				bool antennaChanged = this.AntennaRange != antennaRange;
				bool changed = posChanged  || speedChanged  || antennaChanged;
				this.Pos = pos;
				this.Date = date;
				this.Speed = speed;
				this.AntennaRange = antennaRange;
				ComputeNextPos();
				// assuming no acceleration
				return changed;
			}

			private GPS ComputeNextPos()
			{
				this.NextPos = this.Pos.ComputeGPS(this.Speed, MillisecondsSinceSend() / 1000D);
				return this.NextPos;
			}

			private bool HasChanged<T>(T oldVal, T newVal)
			{
				return (oldVal == null && newVal != null) || (oldVal != null && (newVal == null || !oldVal.Equals(newVal)));
			}

			public bool Update(int antennaRange)
			{
				return Update(Pos, Date, Speed, antennaRange);
			}

			private Vector3D ReadSpeed(string subdata)
			{
				string[] speedArray = subdata.Split(';');
				double speedX = double.Parse(speedArray[0]);
				double speedY = double.Parse(speedArray[0]);
				double speedZ = double.Parse(speedArray[0]);
				return new Vector3D(speedX, speedY, speedZ);
			}
			private string WriteSpeed(Vector3D speed)
			{
				return speed.X + ";" + speed.Y + ";" + speed.Z;
			}
			public override string ToString()
			{
				return this.NextPos + "|" + DateTime.UtcNow.ToString(DATE_FORMAT) + "|" + WriteSpeed(this.Speed) + "|" + this.AntennaRange;
			}

			public override int Compare(MemberInfo x, MemberInfo y)
			{
				return x.Id.CompareTo(y.Id);
			}
		}
	}
}
