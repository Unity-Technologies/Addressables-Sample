using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TestTools;

namespace UnityEditor.AddressableAssets.Tests
{
    public class ContentUpdateTests : AddressableAssetTestBase
    {
        protected override bool PersistSettings { get { return true; } }

        [Test]
        public void CanCreateContentStateData()
        {
            var group = m_Settings.CreateGroup("LocalStuff", false, false, false, null);
            var schema = group.AddSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(m_Settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(m_Settings, AddressableAssetSettings.kLocalLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            group.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

            m_Settings.CreateOrMoveEntry(m_AssetGUID, group);
            var context = new AddressablesDataBuilderInput(m_Settings);

            var op = m_Settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);

            Assert.IsTrue(string.IsNullOrEmpty(op.Error), op.Error);
            var tempPath = Path.GetDirectoryName(Application.dataPath) + "/Library/com.unity.addressables/StreamingAssetsCopy/" + PlatformMappingService.GetPlatform() + "/addressables_content_state.bin";
            var cacheData = ContentUpdateScript.LoadContentState(tempPath);
            Assert.NotNull(cacheData);
        }

        [Test]
        public void PrepareContentUpdate()
        {
            var group = m_Settings.CreateGroup("LocalStuff2", false, false, false, null);
            var schema = group.AddSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(m_Settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(m_Settings, AddressableAssetSettings.kLocalLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            group.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;

            var entry = m_Settings.CreateOrMoveEntry(m_AssetGUID, group);
            entry.address = "test";

            var context = new AddressablesDataBuilderInput(m_Settings);

            m_Settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);

            var path = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            obj.GetComponent<Transform>().SetPositionAndRotation(new Vector3(10, 10, 10), Quaternion.identity);
#if UNITY_2018_3_OR_NEWER
            PrefabUtility.SavePrefabAsset(obj);
#else
            EditorUtility.SetDirty(obj);
#endif
            AssetDatabase.SaveAssets();
            var tempPath = Path.GetDirectoryName(Application.dataPath) + "/Library/com.unity.addressables/StreamingAssetsCopy/" + PlatformMappingService.GetPlatform() + "/addressables_content_state.bin";
            var modifiedEntries = ContentUpdateScript.GatherModifiedEntries(m_Settings, tempPath);
            Assert.IsNotNull(modifiedEntries);
            Assert.GreaterOrEqual(modifiedEntries.Count, 1);
            ContentUpdateScript.CreateContentUpdateGroup(m_Settings, modifiedEntries, "Content Update");
            var contentGroup = m_Settings.FindGroup("Content Update");
            Assert.IsNotNull(contentGroup);
            var movedEntry = contentGroup.GetAssetEntry(m_AssetGUID);
            Assert.AreSame(movedEntry, entry);
        }

        [Test]
        public void BuildContentUpdate()
        {
            var group = m_Settings.CreateGroup("LocalStuff3", false, false, false, null);
            m_Settings.BuildRemoteCatalog = true;
            m_Settings.RemoteCatalogBuildPath = new ProfileValueReference();
            m_Settings.RemoteCatalogBuildPath.SetVariableByName(m_Settings, AddressableAssetSettings.kRemoteBuildPath);
            m_Settings.RemoteCatalogLoadPath = new ProfileValueReference();
            m_Settings.RemoteCatalogLoadPath.SetVariableByName(m_Settings, AddressableAssetSettings.kRemoteLoadPath);
            var schema = group.AddSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(m_Settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(m_Settings, AddressableAssetSettings.kLocalLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            group.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;
            m_Settings.CreateOrMoveEntry(m_AssetGUID, group);
            var context = new AddressablesDataBuilderInput(m_Settings);

            var op = m_Settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);

            Assert.IsTrue(string.IsNullOrEmpty(op.Error), op.Error);
            var tempPath = Path.GetDirectoryName(Application.dataPath) + "/Library/com.unity.addressables/StreamingAssetsCopy/" + PlatformMappingService.GetPlatform() + "/addressables_content_state.bin";
            var buildOp = ContentUpdateScript.BuildContentUpdate(m_Settings, tempPath);
            Assert.IsNotNull(buildOp);
            Assert.IsTrue(string.IsNullOrEmpty(buildOp.Error));
        }

        [Test]
        public void IsCacheDataValid_WhenNoPreviousRemoteCatalogPath_ReturnsFalseWithError()
        {
            AddressablesContentState cacheData = new AddressablesContentState();
            cacheData.editorVersion = Application.unityVersion;
            Assert.IsFalse(ContentUpdateScript.IsCacheDataValid(m_Settings, cacheData));
            LogAssert.Expect(LogType.Error, new Regex("Previous build had 'Build Remote Catalog' disabled.*"));
        }

        [Test]
        public void IsCacheDataValid_WhenRemoteCatalogDisabled_ReturnsFalseWithError()
        {
            AddressablesContentState cacheData = new AddressablesContentState();
            cacheData.editorVersion = Application.unityVersion;
            cacheData.remoteCatalogLoadPath = "somePath";
            var oldSetting = m_Settings.BuildRemoteCatalog;
            m_Settings.BuildRemoteCatalog = false;
            Assert.IsFalse(ContentUpdateScript.IsCacheDataValid(m_Settings, cacheData));
            LogAssert.Expect(LogType.Error, new Regex("Current settings have 'Build Remote Catalog' disabled.*"));
            m_Settings.BuildRemoteCatalog = oldSetting;
        }

        [Test]
        public void IsCacheDataValid_WhenMismatchedCatalogPaths_ReturnsFalseWithError()
        {
            AddressablesContentState cacheData = new AddressablesContentState();
            cacheData.editorVersion = Application.unityVersion;
            cacheData.remoteCatalogLoadPath = "somePath";
            var oldSetting = m_Settings.BuildRemoteCatalog;
            m_Settings.BuildRemoteCatalog = true;
            Assert.IsFalse(ContentUpdateScript.IsCacheDataValid(m_Settings, cacheData));
            LogAssert.Expect(LogType.Error, new Regex("Current 'Remote Catalog Load Path' does not match load path of original player.*"));
            m_Settings.BuildRemoteCatalog = oldSetting;
        }

        [Test]
        public void IsCacheDataValid_WhenMismatchedEditorVersions_LogsWarning()
        {
            AddressablesContentState cacheData = new AddressablesContentState();
            cacheData.editorVersion = "invalid";
            Assert.IsFalse(ContentUpdateScript.IsCacheDataValid(m_Settings, cacheData));
            LogAssert.Expect(LogType.Warning, new Regex(".*with version `" + cacheData.editorVersion + "`.*"));
            LogAssert.Expect(LogType.Error, new Regex("Previous.*"));
        }

        [Test]
        public void BuildContentUpdate_DoesNotDeleteBuiltData()
        {
            var group = m_Settings.CreateGroup("LocalStuff3", false, false, false, null);
            var schema = group.AddSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(m_Settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(m_Settings, AddressableAssetSettings.kLocalLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            group.AddSchema<ContentUpdateGroupSchema>().StaticContent = true;
            m_Settings.CreateOrMoveEntry(m_AssetGUID, group);
            var context = new AddressablesDataBuilderInput(m_Settings);

            var op = m_Settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);

            Assert.IsTrue(string.IsNullOrEmpty(op.Error), op.Error);
            var tempPath = Path.GetDirectoryName(Application.dataPath) + "/Library/com.unity.addressables/StreamingAssetsCopy/" + PlatformMappingService.GetPlatform() + "/addressables_content_state.bin";
            ContentUpdateScript.BuildContentUpdate(m_Settings, tempPath);
            Assert.IsTrue(Directory.Exists(Addressables.BuildPath));
        }
    }
}