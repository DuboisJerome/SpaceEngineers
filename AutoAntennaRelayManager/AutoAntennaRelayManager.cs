using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
		public class AutoAntennaRelayManager : AbstractAutoAntennaRelay
		{
			// Minimum 100m of safe antenna range
			private const double MIN_DELTA_RANGE = 200D;
			public const string TAG_ASK_CHANGE_RANGE = "TAG_ASK_CHANGE_RANGE";
			public const string TAG_ASK_UPDATE_INFO = "TAG_ASK_UPDATE_INFO";
			public const string TAG_ASK_UPDATE_INFO_OPTIONNAL = "TAG_ASK_UPDATE_INFO_OPTIONNAL";
			public const string TAG_INFO_MEMBER = "TAG_INFO_MEMBER";

			private const UpdateType refreshRate = UpdateType.Update10 | UpdateType.Terminal;

			// For every run
			private readonly Dictionary<long, MemberInfo> members = new Dictionary<long, MemberInfo>();

			public AutoAntennaRelayManager(MyGridProgram p, String tagGroup) : base(p, tagGroup)
			{
				p.Runtime.UpdateFrequency = UpdateFrequency.Update10;
				AddListener(TAG_INFO_MEMBER, true);
				WriteToChanel(TAG_ASK_UPDATE_INFO, "");
				members.Add(MyInfo.Id, MyInfo);
			}

			public override void Run(UpdateType updateSource)
			{

				if((updateSource & UpdateType.Terminal) != 0)
				{
					LOGGER.Clear();
				}
				LOGGER.Debug("src = " + updateSource);
				// Compute my info
				bool isNeedUpdate = (updateSource & refreshRate) != 0;
				if (isNeedUpdate)
				{
					UpdateMyInfo();
					string tag = updateSource == UpdateType.Terminal ? TAG_ASK_UPDATE_INFO : TAG_ASK_UPDATE_INFO_OPTIONNAL;
					WriteToChanel(tag, "");
				}

				// Reads all msgs
				bool isNeedReadMessage = (updateSource & UpdateType.IGC) != 0;
				if (isNeedReadMessage)
				{
					ReadChanelMsgs();
				}

				// Manage antennas
				if (isNeedUpdate || isNeedReadMessage)
				{
					ManageAntennaRange();
					SearchForMissing();
				}
			}

			private void DoForEachMember(ICollection<MemberInfo> l, Action<MemberInfo> a)
			{
				foreach (MemberInfo gi in l)
					a(gi);
			}
			private void DoForEachMember(Action<MemberInfo> a)
			{
				DoForEachMember(members.Values, a);
			}

			private List<MemberInfo> GetMembersUnderDirectRange(MemberInfo from)
			{
				LOGGER.Debug("GetMembersUnderDirectRange " + GetId(from));
				// All member under range antenna from farthest to nearest
				return members.Values.Where(m => from.Id != m.Id)
					.Where(m =>
					{
						double d = MinRange(m, from);
						bool isInFromRange = d <= from.AntennaRange;
						bool isInMRange = d <= m.AntennaRange;
						bool isInBothRange = isInFromRange && isInMRange;
						if (!isInBothRange)
						{
							LOGGER.Info("-- " + GetId(m));
							if (!isInFromRange)
								LOGGER.Info("--> from : " + d + " <= " + from.AntennaRange);
							if (!isInMRange)
								LOGGER.Info("--> m : " + d + " <= " + m.AntennaRange);
						} else
							LOGGER.Info("++ " + GetId(m));
						return isInBothRange;
					})
					.OrderByDescending(m => m.NextPos.Distance(from.NextPos))
					.ToList();
			}

			private bool IsAllReachableFrom(MemberInfo from)
			{
				List<MemberInfo> listAllMembersFound = new List<MemberInfo>();
				return IsAllReachableFromRec(from, listAllMembersFound);
			}

			private bool IsAllReachableFromRec(MemberInfo from, List<MemberInfo> listAllMembersFound, int lvl =0)
			{
				if (listAllMembersFound.Count == members.Count)
					return true;
				LOGGER.Debug("IsAllReachableFromRec : " + lvl);
				List<long> listAllMembersFoundId = listAllMembersFound.Select(m => m.Id).ToList();
				List<MemberInfo> membersUnder = GetMembersUnderDirectRange(from);
				// Members not already found order by nearest to farthest
				List<MemberInfo> listLclMembersFound = membersUnder.Where(m => !listAllMembersFoundId.Contains(m.Id)).OrderBy(m => m.CalculDistance(from)).ToList();
				// Add All member at this level
				DoForEachMember(listLclMembersFound, m => listAllMembersFound.Add(m));
				// Search for each level
				bool result = false;
				DoForEachMember(listLclMembersFound, m =>
				{
					if (!result)
						result = IsAllReachableFromRec(m, listAllMembersFound, lvl+1);
				});
				return result;
			}

			private void ManageAntennaRange()
			{
				if (members.Count <= 1)
				{
					LOGGER.Info("No members");
					return;
				}
				Dictionary<MemberInfo, List<MemberInfo>> membersUnderRangeByMember = new Dictionary<MemberInfo, List<MemberInfo>>();
				LOGGER.Debug("Compute all member under range");
				// Compute all members under range by member including me
				DoForEachMember(m => membersUnderRangeByMember.Add(m, GetMembersUnderDirectRange(m)));

				// Order member by max antenna range to min antenna range
				List<MemberInfo> allMembersOrderByAntennaRange = membersUnderRangeByMember.Keys.OrderByDescending(m => m.AntennaRange).ToList();

				Dictionary<MemberInfo, int> oldRangeAntennaByMembers = new Dictionary<MemberInfo, int>();
				Dictionary<MemberInfo, int> newRangeAntennaByMembers = new Dictionary<MemberInfo, int>();
				// Decreased antenna to farthest child
				foreach (MemberInfo m in allMembersOrderByAntennaRange)
				{
					oldRangeAntennaByMembers.Add(m, m.AntennaRange);
					List<MemberInfo> membersUnderRange = membersUnderRangeByMember[m];
					if (membersUnderRange != null && membersUnderRange.Count > 0)
					{
						MemberInfo farthest = membersUnderRange.First();
						int newRange = MinRange(m, farthest);
						// Inutile de faire un if si c'est under range, c'est forcement que newRange <= m.AntennaRange
						m.Update(newRange);
						newRangeAntennaByMembers.Add(m, newRange);
					}
					else
					{
						LOGGER.Warn("None under range of " + GetId(m));
					}
				}

				// Try to decreased largest antenna
				foreach (MemberInfo m in allMembersOrderByAntennaRange)
				{
					LOGGER.Debug("BEGIN try for " + GetId(m));
					List<MemberInfo> membersUnderRange = membersUnderRangeByMember[m];
					int nbMembersUnderRange = membersUnderRange.Count;
					if (nbMembersUnderRange <= 1)
					{
						LOGGER.Info(GetId(m) + " have only "+ nbMembersUnderRange + " members under range");

						// Only 1 child we can't cut without losing signal
						continue;
					}
					MemberInfo farthestMemberUnderRange = membersUnderRangeByMember[m].First();
					LOGGER.Debug("Try Cut " + GetId(farthestMemberUnderRange));
					List<MemberInfo> membersUnderFarthestRange = membersUnderRangeByMember[farthestMemberUnderRange];
					int nbMembersUnderFarthestRange = membersUnderFarthestRange.Count;
					if (nbMembersUnderFarthestRange <= 1)
					{
						if(nbMembersUnderFarthestRange == 1)
						{
							bool hasAlreadyCutMe = membersUnderFarthestRange[0].Id != m.Id;
							if (hasAlreadyCutMe)
							{
								LOGGER.Info(GetId(farthestMemberUnderRange) + " have already cut " + GetId(m));
							} else
							{
								LOGGER.Info(GetId(farthestMemberUnderRange) + " have only " + GetId(m)+ " under range");
								// Our farthest child have only us, we can't cut without losing signal
							}
						}
						else
						{
							LOGGER.Warn(GetId(farthestMemberUnderRange) + " have none under range");
						}
						continue;
					}
					membersUnderRange.RemoveAt(0);
					MemberInfo newFirst = membersUnderRange.First();
					int lclOldRange = m.AntennaRange;
					int newRange = MinRange(m, newFirst);
					bool isOk = false;
					if (newRange < lclOldRange)
					{
						m.Update(newRange);
						isOk = IsAllReachableFrom(m);
						if (isOk)
						{
							newRangeAntennaByMembers[m] = newRange;
						}
						else
						{
							// Re-insert previously delete child
							membersUnderRange.Insert(0, farthestMemberUnderRange);
							// Reset the range
							m.Update(lclOldRange);
						}
					}
					LOGGER.Debug("END try for " + GetId(m) + " from " + lclOldRange + " to " + newRange + " = " + isOk);
				}

				// Try to increase antenna to get back lost member
				DoForEachMember(allMembersOrderByAntennaRange, m =>
				{
					List<MemberInfo> membersUnderRange = membersUnderRangeByMember[m];
					if(membersUnderRange.Count == 0)
					{
						MemberInfo nearest = GetNearest(m);
						int newRange = MinRange(m, nearest);
						newRangeAntennaByMembers[m] = newRange;

						int nearestNewRange = nearest.AntennaRange > newRange ? nearest.AntennaRange : newRange;
						newRangeAntennaByMembers[nearest] = nearestNewRange;
					}
				});

				// Send updates
				foreach (KeyValuePair<MemberInfo, int> e in newRangeAntennaByMembers)
				{
					MemberInfo m = e.Key;
					int newRange = e.Value;
					int oldRange = oldRangeAntennaByMembers[m];
					if (newRange != oldRange)
					{
						LOGGER.Debug("Change " + GetId(m) + " from " + oldRange + " to " + newRange);
						if (m == MyInfo)
						{
							SetAntennaRange(newRange);
						}
						else
						{
							p.IGC.SendUnicastMessage(m.Id, TAG_ASK_CHANGE_RANGE, newRange);
						}
						m.Update(newRange);
					}
				}
			}

			private MemberInfo GetNearest(MemberInfo m)
			{
				MemberInfo nearest = null;
				double nearestDist = -1;
				DoForEachMember(m2 =>
				{
					if (m2 == m)
						return;
					double d = m.CalculDistance(m2);
					if(nearest == null || nearestDist < 0 || d < nearestDist)
					{
						nearest = m2;
						nearestDist = d;
					}
				});
				return nearest;
			}

			private string GetId(MemberInfo m)
			{
				return m == MyInfo ? "me" : m.Id.ToString();
			}

			// ~Distance + 100m
			private int MinRange(MemberInfo from, MemberInfo to)
			{
				return (int)(from.CalculDistance(to) + MIN_DELTA_RANGE);
			}

			private void ReadChanelMsgs()
			{
				ReadChanelMsgs((msg, tag) =>
				{
					if (tag.Equals(TAG_INFO_MEMBER))
					{
						ReadMember(msg.Source, (string)msg.Data);
					}
				});
			}

			private void ReadMember(long id, string data)
			{
				MemberInfo info;
				if (members.ContainsKey(id))
					info = members[id];
				else
				{
					info = new MemberInfo(LOGGER, id);
					members.Add(id, info);
				}
				info.UpdateFromData(data);
				LOGGER.Debug("Update info for " + id + " : " + info);
			}

			private void SearchForMissing()
			{
				DoForEachMember(m=> { 
					if (!p.IGC.IsEndpointReachable(m.Id))
					{
						LOGGER.Warn("Can't access to " + GetId(m));
						// TODO do something
					}
				});
			}
		}
	}
}
