using System;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Utilities;

namespace UnityEditor.AddressableAssets.Tests
{
    public class AddressableAssetUtilityTests
    {
        [Test]
        public void IsInResourcesProperlyHandlesCase()
        {
            Assert.IsTrue(AddressableAssetUtility.IsInResources("/rEsOurces/"));
            Assert.IsTrue(AddressableAssetUtility.IsInResources("/resources/"));
            Assert.IsTrue(AddressableAssetUtility.IsInResources("/RESOURCES/"));
        }
        [Test]
        public void IsInResourcesHandlesExtraPathing()
        {
            Assert.IsTrue(AddressableAssetUtility.IsInResources("path/path/resources/path"));
            Assert.IsTrue(AddressableAssetUtility.IsInResources("path/path/resources/"));
            Assert.IsTrue(AddressableAssetUtility.IsInResources("/resources/path"));
        }

        [Test]
        public void IsInResourcesHandlesResourcesInWrongContext()
        {
            Assert.IsFalse(AddressableAssetUtility.IsInResources("resources/"));
            Assert.IsFalse(AddressableAssetUtility.IsInResources("/resources"));
            Assert.IsFalse(AddressableAssetUtility.IsInResources("path/resourcesOther/path"));
            Assert.IsFalse(AddressableAssetUtility.IsInResources("/path/res/ources/path"));
        }
        [Test]
        public void IsPathValidBlocksCommonStrings()
        {
            Assert.IsFalse(AddressableAssetUtility.IsPathValidForEntry(string.Empty));
            Assert.IsFalse(AddressableAssetUtility.IsPathValidForEntry(CommonStrings.UnityEditorResourcePath));
            Assert.IsFalse(AddressableAssetUtility.IsPathValidForEntry(CommonStrings.UnityDefaultResourcePath));
            Assert.IsFalse(AddressableAssetUtility.IsPathValidForEntry(CommonStrings.UnityBuiltInExtraPath));
        }
        [Test]
        public void IsPathValidBlocksBadExtensions()
        {
            Assert.IsFalse(AddressableAssetUtility.IsPathValidForEntry("file.cs"));
            Assert.IsFalse(AddressableAssetUtility.IsPathValidForEntry("file.js"));
            Assert.IsFalse(AddressableAssetUtility.IsPathValidForEntry("file.boo"));
            Assert.IsFalse(AddressableAssetUtility.IsPathValidForEntry("file.exe"));
            Assert.IsFalse(AddressableAssetUtility.IsPathValidForEntry("file.dll"));
        }
        [Test]
        public void IsPathValidAllowsBasicTypes()
        {
            Assert.IsTrue(AddressableAssetUtility.IsPathValidForEntry("file.asset"));
            Assert.IsTrue(AddressableAssetUtility.IsPathValidForEntry("file.png"));
            Assert.IsTrue(AddressableAssetUtility.IsPathValidForEntry("file.bin"));
            Assert.IsTrue(AddressableAssetUtility.IsPathValidForEntry("file.txt"));
            Assert.IsTrue(AddressableAssetUtility.IsPathValidForEntry("file.prefab"));
            Assert.IsTrue(AddressableAssetUtility.IsPathValidForEntry("file.mat"));
            Assert.IsTrue(AddressableAssetUtility.IsPathValidForEntry("file.wav"));
            Assert.IsTrue(AddressableAssetUtility.IsPathValidForEntry("file.jpg"));
            Assert.IsTrue(AddressableAssetUtility.IsPathValidForEntry("file.avi"));
            Assert.IsTrue(AddressableAssetUtility.IsPathValidForEntry("file.controller"));
        }
    }
}