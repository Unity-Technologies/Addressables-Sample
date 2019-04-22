using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.AddressableAssets.GUI
{
    [InitializeOnLoad]
    static class AddressableAssetInspectorGUI
    {
        static GUIStyle s_ToggleMixed;
        static GUIContent s_AddressableAssetToggleText;

        static AddressableAssetInspectorGUI()
        {
            s_ToggleMixed = null;
            s_AddressableAssetToggleText = new GUIContent("Addressable", "Check this to mark this asset as an Addressable Asset, which includes it in the bundled data and makes it loadable via script by its address.");
            Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
        }

        static void SetAaEntry(AddressableAssetSettings aaSettings, Object[] targets, bool create)
        {
            Undo.RecordObject(aaSettings, "AddressableAssetSettings");
            string path;
            var guid = string.Empty;
            //if (create || EditorUtility.DisplayDialog("Remove Addressable Asset Entries", "Do you want to remove Addressable Asset entries for " + targets.Length + " items?", "Yes", "Cancel"))
            {
                var entriesAdded = new List<AddressableAssetEntry>();

                foreach (var t in targets)
                {
                    if (AddressableAssetUtility.GetPathAndGUIDFromTarget(t, out path, ref guid))
                    {
                        if (create)
                        {
                            if (AddressableAssetUtility.IsInResources(path))
                                AddressableAssetUtility.SafeMoveResourcesToGroup(aaSettings, aaSettings.DefaultGroup, new List<string> {path});
                            else
                                entriesAdded.Add(aaSettings.CreateOrMoveEntry(guid, aaSettings.DefaultGroup, false, false));
                        }
                        else
                            aaSettings.RemoveAssetEntry(guid);
                    }
                }

                if (create)
                {
                    aaSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true);
                }
            }
        }

        static void OnPostHeaderGUI(Editor editor)
        {
            var aaSettings = AddressableAssetSettingsDefaultObject.Settings;
            var guid = string.Empty;
            AddressableAssetEntry entry = null;

            if (editor.targets.Length > 0)
            {
                int addressableCount = 0;
                bool foundValidAsset = false;
                foreach (var t in editor.targets)
                {
                    string path;
                    if ((AddressableAssetUtility.GetPathAndGUIDFromTarget(t, out path, ref guid)) &&
                        (path.ToLower().Contains("assets")))
                    {
                        foundValidAsset = true;

                        if (aaSettings != null)
                        {
                            entry = aaSettings.FindAssetEntry(guid);
                            if (entry != null && !entry.IsSubAsset)
                            {
                                addressableCount++;
                            }
                        }
                    }
                }


                if (!foundValidAsset)
                    return;

                if (addressableCount == 0)
                {
                    if (GUILayout.Toggle(false, s_AddressableAssetToggleText, GUILayout.ExpandWidth(false)))
                        SetAaEntry(AddressableAssetSettingsDefaultObject.GetSettings(true), editor.targets, true);
                }
                else if (addressableCount == editor.targets.Length)
                {
                    GUILayout.BeginHorizontal();
                    if (!GUILayout.Toggle(true, s_AddressableAssetToggleText, GUILayout.ExpandWidth(false)))
                        SetAaEntry(aaSettings, editor.targets, false);

                    if (editor.targets.Length == 1 && entry != null)
                    {
                        entry.address = EditorGUILayout.DelayedTextField(entry.address, GUILayout.ExpandWidth(true));
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    if (s_ToggleMixed == null)
                        s_ToggleMixed = new GUIStyle("ToggleMixed");
                    if (GUILayout.Toggle(false, s_AddressableAssetToggleText, s_ToggleMixed, GUILayout.ExpandWidth(false)))
                        SetAaEntry(AddressableAssetSettingsDefaultObject.GetSettings(true), editor.targets, true);
                    EditorGUILayout.LabelField(addressableCount + " out of " + editor.targets.Length + " assets are addressable.");
                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}
