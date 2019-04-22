using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.AddressableAssets.GUI
{
    [Serializable]
    class AssetSettingsAnalyze
    {
        [SerializeField]
        TreeViewState m_TreeState;
        AssetSettingsAnalyzeTreeView m_Tree;

        [SerializeField]
        List<AnalyzeRule.AnalyzeResult> m_RuleResults;

        internal List<AnalyzeRule.AnalyzeResult> ruleResults
        {
            get { return m_RuleResults; }
        }

        [NonSerialized]
        List<AnalyzeRule> m_Rules;

        List<AnalyzeRule> rules
        {
            get
            {
                if (m_Rules == null || m_Rules.Count == 0)
                {
                    m_Rules = new List<AnalyzeRule>();
                    m_Rules.Add(new CheckDupeDependencies());
                }
                return m_Rules;
            }
        }

        internal void OnGUI(Rect pos, AddressableAssetSettings settings)
        {

            if (m_Tree == null)
            {
                if (m_TreeState == null)
                    m_TreeState = new TreeViewState();

                m_Tree = new AssetSettingsAnalyzeTreeView(m_TreeState, this);
                m_Tree.Reload();
            }


            var buttonHeight = 24f;
            Rect topRect = new Rect(pos.x, pos.y, pos.width, buttonHeight);
            Rect treeRect = new Rect(pos.x, pos.y + buttonHeight, pos.width, pos.height - buttonHeight);

            GUILayout.Space(200);
            GUILayout.BeginArea(topRect);
            GUILayout.BeginHorizontal();
            bool doRun = GUILayout.Button("Run Tests");
         

            bool doFix = GUILayout.Button("Fix All");
            
            if (GUILayout.Button("Clear Results"))
            {
                m_RuleResults = new List<AnalyzeRule.AnalyzeResult>();
                foreach (var r in rules)
                    r.ClearAnalysis();
                m_Tree.Reload();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if(doRun)
            {
                m_RuleResults = new List<AnalyzeRule.AnalyzeResult>();
                foreach (var r in rules)
                    m_RuleResults.AddRange(r.RefreshAnalysis(settings));
                m_Tree.Reload();
            }

            if (doFix)
            {

                m_RuleResults = new List<AnalyzeRule.AnalyzeResult>();
                foreach (var r in rules)
                {
                    r.FixIssues(settings);
                    m_RuleResults.AddRange(r.RefreshAnalysis(settings));
                }

                m_Tree.Reload();
            }

            m_Tree.OnGUI(treeRect);

        }

    }

    class AssetSettingsAnalyzeTreeView : TreeView
    {
        AssetSettingsAnalyze m_AnalyzeSetting;
        internal AssetSettingsAnalyzeTreeView(TreeViewState state, AssetSettingsAnalyze analyzeSetting) : base(state)
        {
            showBorder = true;
            m_AnalyzeSetting = analyzeSetting;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            root.children = new List<TreeViewItem>();
            foreach(var res in m_AnalyzeSetting.ruleResults)
            {
                var resPath = res.resultName.Split(AnalyzeRule.kDelimiter);
                
                var parentNode = root;
                var nameThusFar = string.Empty;
                for (int index = 0; index < resPath.Length; index++)
                {
                    nameThusFar += resPath[index];
                    var hash = nameThusFar.GetHashCode();
                    TreeViewItem currNode = null;
                    foreach (var node in parentNode.children)
                    {
                        if (node.id == hash)
                        {
                            currNode = node;
                            break;
                        }
                    }

                    if (currNode == null)
                    {
                        if (index == resPath.Length - 1)
                        {
                            currNode = new AssetSettingsAnalyzeTreeViewItem(hash, index, resPath[index], res.severity);
                        }
                        else
                        {
                            currNode = new AssetSettingsAnalyzeTreeViewItem(hash, index, resPath[index], MessageType.None);
                        }
                        currNode.children = new List<TreeViewItem>();
                        parentNode.AddChild(currNode);
                    }
                    parentNode = currNode;
                }
            }

            foreach (var node in root.children)
            {
                var analyzeNode = node as AssetSettingsAnalyzeTreeViewItem;
                if (analyzeNode != null)
                    analyzeNode.AddIssueCountToName();
            }


            return root;
        }
        

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as AssetSettingsAnalyzeTreeViewItem;
            if(item != null && item.severity != MessageType.None)
            {
                Texture2D icon = null;
                switch(item.severity)
                {
                    case MessageType.Info:
                        icon = GetInfoIcon();
                        break;
                    case MessageType.Warning:
                        icon = GetWarningIcon();
                        break;
                    case MessageType.Error:
                        icon = GetErrorIcon();
                        break;
                }
                
                UnityEngine.GUI.Label(new Rect(args.rowRect.x + baseIndent, args.rowRect.y, args.rowRect.width - baseIndent, args.rowRect.height), new GUIContent(icon, string.Empty));
            }
            base.RowGUI(args);
        }

        Texture2D m_ErrorIcon;
        Texture2D m_WarningIcon;
        Texture2D m_InfoIcon;

        Texture2D GetErrorIcon()
        {
            if (m_ErrorIcon == null)
                FindMessageIcons();
            return m_ErrorIcon;
        }
        Texture2D GetWarningIcon()
        {
            if (m_WarningIcon == null)
                FindMessageIcons();
            return m_WarningIcon;
        }
        Texture2D GetInfoIcon()
        {
            if (m_InfoIcon == null)
                FindMessageIcons();
            return m_InfoIcon;
        }
        void FindMessageIcons()
        {
            m_ErrorIcon = EditorGUIUtility.FindTexture("console.errorIcon");
            m_WarningIcon = EditorGUIUtility.FindTexture("console.warnicon");
            m_InfoIcon = EditorGUIUtility.FindTexture("console.infoIcon");
        }
    }
    class AssetSettingsAnalyzeTreeViewItem : TreeViewItem
    {
        public MessageType severity { get; set; }
        public AssetSettingsAnalyzeTreeViewItem(int id, int depth, string displayName, MessageType sev) : base(id, depth, displayName)
        {
            severity = sev;
        }

        public int AddIssueCountToName()
        {
            int issueCount = 0;
            if(children != null)
            {
                foreach (var child in children)
                {
                    var analyzeNode = child as AssetSettingsAnalyzeTreeViewItem;
                    if (analyzeNode != null)
                        issueCount += analyzeNode.AddIssueCountToName();
                }
            }

            if (issueCount == 0)
                return 1;

            displayName = displayName + " (" + issueCount + ")";
            return issueCount;
        }

    }
}
