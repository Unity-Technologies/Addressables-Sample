using System.Collections;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.AddressableAssets.GUI
{
    public class AnalyzeWindow : EditorWindow
    {


        private AddressableAssetSettings m_Settings;

        [SerializeField]
        private AnalyzeRuleGUI analyzeEditor;

        private Rect m_ToolbarRect
        {
            get
            {
                return new Rect(0, 0,
                    position.width,
                    position.height * Mathf.Clamp((Event.current.mousePosition.y / 2f) / position.height, 0.2f, 0.9f));
            }
        }

        private Rect m_DisplayAreaRect
        {
            get
            {
                return new Rect(0, 0, position.width, position.height);
            }
        }

        [MenuItem("Window/Asset Management/Analyze", priority = 2052)]
        internal static void Initialize()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Unable to load Addressable Asset Settings default object.");
                return;
            }

            GetWindow<AnalyzeWindow>().Show(true);
        }

        void OnEnable()
        {
            analyzeEditor = new AnalyzeRuleGUI();
            titleContent = new GUIContent("Analyze");
        }

        void OnGUI() 
        {
            GUILayout.BeginArea(m_DisplayAreaRect);
            analyzeEditor.OnGUI(m_DisplayAreaRect);
            GUILayout.EndArea();
        }
    }
}