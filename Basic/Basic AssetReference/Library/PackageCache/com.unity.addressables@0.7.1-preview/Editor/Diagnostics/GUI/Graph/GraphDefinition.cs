using System;
using UnityEditor.AddressableAssets.Diagnostics.Data;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Diagnostics.GUI.Graph
{
    class GraphDefinition
    {
        int m_MaxValueStream;
        internal IGraphLayer[] layers;
        internal GraphDefinition(int maxValueStream, IGraphLayer[] l)
        {
            layers = l;
            m_MaxValueStream = maxValueStream;
        }

        internal int GetMaxValue(EventDataSet e)
        {
            var stream = e.GetStream(m_MaxValueStream);
            return stream == null ? 1 : Mathf.Max(10, (stream.maxValue / 10 + 1) * 10);
        }
    }
}
