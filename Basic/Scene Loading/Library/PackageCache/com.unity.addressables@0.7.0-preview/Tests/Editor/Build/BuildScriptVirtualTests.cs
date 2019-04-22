using System;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests
{
    public class BuildScriptVirtualTests : AddressableAssetTestBase
    {
        [Test]
        public void VirtualModeScript_CannotBuildPlayerContent()
        {
            var buildScript = ScriptableObject.CreateInstance<BuildScriptVirtualMode>();
            
            Assert.IsFalse(buildScript.CanBuildData<AddressablesPlayerBuildResult>());
            
            Assert.IsTrue(buildScript.CanBuildData<AddressableAssetBuildResult>());
            Assert.IsTrue(buildScript.CanBuildData<AddressablesPlayModeBuildResult>());
        }
    }
}