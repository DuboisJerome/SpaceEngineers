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
		public enum TypeContact { NONE = 0, SEPARATION = 1, ALIGNEMENT = 2, COHESION = 3};

		public class BoidsParams
		{
			public readonly String swarmTag;
			public readonly double separationDist, alignementDist, cohesionDist;
			public BoidsParams(String swarmTag, double separationDist, double alignementDist, double cohesionDist)
			{
				this.swarmTag = swarmTag;
				this.separationDist = separationDist;
				this.alignementDist = alignementDist;
				this.cohesionDist = cohesionDist;
			}
		}

		public class Boids : AbstractSystem
		{
			private GPS myPos = null;
			private Dictionary<long, BoidsContact> mapBoidsContact = new Dictionary<long, BoidsContact>();
			private readonly BoidsParams boidsParams;

			public Boids(MyGridProgram p,  BoidsParams boidsParams) : base(p)
			{
				this.boidsParams = boidsParams;
			}

			private void Separation()
			{

			}

			private void Alignement()
			{

			}

			private void Cohesion()
			{

			}

			public override void Run()
			{
				this.myPos = new GPS(p.Me.GetPosition());
				SendMyPosition();
				ListenBoidsPosition();
				Move();
			}


			private void SendMyPosition()
			{
				SendToEveryOne(boidsParams.swarmTag, myPos.ToString());
			}

			private void ListenBoidsPosition()
			{
				ListenToEveryIndirectMsg(boidsParams.swarmTag, RegisterBoidsSameSwarm);
			}

			private void RegisterBoidsSameSwarm(MyIGCMessage msg)
			{
				BoidsContact contact = new BoidsContact(msg);
				ComputeTypeContact(contact);
			}

			private void ComputeTypeContact(BoidsContact contact)
			{
				double distance = this.myPos.Distance(contact.pos);
				TypeContact typeContact;
				if (distance < this.boidsParams.separationDist)
				{
					typeContact = TypeContact.SEPARATION;
				}
				else if (distance < this.boidsParams.alignementDist)
				{
					typeContact = TypeContact.ALIGNEMENT;
				}
				else if (distance < this.boidsParams.cohesionDist)
				{
					typeContact = TypeContact.COHESION;
				}
				else
				{
					typeContact = TypeContact.NONE;
				}

				if (this.mapBoidsContact.ContainsKey(contact.source))
				{
					this.mapBoidsContact.Remove(contact.source);
				}
				if (typeContact != TypeContact.NONE)
				{
					this.mapBoidsContact.Add(contact.source, contact);
				}
			}

			private void Move()
			{
				List<BoidsContact> listSeparation = new List<BoidsContact>();
				List<BoidsContact> listAlignement = new List<BoidsContact>();
				List<BoidsContact> listCohesion = new List<BoidsContact>();
				foreach (BoidsContact v in mapBoidsContact.Values)
				{
					double distance = this.myPos.Distance(v.pos);
					switch (v.typeContact)
					{
						case TypeContact.SEPARATION:
							listSeparation.Add(v); break;
						case TypeContact.ALIGNEMENT:
							listAlignement.Add(v); break;
						case TypeContact.COHESION:
							listCohesion.Add(v); break;
						default: break;
					}
				}

				if(listSeparation.Count > 0)
				{

				} else if(listAlignement.Count > 0)
				{

				} else if(listCohesion.Count > 0)
				{

				}
			}
		}

		public class BoidsContact {
			public long source;
			public GPS pos;
			public TypeContact typeContact;
			public BoidsContact(MyIGCMessage msg)
			{
				this.source = msg.Source;
				this.pos = new GPS(msg.Data.ToString());
			}
			public void SetTypeContact(TypeContact typeContact)
			{
				this.typeContact = typeContact;
			}
		}
	}
}
