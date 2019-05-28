using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;

namespace UnityEditor.AddressableAssets.Tests.AnalyzeRules
{
    class CheckBundleDupeDependencyTests : AddressableAssetTestBase
    {
        const string k_CheckDupePrefabA = k_TestConfigFolder + "/checkDupe_prefabA.prefab";
        const string k_CheckDupePrefabB = k_TestConfigFolder + "/checkDupe_prefabB.prefab";
        const string k_CheckDupeMyMaterial = k_TestConfigFolder + "/checkDupe_myMaterial.mat";

        protected override void OnInit()
        {
            base.OnInit();

            GameObject prefabA = new GameObject("PrefabA");
            GameObject prefabB = new GameObject("PrefabB");
            var meshA = prefabA.AddComponent<MeshRenderer>();
            var meshB = prefabB.AddComponent<MeshRenderer>();


            var mat = new Material(Shader.Find("Unlit/Color"));
            AssetDatabase.CreateAsset(mat, k_CheckDupeMyMaterial);
            meshA.sharedMaterial = mat;
            meshB.sharedMaterial = mat;

#if UNITY_2018_3_OR_NEWER
            PrefabUtility.SaveAsPrefabAsset(prefabA, k_CheckDupePrefabA);
            PrefabUtility.SaveAsPrefabAsset(prefabB, k_CheckDupePrefabB);
#else
            PrefabUtility.CreatePrefab(k_CheckDupePrefabA, prefabA);
            PrefabUtility.CreatePrefab(k_CheckDupePrefabB, prefabB);
#endif
            AssetDatabase.Refresh();
        }

        private AddressableAssetsBuildContext GetAddressableAssetsBuildContext(CheckBundleDupeDependencies rule)
        {
            ResourceManagerRuntimeData runtimeData = new ResourceManagerRuntimeData();
            runtimeData.LogResourceManagerExceptions = Settings.buildSettings.LogResourceManagerExceptions;
            var aaContext = new AddressableAssetsBuildContext
            {
                settings = Settings,
                runtimeData = runtimeData,
                bundleToAssetGroup = rule.m_BundleToAssetGroup,
                locations = rule.m_Locations
            };
            return aaContext;
        }

        [Test]
        public void CheckDupeCanFixIssues()
        {
            var duplicateGroups = Settings.groups.Where(group => group.Name.Contains("Duplicate Asset Isolation"));
            foreach (AddressableAssetGroup group in duplicateGroups)
                Settings.groups.Remove(group);

            var group1 = Settings.CreateGroup("CheckDupeDepencency1", false, false, false, null, typeof(BundledAssetGroupSchema));
            var group2 = Settings.CreateGroup("CheckDupeDepencency2", false, false, false, null, typeof(BundledAssetGroupSchema));

            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(k_CheckDupePrefabA), group1, false, false);
            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(k_CheckDupePrefabB), group2, false, false);

            var matGuid = AssetDatabase.AssetPathToGUID(k_CheckDupeMyMaterial);
            Assert.IsNull(group1.GetAssetEntry(matGuid));
            Assert.IsNull(group2.GetAssetEntry(matGuid));

            var groupCount = Settings.groups.Count;

            var rule = new CheckBundleDupeDependencies();
            rule.FixIssues(Settings);

            Assert.AreEqual(groupCount+1, Settings.groups.Count);

            var dupeGroup = Settings.FindGroup("Duplicate Asset Isolation");
            Assert.IsNotNull(dupeGroup);
            Assert.IsNotNull(dupeGroup.GetAssetEntry(matGuid));
        }

        [Test]
        public void CheckDupeNoIssuesIfValid()
        {
            var group1 = Settings.CreateGroup("CheckDupeDepencency1", false, false, false, null, typeof(BundledAssetGroupSchema));
            var group2 = Settings.CreateGroup("CheckDupeDepencency2", false, false, false, null, typeof(BundledAssetGroupSchema));

            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(k_CheckDupePrefabA), group1, false, false);
            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(k_CheckDupePrefabB), group2, false, false);
            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(k_CheckDupeMyMaterial), group2, false, false);

            var groupCount = Settings.groups.Count;

            var rule = new CheckBundleDupeDependencies();
            rule.FixIssues(Settings);

            Assert.AreEqual(groupCount, Settings.groups.Count);
        }

        [Test]
        public void UniqueAssetBundleBuild_IsCreatedWithCorrectData()
        {
            string bundleName = "myBundleName.bundle";
            string assetGroup = "assetGroup";

            var rule = new CheckBundleDupeDependencies();
            rule.m_BundleToAssetGroup.Add(bundleName, assetGroup);

            AssetBundleBuild build = new AssetBundleBuild { assetBundleName = bundleName };

            for (int i = 1; i < 500; i++)
            {
                AssetBundleBuild uniqueBundle = rule.CreateUniqueBundle(build);
                Assert.AreEqual(string.Format("myBundleName{0}.bundle", i), uniqueBundle.assetBundleName);
                rule.m_BundleToAssetGroup.Add(uniqueBundle.assetBundleName, assetGroup); 
            }
        }

        [Test]
        public void RefreshBuild_ReturnsSuccess()
        {
            var rule = new CheckBundleDupeDependencies();
            AddressableAssetsBuildContext context = GetAddressableAssetsBuildContext(rule);
            Assert.AreEqual(ReturnCode.Success, rule.RefreshBuild(context));
        }

        [Test]
        public void BuildImplicitDuplicatedAssetsSet_BuildCorrectImplicitAssets()
        {
            //Setup
            var group1 = Settings.CreateGroup("CheckDupeDepencency1", false, false, false, null, typeof(BundledAssetGroupSchema));
            var group2 = Settings.CreateGroup("CheckDupeDepencency2", false, false, false, null, typeof(BundledAssetGroupSchema));

            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(k_CheckDupePrefabA), group1, false, false);
            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(k_CheckDupePrefabB), group2, false, false);

            GUID matGuid;
            var matGuidString = AssetDatabase.AssetPathToGUID(k_CheckDupeMyMaterial);
            GUID.TryParse(matGuidString, out matGuid);

            var rule = new CheckBundleDupeDependencies();
            var buildContext = rule.GetBuildContext(Settings);

            rule.CalculateInputDefinitions(Settings);
            rule.RefreshBuild(buildContext);

            var implicitGuids = rule.GetImplicitGuidToFilesMap();
            var results = rule.CalculateDuplicates(implicitGuids, buildContext);

            //Test
            rule.BuildImplicitDuplicatedAssetsSet(results);

            //Assert
            Assert.AreEqual(1, rule.m_ImplicitAssets.Count);
            Assert.IsTrue(rule.m_ImplicitAssets.Contains(matGuid));
        }

        [Test]
        public void CalculateInputDefinitions_CalculatesAllInputDefintions()
        {
            //Setup
            var group1 = Settings.CreateGroup("CheckDupeDepencency1", false, false, false, null, typeof(BundledAssetGroupSchema));
            var group2 = Settings.CreateGroup("CheckDupeDepencency2", false, false, false, null, typeof(BundledAssetGroupSchema));

            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(k_CheckDupePrefabA), group1, false, false);
            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(k_CheckDupePrefabB), group2, false, false);

            GUID matGuid;
            var matGuidString = AssetDatabase.AssetPathToGUID(k_CheckDupeMyMaterial);
            GUID.TryParse(matGuidString, out matGuid);

            var rule = new CheckBundleDupeDependencies();

            //Test
            rule.CalculateInputDefinitions(Settings);

            //Assert
            Assert.AreEqual(2, rule.m_AllBundleInputDefs.Count);
        }

        [Test]
        public void CalculateDuplicates_ReturnsCorrectCheckDupeResultList()
        {
            //Setup
            var group1 = Settings.CreateGroup("CheckDupeDepencency1", false, false, false, null, typeof(BundledAssetGroupSchema));
            var group2 = Settings.CreateGroup("CheckDupeDepencency2", false, false, false, null, typeof(BundledAssetGroupSchema));

            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(k_CheckDupePrefabA), group1, false, false);
            Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(k_CheckDupePrefabB), group2, false, false);

            GUID matGuid;
            var matGuidString = AssetDatabase.AssetPathToGUID(k_CheckDupeMyMaterial);
            GUID.TryParse(matGuidString, out matGuid);

            var rule = new CheckBundleDupeDependencies();

            var buildContext = rule.GetBuildContext(Settings);
            rule.CalculateInputDefinitions(Settings);
            rule.RefreshBuild(buildContext);
            var implicitGuids = rule.GetImplicitGuidToFilesMap();
            
            //Test
            var dupeResults = rule.CalculateDuplicates(implicitGuids, buildContext).ToList();
            
            //Assert
            Assert.AreEqual(2, dupeResults.Count);

            Assert.AreEqual(group1.Name, dupeResults[0].Group.Name);
            Assert.AreEqual(2, dupeResults[0].DuplicatedFiles.Count);
            Assert.AreEqual(k_CheckDupeMyMaterial, dupeResults[0].AssetPath);

            Assert.AreEqual(group2.Name, dupeResults[1].Group.Name);
            Assert.AreEqual(2, dupeResults[1].DuplicatedFiles.Count);
            Assert.AreEqual(k_CheckDupeMyMaterial, dupeResults[1].AssetPath);
        }
    }
}