using System;
using UnityEditor.AddressableAssets.Diagnostics.Data;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Diagnostics.GUI.Graph
{
    class GraphLayerLabel : GraphLayerBase
    {
        Func<int, string> m_LabelFunc;
        Color m_BgColor;
        internal GraphLayerLabel(int stream, string name, string desc, Color color, Color bgColor, Func<int, string> func) : base(stream, name, desc, color) { m_LabelFunc = func; m_BgColor = bgColor; }
        public override void Draw(EventDataSet dataSet, Rect rect, int startFrame, int frameCount, int inspectFrame, bool expanded, Material material, int maxValue)
        {
            if (dataSet == null)
                return;

            var endTime = startFrame + frameCount;
            var stream = dataSet.GetStream(Stream);
            if (stream != null)
            {
                var prevCol = UnityEngine.GUI.color;
                UnityEngine.GUI.color = GraphColor;
                if (expanded)
                {
                    var text = new GUIContent(maxValue.ToString());
                    var size = UnityEngine.GUI.skin.label.CalcSize(text);
                    var labelRect = new Rect(rect.xMin + 2, rect.yMin, size.x, size.y);
                    EditorGUI.LabelField(labelRect, text);
                    labelRect = new Rect(rect.xMax - size.x, rect.yMin, size.x, size.y);
                    EditorGUI.LabelField(labelRect, text);
                }

                if (inspectFrame != endTime)
                {
                    var val = stream.GetValue(inspectFrame);
                    if (val > 0)
                    {
                        var text = new GUIContent(m_LabelFunc(val));
                        var size = UnityEngine.GUI.skin.label.CalcSize(text);
                        var x = GraphUtility.ValueToPixel(inspectFrame, startFrame, endTime, rect.width);
                        float pixelVal = GraphUtility.ValueToPixel(val, 0, maxValue, rect.height);
                        var labelRect = new Rect(rect.xMin + x + 5, Mathf.Max(rect.yMin, rect.yMax - (pixelVal + size.y)), size.x, size.y);
                        UnityEngine.GUI.DrawTexture(labelRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0, m_BgColor, 50, 5);
                        EditorGUI.LabelField(labelRect, text, UnityEngine.GUI.skin.label);
                    }
                }
                UnityEngine.GUI.color = prevCol;
            }
        }
    }
}
