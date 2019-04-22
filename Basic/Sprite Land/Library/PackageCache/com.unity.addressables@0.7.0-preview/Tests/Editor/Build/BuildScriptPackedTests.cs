using System;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests
{
    public class BuildScriptPackedTests : AddressableAssetTestBase
    {
        [Test]
        public void PackedModeScript_CannotBuildPlayContent()
        {
            var buildScript = ScriptableObject.CreateInstance<BuildScriptPackedMode>();
            
            Assert.IsFalse(buildScript.CanBuildData<AddressablesPlayModeBuildResult>());
            
            Assert.IsTrue(buildScript.CanBuildData<AddressableAssetBuildResult>());
            Assert.IsTrue(buildScript.CanBuildData<AddressablesPlayerBuildResult>());
        }
    }
}