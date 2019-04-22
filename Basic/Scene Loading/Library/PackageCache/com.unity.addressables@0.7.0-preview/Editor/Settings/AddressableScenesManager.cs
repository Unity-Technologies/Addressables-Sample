using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Settings
{
    static class AddressableScenesManager
    {
        static public void InitializeGlobalState()
        {
            BuiltinSceneCache.sceneListChanged += OnScenesChanged;
            AddressableAssetSettings.OnModificationGlobal += OnSettingsChanged;
        }

        static public void ShutdownGlobalState()
        {
            AddressableAssetSettings.OnModificationGlobal -= OnSettingsChanged;
            BuiltinSceneCache.sceneListChanged -= OnScenesChanged;
        }

        internal static void OnSettingsChanged(AddressableAssetSettings settings, AddressableAssetSettings.ModificationEvent evt, object obj)
        {
            switch (evt)
            {
                case AddressableAssetSettings.ModificationEvent.EntryCreated:
                case AddressableAssetSettings.ModificationEvent.EntryAdded:
                case AddressableAssetSettings.ModificationEvent.EntryMoved:
                case AddressableAssetSettings.ModificationEvent.EntryModified:
                    var entries = obj as List<AddressableAssetEntry>;
                    if (entries == null)
                    {
                        entries = new List<AddressableAssetEntry>();
                        entries.Add(obj as AddressableAssetEntry);
                    }
                    CheckForScenesInBuildList(entries);
                    break;
            }
        }

        static void OnScenesChanged()
        {
            //ignore the play mode changes...
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return;
            
            foreach (var scene in BuiltinSceneCache.scenes)
            {
                if (scene.enabled)
                {
                    var entry = settings.FindAssetEntry(scene.guid.ToString());
                    if (entry != null)
                    {
                        Debug.LogWarning("An addressable scene was added to the build scenes list and can thus no longer be addressable.  " + scene.path);
                        settings.RemoveAssetEntry(scene.guid.ToString());
                    }
                }
            }
        }

        static void CheckForScenesInBuildList(IList<AddressableAssetEntry> entries)
        {
            if (entries == null)
                return;

            EditorBuildSettingsScene[] scenes = BuiltinSceneCache.scenes;
            bool changed = false;
            foreach (var entry in entries)
            {
                if (entry == null)
                    continue;

                for (int index = 0; index < scenes.Length; index++)
                {
                    var scene = scenes[index];
                    if (scene.enabled && entry.AssetPath == scene.path)
                    {
                        Debug.LogWarning("A scene from the EditorBuildScenes list has been marked as addressable. It has thus been disabled in the build scenes list.  " + scene.path);
                        scenes[index].enabled = false;
                        changed = true;
                    }
                }
            }
            if (changed)
                BuiltinSceneCache.scenes = scenes;
        }
    }
}
