using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.Diagnostics;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Diagnostics.Data
{
    [Serializable]
    class EventDataSet
    {
        internal const int kFPSSortOrder = -100;
        internal const int kMonoHeapSortOrder = -99;
        internal const int kEventCountSortOrder = -98;
        internal const int kInstanceCountSortOrder = -97;

        [SerializeField]
        List<EventDataSetStream> m_Streams = new List<EventDataSetStream>();
        int m_FirstSampleFrame = int.MaxValue;
        int m_ObjectId;
        string m_DisplayName;
        string m_Graph;
        int m_SortOrder = 0;
        public int ObjectId { get { return m_ObjectId; } }
        public string DisplayName { get { return m_DisplayName; }  set { m_DisplayName = value; } }
        public string Graph { get { return m_Graph; } }
        public IEnumerable<EventDataSet> Children { get { return m_Children.Values; } }
        internal bool HasChildren { get { return m_Children != null && m_Children.Count > 0; } }
        internal int FirstSampleFrame { get { return m_FirstSampleFrame; } }
        Dictionary<int, EventDataSet> m_Children;
        internal EventDataSet() { }
        internal EventDataSet(int id, string graph, string displayName, int sortOrder)
        {
            m_SortOrder = sortOrder;
            m_ObjectId = id;
            m_Graph = graph;
            m_DisplayName = displayName;
        }
        internal EventDataSet(DiagnosticEvent evt)
        {
            Init(evt);
        }
        internal void Init(DiagnosticEvent evt)
        {
            m_ObjectId = evt.ObjectId;
            m_DisplayName = evt.DisplayName;
            m_Graph = evt.Graph;
            if (m_DisplayName == "MonoHeap")
                m_SortOrder = kMonoHeapSortOrder;
            else if (m_DisplayName == "FPS")
                m_SortOrder = kFPSSortOrder;
        }

        internal bool HasDataAfterFrame(int frame)
        {
            foreach (var s in m_Streams)
                if (s != null && s.HasDataAfterFrame(frame))
                    return true;
            if (m_Children != null)
            {
                foreach (var c in m_Children)
                    if (c.Value.HasDataAfterFrame(frame))
                        return true;
            }
            return false;
        }

        internal int CompareTo(EventDataSet other)
        {
            var x = m_SortOrder < 0 ? m_SortOrder : FirstSampleFrame;
            var y = other.m_SortOrder < 0 ? other.m_SortOrder : other.FirstSampleFrame;
            return x - y;
        }

        internal void AddSample(int stream, int frame, int val)
        {
            if (frame < m_FirstSampleFrame)
                m_FirstSampleFrame = frame;
            while (stream >= m_Streams.Count)
                m_Streams.Add(null);
            if (m_Streams[stream] == null)
                m_Streams[stream] = new EventDataSetStream();
            m_Streams[stream].AddSample(frame, val);
        }

        internal void AddChild(EventDataSet eventDataSet)
        {
            if (m_Children == null)
                m_Children = new Dictionary<int, EventDataSet>();
            m_Children.Add(eventDataSet.ObjectId, eventDataSet);
        }

        internal void RemoveChild(int d)
        {
            m_Children.Remove(d);
            if (m_Children.Count == 0)
                m_Children = null;
        }

        internal int GetStreamValue(int s, int frame)
        {
            var stream = GetStream(s);
            if (stream == null)
                return 0;
            return stream.GetValue(frame);
        }

        internal EventDataSetStream GetStream(int s)
        {
            if (s >= m_Streams.Count)
                return null;
            return m_Streams[s];
        }

        internal int GetStreamMaxValue(int s)
        {
            var stream = GetStream(s);
            if (stream == null)
                return 0;

            return stream.maxValue;
        }

        internal void Clear()
        {
            m_FirstSampleFrame = int.MaxValue;
            m_Children = null;
            m_Streams.Clear();
        }
    }

}