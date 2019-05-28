using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.Diagnostics;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets.Diagnostics.Data
{
    [Serializable]
    class EventDataPlayerSession
    {
        EventDataSet m_RootStreamEntry = new EventDataSet(0, null, null, -1);
        string m_EventName;
        int m_PlayerId;
        bool m_IsActive;
        int m_LatestFrame;
        int m_StartFrame;
        int m_FrameCount = 300;
        Dictionary<int, List<DiagnosticEvent>> m_FrameEvents = new Dictionary<int, List<DiagnosticEvent>>();

        public EventDataSet RootStreamEntry { get { return m_RootStreamEntry; } }
        public string EventName { get { return m_EventName; } }
        public int PlayerId { get { return m_PlayerId; } }
        public bool IsActive { get { return m_IsActive; } set { m_IsActive = value; } }
        public int LatestFrame { get { return m_LatestFrame; } }
        public int StartFrame { get { return m_StartFrame; } }
        public int FrameCount { get { return m_FrameCount; } }


        public EventDataPlayerSession() { }
        public EventDataPlayerSession(string eventName, int playerId)
        {
            m_EventName = eventName;
            m_PlayerId = playerId;
            m_IsActive = true;
        }

        internal void Clear()
        {
            RootStreamEntry.Clear();
            m_FrameEvents.Clear();
            lastFrameWithEvents = -1;
            lastInstantiationCountValue = 0;
            m_eventCountDataSet = null;
            m_instantitationCountDataSet = null;
            m_dataSets.Clear();
            m_objectToParents.Clear();
        }

        internal List<DiagnosticEvent> GetFrameEvents(int frame)
        {
            List<DiagnosticEvent> frameEvents;
            if (m_FrameEvents.TryGetValue(frame, out frameEvents))
                return frameEvents;
            return null;
        }


        Dictionary<int, EventDataSet> m_dataSets = new Dictionary<int, EventDataSet>();
        Dictionary<int, HashSet<int>> m_objectToParents = new Dictionary<int, HashSet<int>>();
        int lastInstantiationCountFrame = -1;
        int lastInstantiationCountValue = 0;

        int lastFrameWithEvents = -1;
        EventDataSet m_eventCountDataSet;
        EventDataSet m_instantitationCountDataSet;
        internal void AddSample(DiagnosticEvent evt, bool recordEvent, ref bool entryCreated)
        {
            m_LatestFrame = evt.Frame;
            m_StartFrame = m_LatestFrame - m_FrameCount;
            
            if (recordEvent && !evt.DisplayName.StartsWith("Instance"))
            {
                List<DiagnosticEvent> frameEvents;
                if (!m_FrameEvents.TryGetValue(evt.Frame, out frameEvents))
                {
                    if (lastFrameWithEvents >= 0)
                    {
                        if (m_eventCountDataSet == null)
                        {
                            m_eventCountDataSet = new EventDataSet(0, "EventCount", "Event Counts", EventDataSet.kEventCountSortOrder);
                            RootStreamEntry.AddChild(m_eventCountDataSet);
                        }
                        m_eventCountDataSet.AddSample(0, lastFrameWithEvents, m_FrameEvents[lastFrameWithEvents].Count);
                    }
                    lastFrameWithEvents = evt.Frame;
                    m_FrameEvents.Add(evt.Frame, frameEvents = new List<DiagnosticEvent>());
                }
                frameEvents.Add(evt);
            }
            
            if (evt.DisplayName.StartsWith("Instance"))
            {
                if (evt.Stream == (int)ResourceManager.DiagnosticEventType.AsyncOperationCreate)
                {
                    if (evt.Frame != lastInstantiationCountFrame)
                    {
                        if (lastInstantiationCountFrame >= 0 && lastInstantiationCountValue > 0)
                        {
                            if (m_instantitationCountDataSet == null)
                            {
                                m_instantitationCountDataSet = new EventDataSet(1, "InstantiationCount", "Instantiation Counts", EventDataSet.kInstanceCountSortOrder);
                                RootStreamEntry.AddChild(m_instantitationCountDataSet);
                            }
                            m_instantitationCountDataSet.AddSample(0, lastInstantiationCountFrame, lastInstantiationCountValue);
                        }
                        lastInstantiationCountFrame = evt.Frame;
                        lastInstantiationCountValue = 0;
                    }
                    lastInstantiationCountValue++;
                }
                return;
            }

            //if creation event, create a data set and update all dependecies
            if (!m_dataSets.ContainsKey(evt.ObjectId))
            {
                var ds = new EventDataSet(evt);
                m_dataSets.Add(evt.ObjectId, ds);
                if (evt.Dependencies != null)
                {
                    foreach (var d in evt.Dependencies)
                    {
                        EventDataSet depDS;
                        if (m_dataSets.TryGetValue(d, out depDS))
                        {
                            ds.AddChild(depDS);
                            HashSet<int> depParents = null;
                            if (!m_objectToParents.TryGetValue(d, out depParents))
                            {
                                RootStreamEntry.RemoveChild(d);
                                m_objectToParents.Add(d, depParents = new HashSet<int>());
                            }
                            depParents.Add(evt.ObjectId);
                        }
                    }
                }
                if (!m_objectToParents.ContainsKey(evt.ObjectId))
                    RootStreamEntry.AddChild(ds);
            }


            EventDataSet data = null;
            if (m_dataSets.TryGetValue(evt.ObjectId, out data))
            {
                data.AddSample(evt.Stream, evt.Frame, evt.Value);
            }
            
            if (evt.Stream == (int)ResourceManager.DiagnosticEventType.AsyncOperationDestroy)
            {
                if (evt.Dependencies != null)
                {
                    foreach (var d in evt.Dependencies)
                    {
                        HashSet<int> depParents = null;
                        if (m_objectToParents.TryGetValue(d, out depParents))
                        {
                            depParents.Remove(evt.ObjectId);
                            if (depParents.Count == 0)
                            {
                                m_objectToParents.Remove(d);
                                RootStreamEntry.AddChild(m_dataSets[d]);
                            }
                        }
                    }
                }
                m_dataSets.Remove(evt.ObjectId);

                HashSet<int> parents = null;
                if (m_objectToParents.TryGetValue(evt.ObjectId, out parents))
                {
                    foreach (var p in parents)
                    {
                        EventDataSet pp;
                        if (m_dataSets.TryGetValue(p, out pp))
                            pp.RemoveChild(evt.ObjectId);
                    }
                }
                else
                {
                    RootStreamEntry.RemoveChild(evt.ObjectId);
                }
            }
        }
    }
}