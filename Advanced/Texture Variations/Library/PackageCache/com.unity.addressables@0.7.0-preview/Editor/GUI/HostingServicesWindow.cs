using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.HostingServices;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace UnityEditor.AddressableAssets.GUI
{
    /// <summary>
    /// Configuration GUI for <see cref="T:UnityEditor.AddressableAssets.HostingServices.HostingServicesManager" />
    /// </summary>
    public class HostingServicesWindow : EditorWindow, ISerializationCallbackReceiver, ILogHandler
    {
        const float k_DefaultSplitterRatio = 0.67f;
        const int k_SplitterHeight = 15;

        [FormerlySerializedAs("m_logText")]
        [SerializeField]
        string m_LogText;
        [FormerlySerializedAs("m_logScrollPos")]
        [SerializeField]
        Vector2 m_LogScrollPos;
        [FormerlySerializedAs("m_servicesScrollPos")]
        [SerializeField]
        Vector2 m_ServicesScrollPos;
        [FormerlySerializedAs("m_profileVarsFoldout")]
        [SerializeField]
        bool m_ProfileVarsFoldout = true;
        [FormerlySerializedAs("m_servicesFoldout")]
        [SerializeField]
        bool m_ServicesFoldout = true;
        [FormerlySerializedAs("m_splitterRatio")]
        [SerializeField]
        float m_SplitterRatio = k_DefaultSplitterRatio;
        [FormerlySerializedAs("m_settings")]
        [SerializeField]
        AddressableAssetSettings m_Settings;

        ILogger m_Logger;
        bool m_NewLogContent;
        bool m_IsResizingSplitter;

        readonly Dictionary<object, HostingServicesProfileVarsTreeView> m_ProfileVarTables =
            new Dictionary<object, HostingServicesProfileVarsTreeView>();

        readonly List<IHostingService> m_RemovalQueue = new List<IHostingService>();
        HostingServicesProfileVarsTreeView m_GlobalProfileVarTable;

        /// <summary>
        /// Show the <see cref="HostingServicesWindow"/>, initialized with the given <see cref="AddressableAssetSettings"/>
        /// </summary>
        /// <param name="settings"></param>
        public void Show(AddressableAssetSettings settings)
        {
            Initialize(settings);
            Show();
        }

        void Initialize(AddressableAssetSettings settings)
        {
            if (m_Settings == null)
                m_Settings = settings;

            m_Settings.HostingServicesManager.Logger = m_Logger;
        }

        [MenuItem("Window/Asset Management/Hosting Services", priority = 2052)]
        static void InitializeWithDefaultSettings()
        {
            var defaultSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (defaultSettings == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not load default Addressable Asset settings.", "Ok");
                return;
            }

            GetWindow<HostingServicesWindow>().Show(defaultSettings);
        }

        void Awake()
        {
            titleContent = new GUIContent("Hosting");
            m_Logger = new Logger(this);
        }

        void OnGUI()
        {
            if (m_Settings == null) return;

            if (m_IsResizingSplitter)
                m_SplitterRatio = Mathf.Clamp((Event.current.mousePosition.y - k_SplitterHeight / 2f) / position.height, 0.2f, 0.9f);

            var itemRect = new Rect(0, 0, position.width, position.height * m_SplitterRatio);
            var splitterRect = new Rect(0, itemRect.height, position.width, k_SplitterHeight);
            var logRect = new Rect(0, itemRect.height + k_SplitterHeight, position.width,
                position.height - itemRect.height - k_SplitterHeight);

            EditorGUI.LabelField(splitterRect, string.Empty, UnityEngine.GUI.skin.horizontalSlider);
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeVertical);
            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
                m_IsResizingSplitter = true;
            else if (Event.current.type == EventType.MouseUp)
                m_IsResizingSplitter = false;

            GUILayout.BeginArea(itemRect);
            {
                EditorGUILayout.Space();

                m_ProfileVarsFoldout = EditorGUILayout.Foldout(m_ProfileVarsFoldout, "Global Profile Variables");
                if (m_ProfileVarsFoldout)
                    DrawGlobalProfileVarsArea();

                EditorGUILayout.Space();

                m_ServicesFoldout = EditorGUILayout.Foldout(m_ServicesFoldout, "Hosting Services");
                if (m_ServicesFoldout)
                    DrawServicesArea();
            }
            GUILayout.EndArea();

            GUILayout.BeginArea(logRect);
            {
                DrawLogArea(logRect);
            }
            GUILayout.EndArea();

            if (m_IsResizingSplitter)
                Repaint();
        }

        void DrawGlobalProfileVarsArea()
        {
            var manager = m_Settings.HostingServicesManager;
            DrawProfileVarTable(this, manager.GlobalProfileVariables);

            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh", GUILayout.MaxWidth(125f)))
                    manager.RefreshGlobalProfileVariables();
            }
            GUILayout.EndHorizontal();
        }

        void DrawServicesArea()
        {
            var manager = m_Settings.HostingServicesManager;
            m_ServicesScrollPos = EditorGUILayout.BeginScrollView(m_ServicesScrollPos);
            var svcList = manager.HostingServices;

            if (m_RemovalQueue.Count > 0)
            {
                foreach (var svc in m_RemovalQueue)
                    manager.RemoveHostingService(svc);

                m_RemovalQueue.Clear();
            }

            var i = 0;
            foreach (var svc in svcList)
            {
                if (i > 0) EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawServiceElement(svc);
                EditorGUILayout.EndVertical();
                i++;
            }

            GUILayout.BeginHorizontal();
            {
                if (svcList.Count == 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("No Hosting Services configured.");
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add Service...", GUILayout.MaxWidth(125f)))
                {
                    GetWindow<HostingServicesAddServiceWindow>(true, "Add Service").Initialize(m_Settings);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();
        }

        void DrawServiceElement(IHostingService svc)
        {
            EditorGUILayout.BeginHorizontal();
            {
                svc.DescriptiveName = EditorGUILayout.DelayedTextField("Service EventName", svc.DescriptiveName);

                var newIsServiceEnabled = GUILayout.Toggle(svc.IsHostingServiceRunning, "Enable Service", "Button", GUILayout.MaxWidth(150f));

                if (GUILayout.Button("Remove...", GUILayout.MaxWidth(75f)))
                {
                    if (EditorUtility.DisplayDialog("Remove Service", "Are you sure?", "Ok", "Cancel"))
                        m_RemovalQueue.Add(svc);
                }
                else if (newIsServiceEnabled != svc.IsHostingServiceRunning)
                {
                    if (newIsServiceEnabled)
                        svc.StartHostingService();
                    else
                        svc.StopHostingService();
                }
            }
            EditorGUILayout.EndHorizontal();

            var typeAndId = string.Format("{0} ({1})", svc.GetType().Name, svc.InstanceId.ToString());
            EditorGUILayout.LabelField("Service Type (ID)", typeAndId, GUILayout.MinWidth(225f));

            EditorGUILayout.Space();
            
            using (new EditorGUI.DisabledGroupScope(!svc.IsHostingServiceRunning))
            {
                // Allow service to provide additional GUI configuration elements
                svc.OnGUI();

                EditorGUILayout.Space();

                DrawProfileVarTable(svc, svc.ProfileVariables);
            }
        }

        void DrawLogArea(Rect rect)
        {
            if (m_NewLogContent)
            {
                var height = UnityEngine.GUI.skin.GetStyle("Label").CalcHeight(new GUIContent(m_LogText), rect.width);
                m_LogScrollPos = new Vector2(0f, height);
                m_NewLogContent = false;
            }

            m_LogScrollPos = EditorGUILayout.BeginScrollView(m_LogScrollPos);
            GUILayout.Label(m_LogText);
            EditorGUILayout.EndScrollView();
        }

        void DrawProfileVarTable(object tableKey, IEnumerable<KeyValuePair<string, string>> data)
        {
            HostingServicesProfileVarsTreeView table;
            if (!m_ProfileVarTables.TryGetValue(tableKey, out table))
            {
                table = new HostingServicesProfileVarsTreeView(new TreeViewState(),
                    HostingServicesProfileVarsTreeView.CreateHeader());
                m_ProfileVarTables[tableKey] = table;
            }

            var rowHeight = table.RowHeight;
            var tableHeight = table.multiColumnHeader.height + rowHeight; // header + 1 extra line

            table.ClearItems();
            foreach (var kvp in data)
            {
                table.AddOrUpdateItem(kvp.Key, kvp.Value);
                tableHeight += rowHeight;
            }

            table.OnGUI(EditorGUILayout.GetControlRect(false, tableHeight));
        }

        /// <inheritdoc/>
        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            IHostingService svc = null;

            if (args.Length > 0)
                svc = args[args.Length - 1] as IHostingService;

            if (svc != null)
            {
                m_LogText += string.Format("[{0}] ", svc.DescriptiveName) + string.Format(format, args) + "\n";
                m_NewLogContent = true;
            }

            Debug.unityLogger.LogFormat(logType, context, format, args);
        }

        /// <inheritdoc/>
        public void LogException(Exception exception, Object context)
        {
            Debug.unityLogger.LogException(exception, context);
        }

        /// <inheritdoc/>
        public void OnBeforeSerialize()
        {
            // No implementation
        }

        /// <inheritdoc/>
        public void OnAfterDeserialize()
        {
            m_Logger = new Logger(this);
            Initialize(m_Settings);
        }
    }
}