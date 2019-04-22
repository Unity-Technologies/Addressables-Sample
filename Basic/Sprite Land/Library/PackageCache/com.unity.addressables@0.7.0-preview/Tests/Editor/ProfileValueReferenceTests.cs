using System;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace UnityEditor.AddressableAssets.Tests
{
    public class ProfileValueReferenceTests : AddressableAssetTestBase
    {
        [Test]
        public void IsValueValid()
        {
            var group = m_Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            var pid = m_Settings.profileSettings.GetProfileDataById(schema.BuildPath.Id);
            Assert.IsNotNull(pid);
            var varVal = m_Settings.profileSettings.GetValueById(m_Settings.activeProfileId, pid.Id);
            Assert.IsNotNull(varVal);
            var evalVal = m_Settings.profileSettings.EvaluateString(m_Settings.activeProfileId, varVal);
            var val = schema.BuildPath.GetValue(m_Settings);
            Assert.AreEqual(evalVal, val);
        }

        [Test]
        public void CanSetValueByName()
        {
            var group = m_Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            bool callbackInvoked = false;
            schema.BuildPath.OnValueChanged += s => callbackInvoked = true;
            schema.BuildPath.SetVariableByName(m_Settings, AddressableAssetSettings.kLocalLoadPath);
            Assert.IsTrue(callbackInvoked);
            Assert.AreEqual(schema.BuildPath.Id, m_Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kLocalLoadPath).Id);
        }

        [Test]
        public void CanSetValueById()
        {
            var group = m_Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            schema.BuildPath.SetVariableById(m_Settings, m_Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kLocalLoadPath).Id);
            Assert.AreEqual(schema.BuildPath.Id, m_Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kLocalLoadPath).Id);
        }

        [Test]
        public void CallbackInvokedWhenValueChanged()
        {
            var group = m_Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            bool callbackInvoked = false;
            schema.BuildPath.OnValueChanged += s => callbackInvoked = true;
            schema.BuildPath.SetVariableByName(m_Settings, AddressableAssetSettings.kLocalLoadPath);
            Assert.IsTrue(callbackInvoked);
        }

        [Test]
        public void CallbackNotInvokedWhenValueNotChanged()
        {
            var group = m_Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            bool callbackInvoked = false;
            schema.BuildPath.OnValueChanged += s => callbackInvoked = true;
            schema.BuildPath.SetVariableById(m_Settings, "invalid id");
            Assert.IsFalse(callbackInvoked);
        }
    }
}