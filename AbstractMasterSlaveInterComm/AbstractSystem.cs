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
		public abstract class AbstractSystem
		{
			public const string TAG_SEND_ID = "TAG_SEND_ID";
			protected MyGridProgram p;
			protected List<long> listeContactId = new List<long>();
			public AbstractSystem(MyGridProgram p)
			{
				this.p = p;
			}

			public bool HasContact()
			{
				return listeContactId.Count > 0;
			}

			public void AddContact(long id)
			{
				if (!this.listeContactId.Contains(id))
				{
					p.Echo("Add contact" + id);
					this.listeContactId.Add(id);
				}
			}

			public void RemoveNotReachableContact()
			{
				List<long> lstContactToRemove = new List<long>();
				foreach (long id in listeContactId)
				{
					if (!p.IGC.IsEndpointReachable(id))
					{
						p.Echo("Can't access to " + id);
						lstContactToRemove.Add(id);
					} else
					{
						p.Echo("Access to " + id + " OK");
					}
				}
				foreach (long idContactToRemove in lstContactToRemove)
				{
					RemoveContact(idContactToRemove);
				}
			}

			public void RemoveContact(long id)
			{
				p.Echo("Remove contact " + id);
				this.listeContactId.Remove(id);
			}

			public bool IsMyContact(long id)
			{
				return this.listeContactId.Contains(id);
			}

			public bool HasDirectMsg()
			{
				return p.IGC.UnicastListener.HasPendingMessage;
			}

			public void ListenToOneDirectMsg(FcntMessageReader reader)
			{
				IMyUnicastListener l = p.IGC.UnicastListener;
				if (l.HasPendingMessage)
				{
					reader(l.AcceptMessage());
				}
			}

			public void ListenToEveryDirectMsg(FcntMessageReader reader)
			{
				IMyUnicastListener l = p.IGC.UnicastListener;
				while (l.HasPendingMessage)
				{
					reader(l.AcceptMessage());
				}
			}

			public void ListenToOneIndirectMsg(string tag, FcntMessageReader reader)
			{
				IMyBroadcastListener l = p.IGC.RegisterBroadcastListener(tag);
				if (l.HasPendingMessage)
				{
					reader(l.AcceptMessage());
				}
			}

			public void ListenToEveryIndirectMsg(string tag, FcntMessageReader reader)
			{
				IMyBroadcastListener l = p.IGC.RegisterBroadcastListener(tag);
				while (l.HasPendingMessage)
				{
					reader(l.AcceptMessage());
				}
			}

			public void SendToEveryOne(string tag, string data)
			{
				p.IGC.SendBroadcastMessage(tag, data);
			}

			public void SendToContact(long idContact, string tag, string data)
			{
				p.IGC.SendUnicastMessage(idContact, tag, data);
			}

			public abstract void Run();

			public delegate void FcntMessageReader(MyIGCMessage msg);
		}
	} 
}
