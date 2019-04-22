using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace UnityEditor.AddressableAssets.GUI
{
    class AddressableAssetEntryTreeView : TreeView
    {
        AddressableAssetsSettingsGroupEditor m_Editor;
        internal string customSearchString = string.Empty;

        enum ColumnId
        {
            Id,
            Type,
            Path,
            Labels
        }
        ColumnId[] m_SortOptions =
        {
            ColumnId.Id,
            ColumnId.Type,
            ColumnId.Path,
            ColumnId.Labels
        };
        public AddressableAssetEntryTreeView(TreeViewState state, MultiColumnHeaderState mchs, AddressableAssetsSettingsGroupEditor ed) : base(state, new MultiColumnHeader(mchs))
        {
            showBorder = true;
            m_Editor = ed;
            columnIndexForTreeFoldouts = 0;
            multiColumnHeader.sortingChanged += OnSortingChanged;

            BuiltinSceneCache.sceneListChanged += OnScenesChanged;
        }

        void OnScenesChanged()
        {
            Reload();
        }

        void OnSortingChanged(MultiColumnHeader mch)
        {
            SortChildren(rootItem);
            Reload();
        }
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            if (selectedIds.Count == 1)
            {
                var item = FindItemInVisibleRows(selectedIds[0]);
                if (item != null && item.group != null)
                {
                    Selection.activeObject = item.group;
                }
            }

        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            foreach (var group in m_Editor.settings.groups)
                AddGroupChildrenBuild(group, root);

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            SortChildren(root);
            var rows = base.BuildRows(root);
            if (!string.IsNullOrEmpty(customSearchString))
            {
                var z = rows.Where(s => DoesItemMatchSearch(s, customSearchString)).ToList();
                return z;
            }
            return rows;
        }

        internal void ClearSearch()
        {
            customSearchString = string.Empty;
            searchString = string.Empty;
        }

        void SortChildren(TreeViewItem root)
        {
            if (!root.hasChildren)
                return;
            foreach (var child in root.children)
            {
                if (child != null)
                    SortHierarchical(child.children);
            }
        }

        void SortHierarchical(List<TreeViewItem> children)
        {
            if (children == null)
                return;

            var sortedColumns = multiColumnHeader.state.sortedColumns;
            if (sortedColumns.Length == 0)
                return;

            List<AssetEntryTreeViewItem> kids = new List<AssetEntryTreeViewItem>();
            foreach (var c in children)
            {
                var child = c as AssetEntryTreeViewItem;
                if (child != null && child.entry != null)
                    kids.Add(child);
            }

            ColumnId col = m_SortOptions[sortedColumns[0]];
            bool ascending = multiColumnHeader.IsSortedAscending(sortedColumns[0]);

            IEnumerable<AssetEntryTreeViewItem> orderedKids = kids;
            switch (col)
            {
                case ColumnId.Type:
                    break;
                case ColumnId.Path:
                    orderedKids = kids.Order(l => l.entry.AssetPath, ascending);
                    break;
                case ColumnId.Labels:
                    orderedKids = OrderByLabels(kids, ascending);
                    break;
                default:
                    orderedKids = kids.Order(l => l.displayName, ascending);
                    break;
            }

            children.Clear();
            foreach (var o in orderedKids)
                children.Add(o);


            foreach (var child in children)
            {
                if (child != null)
                    SortHierarchical(child.children);
            }

        }

        IEnumerable<AssetEntryTreeViewItem> OrderByLabels(List<AssetEntryTreeViewItem> kids, bool ascending)
        {
            var emptyHalf = new List<AssetEntryTreeViewItem>();
            var namedHalf = new List<AssetEntryTreeViewItem>();
            foreach (var k in kids)
            {
                if (k.entry == null || k.entry.labels == null || k.entry.labels.Count < 1)
                    emptyHalf.Add(k);
                else
                    namedHalf.Add(k);
            }
            var orderedKids = namedHalf.Order(l => m_Editor.settings.labelTable.GetString(l.entry.labels, 200), ascending);

            List<AssetEntryTreeViewItem> result = new List<AssetEntryTreeViewItem>();
            if (ascending)
            {
                result.AddRange(emptyHalf);
                result.AddRange(orderedKids);
            }
            else
            {
                result.AddRange(orderedKids);
                result.AddRange(emptyHalf);
            }

            return result;
        }
        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            if (item == null)
                return false;
            var aeItem = item as AssetEntryTreeViewItem;
            if (ProjectConfigData.hierarchicalSearch)
            {
                //does this item match?
                if (DoesAeItemMatchSearch(aeItem, search))
                    return true;

                //else check if children match.
                if (item.children != null)
                {
                    foreach (var c in item.children)
                    {
                        if (DoesItemMatchSearch(c, search))
                            return true;
                    }
                }

                //nope.
                return false;
            }

            return DoesAeItemMatchSearch(aeItem, search);
            //SortSearchResult(result);
        }

        protected bool DoesAeItemMatchSearch(AssetEntryTreeViewItem aeItem, string search)
        {
            if (aeItem == null || aeItem.entry == null)
                return false;

            //check if item matches.
            if (aeItem.displayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (aeItem.entry.AssetPath.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (m_Editor.settings.labelTable.GetString(aeItem.entry.labels, 200).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        void AddGroupChildrenBuild(AddressableAssetGroup group, TreeViewItem root)
        {
            var groupItem = new AssetEntryTreeViewItem(group, 0);
            root.AddChild(groupItem);
            if (group.entries.Count > 0)
            {

                foreach (var entry in group.entries)
                {
                    AddAndRecurseEntriesBuild(entry, groupItem, 1);
                }
            }
        }

        void AddAndRecurseEntriesBuild(AddressableAssetEntry entry, AssetEntryTreeViewItem parent, int depth)
        {
            var item = new AssetEntryTreeViewItem(entry, depth);
            parent.AddChild(item);
            var subAssets = new List<AddressableAssetEntry>();
            entry.GatherAllAssets(subAssets, false, false);
            if (subAssets.Count > 0)
            {
                foreach (var e in subAssets)
                {
                    AddAndRecurseEntriesBuild(e, item, depth + 1);
                }
            }
        }

        public override void OnGUI(Rect rect)
        {
            base.OnGUI(rect);

            //TODO - this occasionally causes a "hot control" issue.
            if (Event.current.type == EventType.MouseDown &&
                Event.current.button == 0 &&
                rect.Contains(Event.current.mousePosition))
            {
                SetSelection(new int[0], TreeViewSelectionOptions.FireSelectionChanged);
            }
        }

        protected override void BeforeRowsGUI()
        {
            base.BeforeRowsGUI();

            if (Event.current.type == EventType.Repaint)
            {
                var rows = GetRows();
                if (rows.Count > 0)
                {
                    int first;
                    int last;
                    GetFirstAndLastVisibleRows(out first, out last);
                    for (int rowId = first; rowId <= last; rowId++)
                    {
                        var aeI = rows[rowId] as AssetEntryTreeViewItem;
                        if (aeI != null && aeI.entry != null)
                        {
                            DefaultStyles.backgroundEven.Draw(GetRowRect(rowId), false, false, false, false);
                        }
                    }
                }
            }
        }

        GUIStyle m_LabelStyle;
        protected override void RowGUI(RowGUIArgs args)
        {
            if (m_LabelStyle == null)
            {
                m_LabelStyle = new GUIStyle("PR Label");
                if (m_LabelStyle == null)
                    m_LabelStyle = UnityEngine.GUI.skin.GetStyle("Label");
            }

            var item = args.item as AssetEntryTreeViewItem;
            if (item == null)
            {
                base.RowGUI(args);
            }
            else if (item.group != null)
            {
                if (item.isRenaming && !args.isRenaming)
                    item.isRenaming = false;
                using (new EditorGUI.DisabledScope(item.group.ReadOnly))
                {
                    base.RowGUI(args);
                }
            }
            else if (item.entry != null && !args.isRenaming)
            {
                using (new EditorGUI.DisabledScope(item.entry.ReadOnly))
                {
                    for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                    {
                        CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
                    }
                }
            }
        }

        void CellGUI(Rect cellRect, AssetEntryTreeViewItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            switch ((ColumnId)column)
            {
                case ColumnId.Id:
                    {
                        // The rect is assumed indented and sized after the content when pinging
                        float indent = GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                        cellRect.xMin += indent;

                        if (Event.current.type == EventType.Repaint)
                            m_LabelStyle.Draw(cellRect, item.entry.address, false, false, args.selected, args.focused);
                    }
                    break;
                case ColumnId.Path:
                    if (Event.current.type == EventType.Repaint)
                    {
                        var path = item.entry.AssetPath;
                        if (string.IsNullOrEmpty(path))
                            path = "Missing File";
                        m_LabelStyle.Draw(cellRect, path, false, false, args.selected, args.focused);
                    }
                    break;
                case ColumnId.Type:
                    if (item.assetIcon != null)
                        UnityEngine.GUI.DrawTexture(cellRect, item.assetIcon, ScaleMode.ScaleToFit, true);
                    break;
                case ColumnId.Labels:
                    if (EditorGUI.DropdownButton(cellRect, new GUIContent(m_Editor.settings.labelTable.GetString(item.entry.labels, cellRect.width)), FocusType.Passive))
                    {
                        var selection = GetItemsForContext(args.item.id);
                        Dictionary<string, int> labelCounts = new Dictionary<string, int>();
                        List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
                        var newSelection = new List<int>();
                        foreach (var s in selection)
                        {
                            var aeItem = FindItem(s, rootItem) as AssetEntryTreeViewItem;
                            if (aeItem == null || aeItem.entry == null)
                                continue;

                            entries.Add(aeItem.entry);
                            newSelection.Add(s);
                            foreach (var label in aeItem.entry.labels)
                            {
                                int count;
                                labelCounts.TryGetValue(label, out count);
                                count++;
                                labelCounts[label] = count;
                            }
                        }
                        SetSelection(newSelection);
                        PopupWindow.Show(cellRect, new LabelMaskPopupContent(m_Editor.settings, entries, labelCounts));
                    }
                    break;

            }
        }

        IList<int> GetItemsForContext(int row)
        {
            var selection = GetSelection();
            if (selection.Contains(row))
                return selection;

            selection = new List<int>();
            selection.Add(row);
            return selection;
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            return new MultiColumnHeaderState(GetColumns());
        }

        static MultiColumnHeaderState.Column[] GetColumns()
        {
            var retVal = new[] {
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column()
                //new MultiColumnHeaderState.Column(),
            };

            int counter = 0;

            retVal[counter].headerContent = new GUIContent("Asset Address", "Address used to load asset at runtime");
            retVal[counter].minWidth = 100;
            retVal[counter].width = 260;
            retVal[counter].maxWidth = 10000;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;
            counter++;

            retVal[counter].headerContent = new GUIContent(EditorGUIUtility.FindTexture("FilterByType"), "Asset type");
            retVal[counter].minWidth = 20;
            retVal[counter].width = 20;
            retVal[counter].maxWidth = 20;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = false;
            retVal[counter].autoResize = true;
            counter++;

            retVal[counter].headerContent = new GUIContent("Path", "Current Path of asset");
            retVal[counter].minWidth = 100;
            retVal[counter].width = 150;
            retVal[counter].maxWidth = 10000;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;
            counter++;

            retVal[counter].headerContent = new GUIContent("Labels", "Assets can have multiple labels");
            retVal[counter].minWidth = 20;
            retVal[counter].width = 160;
            retVal[counter].maxWidth = 1000;
            retVal[counter].headerTextAlignment = TextAlignment.Left;
            retVal[counter].canSort = true;
            retVal[counter].autoResize = true;

            return retVal;
        }

        protected bool CheckForRename(TreeViewItem item, bool isActualRename)
        {
            bool result = false;
            var assetItem = item as AssetEntryTreeViewItem;
            if (assetItem != null)
            {
                if (assetItem.group != null)
                    result = !assetItem.group.ReadOnly;
                else if (assetItem.entry != null)
                    result = !assetItem.entry.ReadOnly;
                if (isActualRename)
                    assetItem.isRenaming = result;
            }
            return result;
        }
        protected override bool CanRename(TreeViewItem item)
        {
            return CheckForRename(item, true);
        }

        AssetEntryTreeViewItem FindItemInVisibleRows(int id)
        {
            var rows = GetRows();
            foreach (var r in rows)
            {
                if (r.id == id)
                {
                    return r as AssetEntryTreeViewItem;
                }
            }
            return null;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            var item = FindItemInVisibleRows(args.itemID);
            if (item != null)
            {
                item.isRenaming = false;
            }

            if (args.originalName == args.newName)
                return;

            if (item != null)
            {
                if (item.entry != null)
                {
                    item.entry.address = args.newName;
                }
                else if (item.group != null)
                {
                    if (m_Editor.settings.IsNotUniqueGroupName(args.newName))
                    {
                        args.acceptedRename = false;
                        Addressables.LogWarning("There is already a group named '" + args.newName + "'.  Cannot rename this group to match");
                    }
                    else
                        item.group.Name = args.newName;
                }
                Reload();
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItemInVisibleRows(id);
            if (item != null)
            {

                Object o = null;
                if (item.entry != null)
                    o = AssetDatabase.LoadAssetAtPath<Object>(item.entry.AssetPath);
                else if (item.group != null)
                    o = item.group;

                if (o != null)
                {
                    EditorGUIUtility.PingObject(o);
                    Selection.activeObject = o;
                }
            }
        }

        bool m_ContextOnItem;
        protected override void ContextClicked()
        {
            if (m_ContextOnItem)
            {
                m_ContextOnItem = false;
                return;
            }

            GenericMenu menu = new GenericMenu();
            PopulateGeneralContextMenu(ref menu);
            menu.ShowAsContext();
        }

        void PopulateGeneralContextMenu(ref GenericMenu menu)
        {
            foreach( var templateObject in m_Editor.settings.GroupTemplateObjects )
            {
                Assert.IsNotNull( templateObject );
                menu.AddItem( new GUIContent( "Create New Group/" + templateObject.name ), false, CreateNewGroup, templateObject );
            }
            var bundleList = AssetDatabase.GetAllAssetBundleNames();
            if (bundleList != null && bundleList.Length > 0)
                menu.AddItem(new GUIContent("Convert Legacy Bundles"), false, m_Editor.window.OfferToConvert);
        }

        protected override void ContextClickedItem(int id)
        {
            List<AssetEntryTreeViewItem> selectedNodes = new List<AssetEntryTreeViewItem>();
            foreach (var nodeId in GetSelection())
            {
                var item = FindItemInVisibleRows(nodeId); //TODO - this probably makes off-screen but selected items not get added to list.
                if (item != null)
                    selectedNodes.Add(item);
            }
            if (selectedNodes.Count == 0)
                return;

            m_ContextOnItem = true;

            bool isGroup = false;
            bool isEntry = false;
            bool hasReadOnly = false;
            int resourceCount = 0;
            bool isResourcesHeader = false;
            foreach (var item in selectedNodes)
            {
                if (item.group != null)
                {
                    hasReadOnly |= item.group.ReadOnly;
                    isGroup = true;
                }
                else if (item.entry != null)
                {
                    if (item.entry.AssetPath == AddressableAssetEntry.ResourcesPath)
                    {
                        if (selectedNodes.Count > 1)
                            return;
                        isResourcesHeader = true;
                    }
                    else if (item.entry.AssetPath == AddressableAssetEntry.EditorSceneListPath)
                    {
                        return;
                    }
                    hasReadOnly |= item.entry.ReadOnly;
                    isEntry = true;
                    resourceCount += item.entry.IsInResources ? 1 : 0;
                }
            }
            if (isEntry && isGroup)
                return;

            GenericMenu menu = new GenericMenu();
            if (isResourcesHeader)
            {
                foreach (var g in m_Editor.settings.groups)
                {
                    if (!g.ReadOnly)
                        menu.AddItem(new GUIContent("Move ALL Resources to group/" + g.Name), false, MoveAllResourcesToGroup, g);
                }
            }
            else if (!hasReadOnly)
            {
                if (isGroup)
                {
                    var group = selectedNodes.First().group; 
                    if (!group.IsDefaultGroup())
                        menu.AddItem(new GUIContent("Remove Group(s)"), false, RemoveGroup, selectedNodes);

                    if (selectedNodes.Count == 1)
                    {
                        if (!group.IsDefaultGroup() && group.CanBeSetAsDefault())
                            menu.AddItem(new GUIContent("Set as Default"), false, SetGroupAsDefault, selectedNodes);
                        menu.AddItem(new GUIContent("Inspect Group Settings"), false, GoToGroupAsset, selectedNodes);
                    }
                }
                if (isEntry)
                {
                    foreach (var g in m_Editor.settings.groups)
                    {
                        if (!g.ReadOnly)
                            menu.AddItem(new GUIContent("Move entries to group/" + g.Name), false, MoveEntriesToGroup, g);
                    }

                    var commonPrefix = FindCommonPrefix(selectedNodes);
                    if (!string.IsNullOrEmpty(commonPrefix))
                    {
                        var groups = new HashSet<AddressableAssetGroup>();
                        foreach (var n in selectedNodes)
                            groups.Add(n.entry.parentGroup);
                        foreach(var g in groups)
                            menu.AddItem(new GUIContent("Move entries to new group/" + commonPrefix + " (use " + g.Name + " settings)"), false, MoveEntriesToNewGroup, new KeyValuePair<string, AddressableAssetGroup>(commonPrefix, g));
                    }

                    menu.AddItem(new GUIContent("Remove Entry(s)"), false, RemoveEntry, selectedNodes);
                    menu.AddItem(new GUIContent("Simplify Entry Names"), false, SimplifyAddresses, selectedNodes);
                    menu.AddItem(new GUIContent("Export Entries..."), false, CreateExternalEntryCollection, selectedNodes);

                }
            }
            else
            {
                if (isEntry)
                {
                    if (resourceCount == selectedNodes.Count)
                    {
                        foreach (var g in m_Editor.settings.groups)
                        {
                            if (!g.ReadOnly)
                                menu.AddItem(new GUIContent("Move entries to group/" + g.Name), false, MoveResourcesToGroup, g);
                        }
                    }
                    else if (resourceCount == 0)
                    {
                        foreach (var g in m_Editor.settings.groups)
                        {
                            if (!g.ReadOnly)
                                menu.AddItem(new GUIContent("Move entries to group/" + g.Name), false, MoveEntriesToGroup, g);
                        }
                    }
                }
            }

            if (selectedNodes.Count == 1)
            {
                if (CheckForRename(selectedNodes.First(), false))
                    menu.AddItem(new GUIContent("Rename"), false, RenameItem, selectedNodes);
            }
            
            
            PopulateGeneralContextMenu(ref menu);
            
            menu.ShowAsContext();
        }

        string FindCommonPrefix(List<AssetEntryTreeViewItem> selectedNodes)
        {
            var names = new List<string>();
            foreach (var n in selectedNodes)
                names.Add(n.displayName.Replace('/', '-'));
            bool same = true;
            int index = 0;
            while (same)
            {
                if (index >= names[0].Length)
                {
                    break;
                }
                char c = names[0][index];
                foreach (var n in names)
                {
                    if (index >= n.Length || n[index] != c)
                    {
                        same = false;
                        break;
                    }
                }
                index++;
            }
            if (index < 1)
                return null;
            var prefix = names[0].Substring(0, index);
            return prefix.Trim(' ', '-');
        }

        void GoToGroupAsset(object context)
        {
            List<AssetEntryTreeViewItem> selectedNodes = context as List<AssetEntryTreeViewItem>;
            if (selectedNodes == null || selectedNodes.Count == 0)
                return;
            var group = selectedNodes.First().group;
            if (group == null)
                return;
            EditorGUIUtility.PingObject(group);
            Selection.activeObject = group;
        }

        void CreateExternalEntryCollection(object context)
        {
            var path = EditorUtility.SaveFilePanel("Create Entry Collection", "Assets", "AddressableEntryCollection", "asset");
            List<AssetEntryTreeViewItem> selectedNodes = context as List<AssetEntryTreeViewItem>;
            
            if (!string.IsNullOrEmpty(path) && selectedNodes != null)
            {
                var col = ScriptableObject.CreateInstance<AddressableAssetEntryCollection>();
                foreach (var item in selectedNodes)
                {
                    item.entry.ReadOnly = true;
                    item.entry.IsSubAsset = true;
                    col.Entries.Add(item.entry);
                    m_Editor.settings.RemoveAssetEntry(item.entry.guid, false);
                }
                path = path.Substring(path.ToLower().IndexOf("assets/"));
                AssetDatabase.CreateAsset(col, path);
                AssetDatabase.Refresh();
                var guid = AssetDatabase.AssetPathToGUID(path);
                m_Editor.settings.CreateOrMoveEntry(guid, m_Editor.settings.DefaultGroup);
            }
        }

        void MoveAllResourcesToGroup(object context)
        {
            var targetGroup = context as AddressableAssetGroup;
            var firstId = GetSelection().First();
            var item = FindItemInVisibleRows(firstId);
            if (item != null && item.children != null)
            {
                SafeMoveResourcesToGroup(targetGroup, item.children.ConvertAll(instance => (AssetEntryTreeViewItem)instance));
            }
            else
                Debug.LogWarning("No Resources found to move");
        }

        void MoveResourcesToGroup(object context)
        {
            var targetGroup = context as AddressableAssetGroup;
            var itemList = new List<AssetEntryTreeViewItem>();
            foreach (var nodeId in GetSelection())
            {
                var item = FindItemInVisibleRows(nodeId);
                if (item != null)
                    itemList.Add(item);
            }

            SafeMoveResourcesToGroup(targetGroup, itemList);
        }

        bool SafeMoveResourcesToGroup(AddressableAssetGroup targetGroup, List<AssetEntryTreeViewItem> itemList)
        {
            var guids = new List<string>();
            var paths = new List<string>();
            foreach (AssetEntryTreeViewItem child in itemList)
            {
                if (child != null)
                {
                    guids.Add(child.entry.guid);
                    paths.Add(child.entry.AssetPath);
                }
            }
            return AddressableAssetUtility.SafeMoveResourcesToGroup(m_Editor.settings, targetGroup, paths, guids);
        }

        void MoveEntriesToNewGroup(object context)
        {
            var k = (KeyValuePair<string, AddressableAssetGroup>)context;
            var g = m_Editor.settings.CreateGroup(k.Key, false, false, true, k.Value.Schemas);
            MoveEntriesToGroup(g);
        }

        void MoveEntriesToGroup(object context)
        {
            var targetGroup = context as AddressableAssetGroup;
            var entries = new List<AddressableAssetEntry>();
            foreach (var nodeId in GetSelection())
            {
                var item = FindItemInVisibleRows(nodeId);
                if (item != null)
                    entries.Add(item.entry);
            }
            if (entries.Count > 0)
                m_Editor.settings.MoveEntries(entries, targetGroup);
        }

        protected void CreateNewGroup(object context)
        {
            var groupTemplate = context as AddressableAssetGroupTemplate;
            if (groupTemplate != null)
            {
                AddressableAssetGroup newGroup = m_Editor.settings.CreateGroup( groupTemplate.Name, false, false, true, null, groupTemplate.GetTypes() );
                groupTemplate.ApplyToAddressableAssetGroup( newGroup );
            }
            else
            {
                m_Editor.settings.CreateGroup("", false, false, false, null);
            }
        }

        void SetGroupAsDefault(object context)
        {
            List<AssetEntryTreeViewItem> selectedNodes = context as List<AssetEntryTreeViewItem>;
            if (selectedNodes == null || selectedNodes.Count == 0)
                return;
            var group = selectedNodes.First().group;
            if (group == null)
                return;
            m_Editor.settings.DefaultGroup = group;
            Reload();
        }

        protected void RemoveGroup(object context)
        {
            if (EditorUtility.DisplayDialog("Delete selected groups?", "Are you sure you want to delete the selected groups?\n\nYou cannot undo this action.", "Yes", "No"))
            {
                List<AssetEntryTreeViewItem> selectedNodes = context as List<AssetEntryTreeViewItem>;
                if (selectedNodes == null)
                    return;
                var groups = new List<AddressableAssetGroup>();
                foreach (var item in selectedNodes)
                {
                    m_Editor.settings.RemoveGroupInternal(item.group, true, false);
                    groups.Add(item.group);
                }
                m_Editor.settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupRemoved, groups, true);
            }
        }

        protected void SimplifyAddresses(object context)
        {
            List<AssetEntryTreeViewItem> selectedNodes = context as List<AssetEntryTreeViewItem>;
            if (selectedNodes == null)
                return;
            var entries = new List<AddressableAssetEntry>();

            foreach (var item in selectedNodes)
            {
                item.entry.SetAddress(Path.GetFileNameWithoutExtension(item.entry.address), false);
                entries.Add(item.entry);
            }
            m_Editor.settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entries, true);
        }

        protected void RemoveEntry(object context)
        {
            if (EditorUtility.DisplayDialog("Delete selected entries?", "Are you sure you want to delete the selected entries?\n\nYou cannot undo this action.", "Yes", "No"))
            {
                List<AssetEntryTreeViewItem> selectedNodes = context as List<AssetEntryTreeViewItem>;
                if (selectedNodes == null)
                    return;
                var entries = new List<AddressableAssetEntry>();
                foreach (var item in selectedNodes)
                {
                    if (item.entry != null)
                    {
                        m_Editor.settings.RemoveAssetEntry(item.entry.guid, false);
                        entries.Add(item.entry);
                    }
                }
                m_Editor.settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, entries, true);
            }
        }

        protected void RenameItem(object context)
        {
            List<AssetEntryTreeViewItem> selectedNodes = context as List<AssetEntryTreeViewItem>;
            if (selectedNodes != null && selectedNodes.Count >= 1)
            {
                var item = selectedNodes.First();
                if (CanRename(item))
                    BeginRename(item);
            }
        }

        protected override bool CanBeParent(TreeViewItem item)
        {
            var aeItem = item as AssetEntryTreeViewItem;
            if (aeItem != null && aeItem.group != null)
                return true;

            return false;
        }

        protected override void KeyEvent()
        {
            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Delete && GetSelection().Count > 0)
            {
                List<AssetEntryTreeViewItem> selectedNodes = new List<AssetEntryTreeViewItem>();
                bool allGroups = true;
                bool allEntries = true;
                foreach (var nodeId in GetSelection())
                {
                    var item = FindItemInVisibleRows(nodeId);
                    if (item != null)
                    {
                        selectedNodes.Add(item);
                        if (item.entry == null)
                            allEntries = false;
                        else
                            allGroups = false;
                    }
                }
                if (allEntries)
                    RemoveEntry(selectedNodes);
                if (allGroups)
                    RemoveGroup(selectedNodes);
            }
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            int resourcesCount = 0;
            foreach (var id in args.draggedItemIDs)
            {
                var item = FindItemInVisibleRows(id);
                if (item != null)
                {
                    //can't drag groups
                    if (item.group != null)
                        return false;

                    if (item.entry != null)
                    {
                        //can't drag the root "EditorSceneList" entry
                        if (item.entry.guid == AddressableAssetEntry.EditorSceneListName)
                            return false;

                        //can't drag the root "Resources" entry
                        if (item.entry.guid == AddressableAssetEntry.ResourcesName)
                            return false;

                        //if we're dragging resources, we should _only_ drag resources.
                        if (item.entry.IsInResources)
                            resourcesCount++;
                    }
                }
            }
            if ((resourcesCount > 0) && (resourcesCount < args.draggedItemIDs.Count))
                return false;

            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();

            var selectedNodes = new List<AssetEntryTreeViewItem>();
            foreach (var id in args.draggedItemIDs)
            {
                var item = FindItemInVisibleRows(id);
                selectedNodes.Add(item);
            }
            DragAndDrop.paths = null;
            DragAndDrop.objectReferences = new Object[] { };
            DragAndDrop.SetGenericData("AssetEntryTreeViewItem", selectedNodes);
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.StartDrag("AssetBundleTree");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.None;

            var target = args.parentItem as AssetEntryTreeViewItem;
            if (target == null)
                return DragAndDropVisualMode.None;

            if (target.entry != null && target.entry.ReadOnly)
                return DragAndDropVisualMode.None;

            if (target.group != null && target.group.ReadOnly)
                return DragAndDropVisualMode.None;


            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                if (!AddressableAssetUtility.IsPathValidForEntry(DragAndDrop.paths[0]))
                    visualMode = DragAndDropVisualMode.Rejected;
                else
                    visualMode = DragAndDropVisualMode.Copy;

                if (args.performDrop && visualMode != DragAndDropVisualMode.Rejected)
                {
                    AddressableAssetGroup parent = null;
                    if (target.group != null)
                        parent = target.group;
                    else if (target.entry != null)
                        parent = target.entry.parentGroup;

                    if (parent != null)
                    {
                        var resourcePaths = new List<string>();
                        var nonResourcePaths = new List<string>();
                        foreach (var p in DragAndDrop.paths)
                        {
                            if (AddressableAssetUtility.IsInResources(p))
                                resourcePaths.Add(p);
                            else
                                nonResourcePaths.Add(p);
                        }
                        bool canMarkNonResources = true;
                        if (resourcePaths.Count > 0)
                        {
                            canMarkNonResources = AddressableAssetUtility.SafeMoveResourcesToGroup(m_Editor.settings, parent, resourcePaths);
                        }
                        if (canMarkNonResources)
                        {
                            var entries = new List<AddressableAssetEntry>();
                            foreach (var p in nonResourcePaths)
                            {
                                entries.Add(m_Editor.settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(p), parent, false, false));
                            }
                            m_Editor.settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entries, true);
                        }

                    }
                }
            }
            else
            {
                var draggedNodes = DragAndDrop.GetGenericData("AssetEntryTreeViewItem") as List<AssetEntryTreeViewItem>;
                if (draggedNodes != null && draggedNodes.Count > 0)
                {
                    visualMode = DragAndDropVisualMode.Copy;
                    if (args.performDrop)
                    {
                        AddressableAssetGroup parent = null;
                        if (target.group != null)
                            parent = target.group;
                        else if (target.entry != null)
                            parent = target.entry.parentGroup;

                        if (parent != null)
                        {
                            if (draggedNodes.First().entry.IsInResources)
                            {
                                SafeMoveResourcesToGroup(parent, draggedNodes);
                            }
                            else
                            {
                                var entries = new List<AddressableAssetEntry>();
                                foreach (var node in draggedNodes)
                                    entries.Add(m_Editor.settings.CreateOrMoveEntry(node.entry.guid, parent, false, false));
                                m_Editor.settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entries, true);
                            }
                        }
                    }
                }
            }

            return visualMode;
        }

    }

    class AssetEntryTreeViewItem : TreeViewItem
    {
        public AddressableAssetEntry entry;
        public AddressableAssetGroup group;
        public Texture2D assetIcon;
        public bool isRenaming;

        public AssetEntryTreeViewItem(AddressableAssetEntry e, int d) : base((e.address + e.guid).GetHashCode(), d, e.address)
        {
            entry = e;
            group = null;
            assetIcon = AssetDatabase.GetCachedIcon(e.AssetPath) as Texture2D;
            isRenaming = false;
        }

        public AssetEntryTreeViewItem(AddressableAssetGroup g, int d) : base(g.Guid.GetHashCode(), d, g.Name)
        {
            entry = null;
            group = g;
            assetIcon = null;
            isRenaming = false;
        }

        public override string displayName
        {
            get
            {
                if (!isRenaming && group != null && group.Default)
                    return base.displayName + " (Default)";
                return base.displayName;
            }

            set
            {
                base.displayName = value;
            }
        }
    }


    //TODO - ideally need to get rid of this
    static class MyExtensionMethods
    {
        public static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.OrderBy(selector);
            }

            return source.OrderByDescending(selector);
        }

        public static IOrderedEnumerable<T> ThenBy<T, TKey>(this IOrderedEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.ThenBy(selector);
            }

            return source.ThenByDescending(selector);
        }

        internal static void DrawOutline(Rect rect, float size)
        {
            Color color = new Color(0.6f, 0.6f, 0.6f, 1.333f);
            if (EditorGUIUtility.isProSkin)
            {
                color.r = 0.12f;
                color.g = 0.12f;
                color.b = 0.12f;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            Color orgColor = UnityEngine.GUI.color;
            UnityEngine.GUI.color = UnityEngine.GUI.color * color;
            UnityEngine.GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, size), EditorGUIUtility.whiteTexture);
            UnityEngine.GUI.DrawTexture(new Rect(rect.x, rect.yMax - size, rect.width, size), EditorGUIUtility.whiteTexture);
            UnityEngine.GUI.DrawTexture(new Rect(rect.x, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);
            UnityEngine.GUI.DrawTexture(new Rect(rect.xMax - size, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);

            UnityEngine.GUI.color = orgColor;
        }
    }
}
