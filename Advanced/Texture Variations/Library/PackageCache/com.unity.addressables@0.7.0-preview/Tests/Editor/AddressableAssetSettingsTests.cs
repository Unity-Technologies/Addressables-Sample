using System;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Settings;

namespace UnityEditor.AddressableAssets.Tests
{
    public class AddressableAssetSettingsTests : AddressableAssetTestBase
    {
        [Test]
        public void HasDefaultInitialGroups()
        {
            Assert.IsNotNull(m_Settings.FindGroup(AddressableAssetSettings.PlayerDataGroupName));
            Assert.IsNotNull(m_Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName));
        }

        [Test]
        public void AddRemovelabel()
        {
            const string labelName = "Newlabel";
            m_Settings.AddLabel(labelName);
            Assert.Contains(labelName, m_Settings.labelTable.labelNames);
            m_Settings.RemoveLabel(labelName);
            Assert.False(m_Settings.labelTable.labelNames.Contains(labelName));
        }

        [Test]
        public void AddRemoveGroup()
        {
            const string groupName = "NewGroup";
            var group = m_Settings.CreateGroup(groupName, false, false, false, null);
            Assert.IsNotNull(group);
            m_Settings.RemoveGroup(group);
            Assert.IsNull(m_Settings.FindGroup(groupName));
        }

        [Test]
        public void CreateNewEntry()
        {
            var group = m_Settings.CreateGroup("NewGroupForCreateOrMoveEntryTest", false, false, false, null);
            Assert.IsNotNull(group);
            var entry = m_Settings.CreateOrMoveEntry(m_AssetGUID, group);
            Assert.IsNotNull(entry);
            Assert.AreSame(group, entry.parentGroup);
            var localDataGroup = m_Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(localDataGroup);
            entry = m_Settings.CreateOrMoveEntry(m_AssetGUID, localDataGroup);
            Assert.IsNotNull(entry);
            Assert.AreNotSame(group, entry.parentGroup);
            Assert.AreSame(localDataGroup, entry.parentGroup);
            m_Settings.RemoveGroup(group);
            localDataGroup.RemoveAssetEntry(entry);
        }

        [Test]
        public void FindAssetEntry()
        {
            var localDataGroup = m_Settings.FindGroup(AddressableAssetSettings.DefaultLocalGroupName);
            Assert.IsNotNull(localDataGroup);
            var entry = m_Settings.CreateOrMoveEntry(m_AssetGUID, localDataGroup);
            var foundEntry = m_Settings.FindAssetEntry(m_AssetGUID);
            Assert.AreSame(entry, foundEntry);
        }

    }
}