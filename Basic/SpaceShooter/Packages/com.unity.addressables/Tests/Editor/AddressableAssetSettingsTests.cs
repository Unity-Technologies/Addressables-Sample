using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UnityEditor.AddressableAssets.Tests
{
    public class AddressableAssetSettingsTests : AddressableAssetTestBase
    {
        [Test]
        public void HasDefaultInitialGroups()
        {
            Assert.IsNotNull(Settings.FindGroup(AddressableAssetSettings.PlayerDataGroupName));
            Assert.IsNotNull(Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName));
        }

        [Test]
        public void AddRemovelabel()
        {
            const string labelName = "Newlabel";
            Settings.AddLabel(labelName);
            Assert.Contains(labelName, Settings.labelTable.labelNames);
            Settings.RemoveLabel(labelName);
            Assert.False(Settings.labelTable.labelNames.Contains(labelName));
        }

        [Test]
        public void AddRemoveGroup()
        {
            const string groupName = "NewGroup";
            var group = Settings.CreateGroup(groupName, false, false, false, null);
            Assert.IsNotNull(group);
            Settings.RemoveGroup(group);
            Assert.IsNull(Settings.FindGroup(groupName));
        }

        [Test]
        public void CreateNewEntry()
        {
            var group = Settings.CreateGroup("NewGroupForCreateOrMoveEntryTest", false, false, false, null);
            Assert.IsNotNull(group);
            var entry = Settings.CreateOrMoveEntry(m_AssetGUID, group);
            Assert.IsNotNull(entry);
            Assert.AreSame(group, entry.parentGroup);
            var localDataGroup = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(localDataGroup);
            entry = Settings.CreateOrMoveEntry(m_AssetGUID, localDataGroup);
            Assert.IsNotNull(entry);
            Assert.AreNotSame(group, entry.parentGroup);
            Assert.AreSame(localDataGroup, entry.parentGroup);
            Settings.RemoveGroup(group);
            localDataGroup.RemoveAssetEntry(entry);
        }

        [Test]
        public void FindAssetEntry()
        {
            var localDataGroup = Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(localDataGroup);
            var entry = Settings.CreateOrMoveEntry(m_AssetGUID, localDataGroup);
            var foundEntry = Settings.FindAssetEntry(m_AssetGUID);
            Assert.AreSame(entry, foundEntry);
        }

        [Test]
        public void AddressablesClearCachedData_DoesNotThrowError()
        {
            //individual clean paths
            foreach (ScriptableObject so in Settings.DataBuilders)
            {
                BuildScriptBase db = so as BuildScriptBase;
                Assert.DoesNotThrow(() => Settings.CleanPlayerContentImpl(db));
            }

            //Clean all path
            Assert.DoesNotThrow(() => Settings.CleanPlayerContentImpl());

            //Cleanup
            Settings.BuildPlayerContentImpl();
        }

        [Test]
        public void AddressablesCleanCachedData_ClearsData()
        {
            //Setup
            Settings.BuildPlayerContentImpl();

            //Check after each clean that the data is not built
            foreach (ScriptableObject so in Settings.DataBuilders)
            {
                BuildScriptBase db = so as BuildScriptBase;
                Settings.CleanPlayerContentImpl(db);
                Assert.IsFalse(db.IsDataBuilt());
            }
        }

        [Test]
        public void AddressablesCleanAllCachedData_ClearsAllData()
        {
            //Setup
            Settings.BuildPlayerContentImpl();

            //Clean ALL data builders
            Settings.CleanPlayerContentImpl();

            //Check none have data built
            foreach (ScriptableObject so in Settings.DataBuilders)
            {
                BuildScriptBase db = so as BuildScriptBase;
                Assert.IsFalse(db.IsDataBuilt());
            }
        }
    }
}