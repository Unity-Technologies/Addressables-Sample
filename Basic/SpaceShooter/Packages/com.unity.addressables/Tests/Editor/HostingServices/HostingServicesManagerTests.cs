using System;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEditor.AddressableAssets.HostingServices;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests.HostingServices
{
    public class HostingServicesManagerTests
    {
        const string k_TestConfigName = "AddressableAssetSettings.HostingServicesManagerTests";
        const string k_TestConfigFolder = "Assets/AddressableAssetsData_HostingServicesManagerTests";

        HostingServicesManager m_Manager;
        AddressableAssetSettings m_Settings;

        [SetUp]
        public void Setup()
        {
            m_Manager = new HostingServicesManager();
            m_Settings = AddressableAssetSettings.Create(k_TestConfigFolder, k_TestConfigName, false, false);
            m_Settings.HostingServicesManager = m_Manager;
            var group = m_Settings.CreateGroup("testGroup", false, false, false, null);
            group.AddSchema<BundledAssetGroupSchema>();
            m_Settings.groups.Add(group);
        }

        [TearDown]
        public void TearDown()
        {
            var services = m_Manager.HostingServices.ToArray();
            foreach (var svc in services)
            {
                svc.StopHostingService();
                m_Manager.RemoveHostingService(svc);
            }
            if (Directory.Exists(k_TestConfigFolder))
                AssetDatabase.DeleteAsset(k_TestConfigFolder);
            EditorBuildSettings.RemoveConfigObject(k_TestConfigName);
        }

        // GlobalProfileVariables

        [Test]
        public void GlobalProfileVariablesShould_ReturnDictionaryOfKeyValuePairs()
        {
            var vars = m_Manager.GlobalProfileVariables;
            Assert.NotNull(vars);
        }

        [Test]
        public void GlobalProfileVariablesShould_ContainPrivateIpAddressKey()
        {
            m_Manager.Initialize(m_Settings);
            var vars = m_Manager.GlobalProfileVariables;
            Assert.NotNull(vars);
            const string key = HostingServicesManager.KPrivateIpAddressKey;
            Assert.Contains(key, vars.Keys);
            Assert.NotNull(vars[key]);
        }

        // IsInitialized

        [Test]
        public void IsInitializedShould_BecomeTrueAfterInitializeCall()
        {
            Assert.IsFalse(m_Manager.IsInitialized);
            m_Manager.Initialize(m_Settings);
            Assert.IsTrue(m_Manager.IsInitialized);
        }

        // HostingServices

        [Test]
        public void HostingServicesShould_ReturnListOfManagedServices()
        {
            m_Manager.Initialize(m_Settings);
            Assert.NotNull(m_Manager.HostingServices);
            Assert.IsEmpty(m_Manager.HostingServices);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test");
            Assert.IsTrue(m_Manager.HostingServices.Contains(svc));
        }

        // RegisteredServiceTypes

        [Test]
        public void RegisteredServiceTypesShould_AlwaysContainBuiltinServiceTypes()
        {
            Assert.NotNull(m_Manager.RegisteredServiceTypes);
            Assert.Contains(typeof(HttpHostingService), m_Manager.RegisteredServiceTypes);
        }

        [Test]
        public void RegisteredServiceTypesShould_NotContainDuplicates()
        {
            m_Manager.Initialize(m_Settings);
            m_Manager.AddHostingService(typeof(TestHostingService), "test1");
            m_Manager.AddHostingService(typeof(TestHostingService), "test2");
            Assert.IsTrue(m_Manager.RegisteredServiceTypes.Length == 1);
        }

        // NextInstanceId

        [Test]
        public void NextInstanceIdShould_IncrementAfterServiceIsAdded()
        {
            m_Manager.Initialize(m_Settings);
            Assert.IsTrue(m_Manager.NextInstanceId == 0);
            m_Manager.AddHostingService(typeof(TestHostingService), "test1");
            Assert.IsTrue(m_Manager.NextInstanceId == 1);
        }

        // Initialize

        [Test]
        public void InitializeShould_AssignTheGivenSettingsObject()
        {
            Assert.Null(m_Manager.Settings);
            m_Manager.Initialize(m_Settings);
            Assert.IsTrue(m_Manager.IsInitialized);
            Assert.NotNull(m_Manager.Settings);
            Assert.AreSame(m_Manager.Settings, m_Settings);
        }

        [Test]
        public void InitializeShould_SetGlobalProfileVariables()
        {
            Assert.IsTrue(m_Manager.GlobalProfileVariables.Count == 0);
            m_Manager.Initialize(m_Settings);
            Assert.IsTrue(m_Manager.IsInitialized);
            Assert.IsTrue(m_Manager.GlobalProfileVariables.Count > 0);
        }

        [Test]
        public void InitializeShould_OnlyInitializeOnce()
        {
            var so = ScriptableObject.CreateInstance<AddressableAssetSettings>();
            m_Manager.Initialize(m_Settings);
            Assert.IsTrue(m_Manager.IsInitialized);
            m_Manager.Initialize(so);
            Assert.AreSame(m_Manager.Settings, m_Settings);
            Assert.AreNotSame(m_Manager.Settings, so);
        }

        // StopAllService

        [Test]
        public void StopAllServicesShould_StopAllRunningServices()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test");
            svc.HostingServiceContentRoots.Add("/");
            Assert.IsFalse(svc.IsHostingServiceRunning);
            svc.StartHostingService();
            Assert.IsTrue(svc.IsHostingServiceRunning);
            m_Manager.StopAllServices();
            Assert.IsFalse(svc.IsHostingServiceRunning);
        }

        // StartAllServices

        [Test]
        public void StartAllServicesShould_StartAllStoppedServices()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test");
            svc.HostingServiceContentRoots.Add("/");
            Assert.IsFalse(svc.IsHostingServiceRunning);
            m_Manager.StartAllServices();
            Assert.IsTrue(svc.IsHostingServiceRunning);
        }

        // AddHostingService

        [Test]
        public void AddHostingServiceShould_ThrowIfTypeDoesNotImplementInterface()
        {
            Assert.Throws<ArgumentException>(() => { m_Manager.AddHostingService(typeof(object), "test"); });
        }

        [Test]
        public void AddHostingServiceShould_ThrowIfTypeIsAbstract()
        {
            Assert.Throws<MissingMethodException>(() =>
            {
                m_Manager.AddHostingService(typeof(AbstractTestHostingService), "test");
            });
        }

        [Test]
        public void AddHostingServiceShould_AddTypeToRegisteredServiceTypes()
        {
            m_Manager.Initialize(m_Settings);
            Assert.NotNull(m_Manager.RegisteredServiceTypes);
            m_Manager.AddHostingService(typeof(TestHostingService), "test");
            Assert.Contains(typeof(TestHostingService), m_Manager.RegisteredServiceTypes);
        }

        [Test]
        public void AddHostingServiceShould_RegisterLoggerForService()
        {
            m_Manager.Initialize(m_Settings);
            m_Manager.AddHostingService(typeof(TestHostingService), "test");
        }

        [Test]
        public void AddHostingServiceShould_SetDescriptiveNameOnService()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test");
            Assert.AreEqual(svc.DescriptiveName, "test");
        }

        [Test]
        public void AddHostingServiceShould_SetNextInstanceIdOnService()
        {
            m_Manager.Initialize(m_Settings);
            m_Manager.AddHostingService(typeof(TestHostingService), "test");
            m_Manager.AddHostingService(typeof(TestHostingService), "test");
            var id = m_Manager.NextInstanceId;
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test");
            Assert.AreEqual(id, svc.InstanceId);
        }

        [Test]
        public void AddHostingServiceShould_SetContentRootsOnService()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test");
            Assert.IsNotEmpty(svc.HostingServiceContentRoots);
        }

        [Test]
        public void AddHostingServiceShould_PostModificationEventToSettings()
        {
            var wait = new ManualResetEvent(false);

            m_Settings.OnModification = null;
            m_Settings.OnModification += (s, evt, obj) =>
            {
                if (evt == AddressableAssetSettings.ModificationEvent.HostingServicesManagerModified)
                    wait.Set();
            };

            m_Manager.Initialize(m_Settings);
            m_Manager.AddHostingService(typeof(TestHostingService), "test");
            Assert.IsTrue(wait.WaitOne(100));
        }

        [Test]
        public void AddHostingServiceShould_ReturnServiceInstance()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test");
            Assert.NotNull(svc);
        }

        [Test]
        public void AddHostingServiceShould_RegisterStringEvalFuncs()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test");
            Assert.IsTrue(ProfileStringEvalDelegateIsRegistered(m_Settings, svc));
        }

        // RemoveHostingService

        [Test]
        public void RemoveHostingServiceShould_StopRunningService()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test");
            Assert.IsFalse(svc.IsHostingServiceRunning);
            svc.StartHostingService();
            Assert.IsTrue(svc.IsHostingServiceRunning);
            m_Manager.RemoveHostingService(svc);
            Assert.IsFalse(svc.IsHostingServiceRunning);
        }

        [Test]
        public void RemoveHostingServiceShould_UnregisterStringEvalFuncs()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test");
            Assert.IsTrue(ProfileStringEvalDelegateIsRegistered(m_Settings, svc));
            m_Manager.RemoveHostingService(svc);
            Assert.IsFalse(ProfileStringEvalDelegateIsRegistered(m_Settings, svc));
        }

        [Test]
        public void RemoveHostingServiceShould_PostModificationEventToSettings()
        {
            var wait = new ManualResetEvent(false);

            m_Settings.OnModification = null;
            m_Settings.OnModification += (s, evt, obj) =>
            {
                if (evt == AddressableAssetSettings.ModificationEvent.HostingServicesManagerModified)
                    wait.Set();
            };

            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test");
            m_Manager.RemoveHostingService(svc);
            Assert.IsTrue(wait.WaitOne(100));
        }

        // OnEnable

        [Test]
        public void OnEnableShould_RegisterForSettingsModificationEvents()
        {
            var len = m_Settings.OnModification.GetInvocationList().Length;
            m_Manager.Initialize(m_Settings);
            m_Manager.OnEnable();
            Assert.Greater(m_Settings.OnModification.GetInvocationList().Length, len);
        }

        [Test]
        public void OnEnableShould_RegisterProfileStringEvalFuncsForServices()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test");
            m_Settings.profileSettings.onProfileStringEvaluation = null;
            m_Manager.OnEnable();
            Assert.IsTrue(ProfileStringEvalDelegateIsRegistered(m_Settings, svc));
        }

        [Test]
        public void OnEnableShould_RegisterLoggerWithServices()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test") as TestHostingService;
            Assert.NotNull(svc);
            svc.Logger = null;
            Assert.Null(svc.Logger);
            m_Manager.OnEnable();
            Assert.NotNull(svc.Logger);
        }

        [Test]
        public void OnEnableShould_RegisterProfileStringEvalFuncForManager()
        {
            m_Manager.Initialize(m_Settings);
            m_Manager.OnEnable();
            Assert.IsTrue(ProfileStringEvalDelegateIsRegistered(m_Settings, m_Manager));
        }

        [Test]
        public void OnEnableShould_RefreshGlobalProfileVariables()
        {
            m_Manager.Initialize(m_Settings);
            m_Manager.GlobalProfileVariables.Clear();
            m_Manager.OnEnable();
            Assert.GreaterOrEqual(m_Manager.GlobalProfileVariables.Count, 1);
        }

        // OnDisable

        [Test]
        public void OnDisableShould_DeRegisterForSettingsModificationEvents()
        {
            var len = m_Settings.OnModification.GetInvocationList().Length;
            m_Manager.Initialize(m_Settings);
            m_Manager.OnEnable();
            m_Manager.OnEnable();
            m_Manager.OnEnable();
            Assert.Greater(m_Settings.OnModification.GetInvocationList().Length, len);
            m_Manager.OnDisable();
            Assert.AreEqual(len, m_Settings.OnModification.GetInvocationList().Length);
        }

        [Test]
        public void OnEnableShould_UnregisterProfileStringEvalFuncForManager()
        {
            m_Manager.Initialize(m_Settings);
            m_Manager.OnEnable();
            Assert.IsTrue(ProfileStringEvalDelegateIsRegistered(m_Settings, m_Manager));
            m_Manager.OnDisable();
            Assert.IsFalse(ProfileStringEvalDelegateIsRegistered(m_Settings, m_Manager));
        }

        [Test]
        public void OnDisableShould_RegisterNullLoggerForServices()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test") as TestHostingService;
            Assert.IsNotNull(svc);
            m_Manager.Initialize(m_Settings);
            m_Manager.OnEnable();
            Assert.IsNotNull(svc.Logger);
            m_Manager.OnDisable();
            Assert.IsNull(svc.Logger);
        }

        [Test]
        public void OnDisableShould_DeRegisterProfileStringEvalFuncsForServices()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test") as TestHostingService;
            Assert.IsNotNull(svc);
            m_Manager.Initialize(m_Settings);
            m_Manager.OnEnable();
            Assert.IsTrue(ProfileStringEvalDelegateIsRegistered(m_Settings, svc));
            m_Manager.OnDisable();
            Assert.IsFalse(ProfileStringEvalDelegateIsRegistered(m_Settings, svc));
        }

        // RegisterLogger

        [Test]
        public void LoggerShould_SetLoggerForManagerAndManagedServices()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test") as TestHostingService;
            Assert.IsNotNull(svc);
            m_Manager.Initialize(m_Settings);
            var logger = new Logger(Debug.unityLogger.logHandler);
            Assert.AreNotEqual(logger, svc.Logger);
            Assert.AreNotEqual(logger, m_Manager.Logger);
            m_Manager.Logger = logger;
            Assert.AreEqual(logger, svc.Logger);
            Assert.AreEqual(logger, m_Manager.Logger);
        }

        [Test]
        public void LoggerShould_SetDebugUnityLoggerIfNull()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test") as TestHostingService;
            Assert.IsNotNull(svc);
            m_Manager.Initialize(m_Settings);
            var logger = new Logger(Debug.unityLogger.logHandler);
            m_Manager.Logger = logger;
            Assert.AreNotEqual(Debug.unityLogger, svc.Logger);
            Assert.AreNotEqual(Debug.unityLogger, m_Manager.Logger);
            m_Manager.Logger = null;
            Assert.AreEqual(Debug.unityLogger, svc.Logger);
            Assert.AreEqual(Debug.unityLogger, m_Manager.Logger);
        }

        // RefreshGLobalProfileVariables

        [Test]
        public void RefreshGlobalProfileVariablesShould_AddOrUpdatePrivateIpAddressVar()
        {
            m_Manager.GlobalProfileVariables.Clear();
            Assert.IsEmpty(m_Manager.GlobalProfileVariables);
            m_Manager.RefreshGlobalProfileVariables();
            Assert.IsNotEmpty(m_Manager.GlobalProfileVariables);
        }

        [Test]
        public void RefreshGlobalProfileVariablesShould_RemoveUnknownVars()
        {
            m_Manager.GlobalProfileVariables.Add("test", "test");
            Assert.IsTrue(m_Manager.GlobalProfileVariables.ContainsKey("test"));
            m_Manager.RefreshGlobalProfileVariables();
            Assert.IsFalse(m_Manager.GlobalProfileVariables.ContainsKey("test"));
        }

        // BatchMode

        [Test]
        public void BatchModeShould_InitializeManagerWithDefaultSettings()
        {
            Assert.IsFalse(m_Manager.IsInitialized);
            HostingServicesManager.BatchMode(m_Settings);
            Assert.IsTrue(m_Manager.IsInitialized);
        }

        [Test]
        public void BatchModeShould_StartAllServices()
        {
            m_Manager.Initialize(m_Settings);
            var svc = m_Manager.AddHostingService(typeof(TestHostingService), "test");
            Assert.IsFalse(svc.IsHostingServiceRunning);
            HostingServicesManager.BatchMode(m_Settings);
            Assert.IsTrue(svc.IsHostingServiceRunning);
        }

        static bool ProfileStringEvalDelegateIsRegistered(AddressableAssetSettings s, IHostingService svc)
        {
            var del = new AddressableAssetProfileSettings.ProfileStringEvaluationDelegate(svc.EvaluateProfileString);
            var list = s.profileSettings.onProfileStringEvaluation.GetInvocationList();
            return list.Contains(del);
        }

        static bool ProfileStringEvalDelegateIsRegistered(AddressableAssetSettings s, HostingServicesManager m)
        {
            var del = new AddressableAssetProfileSettings.ProfileStringEvaluationDelegate(m.EvaluateGlobalProfileVariableKey);
            var list = s.profileSettings.onProfileStringEvaluation.GetInvocationList();
            return list.Contains(del);
        }
    }
}