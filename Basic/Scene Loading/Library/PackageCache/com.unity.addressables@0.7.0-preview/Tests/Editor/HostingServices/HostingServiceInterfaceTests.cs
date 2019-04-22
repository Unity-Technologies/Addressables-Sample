using System;
using System.IO;
using NUnit.Framework;
using UnityEditor.AddressableAssets.HostingServices;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests.HostingServices
{
    [TestFixtureSource("HostingServices")]
    public class HostingServiceInterfaceTests
    {
        protected const string k_TestConfigName = "AddressableAssetSettings.HostingServiceInterfaceTests";
        protected const string k_TestConfigFolder = "Assets/AddressableAssetsData_HostingServiceInterfaceTests";

        // ReSharper disable once UnusedMember.Local
        static IHostingService[] HostingServices
        {
            get
            {
                return new[]
                {
                    new HttpHostingService() as IHostingService
                };
            }
        }

        readonly IHostingService m_Service;

        public HostingServiceInterfaceTests(IHostingService svc)
        {
            m_Service = svc;
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            if (Directory.Exists(k_TestConfigFolder))
                AssetDatabase.DeleteAsset(k_TestConfigFolder);
            EditorBuildSettings.RemoveConfigObject(k_TestConfigName);
        }

        IHostingService GetManagedService(out AddressableAssetSettings settings)
        {
            var m = new HostingServicesManager();
            settings = AddressableAssetSettings.Create(k_TestConfigFolder, k_TestConfigName, false, false);
            settings.HostingServicesManager = m;
            var group = settings.CreateGroup("testGroup", false, false, false, null);
            group.AddSchema<BundledAssetGroupSchema>();
            settings.groups.Add(group);
            m.Initialize(settings);
            return m.AddHostingService(m_Service.GetType(), "test");
        }

        // HostingServiceContentRoots

        [Test]
        public void HostingServiceContentRootsShould_ReturnListOfContentRoots()
        {
            AddressableAssetSettings s;
            var svc = GetManagedService(out s);
            Assert.IsNotEmpty(s.groups);
            Assert.IsNotEmpty(svc.HostingServiceContentRoots);
            var schema = s.groups[0].GetSchema<BundledAssetGroupSchema>();
            Assert.Contains(schema.HostingServicesContentRoot, svc.HostingServiceContentRoots);
        }

        // ProfileVariables

        [Test]
        public void ProfileVariablesShould_ReturnProfileVariableKeyValuePairs()
        {
            var vars = m_Service.ProfileVariables;
            Assert.IsNotEmpty(vars);
        }

        // IsHostingServiceRunning, StartHostingService, and StopHostingService

        [Test]
        public void IsHostingServiceRunning_StartHostingService_StopHostingService()
        {
            AddressableAssetSettings s;
            var svc = GetManagedService(out s);
            Assert.IsFalse(svc.IsHostingServiceRunning);
            svc.StartHostingService();
            Assert.IsTrue(svc.IsHostingServiceRunning);
            svc.StopHostingService();
            Assert.IsFalse(svc.IsHostingServiceRunning);
        }

        // OnBeforeSerialize

        [Test]
        public void OnBeforeSerializeShould_PersistExpectedDataToKeyDataStore()
        {
            var data = new KeyDataStore();
            m_Service.DescriptiveName = "Testing 123";
            m_Service.InstanceId = 123;
            m_Service.HostingServiceContentRoots.Clear();
            m_Service.HostingServiceContentRoots.AddRange(new[] {"/test123", "/test456"});
            m_Service.OnBeforeSerialize(data);
            Assert.AreEqual("Testing 123", data.GetData("DescriptiveName", string.Empty));
            Assert.AreEqual(123, data.GetData("InstanceId", 0));
            Assert.AreEqual("/test123;/test456", data.GetData("ContentRoot", string.Empty));
        }

        // OnAfterDeserialize

        [Test]
        public void OnAfterDeserializeShould_RestoreExpectedDataFromKeyDataStore()
        {
            var data = new KeyDataStore();
            data.SetData("DescriptiveName", "Testing 123");
            data.SetData("InstanceId", 123);
            data.SetData("ContentRoot", "/test123;/test456");
            m_Service.OnAfterDeserialize(data);
            Assert.AreEqual("Testing 123", m_Service.DescriptiveName);
            Assert.AreEqual(123, m_Service.InstanceId);
            Assert.Contains("/test123", m_Service.HostingServiceContentRoots);
            Assert.Contains("/test456", m_Service.HostingServiceContentRoots);
        }

        // EvaluateProfileString

        [Test]
        public void EvaluateProfileStringShould_CorrectlyReplaceKeyValues()
        {
            var vars = m_Service.ProfileVariables;
            vars.Add("foo", "bar");
            var val = m_Service.EvaluateProfileString("foo");
            Assert.AreEqual("bar", val);
        }

        [Test]
        public void EvaluateProfileStringShould_ReturnNullForNonMatchingKey()
        {
            var val = m_Service.EvaluateProfileString("foo2");
            Assert.IsNull(val);
        }

        // RegisterLogger

        [Test]
        public void LoggerShould_UseTheProvidedLogger()
        {
            var l = new Logger(Debug.unityLogger.logHandler);
            m_Service.Logger = l;
            Assert.AreEqual(l, m_Service.Logger);
        }

        [Test]
        public void RegisterLoggerShould_UseTheDebugUnityLoggerWhenParamIsNull()
        {
            m_Service.Logger = null;
            Assert.AreEqual(Debug.unityLogger, m_Service.Logger);
        }

        // DescriptiveName

        [Test]
        public void DescriptiveNameShould_AllowGetAndSetOfDescriptiveName()
        {
            m_Service.DescriptiveName = "test";
            Assert.AreEqual("test", m_Service.DescriptiveName);
        }

        // InstanceId

        [Test]
        public void InstanceIdShould_AllowGetAndSetOfInstanceId()
        {
            m_Service.InstanceId = 999;
            Assert.AreEqual(999, m_Service.InstanceId);
        }
    }
}