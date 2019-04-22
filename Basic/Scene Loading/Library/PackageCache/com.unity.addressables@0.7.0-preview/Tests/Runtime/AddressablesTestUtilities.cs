using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.ResourceManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
#endif

static class AddressablesTestUtility
{

    static public void Reset(AddressablesImpl aa)
    {
        aa.ResourceLocators.Clear();
        aa.ResourceManager.ResourceProviders.Clear();
        aa.InstanceProvider = null;
    }

    static public void TearDown(string testType, string pathFormat)
    {
#if UNITY_EDITOR
        Reset(Addressables.Instance);
        var RootFolder = string.Format(pathFormat, testType);
        AssetDatabase.DeleteAsset(RootFolder);
#endif 
    }

    static public string GetPrefabLabel(string suffix) { return "prefabs" + suffix; }
    static public string GetPrefabAlternatingLabel(string suffix, int index) { return string.Format("prefabs_{0}{1}", ((index % 2) == 0) ? "even" : "odd", suffix); }
    static public string GetPrefabUniqueLabel(string suffix, int index) { return string.Format("prefab_{0}{1}", index, suffix); }
    public const int kPrefabCount = 10;
    static public void Setup(string testType, string pathFormat, string suffix)
    {
#if UNITY_EDITOR
        var RootFolder = string.Format(pathFormat, testType);
        
        Directory.CreateDirectory(RootFolder);

        var settings = AddressableAssetSettings.Create(RootFolder + "/Settings", "AddressableAssetSettings.Tests", true, true);
        var playerData = settings.FindGroup(g => g.HasSchema<PlayerDataGroupSchema>());
        if (playerData != null)
        {
            var s = playerData.GetSchema<PlayerDataGroupSchema>();
            s.IncludeBuildSettingsScenes = false;
            s.IncludeResourcesFolders = false;
        }
        settings.DefaultGroup.GetSchema<BundledAssetGroupSchema>().IncludeInBuild = false;
        var group = settings.CreateGroup("TestStuff" + suffix, true, false, false, null, typeof(BundledAssetGroupSchema));
        settings.DefaultGroup = group;
        AssetDatabase.StartAssetEditing();
        for (int i = 0; i < kPrefabCount; i++)
        {
            var guid = CreateAsset(RootFolder + "/test" + i + suffix + ".prefab", "testPrefab" + i);
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = Path.GetFileNameWithoutExtension(entry.AssetPath);

            entry.SetLabel(GetPrefabLabel(suffix), true, false);
            entry.SetLabel(GetPrefabAlternatingLabel(suffix, i), true, false);
            entry.SetLabel(GetPrefabUniqueLabel(suffix, i), true, false);
        }

        string assetRefGuid = CreateAsset(RootFolder + "/testIsReference.prefab", "IsReference");
        GameObject go = new GameObject("AssetReferenceBehavior");
        go.AddComponent<AssetReferenceTestBehavior>().Reference = settings.CreateAssetReference(assetRefGuid);

        string hasBehaviorPath = RootFolder + "/AssetReferenceBehavior.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, hasBehaviorPath);
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(hasBehaviorPath), group, false, false);

        AssetDatabase.StopAssetEditing();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        RunBuilder(settings, testType, suffix);
#endif
    }

#if UNITY_EDITOR
    static string CreateAsset(string assetPath, string objectName)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
#if UNITY_2018_3_OR_NEWER
        PrefabUtility.SaveAsPrefabAsset(go, assetPath);
#else
        PrefabUtility.CreatePrefab(assetPath, go);
#endif
        go.name = objectName;
        UnityEngine.Object.DestroyImmediate(go, false);
        return AssetDatabase.AssetPathToGUID(assetPath);
    }


    static void RunBuilder(AddressableAssetSettings settings, string testType, string suffix)
    {
        var buildContext = new AddressablesDataBuilderInput(settings);
        buildContext.RuntimeSettingsFilename = "settings" + suffix + ".json";
        buildContext.RuntimeCatalogFilename = "catalog" + suffix + ".json";

        foreach (var db in settings.DataBuilders)
        {
            var b = db as IDataBuilder;
            if (b.GetType().Name != testType)
                continue;
            buildContext.PathFormat = "{0}Library/com.unity.addressables/{1}_" + testType + "_TEST_" + suffix + ".json";
            b.BuildData<AddressableAssetBuildResult>(buildContext);
        }
    }
#endif
    }
