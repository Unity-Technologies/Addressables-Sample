using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Diagnostics.Data;
using UnityEditor.AddressableAssets.Diagnostics.GUI.Graph;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Diagnostics.GUI
{
    class EventGraphListView : TreeView
    {
        class DataStreamEntry : TreeViewItem
        {
            public GUIContent Content { get; private set; }
            public EventDataSet Entry { get; private set; }
            public DataStreamEntry(EventDataSet dataSet, int depth) : base(dataSet.ObjectId, depth, dataSet.DisplayName)
            {
                Entry = dataSet;
                Content = new GUIContent(dataSet.DisplayName);
            }
        }
        Dictionary<int, bool> m_MaximizedState = new Dictionary<int, bool>();
        Func<string, bool> m_FilterFunc;
        IComparer<EventDataSet> m_DataSetComparer;
        Material m_GraphMaterial;
        Dictionary<string, GraphDefinition> m_GraphDefinitions = new Dictionary<string, GraphDefinition>();
        GUIContent m_PlusGUIContent = new GUIContent("+", "Expand");
        GUIContent m_MinusGUIContent = new GUIContent("-", "Collapse");
        Func<EventDataSet> m_GetRootDataSetAction;

        float m_LastReloadTime;
        int m_InspectFrame = -1;
        internal int visibleStartTime { get; private set; }
        internal int visibleDuration { get; private set; }

        public Rect GraphRect
        {
            get
            {
                return new Rect(treeViewRect.x + (multiColumnHeader.state.columns[1].width + multiColumnHeader.state.columns[0].width), treeViewRect.y,
                    multiColumnHeader.state.columns[2].width, treeViewRect.height);
            }
        }

        internal EventGraphListView(Func<EventDataSet> dsFunc,  TreeViewState tvs, MultiColumnHeaderState mchs, Func<string, bool> filter, IComparer<EventDataSet> dsComparer) : base(tvs, new MultiColumnHeader(mchs))
        {
            m_GetRootDataSetAction = dsFunc;
            showBorder = true;
            visibleStartTime = 0;
            visibleDuration = 300;
            m_DataSetComparer = dsComparer;
            m_FilterFunc = filter;
            columnIndexForTreeFoldouts = 1;
        }

        protected override TreeViewItem BuildRoot()
        {
            var rootDS = m_GetRootDataSetAction();
            if (rootDS == null)
                return new TreeViewItem();
            return AddItems(new DataStreamEntry(rootDS, -1));
        }

        DataStreamEntry AddItems(DataStreamEntry root)
        {
            root.children = new List<TreeViewItem>();
            if (!root.Entry.HasChildren)
                return root;

            List<EventDataSet> entries = new List<EventDataSet>();
            foreach (var e in root.Entry.Children)
            {
                if (!e.HasDataAfterFrame(visibleStartTime))
                    continue;
                if (m_FilterFunc(e.Graph))
                    entries.Add(e);
            }

            if (m_DataSetComparer != null)
                entries.Sort(m_DataSetComparer);

            foreach (var e in entries)
            {
                var dse = new DataStreamEntry(e, root.depth + 1);
                root.AddChild(dse);
                AddItems(dse);
            }

            return root;
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            if (item == null)
                return 0;
            return IsItemMaximized(item.id) ? 100 : base.GetCustomRowHeight(row, item);
        }

        public void DrawGraphs(Rect rect, EventDataPlayerSession data, int inspectFrame)
        {
            EditorGUI.DrawRect(GraphRect, GraphColors.WindowBackground);
            if (data != null)
            {
                m_InspectFrame = inspectFrame;
                if (Event.current.type == EventType.Repaint)
                    multiColumnHeader.state.columns[2].width = rect.width - (multiColumnHeader.state.columns[1].width + multiColumnHeader.state.columns[0].width + 20);

                visibleDuration = Mathf.Max(300, (int)(multiColumnHeader.state.columns[2].width));
                if (data.IsActive)
                    visibleStartTime = data.LatestFrame - visibleDuration;
                if (Time.unscaledTime - m_LastReloadTime > 1 && (EditorApplication.isPlaying || data.PlayerId != 0))
                {
                    Reload();
                    m_LastReloadTime = Time.unscaledTime;
                }
            }
            base.OnGUI(rect);
        }


        bool IsItemMaximized(int id)
        {
            bool expanded;
            m_MaximizedState.TryGetValue(id, out expanded);
            return expanded;
        }

        void ToggleItemMaximize(int id)
        {
            bool expanded = IsItemMaximized(id);
            m_MaximizedState[id] = !expanded;
            Reload();
            m_LastReloadTime = Time.unscaledTime;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                CellGUI(args.GetCellRect(i), args.item as DataStreamEntry, args.GetColumn(i));
        }

        void CellGUI(Rect cellRect, DataStreamEntry item, int column)
        {
            switch (column)
            {
                case 0:
                    {
                        var maximized = IsItemMaximized(item.id);
                        if (UnityEngine.GUI.Button(cellRect, maximized ? m_MinusGUIContent : m_PlusGUIContent, EditorStyles.toolbarButton))
                        {
                            if (!IsSelected(item.id))
                            {
                                m_MaximizedState[item.id] = !maximized;
                            }
                            else
                            {
                                foreach (var i in GetSelection())
                                    m_MaximizedState[i] = !maximized;
                            }
                            Reload();
                            m_LastReloadTime = Time.unscaledTime;
                        }
                    }
                    break;
                case 1:
                    {
                        cellRect.xMin += (GetContentIndent(item) + extraSpaceBeforeIconAndLabel);
                        EditorGUI.LabelField(cellRect, item.Content);
                    }
                    break;
                case 2:
                    DrawGraph(item.Entry, cellRect, visibleStartTime, visibleDuration, IsItemMaximized(item.id));
                    break;
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            ToggleItemMaximize(id);
            base.DoubleClickedItem(id);
        }

        public void DefineGraph(string name, int maxValueStream, params IGraphLayer[] layers)
        {
            m_GraphDefinitions.Add(name, new GraphDefinition(maxValueStream, layers));
        }

        void DrawGraph(EventDataSet dataSet, Rect rect, int startTime, int duration, bool expanded)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            rect = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);
            GraphDefinition gd;
            if (!m_GraphDefinitions.TryGetValue(dataSet.Graph, out gd))
                return;

            if (m_GraphMaterial == null)
            {
                // best material options are "Unlit/Color" or "UI/Default". 
                //  Unlit/Color is more efficient, but does not support alpha
                //  UI/Default does support alpha
                m_GraphMaterial = new Material(Shader.Find("Unlit/Color"));
            }

            int maxValue = gd.GetMaxValue(dataSet);

            foreach (var l in gd.layers)
                l.Draw(dataSet, rect, startTime, duration, m_InspectFrame, expanded, m_GraphMaterial, maxValue);
        }

        public static MultiColumnHeaderState CreateDefaultHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column()
            };

            columns[0].headerContent = new GUIContent("", "Expand");
            columns[0].minWidth = 24;
            columns[0].width = 24;
            columns[0].maxWidth = 24;
            columns[0].headerTextAlignment = TextAlignment.Center;
            columns[0].canSort = false;
            columns[0].autoResize = false;

            columns[1].headerContent = new GUIContent("Assets", "");
            columns[1].minWidth = 100;
            columns[1].width = 250;
            columns[1].maxWidth = 500;
            columns[1].headerTextAlignment = TextAlignment.Left;
            columns[1].canSort = false;
            columns[1].autoResize = false;

            columns[2].headerContent = new GUIContent("", "");
            columns[2].minWidth = 100;
            columns[2].width = 1000;
            columns[2].maxWidth = 10000;
            columns[2].headerTextAlignment = TextAlignment.Left;
            columns[2].canSort = false;
            columns[2].autoResize = false;

            return new MultiColumnHeaderState(columns);
        }
    }
}
