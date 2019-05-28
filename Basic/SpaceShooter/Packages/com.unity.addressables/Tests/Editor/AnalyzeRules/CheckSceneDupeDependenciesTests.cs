using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.SceneManagement;

namespace UnityEditor.AddressableAssets.Tests.AnalyzeRules
{
    public class CheckSceneDupeDependenciesTests : AddressableAssetTestBase
    {
        const string k_CheckDupePrefabA = k_TestConfigFolder + "/checkDupe_prefabA.prefab";
        const string k_CheckDupePrefabB = k_TestConfigFolder + "/checkDupe_prefabB.prefab";
        const string k_CheckDupeMyMaterial = k_TestConfigFolder + "/checkDupe_myMaterial.mat";
        const string k_ScenePath = k_TestConfigFolder + "/dupeSceneTest.unity";
        const string k_PrefabWithMaterialPath = k_TestConfigFolder + "/checkDupe_prefabWithMaterial.prefab";

        protected override void OnInit()
        {
            base.OnInit();

            GameObject prefabA = new GameObject("PrefabA");
            GameObject prefabB = new GameObject("PrefabB");
            GameObject prefabWithMaterial = new GameObject("PrefabWithMaterial");
            var meshA = prefabA.AddComponent<MeshRenderer>();
            var meshB = prefabB.AddComponent<MeshRenderer>();
            
            var mat = new Material(Shader.Find("Unlit/Color"));
            AssetDatabase.CreateAsset(mat, k_CheckDupeMyMaterial);
            meshA.sharedMaterial = mat;
            meshB.sharedMaterial = mat;

            var meshPrefabWithMaterial = prefabWithMaterial.AddComponent<MeshRenderer>();
            meshPrefabWithMaterial.material = AssetDatabase.LoadAssetAtPath<Material>(k_CheckDupeMyMaterial);

#if UNITY_2018_3_OR_NEWER
            PrefabUtility.SaveAsPrefabAsset(prefabA, k_CheckDupePrefabA);
            PrefabUtility.SaveAsPrefabAsset(prefabB, k_CheckDupePrefabB);
            PrefabUtility.SaveAsPrefabAsset(prefabWithMaterial, k_PrefabWithMaterialPath);
#else
            PrefabUtility.CreatePrefab(k_CheckDupePrefabA, prefabA);
            PrefabUtility.CreatePrefab(k_CheckDupePrefabB, prefabB);
            PrefabUtility.CreatePrefab(k_PrefabWithMaterialPath, prefabWithMaterial);
#endif
            AssetDatabase.Refresh();
        }

        [Test]
        public void CheckSceneDupe_SceneDependenciesMatchWithExplicitBundleDependencies()
        {
            var group1 = Settings.CreateGroup("CheckDupeDepencency1", false, false, false, null, typeof(BundledAssetGroupSchema));
            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(k_CheckDupePrefabA), group1, false, false);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(k_CheckDupePrefabA);
            PrefabUtility.InstantiatePrefab(go, scene);
            EditorSceneManager.SaveScene(scene, k_ScenePath);

            var rule = new CheckSceneDupeDependencies();

            EditorBuildSettingsScene editorScene = new EditorBuildSettingsScene(k_ScenePath, true);
            rule.BuildSceneToDependenciesMap(new EditorBuildSettingsScene[]{ editorScene });
            rule.IntersectSceneDependenciesWithBundleDependencies(new List<GUID>() { new GUID(AssetDatabase.AssetPathToGUID(k_CheckDupePrefabA)) });

            Assert.IsTrue(rule.m_SceneToDependencies.ContainsKey(editorScene));
            Assert.AreEqual(1, rule.m_SceneToDependencies[editorScene].Count);
            Assert.AreEqual(AssetDatabase.AssetPathToGUID(k_CheckDupePrefabA), rule.m_SceneToDependencies[editorScene][0].ToString());

            //Cleanup
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            Settings.RemoveGroup(group1);
        }

        [Test]
        public void CheckSceneDupe_SceneDependenciesMatchWithImplicitBundleDependencies()
        {
            var group1 = Settings.CreateGroup("CheckDupeDepencency1", false, false, false, null, typeof(BundledAssetGroupSchema));
            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(k_PrefabWithMaterialPath), group1, false, false);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabWithMaterialPath), scene);
            EditorSceneManager.SaveScene(scene, k_ScenePath);

            var rule = new CheckSceneDupeDependencies();

            EditorBuildSettingsScene editorScene = new EditorBuildSettingsScene(k_ScenePath, true);
            rule.BuildSceneToDependenciesMap(new EditorBuildSettingsScene[] { editorScene });
            rule.IntersectSceneDependenciesWithBundleDependencies(new List<GUID>() { new GUID(AssetDatabase.AssetPathToGUID(k_CheckDupeMyMaterial)) });

            Assert.IsTrue(rule.m_SceneToDependencies.ContainsKey(editorScene));
            Assert.AreEqual(1, rule.m_SceneToDependencies[editorScene].Count);
            Assert.AreEqual(AssetDatabase.AssetPathToGUID(k_CheckDupeMyMaterial), rule.m_SceneToDependencies[editorScene][0].ToString());

            //Cleanup
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            Settings.RemoveGroup(group1);
        }

        [Test]
        public void CheckSceneDupe_AllSceneToBundleDependenciesAreReturned()
        {
            var group1 = Settings.CreateGroup("CheckDupeDepencency1", false, false, false, null, typeof(BundledAssetGroupSchema));
            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(k_PrefabWithMaterialPath), group1, false, false);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabWithMaterialPath), scene);
            EditorSceneManager.SaveScene(scene, k_ScenePath);

            var rule = new CheckSceneDupeDependencies();

            EditorBuildSettingsScene editorScene = new EditorBuildSettingsScene(k_ScenePath, true);
            rule.BuildSceneToDependenciesMap(new EditorBuildSettingsScene[] { editorScene });
            rule.IntersectSceneDependenciesWithBundleDependencies(new List<GUID>() {
                new GUID(AssetDatabase.AssetPathToGUID(k_CheckDupeMyMaterial)),
                new GUID(AssetDatabase.AssetPathToGUID(k_PrefabWithMaterialPath)) });

            Assert.IsTrue(rule.m_SceneToDependencies.ContainsKey(editorScene));
            Assert.AreEqual(2, rule.m_SceneToDependencies[editorScene].Count);
            Assert.IsTrue(rule.m_SceneToDependencies[editorScene].Contains(new GUID(AssetDatabase.AssetPathToGUID(k_CheckDupeMyMaterial))));
            Assert.IsTrue(rule.m_SceneToDependencies[editorScene].Contains(new GUID(AssetDatabase.AssetPathToGUID(k_PrefabWithMaterialPath))));

            //Cleanup
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            Settings.RemoveGroup(group1);
        }
    }
}