using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor.AddressableAssets.Settings.Tests
{
    public class BuiltinSceneCacheTests
    {
        const string kTestScenePath = "Assets/TestScenes";
        const int kTestSceneCount = 3;
        private EditorBuildSettingsScene []m_OldScenes;
        private GUID[] m_TestGUIDs;

        private static string GetTestScenePath(int index)
        {
            return string.Format("{0}/myscene{1}.unity", kTestScenePath, index);
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Since this system is a global. We need to shutdown global state
            GlobalInitialization.ShutdownGlobalState();

            // Create folder for test scenes
            Directory.CreateDirectory("Assets/TestScenes");
            m_OldScenes = EditorBuildSettings.scenes;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            for (int i = 0; i < kTestSceneCount; i++)
                EditorSceneManager.SaveScene(scene, GetTestScenePath(i), true);

            AssetDatabase.Refresh();

            m_TestGUIDs = new GUID[kTestSceneCount];
            for (int i = 0; i < kTestSceneCount; i++)
                m_TestGUIDs[i] = new GUID(AssetDatabase.AssetPathToGUID(GetTestScenePath(i)));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            EditorBuildSettings.scenes = m_OldScenes;
            Directory.Delete(kTestScenePath, true);
            GlobalInitialization.InitializeGlobalState();
        }

        private void SetupBuildScenes()
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[2];
            scenes[0] = new EditorBuildSettingsScene(GetTestScenePath(0), true);
            scenes[1] = new EditorBuildSettingsScene(GetTestScenePath(1), true);
            EditorBuildSettings.scenes = scenes;
        }

        [SetUp]
        public void Setup()
        {
            SetupBuildScenes();
            BuiltinSceneCache.ClearState();
        }

        [Test]
        public void CanAccessSceneData()
        {
            Assert.AreEqual(m_TestGUIDs[0], BuiltinSceneCache.scenes[0].guid);
            Assert.AreEqual(m_TestGUIDs[1], BuiltinSceneCache.scenes[1].guid);
        }

        [Test]
        public void WhenScenesChange_CallbackInvoked()
        {
            int[] called = new int[] { 0 };
            Action callback = () => called[0]++;
            EditorBuildSettingsScene[] scenes = BuiltinSceneCache.scenes;
            scenes[0].enabled = !scenes[0].enabled;
            BuiltinSceneCache.sceneListChanged += callback;
            BuiltinSceneCache.scenes = scenes;
            Assert.AreEqual(1, called[0]);

            // Set Directly through API
            scenes[0].enabled = !scenes[0].enabled;
            EditorBuildSettings.scenes = scenes;
            Assert.AreEqual(2, called[0]);
        }

        [Test]
        public void WhenSceneOrderChanges_GetSceneIndexCacheUpdates()
        {
            Assert.AreEqual(0, BuiltinSceneCache.GetSceneIndex(m_TestGUIDs[0]));
            Assert.AreEqual(1, BuiltinSceneCache.GetSceneIndex(m_TestGUIDs[1]));

            // Insert new scene
            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(BuiltinSceneCache.scenes);
            list.Insert(0, new EditorBuildSettingsScene(m_TestGUIDs[2], true));
            BuiltinSceneCache.scenes = list.ToArray();

            Assert.AreEqual(1, BuiltinSceneCache.GetSceneIndex(m_TestGUIDs[0]));
            Assert.AreEqual(2, BuiltinSceneCache.GetSceneIndex(m_TestGUIDs[1]));
            Assert.AreEqual(0, BuiltinSceneCache.GetSceneIndex(m_TestGUIDs[2]));
        }
    }
}