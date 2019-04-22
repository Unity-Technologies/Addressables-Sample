using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.AddressableAssets.Diagnostics.GUI;
using UnityEditor.AddressableAssets.Diagnostics.GUI.Graph;
using UnityEngine.AddressableAssets;
using UnityEngine;
using UnityEngine.ResourceManagement.Diagnostics;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.ResourceManagement;
using UnityEditor.IMGUI.Controls;

namespace UnityEditor.AddressableAssets.Diagnostics
{
    /*
     * ResourceManager specific implementation of an EventViewerWindow
     */
    class ResourceManagerCacheWindow : EditorWindow
    {
    //    [MenuItem("Window/Asset Management/Resource Manager Cache", priority = 2051)]
        static void ShowWindow()
        {
            var window = GetWindow<ResourceManagerCacheWindow>();
            window.titleContent = new GUIContent("Resource Manager Cache", "Resource Manager Cache");
            window.Show();
        }

        class CacheDataTree : TreeView
        {
            ResourceManagerCacheWindow m_Window;
            static int Compare(TreeViewItem a, TreeViewItem b)
            {
                return ((EventTreeViewItem)b).m_state.ReferenceCount - ((EventTreeViewItem)a).m_state.ReferenceCount;
            }
              
            class EventTreeViewItem : TreeViewItem
            {
                internal OperationState m_state;
                public EventTreeViewItem(Dictionary<int, OperationState> states, OperationState e, int depth) : base(e.ObjectId, depth, string.Format("{0}\t{1}", e.ReferenceCount, e.DisplayName))
                {
                    m_state = e;
                    if (e.Dependencies != null && e.Dependencies.Length > 0)
                    {
                        children = new List<TreeViewItem>(e.Dependencies.Length);
                        foreach (var d in e.Dependencies)
                            AddChild(new EventTreeViewItem(states, states[d], depth + 1));
                        children.Sort(Compare);
                    }
                }
            }
            public CacheDataTree(ResourceManagerCacheWindow rmcw, TreeViewState tvs) : base(tvs)
            {
                m_Window = rmcw;
            }

            protected override TreeViewItem BuildRoot()
            {
                TreeViewItem root = new TreeViewItem(-1, -1);
                root.children = new List<TreeViewItem>(m_Window.m_OpStates.Count);
                foreach (var l in m_Window.m_OpStates)
                    root.AddChild(new EventTreeViewItem(m_Window.m_OpStates, l.Value, 0));
                root.children.Sort(Compare);
                return root;
            }
        }

        void OnEnable()
        {
            if (m_EventListTreeViewState == null)
                m_EventListTreeViewState = new TreeViewState();
            m_OpStates = new Dictionary<int, OperationState>();
            m_cacheTree = new CacheDataTree(this, m_EventListTreeViewState);
            m_cacheTree.Reload();
            EditorApplication.playModeStateChanged += OnEditorPlayModeChanged;
            DiagnosticEventCollector.RegisterEventHandler(OnEvent, true, false);
        }

        private void OnDisable()
        {
            DiagnosticEventCollector.RegisterEventHandler(OnEvent, false, false);
            EditorApplication.playModeStateChanged -= OnEditorPlayModeChanged;
        }

        class OperationState
        {
            public int ObjectId;
            public string DisplayName;
            public int ReferenceCount;
            public int[] Dependencies;
        }

        Dictionary<int, OperationState> m_OpStates = new Dictionary<int, OperationState>();
        int m_lastRepaintedFrame =-1;
        public void OnEvent(DiagnosticEvent evt)
        {
            var hash = evt.ObjectId;
            if (evt.Stream == (int)ResourceManager.DiagnosticEventType.AsyncOperationDestroy)
            {
                m_OpStates.Remove(hash);
                m_cacheTree.Reload();
                Repaint();
                return;
            }


            OperationState op;
            if (!m_OpStates.TryGetValue(hash, out op))
            {
                if (evt.Stream != (int)ResourceManager.DiagnosticEventType.AsyncOperationCreate)
                {
                    Debug.LogWarningFormat("Unable to find op info for id {0} - {1}, stream={2}", hash, evt.DisplayName, evt.Stream);
                }
                m_OpStates.Add(hash, op = new OperationState() { ObjectId = evt.ObjectId, DisplayName = evt.DisplayName, Dependencies = evt.Dependencies, ReferenceCount = 0 });
            }

            if (evt.Stream == (int)ResourceManager.DiagnosticEventType.AsyncOperationReferenceCount)
                op.ReferenceCount = evt.Value;


            if (evt.Frame != m_lastRepaintedFrame)
            {
                m_lastRepaintedFrame = evt.Frame;
                m_cacheTree.Reload();
                Repaint();
            }
        }

        void OnEditorPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                DiagnosticEventCollector.RegisterEventHandler(OnEvent, true, false);
            else if (state == PlayModeStateChange.EnteredEditMode)
                DiagnosticEventCollector.RegisterEventHandler(OnEvent, false, false);
        }

        TreeViewState m_EventListTreeViewState;
        CacheDataTree m_cacheTree;

        private void OnGUI()
        {
            var r = EditorGUILayout.GetControlRect();
            Rect contentRect = new Rect(r.x, r.y, r.width, position.height - (r.y + r.x));
            m_cacheTree.OnGUI(contentRect);
        }
    }
}
