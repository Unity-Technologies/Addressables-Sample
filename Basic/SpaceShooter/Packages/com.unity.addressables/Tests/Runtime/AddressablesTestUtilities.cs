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
using UnityEditor.SceneManagement;
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

    static public void TearDown(string testType, string pathFormat, string suffix)
    {
#if UNITY_EDITOR
        Reset(Addressables.Instance);
        var RootFolder = string.Format(pathFormat, testType, suffix);
        AssetDatabase.DeleteAsset(RootFolder);
#endif 
    }

    static public string GetPrefabLabel(string suffix) { return "prefabs" + suffix; }
    static public string GetPrefabAlternatingLabel(string suffix, int index) { return string.Format("prefabs_{0}{1}", ((index % 2) == 0) ? "even" : "odd", suffix); }
    static public string GetPrefabUniqueLabel(string suffix, int index) { return string.Format("prefab_{0}{1}", index, suffix); }
    public const int kPrefabCount = 10;
    public const int kSceneCount = 3;

    static public void Setup(string testType, string pathFormat, string suffix)
    {
#if UNITY_EDITOR
        var activeScenePath = EditorSceneManager.GetActiveScene().path;
        var RootFolder = string.Format(pathFormat, testType, suffix);
        
        Directory.CreateDirectory(RootFolder);

        var settings = AddressableAssetSettings.Create(RootFolder + "/Settings", "AddressableAssetSettings.Tests", false, true);
        var group = settings.FindGroup("TestStuff" + suffix);
        if (group == null)
            group = settings.CreateGroup("TestStuff" + suffix, true, false, false, null, typeof(BundledAssetGroupSchema));
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
#if ENABLE_SCENE_TESTS
        for (int i = 0; i < kSceneCount; i++)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, RootFolder + "/test_scene" + i + suffix + ".unity");
            var guid = AssetDatabase.AssetPathToGUID(scene.path);
            var entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = Path.GetFileNameWithoutExtension(entry.AssetPath);
        } 
#endif
        string assetRefGuid = CreateAsset(RootFolder + "/testIsReference.prefab", "IsReference");
        GameObject go = new GameObject("AssetReferenceBehavior");
        AssetReferenceTestBehavior aRefTestBehavior = go.AddComponent<AssetReferenceTestBehavior>();
        aRefTestBehavior.Reference = settings.CreateAssetReference(assetRefGuid);
        aRefTestBehavior.LabelReference = new AssetLabelReference()
        {
            labelString = settings.labelTable.labelNames[0]
        };

        string hasBehaviorPath = RootFolder + "/AssetReferenceBehavior.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, hasBehaviorPath);
        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(hasBehaviorPath), group, false, false);

        AssetDatabase.StopAssetEditing();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        RunBuilder(settings, testType, suffix);

#if ENABLE_SCENE_TESTS
        EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
#endif

#endif
    }

#if UNITY_EDITOR
    static string CreateAsset(string assetPath, string objectName)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objectName;
        //this is to ensure that bundles are different for every run.
        go.transform.localPosition = UnityEngine.Random.onUnitSphere;

        PrefabUtility.SaveAsPrefabAsset(go, assetPath);
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
