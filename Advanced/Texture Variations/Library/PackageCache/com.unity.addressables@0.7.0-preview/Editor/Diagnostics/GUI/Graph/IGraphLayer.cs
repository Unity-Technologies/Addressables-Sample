using System;
using UnityEditor.AddressableAssets.Diagnostics.Data;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Diagnostics.GUI.Graph
{
    interface IGraphLayer
    {
        string LayerName { get; }
        string Description { get; }
        Color GraphColor { get; }
        void Draw(EventDataSet dataSet, Rect rect, int startFrame, int frameCount, int inspectFrame, bool expanded, Material material, int maxValue);
    }
}
