using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.AddressableAssets.GUI
{
    class HostingServicesProfileVarsTreeView : TreeView
    {
        class ProfileVarItem : TreeViewItem
        {
            public string Key { get; private set; }
            public string Value { get; set; }

            public ProfileVarItem(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }

        protected override void ContextClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as ProfileVarItem;
            if (item == null) return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy Key"), false, () => EditorGUIUtility.systemCopyBuffer = string.Format("[{0}]", item.Key));
            menu.AddItem(new GUIContent("Copy Value"), false, () => EditorGUIUtility.systemCopyBuffer = item.Value);
            menu.ShowAsContext();
        }

        public static MultiColumnHeader CreateHeader()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Profile Variable"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 180,
                    minWidth = 60,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Value"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 180,
                    minWidth = 60,
                    autoResize = true
                }
            };

            var header = new MultiColumnHeader(new MultiColumnHeaderState(columns))
            {
                height = 20f
            };

            return header;
        }

        readonly Dictionary<string, ProfileVarItem> m_ItemMap;

        public float RowHeight
        {
            get { return rowHeight; }
        }
        
        public HostingServicesProfileVarsTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader) : base(state, multiColumnHeader)
        {
            showBorder = true;
            showAlternatingRowBackgrounds = true;
            m_ItemMap = new Dictionary<string, ProfileVarItem>();
            Reload();
        }

        public void ClearItems()
        {
            m_ItemMap.Clear();
        }

        public void AddOrUpdateItem(string key, string value)
        {
            if (m_ItemMap.ContainsKey(key))
            {
                if (m_ItemMap[key].Value == value)
                    return;

                m_ItemMap[key].Value = value;
                Reload();
                return;
            }

            var item = new ProfileVarItem(key, value) {id = m_ItemMap.Count};
            m_ItemMap.Add(key, item);
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1) {children = new List<TreeViewItem>()};
            foreach (var item in m_ItemMap.Values)
                root.AddChild(item);

            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGui(ref args, i);
            }
        }

        void CellGui(ref RowGUIArgs args, int i)
        {
            var cellRect = args.GetCellRect(i);
            CenterRectUsingSingleLineHeight(ref cellRect);
            var item = args.item as ProfileVarItem;
            if (item == null) return;

            switch (args.GetColumn(i))
            {
                case 0:
                    EditorGUI.LabelField(cellRect, item.Key);
                    break;
                case 1:
                    EditorGUI.LabelField(cellRect, item.Value);
                    break;
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }
    }
}