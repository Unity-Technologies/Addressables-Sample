using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.AddressableAssets.GUI
{
    class AssetSettingsAnalyzeTreeView : TreeView
    {
        AnalyzeRuleGUI m_AnalyzeSetting;
        private AnalyzeResultData resultData;
        private int m_CurrentDepth;

        internal AssetSettingsAnalyzeTreeView(TreeViewState state, AnalyzeRuleGUI analyzeSetting)
            : base(state)
        {
            m_AnalyzeSetting = analyzeSetting;
            resultData = m_AnalyzeSetting.AnalyzeData;
            showAlternatingRowBackgrounds = true;
            showBorder = true;

            Reload();
        }

        private List<AnalyzeRuleContainerTreeViewItem> GatherAllInheritRuleContainers(TreeViewItem baseContainer)
        {
            List<AnalyzeRuleContainerTreeViewItem> retValue = new List<AnalyzeRuleContainerTreeViewItem>();
            if (!baseContainer.hasChildren)
                return new List<AnalyzeRuleContainerTreeViewItem>();

            foreach (var child in baseContainer.children)
            {
                if (child is AnalyzeRuleContainerTreeViewItem)
                {
                    retValue.AddRange(GatherAllInheritRuleContainers(child as AnalyzeRuleContainerTreeViewItem));
                    retValue.Add(child as AnalyzeRuleContainerTreeViewItem);
                }
            }

            return retValue;
        }

        private void PerformActionForEntireRuleSelection(Action<AnalyzeRuleContainerTreeViewItem> action)
        {
            List<AnalyzeRuleContainerTreeViewItem> activeSelection = (from id in GetSelection()
                let selection = FindItem(id, rootItem)
                where selection is AnalyzeRuleContainerTreeViewItem
                select selection as AnalyzeRuleContainerTreeViewItem).ToList();

            List<AnalyzeRuleContainerTreeViewItem> inheritSelection = new List<AnalyzeRuleContainerTreeViewItem>();
            foreach (var selected in activeSelection)
                inheritSelection.AddRange(GatherAllInheritRuleContainers(selected));

            List<AnalyzeRuleContainerTreeViewItem> entireSelection = activeSelection.Union(inheritSelection).ToList();

            foreach (AnalyzeRuleContainerTreeViewItem ruleContainer in entireSelection)
            {
                if (ruleContainer.analyzeRule != null)
                {
                    action(ruleContainer);
                }
            }
        }

        public void RunAllSelectedRules()
        {
            PerformActionForEntireRuleSelection((ruleContainer) =>
            {
                if (resultData.Data.ContainsKey(ruleContainer.analyzeRule.ruleName))
                {
                    var results = ruleContainer.analyzeRule.RefreshAnalysis(m_AnalyzeSetting.Settings);
                    resultData.Data[ruleContainer.analyzeRule.ruleName] = results;

                    BuildResults(ruleContainer, resultData.Data[ruleContainer.analyzeRule.ruleName]);
                    Reload();
                    UpdateSelections(GetSelection());
                }
            });
        }

        public void FixAllSelectedRules()
        {
            PerformActionForEntireRuleSelection((ruleContainer) =>
            {
                if (resultData.Data.ContainsKey(ruleContainer.analyzeRule.ruleName))
                {
                    ruleContainer.analyzeRule.FixIssues(m_AnalyzeSetting.Settings);

                    var results = ruleContainer.analyzeRule.RefreshAnalysis(m_AnalyzeSetting.Settings);
                    resultData.Data[ruleContainer.analyzeRule.ruleName] = results;

                    BuildResults(ruleContainer, resultData.Data[ruleContainer.analyzeRule.ruleName]);
                    Reload();
                    UpdateSelections(GetSelection());
                }

            });
        }

        public void ClearAllSelectedRules()
        {
            PerformActionForEntireRuleSelection((ruleContainer) =>
            {
                if (resultData.Data.ContainsKey(ruleContainer.analyzeRule.ruleName))
                {
                    ruleContainer.analyzeRule.ClearAnalysis();
                    resultData.Data[ruleContainer.analyzeRule.ruleName].Clear();
                    BuildResults(ruleContainer, resultData.Data[ruleContainer.analyzeRule.ruleName]);
                    Reload();
                    UpdateSelections(GetSelection());
                }
            });
        }

        public void RevertAllSelectedRules()
        {
            //TODO
        }

        public bool SelectionContainsFixableRule { get; private set; }
        public bool SelectionContainsRuleContainer { get; private set; }

        public bool SelectionContainsErrors { get; private set; }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            UpdateSelections(selectedIds);
        }

        void UpdateSelections(IList<int> selectedIds)
        {
            var allSelectedRuleContainers = (from id in selectedIds
                let ruleContainer = FindItem(id, rootItem) as AnalyzeRuleContainerTreeViewItem
                where ruleContainer != null
                select ruleContainer);

            List<AnalyzeRuleContainerTreeViewItem> allRuleContainers = new List<AnalyzeRuleContainerTreeViewItem>();
            foreach (var ruleContainer in allSelectedRuleContainers)
            {
                allRuleContainers.AddRange(GatherAllInheritRuleContainers(ruleContainer));
                allRuleContainers.Add(ruleContainer);
            }

            allRuleContainers = allRuleContainers.Distinct().ToList();

            SelectionContainsErrors = (from container in allRuleContainers
                                       from child in container.children
                                       where child is AnalyzeResultsTreeViewItem && (child as AnalyzeResultsTreeViewItem).IsError
                                       select child).Any();

            SelectionContainsRuleContainer = allRuleContainers.Any();

            SelectionContainsFixableRule = (from container in allRuleContainers
                where container.analyzeRule.CanFix
                select container).Any();
        }

        protected override void ContextClicked()
        {
            if (SelectionContainsRuleContainer)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Run Analyze Rule"), false, RunAllSelectedRules);
                menu.AddItem(new GUIContent("Clear Analyze Results"), false, ClearAllSelectedRules);

                if (SelectionContainsFixableRule && SelectionContainsErrors)
                    menu.AddItem(new GUIContent("Fix Analyze Rule"), false, FixAllSelectedRules);
                else
                    menu.AddDisabledItem(new GUIContent("Fix Analyze Rule"));

                //TODO
                //menu.AddItem(new GUIContent("Revert Analyze Rule"), false, RevertAllSelectedRules);

                menu.ShowAsContext();
                Repaint();
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            m_CurrentDepth = 0;
            var root = new TreeViewItem(-1, -1);
            root.children = new List<TreeViewItem>();

            string baseName = "Analyze Rules";
            string fixableRules = "Fixable Rules";
            string unfixableRules = "Unfixable Rules";

            AnalyzeRuleContainerTreeViewItem baseViewItem = new AnalyzeRuleContainerTreeViewItem(baseName.GetHashCode(), m_CurrentDepth, baseName);
            baseViewItem.children = new List<TreeViewItem>();
            baseViewItem.analyzeRule.CanFix = true;

            root.AddChild(baseViewItem);

            m_CurrentDepth++;

            var fixable = new AnalyzeRuleContainerTreeViewItem(fixableRules.GetHashCode(), m_CurrentDepth, fixableRules);
            var unfixable = new AnalyzeRuleContainerTreeViewItem(unfixableRules.GetHashCode(), m_CurrentDepth, unfixableRules);

            fixable.analyzeRule.CanFix = true;
            unfixable.analyzeRule.CanFix = false;

            baseViewItem.AddChild(fixable);
            baseViewItem.AddChild(unfixable);

            m_CurrentDepth++;

            for (int i = 0; i < m_AnalyzeSetting.m_Rules.Count; i++)
            {
                AnalyzeRuleContainerTreeViewItem ruleContainer = new AnalyzeRuleContainerTreeViewItem(
                    m_AnalyzeSetting.m_Rules[i].ruleName.GetHashCode(), m_CurrentDepth, m_AnalyzeSetting.m_Rules[i]);

                if(ruleContainer.analyzeRule.CanFix)
                    fixable.AddChild(ruleContainer);
                else
                    unfixable.AddChild(ruleContainer);

            }

            m_CurrentDepth++;

            foreach (var ruleContainer in GatherAllInheritRuleContainers(baseViewItem))
            {
                if (ruleContainer != null && resultData.Data.ContainsKey(ruleContainer.analyzeRule.ruleName))
                    BuildResults(ruleContainer, resultData.Data[ruleContainer.analyzeRule.ruleName]);
            }

            return root;
        }

        void BuildResults(TreeViewItem root, List<AnalyzeRule.AnalyzeResult> ruleResults)
        {
            foreach (var res in ruleResults)
            {
                var resPath = res.resultName.Split(AnalyzeRule.kDelimiter);

                var parentNode = root;
                var nameThusFar = string.Empty;
                for (int index = 0; index < resPath.Length; index++)
                {
                    nameThusFar += resPath[index];
                    var hash = nameThusFar.GetHashCode();
                    TreeViewItem currNode = null;

                    if (parentNode.id == hash)
                        currNode = parentNode;
                    else
                    {
                        foreach (var node in parentNode.children)
                        {
                            if (node.id == hash)
                            {
                                currNode = node;
                                break;
                            }
                        }
                    }

                    if (currNode == null)
                    {
                        if (index == resPath.Length - 1)
                        {
                            currNode = new AnalyzeResultsTreeViewItem(hash, index + m_CurrentDepth, resPath[index],
                                res.severity);
                        }
                        else
                        {
                            currNode = new AnalyzeResultsTreeViewItem(hash, index + m_CurrentDepth, resPath[index],
                                MessageType.None);
                        }

                        currNode.children = new List<TreeViewItem>();
                        parentNode.AddChild(currNode);
                    }

                    parentNode = currNode;
                }
            }

            List<TreeViewItem> allTreeViewItems = new List<TreeViewItem>();
            allTreeViewItems.Add(root);
            allTreeViewItems.AddRange(root.children);

            foreach (var node in allTreeViewItems)
                (node as AnalyzeTreeViewItemBase)?.AddIssueCountToName();

            EditorUtility.SetDirty(m_AnalyzeSetting.AnalyzeData);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as AnalyzeResultsTreeViewItem;
            if (item != null && item.severity != MessageType.None)
            {
                Texture2D icon = null;
                switch (item.severity)
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

                UnityEngine.GUI.Label(
                    new Rect(args.rowRect.x + baseIndent, args.rowRect.y, args.rowRect.width - baseIndent,
                        args.rowRect.height), new GUIContent(icon, string.Empty));
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

    class AnalyzeTreeViewItemBase : TreeViewItem
    {
        private string baseDisplayName;
        private string currentDisplayName;

        public override string displayName
        {
            get { return currentDisplayName; }
            set { baseDisplayName = value; }

        }

        public AnalyzeTreeViewItemBase(int id, int depth, string displayName) : base(id, depth,
            displayName)
        {
            currentDisplayName = baseDisplayName = displayName;
        }

        public int AddIssueCountToName()
        {
            int issueCount = 0;
            if (children != null)
            {
                foreach (var child in children)
                {
                    var analyzeNode = child as AnalyzeResultsTreeViewItem;
                    if (analyzeNode != null)
                        issueCount += analyzeNode.AddIssueCountToName();
                }
            }

            if (issueCount == 0)
                return 1;

            currentDisplayName = baseDisplayName + " (" + issueCount + ")";
            return issueCount;
        }
    }

    class AnalyzeResultsTreeViewItem : AnalyzeTreeViewItemBase
    {
        public MessageType severity { get; set; }

        public bool IsError
        {
            get { return !displayName.Contains("No issues found"); }
        }

        public AnalyzeResultsTreeViewItem(int id, int depth, string displayName, MessageType type) : base(id, depth,
            displayName)
        {
            severity = type;
        }
    }

    class AnalyzeRuleContainerTreeViewItem : AnalyzeTreeViewItemBase
    {
        internal AnalyzeRule analyzeRule;

        public AnalyzeRuleContainerTreeViewItem(int id, int depth, AnalyzeRule rule) : base(id, depth, rule.ruleName)
        {
            analyzeRule = rule;
            children = new List<TreeViewItem>();
        }

        public AnalyzeRuleContainerTreeViewItem(int id, int depth, string displayName) : base(id, depth, displayName)
        {
            analyzeRule = new AnalyzeRule();
            children = new List<TreeViewItem>();
        }
    }
}