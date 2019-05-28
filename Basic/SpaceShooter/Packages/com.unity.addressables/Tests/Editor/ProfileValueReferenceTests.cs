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
            var group = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            var pid = Settings.profileSettings.GetProfileDataById(schema.BuildPath.Id);
            Assert.IsNotNull(pid);
            var varVal = Settings.profileSettings.GetValueById(Settings.activeProfileId, pid.Id);
            Assert.IsNotNull(varVal);
            var evalVal = Settings.profileSettings.EvaluateString(Settings.activeProfileId, varVal);
            var val = schema.BuildPath.GetValue(Settings);
            Assert.AreEqual(evalVal, val);
        }

        [Test]
        public void CanSetValueByName()
        {
            var group = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            bool callbackInvoked = false;
            schema.BuildPath.OnValueChanged += s => callbackInvoked = true;
            schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
            Assert.IsTrue(callbackInvoked);
            Assert.AreEqual(schema.BuildPath.Id, Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kLocalLoadPath).Id);
        }

        [Test]
        public void CanSetValueById()
        {
            var group = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            schema.BuildPath.SetVariableById(Settings, Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kLocalLoadPath).Id);
            Assert.AreEqual(schema.BuildPath.Id, Settings.profileSettings.GetProfileDataByName(AddressableAssetSettings.kLocalLoadPath).Id);
        }

        [Test]
        public void CallbackInvokedWhenValueChanged()
        {
            var group = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            bool callbackInvoked = false;
            schema.BuildPath.OnValueChanged += s => callbackInvoked = true;
            schema.BuildPath.SetVariableByName(Settings, AddressableAssetSettings.kLocalLoadPath);
            Assert.IsTrue(callbackInvoked);
        }

        [Test]
        public void CallbackNotInvokedWhenValueNotChanged()
        {
            var group = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(group);
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            Assert.IsNotNull(schema);
            bool callbackInvoked = false;
            schema.BuildPath.OnValueChanged += s => callbackInvoked = true;
            schema.BuildPath.SetVariableById(Settings, "invalid id");
            Assert.IsFalse(callbackInvoked);
        }
    }
}