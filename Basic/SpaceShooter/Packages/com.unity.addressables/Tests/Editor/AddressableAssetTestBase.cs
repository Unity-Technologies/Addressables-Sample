using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.AddressableAssets.Tests
{
    public abstract class AddressableAssetTestBase
    {
        protected const string k_TestConfigName = "AddressableAssetSettings.Tests";
        protected const string k_TestConfigFolder = "Assets/AddressableAssetsData_AddressableAssetSettingsTests";

        private AddressableAssetSettings m_Settings;

        protected AddressableAssetSettings Settings
        {
            get
            {
                if (m_Settings == null)
                    m_Settings = AddressableAssetSettings.Create(k_TestConfigFolder, k_TestConfigName, true, PersistSettings);
                return m_Settings;
            }
        }
        protected string m_AssetGUID;
        protected virtual bool PersistSettings { get { return true; } }
        [OneTimeSetUp]
        public void Init()
        {
            //TODO: Remove when NSImage warning issue on bokken is fixed
            Application.logMessageReceived += CheckLogForWarning;

            if (Directory.Exists(k_TestConfigFolder))
                AssetDatabase.DeleteAsset(k_TestConfigFolder);
            if (!Directory.Exists(k_TestConfigFolder))
            {
                Directory.CreateDirectory(k_TestConfigFolder);
                AssetDatabase.Refresh();
            }

            Settings.labelTable.labelNames.Clear();
            GameObject testObject = new GameObject("TestObject");
#if UNITY_2018_3_OR_NEWER
            PrefabUtility.SaveAsPrefabAsset(testObject, k_TestConfigFolder + "/test.prefab");
#else
            PrefabUtility.CreatePrefab(k_TestConfigFolder + "/test.prefab", testObject);
#endif
            m_AssetGUID = AssetDatabase.AssetPathToGUID(k_TestConfigFolder + "/test.prefab");
            OnInit();

            //TODO: Remove when NSImage warning issue on bokken is fixed
            //Removing here in the event we didn't recieve any messages during the setup, we can respond appropriately to 
            //logs in the tests.
            Application.logMessageReceived -= CheckLogForWarning;
            if (resetFailingMessages)
                LogAssert.ignoreFailingMessages = false;
        }

        private bool resetFailingMessages = false;
        //TODO: Remove when NSImage warning issue on bokken is fixed
        private void CheckLogForWarning(string condition, string stackTrace, LogType type)
        {
            LogAssert.ignoreFailingMessages = true;
            resetFailingMessages = true;
        }

        protected virtual void OnInit() { }

        [OneTimeTearDown]
        public void Cleanup()
        {
            OnCleanup();
            if (Directory.Exists(k_TestConfigFolder))
                AssetDatabase.DeleteAsset(k_TestConfigFolder);
            EditorBuildSettings.RemoveConfigObject(k_TestConfigName);
        }

        protected virtual void OnCleanup()
        {
        }
    }
}