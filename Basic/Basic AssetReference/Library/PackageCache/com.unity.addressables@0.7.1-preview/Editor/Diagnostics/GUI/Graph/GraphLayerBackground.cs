using System;
using UnityEditor.AddressableAssets.Diagnostics.Data;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Diagnostics.GUI.Graph
{
    class GraphLayerBackgroundGraph : GraphLayerBase
    {
        bool IsContinuationOfSegment(int prevData, int nextData)
        {
            return (prevData == 0 != (nextData == 0));
        }

        Color m_LoadColor;
        int m_LoadStatusStream;
        internal GraphLayerBackgroundGraph(int refCountStream, Color refBgColor, int loadStatusStream, Color loadStatusColor, string name, string desc) : base(refCountStream, name, desc, refBgColor) { m_LoadColor = loadStatusColor; m_LoadStatusStream = loadStatusStream; }
        public override void Draw(EventDataSet dataSet, Rect rect, int startFrame, int frameCount, int inspectFrame, bool expanded, Material material, int maxValue)
        {
            if (dataSet == null)
                return;

            int endFrame = startFrame + frameCount;
            
            EventDataSetStream refStream = dataSet.GetStream(Stream);
            if (refStream != null)
            {
                foreach (GraphUtility.Segment s in GraphUtility.IterateSegments(refStream, startFrame, endFrame, IsContinuationOfSegment))
                {
                    if (s.data != 0)
                    {
                        float x = rect.xMin + GraphUtility.ValueToPixel(s.frameStart, startFrame, endFrame, rect.width);
                        float w = (rect.xMin + GraphUtility.ValueToPixel(s.frameEnd, startFrame, endFrame, rect.width)) - x;
                        EditorGUI.DrawRect(new Rect(x, rect.yMin, w, rect.height), GraphColor);
                    }
                }
            }

            EventDataSetStream loadStream = dataSet.GetStream(m_LoadStatusStream);
            if (loadStream != null)
            {
                foreach (GraphUtility.Segment s in GraphUtility.IterateSegments(loadStream, startFrame, endFrame, IsContinuationOfSegment))
                {
                    if (s.data == 0)
                    {
                        float x = rect.xMin + GraphUtility.ValueToPixel(s.frameStart, startFrame, endFrame, rect.width);
                        float w = (rect.xMin + GraphUtility.ValueToPixel(s.frameEnd, startFrame, endFrame, rect.width)) - x;
                        EditorGUI.DrawRect(new Rect(x, rect.yMin, w, rect.height), m_LoadColor);
                    }
                }
            }
        }
    }
}
