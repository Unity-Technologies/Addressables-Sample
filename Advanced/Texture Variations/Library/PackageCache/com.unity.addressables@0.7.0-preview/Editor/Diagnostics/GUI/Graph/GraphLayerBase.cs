using System;
using UnityEditor.AddressableAssets.Diagnostics.Data;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Diagnostics.GUI.Graph
{
    class GraphLayerBase : IGraphLayer
    {
        string m_LayerName;
        string m_Description;
        Color m_Color;
        public int Stream { get; private set; }

        public GraphLayerBase(int stream, string name, string description, Color color)
        {
            Stream = stream;
            m_LayerName = name;
            m_Description = description;
            m_Color = color;
        }

        public Color GraphColor { get { return m_Color; } }

        public string LayerName { get { return m_LayerName; } }

        public string Description { get { return m_Description; } }

        public virtual void Draw(EventDataSet dataSet, Rect rect, int startFrame, int frameCount, int inspectFrame, bool expanded, Material material, int maxValue)
        {
        }
    }
}
