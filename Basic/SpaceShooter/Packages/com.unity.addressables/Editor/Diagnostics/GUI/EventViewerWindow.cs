using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Diagnostics.Data;
using UnityEditor.AddressableAssets.Diagnostics.GUI.Graph;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.ResourceManagement.Diagnostics;
using UnityEngine.Serialization;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine.Networking.PlayerConnection;

namespace UnityEditor.AddressableAssets.Diagnostics.GUI
{
    class EventViewerWindow : EditorWindow, IComparer<EventDataSet>
    {
        EventDataPlayerSessionCollection m_EventData;
        GUIContent m_PrevFrameIcon;
        GUIContent m_NextFrameIcon;
        int m_PlayerSessionIndex;
        int m_InspectFrame;
        bool m_Record = true;
        int m_EventListFrame = -1;
        VerticalSplitter m_VerticalSplitter = new VerticalSplitter();
        HorizontalSplitter m_HorizontalSplitter = new HorizontalSplitter();
        float m_LastEventListUpdate;
        bool m_DraggingInspectLine;
        int m_LatestFrame;

        TreeViewState m_EventListTreeViewState;
        MultiColumnHeaderState m_EventListMchs;
        EventListView m_EventList;

        TreeViewState m_GraphListTreeViewState;
        MultiColumnHeaderState m_GraphListMchs;
        EventGraphListView m_GraphList;

        EventDataPlayerSession activeSession { get { return m_EventData == null ? null : m_EventData.GetSessionByIndex(m_PlayerSessionIndex); } }
        protected virtual bool ShowEventDetailPanel { get { return false; } }
        protected virtual bool ShowEventPanel { get { return false; } }

        void OnEnable()
        {
            EditorConnection.instance.Initialize();
            EditorConnection.instance.Register(DiagnosticEventCollector.PlayerConnectionGuid, OnPlayerConnectionMessage);
            EditorConnection.instance.RegisterConnection(OnPlayerConnection);
            EditorConnection.instance.RegisterDisconnection(OnPlayerDisconnection);

            m_LastEventListUpdate = 0;
            m_PrevFrameIcon = EditorGUIUtility.IconContent("Profiler.PrevFrame", "|Go one frame backwards");
            m_NextFrameIcon = EditorGUIUtility.IconContent("Profiler.NextFrame", "|Go one frame forwards");
            EditorApplication.playModeStateChanged += OnEditorPlayModeChanged;
            RegisterEventHandler(true);
            if (m_EventData == null)
                m_EventData = new EventDataPlayerSessionCollection(OnRecordEvent);
            m_EventData.GetPlayerSession(0, true).IsActive = true;
        }

        void OnDisable()
        {
            EditorConnection.instance.Unregister(DiagnosticEventCollector.PlayerConnectionGuid, OnPlayerConnectionMessage);
            RegisterEventHandler(false);
            EditorApplication.playModeStateChanged -= OnEditorPlayModeChanged;
        }

        void OnPlayerConnection(int id)
        {
            if (m_EventData == null)
                m_EventData = new EventDataPlayerSessionCollection(OnRecordEvent);
            m_EventData.GetPlayerSession(id, true).IsActive = true;
        }

        void OnPlayerDisconnection(int id)
        {
            if (m_EventData == null)
                return;
            var playerSession = m_EventData.GetPlayerSession(id, false);
            if (playerSession != null)
                playerSession.IsActive = false;
        }

        void OnPlayerConnectionMessage(MessageEventArgs args)
        {
            var evt = DiagnosticEvent.Deserialize(args.data);
            OnEvent(evt, args.playerId);
        }

        void RegisterEventHandler(bool reg)
        {
            DiagnosticEventCollector.RegisterEventHandler(OnEditorPlayModeEvent, reg, false);
        }

        void OnEditorPlayModeEvent(DiagnosticEvent evt)
        {
            if (m_EventData == null)
                m_EventData = new EventDataPlayerSessionCollection(OnRecordEvent);
            if (m_EventData.GetPlayerSession(0, false) == null)
                m_EventData.AddSession("Editor", m_PlayerSessionIndex = 0);
            OnEvent(evt, 0);
        }

        public void OnEvent(DiagnosticEvent diagnosticEvent, int session)
        {
            if (m_EventData == null)
                m_EventData = new EventDataPlayerSessionCollection(OnRecordEvent);

            var entryCreated = m_EventData.ProcessEvent(diagnosticEvent, session);
            OnEventProcessed(diagnosticEvent, entryCreated);
        }


        public int Compare(EventDataSet x, EventDataSet y)
        {
            return x.CompareTo(y);
        }

        protected virtual bool CanHandleEvent(string graph)
        {
            if (graph.Contains("Count"))
                return true;
            return OnCanHandleEvent(graph);
        }

        protected virtual bool OnCanHandleEvent(string graph) { return true; }

        void OnEditorPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                m_EventData = new EventDataPlayerSessionCollection(OnRecordEvent);
                m_EventData.GetPlayerSession(0, true).IsActive = true;

                m_LastEventListUpdate = 0;
                m_InspectFrame = -1;
                m_LatestFrame = -1;
                m_PlayerSessionIndex = 0;
                RegisterEventHandler(true);
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                RegisterEventHandler(false);
            }
        }

        protected virtual bool OnDisplayEvent(DiagnosticEvent diagnosticEvent)
        {
            var sel = m_GraphList.GetSelection();
            if (sel == null || sel.Count == 0)
                return true;
            foreach (var s in sel)
            {
//                m_GraphList.Fi
            }
            
            return true;
        }

        protected virtual bool OnRecordEvent(DiagnosticEvent diagnosticEvent)
        {
            return false;
        }

        int m_lastRepaintedFrame = -1;
        void OnEventProcessed(DiagnosticEvent diagnosticEvent, bool entryCreated)
        {
            if (!CanHandleEvent(diagnosticEvent.Graph))
                return;

            bool moveInspectFrame = m_LatestFrame < 0 || m_InspectFrame == m_LatestFrame;
            m_LatestFrame = diagnosticEvent.Frame;
            if (entryCreated)
            {
                if (m_GraphList != null)
                    m_GraphList.Reload();
            }

            if (moveInspectFrame)
                SetInspectFrame(m_LatestFrame);

            if (diagnosticEvent.Frame != m_lastRepaintedFrame)
            {
                Repaint();
                m_lastRepaintedFrame = diagnosticEvent.Frame;
            }
        }

        void SetInspectFrame(int frame)
        {
            m_InspectFrame = frame;
            if (m_InspectFrame > m_LatestFrame)
                m_InspectFrame = m_LatestFrame;
            if (m_InspectFrame < 0)
                m_InspectFrame = 0;

            if(m_EventList != null)
                m_EventList.SetEvents(activeSession == null ? null : activeSession.GetFrameEvents(m_InspectFrame));
            m_LastEventListUpdate = Time.unscaledTime;
            m_EventListFrame = m_InspectFrame;
        }

        void OnGUI()
        {
            if (activeSession == null)
                return;
            InitializeGui();

            //this prevent arrow key events from reaching the treeview, so navigation via keys is disabled
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.RightArrow)
                {
                    SetInspectFrame(m_InspectFrame + 1);
                    return;
                }
                if (Event.current.keyCode == KeyCode.LeftArrow)
                {
                    SetInspectFrame(m_InspectFrame - 1);
                    return;
                }
            }

            DrawToolBar(activeSession);

            var r = EditorGUILayout.GetControlRect();
            Rect contentRect = new Rect(r.x, r.y, r.width, position.height - (r.y + r.x));
            var graphRect = m_GraphList.GraphRect;
            if (ShowEventPanel)
            {
                Rect top, bot;
                bool resizingVer = m_VerticalSplitter.OnGUI(contentRect, out top, out bot);

                ProcessInspectFrame(graphRect);

                m_GraphList.DrawGraphs(top, activeSession, m_InspectFrame);

                DrawInspectFrame(graphRect);

                bool resizingHor = false;
                if (ShowEventDetailPanel)
                {
                    Rect left, right;
                    resizingHor = m_HorizontalSplitter.OnGUI(bot, out left, out right);
                    m_EventList.OnGUI(left);
                    OnDrawEventDetail(right, m_EventList.selectedEvent);
                }
                else
                {
                    m_EventList.OnGUI(bot);
                }
                if (resizingVer || resizingHor)
                    Repaint();
            }
            else
            {
                ProcessInspectFrame(graphRect);
                m_GraphList.DrawGraphs(contentRect, activeSession, m_InspectFrame);
                DrawInspectFrame(graphRect);
            }
        }

        protected virtual void OnDrawEventDetail(Rect right, DiagnosticEvent selectedEvent)
        {
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void ProcessInspectFrame(Rect graphRect)
        {
            if (Event.current.type == EventType.MouseDown && graphRect.Contains(Event.current.mousePosition))
            {
                if (EditorApplication.isPlaying)
                    EditorApplication.isPaused = true;
                m_DraggingInspectLine = true;
                SetInspectFrame(m_InspectFrame);
            }

            if (m_DraggingInspectLine && (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.Repaint))
                SetInspectFrame(m_GraphList.visibleStartTime + (int)GraphUtility.PixelToValue(Event.current.mousePosition.x, graphRect.xMin, graphRect.xMax, m_GraphList.visibleDuration));
            if (Event.current.type == EventType.MouseUp)
            {
                m_DraggingInspectLine = false;
                SetInspectFrame(m_InspectFrame);
            }
        }

        void DrawInspectFrame(Rect graphPanelRect)
        {
            if (m_InspectFrame != m_LatestFrame)
            {
                var ix = graphPanelRect.xMin + GraphUtility.ValueToPixel(m_InspectFrame, m_GraphList.visibleStartTime, m_GraphList.visibleStartTime + m_GraphList.visibleDuration, graphPanelRect.width);
                EditorGUI.DrawRect(new Rect(ix - 1, graphPanelRect.yMin, 3, graphPanelRect.height), Color.white * .8f);
            }
        }

        void DrawToolBar(EventDataPlayerSession session)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            m_Record = GUILayout.Toggle(m_Record, "Record", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
            {
                RegisterEventHandler(false);
                session.Clear();
                if (m_GraphList != null)
                    m_GraphList.Reload();
                RegisterEventHandler(true);
            }
            
            /*
            if (GUILayout.Button("Load", EditorStyles.toolbarButton))
                EditorUtility.DisplayDialog("Feature not implemented", "Saving and loading profile data is not yet supported", "Close");
            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                EditorUtility.DisplayDialog("Feature not implemented", "Saving and loading profile data is not yet supported", "Close");
             */

            GUILayout.FlexibleSpace();
            GUILayout.Label(m_InspectFrame == m_LatestFrame ? "Frame:     " : "Frame: " + m_InspectFrame + "/" + m_LatestFrame, EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(m_InspectFrame <= 0))
                if (GUILayout.Button(m_PrevFrameIcon, EditorStyles.toolbarButton))
                    SetInspectFrame(m_InspectFrame - 1);


            using (new EditorGUI.DisabledScope(m_InspectFrame >= m_LatestFrame))
                if (GUILayout.Button(m_NextFrameIcon, EditorStyles.toolbarButton))
                    SetInspectFrame(m_InspectFrame + 1);


            if (GUILayout.Button("Current", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                SetInspectFrame(m_LatestFrame);

            GUILayout.EndHorizontal();
        }

        protected virtual void OnGetColumns(List<string> columnNames, List<float> columnSizes)
        {
            if (columnNames == null || columnSizes == null)
                return;
            columnNames.Add("Event"); columnSizes.Add(50);
            columnNames.Add("Id"); columnSizes.Add(200);
    //        columnNames.Add("Data"); columnSizes.Add(400);
        }

        protected virtual bool OnDrawColumnCell(Rect cellRect, DiagnosticEvent diagnosticEvent, int column)
        {
            return false;
        }

        protected virtual void DrawColumnCell(Rect cellRect, DiagnosticEvent diagnosticEvent, int column)
        {
            if (!OnDrawColumnCell(cellRect, diagnosticEvent, column))
            {
                switch (column)
                {
                    case 0: EditorGUI.LabelField(cellRect, diagnosticEvent.Stream.ToString()); break;
                    case 1: EditorGUI.LabelField(cellRect, diagnosticEvent.DisplayName); break;
//                    case 2: EditorGUI.LabelField(cellRect, diagnosticEvent. == null ? "null" : diagnosticEvent.Data.ToString()); break;
                }
            }
        }

        void InitializeGui()
        {
            if (m_GraphList == null)
            {
                if (m_GraphListTreeViewState == null)
                    m_GraphListTreeViewState = new TreeViewState();

                var headerState = EventGraphListView.CreateDefaultHeaderState();
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_GraphListMchs, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_GraphListMchs, headerState);

                m_GraphListMchs = headerState;
                m_GraphList = new EventGraphListView(()=> { return activeSession == null ? null : activeSession.RootStreamEntry; }, m_GraphListTreeViewState, m_GraphListMchs, CanHandleEvent, this);
                InitializeGraphView(m_GraphList);
                m_GraphList.Reload();
            }

            if (m_EventList == null)
            {
                if (m_EventListTreeViewState == null)
                    m_EventListTreeViewState = new TreeViewState();

                var columns = new List<string>();
                var sizes = new List<float>();
                OnGetColumns(columns, sizes);
                var headerState = EventListView.CreateDefaultMultiColumnHeaderState(columns, sizes);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_EventListMchs, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_EventListMchs, headerState);

                m_EventListMchs = headerState;
                m_EventList = new EventListView(m_EventListTreeViewState, m_EventListMchs, DrawColumnCell, OnDisplayEvent);
                m_EventList.Reload();
            }

            if (m_EventListFrame != m_InspectFrame && m_InspectFrame != m_LatestFrame && !m_DraggingInspectLine && Time.unscaledTime - m_LastEventListUpdate > .25f)
            {
                if(activeSession != null)
                    m_EventList.SetEvents(activeSession.GetFrameEvents(m_InspectFrame));
                m_LastEventListUpdate = Time.unscaledTime;
                m_EventListFrame = m_InspectFrame;
            }
            
            if (m_GraphListMchs != null && m_GraphListMchs.columns.Length > 2)
            {
                string warningText = string.Empty;
                if (!ProjectConfigData.postProfilerEvents)
                    warningText = "Warning: Profile events must be enabled in your Addressable Asset settings to view profile data";
                    m_GraphListMchs.columns[2].headerContent.text = warningText;
            }
        }

        void InitializeGraphView(EventGraphListView graphView)
        {
            graphView.DefineGraph("EventCount", 0, new GraphLayerVertValueLine(0, "Events", "Event count per frame", Color.green));
            graphView.DefineGraph("FrameCount", 1, new GraphLayerBarChartMesh(1, "FPS", "Current Frame Rate", Color.blue),
                                new GraphLayerLabel(1, "FPS", "Current Frame Rate", Color.white, GraphColors.LabelGraphLabelBackground, v => string.Format("{0} FPS", v)));
            graphView.DefineGraph("MemoryCount", 2, new GraphLayerBarChartMesh(2, "MonoHeap", "Current Mono Heap Size", Color.green * .75f),
                                new GraphLayerLabel(2, "MonoHeap", "Current Mono Heap Size", Color.white, GraphColors.LabelGraphLabelBackground, v => string.Format("{0:0.0}MB", (v/1024f))));

            OnInitializeGraphView(graphView);
        }

        protected virtual void OnInitializeGraphView(EventGraphListView graphView) { }
    }

    [Serializable]
    class VerticalSplitter
    {
        [NonSerialized]
        Rect m_Rect = new Rect(0, 0, 0, 3);
        public Rect SplitterRect { get { return m_Rect; } }
        [FormerlySerializedAs("m_currentPercent")]
        [SerializeField]
        float m_CurrentPercent;

        bool m_Resizing;
        float m_MinPercent;
        float m_MaxPercent;
        public VerticalSplitter(float percent = .8f, float minPer = .2f, float maxPer = .9f)
        {
            m_CurrentPercent = percent;
            m_MinPercent = minPer;
            m_MaxPercent = maxPer;
        }

        public bool OnGUI(Rect content, out Rect top, out Rect bot)
        {
            m_Rect.x = content.x;
            m_Rect.y = (int)(content.y + content.height * m_CurrentPercent);
            m_Rect.width = content.width;

            EditorGUIUtility.AddCursorRect(m_Rect, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.MouseDown && m_Rect.Contains(Event.current.mousePosition))
                m_Resizing = true;

            if (m_Resizing)
            {
                EditorGUIUtility.AddCursorRect(content, MouseCursor.ResizeVertical);

                var mousePosInRect = Event.current.mousePosition.y - content.y;
                m_CurrentPercent = Mathf.Clamp(mousePosInRect / content.height, m_MinPercent, m_MaxPercent);
                m_Rect.y = Mathf.Min((int)(content.y + content.height * m_CurrentPercent), content.yMax - m_Rect.height);
                if (Event.current.type == EventType.MouseUp)
                    m_Resizing = false;
            }

            top = new Rect(content.x, content.y, content.width, m_Rect.yMin - content.yMin);
            bot = new Rect(content.x, m_Rect.yMax, content.width, content.yMax - m_Rect.yMax);
            return m_Resizing;
        }
    }

    [Serializable]
    class HorizontalSplitter
    {
        [NonSerialized]
        Rect m_Rect = new Rect(0, 0, 3, 0);
        public Rect SplitterRect { get { return m_Rect; } }
        [FormerlySerializedAs("m_currentPercent")]
        [SerializeField]
        float m_CurrentPercent;

        bool m_Resizing;
        float m_MinPercent;
        float m_MaxPercent;
        public HorizontalSplitter(float percent = .8f, float minPer = .2f, float maxPer = .9f)
        {
            m_CurrentPercent = percent;
            m_MinPercent = minPer;
            m_MaxPercent = maxPer;
        }

        public bool OnGUI(Rect content, out Rect left, out Rect right)
        {
            m_Rect.y = content.y;
            m_Rect.x = (int)(content.x + content.width * m_CurrentPercent);
            m_Rect.height = content.height;

            EditorGUIUtility.AddCursorRect(m_Rect, MouseCursor.ResizeHorizontal);
            if (Event.current.type == EventType.MouseDown && m_Rect.Contains(Event.current.mousePosition))
                m_Resizing = true;

            if (m_Resizing)
            {
                EditorGUIUtility.AddCursorRect(content, MouseCursor.ResizeHorizontal);

                var mousePosInRect = Event.current.mousePosition.x - content.x;
                m_CurrentPercent = Mathf.Clamp(mousePosInRect / content.width, m_MinPercent, m_MaxPercent);
                m_Rect.x = Mathf.Min((int)(content.x + content.width * m_CurrentPercent), content.xMax - m_Rect.width);
                if (Event.current.type == EventType.MouseUp)
                    m_Resizing = false;
            }

            left = new Rect(content.x, content.y, m_Rect.xMin, content.height);
            right = new Rect(m_Rect.xMax, content.y, content.width - m_Rect.xMax, content.height);
            return m_Resizing;
        }
    }
}
