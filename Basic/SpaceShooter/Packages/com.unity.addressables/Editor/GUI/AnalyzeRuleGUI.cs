using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;
using TreeViewItem = UnityEditor.IMGUI.Controls.TreeViewItem;

namespace UnityEditor.AddressableAssets.GUI
{
    [Serializable]
    class AnalyzeRuleGUI
    {
        [SerializeField]
        private TreeViewState m_TreeState;

        AssetSettingsAnalyzeTreeView m_Tree;

        internal List<AnalyzeRule> m_Rules = new List<AnalyzeRule>()
        {
            new CheckBundleDupeDependencies(),
            new CheckSceneDupeDependencies()
        };

        internal AddressableAssetSettings Settings { get { return AddressableAssetSettingsDefaultObject.Settings; } }

        private const float k_ButtonHeight = 24f;

        internal string AnalyzeRuleDataFolder
        {
            get { return AddressableAssetSettingsDefaultObject.kDefaultConfigFolder + "/AnalyzeData"; }
        }

        internal string AnalyzeRuleDataName
        {
            get { return "AnalyzeRuleData.asset"; }
        }

        internal string AnalyzeRuleDataPath
        {
            get { return AnalyzeRuleDataFolder +"/"+ AnalyzeRuleDataName; }
        }

        [SerializeField] private AnalyzeResultData m_Data;
        internal AnalyzeResultData AnalyzeData
        {
            get
            { 
                if (m_Data == null)
                {
                    if (!Directory.Exists(AnalyzeRuleDataFolder))
                        Directory.CreateDirectory(AnalyzeRuleDataFolder);

                    if (!File.Exists(AnalyzeRuleDataPath))
                    {
                        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance(typeof(AnalyzeResultData)),
                            AnalyzeRuleDataPath);
                    }

                    m_Data = AssetDatabase.LoadAssetAtPath<AnalyzeResultData>(AnalyzeRuleDataPath);

                    foreach (var rule in m_Rules)
                    {
                        if (!m_Data.Data.ContainsKey(rule.ruleName)) 
                            m_Data.Data.Add(rule.ruleName, new List<AnalyzeRule.AnalyzeResult>());
                    }
                }

                return m_Data;
            }
        }

        public void OnGUI(Rect rect)
        {
            if(m_TreeState == null)
                m_TreeState = new TreeViewState();

            if (m_Tree == null)
            {
                m_Tree = new AssetSettingsAnalyzeTreeView(m_TreeState, this);
                m_Tree.Reload();
            }

            var treeRect = new Rect(rect.xMin, rect.yMin + k_ButtonHeight, rect.width, rect.height - k_ButtonHeight);
            m_Tree.OnGUI(treeRect);

            var buttonRect = new Rect(rect.xMin, rect.yMin, rect.width, rect.height);

            GUILayout.BeginArea(buttonRect);
            GUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!m_Tree.SelectionContainsRuleContainer);
            if (GUILayout.Button("Analyze Selected Rules"))
            {
                EditorApplication.delayCall += () => m_Tree.RunAllSelectedRules();
            }

            if (GUILayout.Button("Clear Selected Rules"))
            {
                EditorApplication.delayCall += () => m_Tree.ClearAllSelectedRules();
            }

            EditorGUI.BeginDisabledGroup(!m_Tree.SelectionContainsFixableRule || !m_Tree.SelectionContainsErrors);
            if (GUILayout.Button("Fix Selected Rules"))
            {
                EditorApplication.delayCall += () => m_Tree.FixAllSelectedRules();
            }
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            //TODO
            //if (GUILayout.Button("Revert Selected"))
            //{
            //    m_Tree.RevertAllActiveRules();
            //}
        }
    }
}
