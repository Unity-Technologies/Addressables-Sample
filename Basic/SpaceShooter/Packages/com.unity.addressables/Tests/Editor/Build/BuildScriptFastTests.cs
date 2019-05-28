using System;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Tests
{
    public class BuildScriptFastTests : AddressableAssetTestBase
    {
        [Test]
        public void FastModeScript_CannotBuildPlayerContent()
        {
            var buildScript = ScriptableObject.CreateInstance<BuildScriptFastMode>();
            
            Assert.IsFalse(buildScript.CanBuildData<AddressablesPlayerBuildResult>());
            
            Assert.IsTrue(buildScript.CanBuildData<AddressableAssetBuildResult>());
            Assert.IsTrue(buildScript.CanBuildData<AddressablesPlayModeBuildResult>());
        }
    }
}