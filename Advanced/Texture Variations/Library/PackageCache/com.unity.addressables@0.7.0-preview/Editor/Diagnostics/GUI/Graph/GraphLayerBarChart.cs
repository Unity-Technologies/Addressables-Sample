using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Diagnostics.Data;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Diagnostics.GUI.Graph
{
    class GraphLayerBarChartMesh : GraphLayerBase
    {
        Mesh m_Mesh;
        List<Vector3> m_Verts = new List<Vector3>();
        List<int> m_Indices = new List<int>();

        Rect m_Bounds;
        Vector2 m_GridSize;

        public GraphLayerBarChartMesh(int stream, string name, string description, Color color) : base(stream, name, description, color) { }

        void AddQuadToMesh(float left, float right, float bot, float top)
        {
            float xLeft = m_Bounds.xMin + left * m_GridSize.x;
            float xRight = m_Bounds.xMin + right * m_GridSize.x;
            float yBot = m_Bounds.yMax - bot * m_GridSize.y;
            float yTop = m_Bounds.yMax - top * m_GridSize.y;

            int start = m_Verts.Count;
            m_Verts.Add(new Vector3(xLeft, yBot, 0));
            m_Verts.Add(new Vector3(xLeft, yTop, 0));
            m_Verts.Add(new Vector3(xRight, yTop, 0));
            m_Verts.Add(new Vector3(xRight, yBot, 0));

            m_Indices.Add(start);
            m_Indices.Add(start + 1);
            m_Indices.Add(start + 2);

            m_Indices.Add(start);
            m_Indices.Add(start + 2);
            m_Indices.Add(start + 3);
        }

        public override void Draw(EventDataSet dataSet, Rect rect, int startFrame, int frameCount, int inspectFrame, bool expanded, Material material, int maxValue)
        {
            if (dataSet == null || material == null)
                return;

            var stream = dataSet.GetStream(Stream);
            if (stream != null && stream.samples.Count > 0)
            {
                material.color = GraphColor;

                if (m_Mesh == null)
                    m_Mesh = new Mesh();
                m_Verts.Clear();
                m_Indices.Clear();
                var endTime = startFrame + frameCount;

                m_Bounds = new Rect(rect);
                m_GridSize.x = m_Bounds.width / frameCount;
                m_GridSize.y = m_Bounds.height / maxValue;

                int previousFrameNumber = endTime;
                int currentFrame = endTime;

                for (int i = stream.samples.Count - 1; i >= 0 && currentFrame > startFrame; --i)
                {
                    currentFrame = stream.samples[i].frame;
                    var frame = Mathf.Max(currentFrame, startFrame);
                    if (stream.samples[i].data > 0)
                    {
                        AddQuadToMesh(frame - startFrame, previousFrameNumber - startFrame, 0, stream.samples[i].data);
                    }
                    previousFrameNumber = frame;
                }

                if (m_Verts.Count > 0)
                {
                    m_Mesh.Clear(true);
                    m_Mesh.SetVertices(m_Verts);
                    m_Mesh.triangles = m_Indices.ToArray();
                    material.SetPass(0);
                    Graphics.DrawMeshNow(m_Mesh, Vector3.zero, Quaternion.identity);
                }
            }
        }
    }
}