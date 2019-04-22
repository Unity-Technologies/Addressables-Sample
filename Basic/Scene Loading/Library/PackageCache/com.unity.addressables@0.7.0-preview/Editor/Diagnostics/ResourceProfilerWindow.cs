using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.AddressableAssets.Diagnostics.GUI;
using UnityEditor.AddressableAssets.Diagnostics.GUI.Graph;
using UnityEngine;
using UnityEngine.AddressableAssets.Utility;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.Diagnostics;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    /*
     * ResourceManager specific implementation of an EventViewerWindow
     */
    class ResourceProfilerWindow : EventViewerWindow
    {
        [MenuItem("Window/Asset Management/Addressable Profiler", priority = 2051)]
        static void ShowWindow()
        {
            var window = GetWindow<ResourceProfilerWindow>();
            window.titleContent = new GUIContent("Addressable Profiler", "Addressable Profiler");
            window.Show();
        }

        protected override bool ShowEventDetailPanel { get { return false; } }
        protected override bool ShowEventPanel { get { return true; } }

        protected static string GetDataStreamName(int stream)
        {
            return ((ResourceManager.DiagnosticEventType)stream).ToString();
        }

        protected override bool OnCanHandleEvent(string graph)
        {
            return graph == "ResourceManager";
        }

        protected override bool OnRecordEvent(DiagnosticEvent evt)
        {
            if (evt.Graph == "ResourceManager")
            {
                switch ((ResourceManager.DiagnosticEventType)evt.Stream)
                {
                    case ResourceManager.DiagnosticEventType.AsyncOperationCreate:
                    case ResourceManager.DiagnosticEventType.AsyncOperationDestroy:
                    case ResourceManager.DiagnosticEventType.AsyncOperationComplete:
                    case ResourceManager.DiagnosticEventType.AsyncOperationFail:
                        return true;
                }
            }
            return base.OnRecordEvent(evt);
        }

        protected override void OnDrawEventDetail(Rect rect, DiagnosticEvent evt)
        {
  }

        protected override void OnGetColumns(List<string> columnNames, List<float> columnSizes)
        {
            if (columnNames == null || columnSizes == null)
                return;
            columnNames.AddRange(new[] { "Event", "Key"});
            columnSizes.AddRange(new float[] { 150, 400 });
        }

        protected override bool OnDrawColumnCell(Rect cellRect, DiagnosticEvent evt, int column)
        {
            switch (column)
            {
                case 0: EditorGUI.LabelField(cellRect, ((ResourceManager.DiagnosticEventType)evt.Stream).ToString()); break;
                case 1: EditorGUI.LabelField(cellRect, evt.DisplayName); break;
               }

            return true;
        }

        protected override void OnInitializeGraphView(EventGraphListView graphView)
        {
            if (graphView == null)
                return;

            Color labelBgColor = GraphColors.LabelGraphLabelBackground;

            Color refCountBgColor = new Color(53 / 255f, 136 / 255f, 167 / 255f, 1);
            Color loadingBgColor = Color.Lerp(refCountBgColor, GraphColors.WindowBackground, 0.5f);

            graphView.DefineGraph("ResourceManager", (int)ResourceManager.DiagnosticEventType.AsyncOperationReferenceCount,
                new GraphLayerBackgroundGraph((int)ResourceManager.DiagnosticEventType.AsyncOperationReferenceCount, refCountBgColor, (int)ResourceManager.DiagnosticEventType.AsyncOperationPercentComplete, loadingBgColor, "LoadPercent", "Loaded"),
                new GraphLayerBarChartMesh((int)ResourceManager.DiagnosticEventType.AsyncOperationReferenceCount, "RefCount", "Reference Count", new Color(123 / 255f, 158 / 255f, 6 / 255f, 1)),
                new GraphLayerEventMarker((int)ResourceManager.DiagnosticEventType.AsyncOperationCreate, "", "", Color.grey, Color.grey),
                new GraphLayerEventMarker((int)ResourceManager.DiagnosticEventType.AsyncOperationComplete, "", "", Color.white, Color.white),
                new GraphLayerEventMarker((int)ResourceManager.DiagnosticEventType.AsyncOperationDestroy, "", "", Color.black, Color.black),
                new GraphLayerEventMarker((int)ResourceManager.DiagnosticEventType.AsyncOperationFail, "", "", Color.red, Color.red),
                new GraphLayerLabel((int)ResourceManager.DiagnosticEventType.AsyncOperationReferenceCount, "RefCount", "Reference Count", new Color(123 / 255f, 158 / 255f, 6 / 255f, 1), labelBgColor, v => v.ToString())
                );

            graphView.DefineGraph("InstantiationCount", 0, new GraphLayerVertValueLine(0, "Instantiation Count", "Instantiation Count", Color.green));

        }
    }
}
