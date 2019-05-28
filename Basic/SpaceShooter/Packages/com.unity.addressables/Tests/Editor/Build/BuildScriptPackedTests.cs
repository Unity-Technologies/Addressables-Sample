using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.TestTools;

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
        
        [Test]
        public void ErrorCheckBundleSettings_FindsNoProblemsInDefaultScema()
        {
            var group = Settings.CreateGroup("PackedTest", false, false, false, null, typeof(BundledAssetGroupSchema));
            var schema = group.GetSchema<BundledAssetGroupSchema>();
               
            var errorStr = BuildScriptPackedMode.ErrorCheckBundleSettings(schema, group, Settings);
            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(string.IsNullOrEmpty(errorStr));
        }

        [Test]
        public void ErrorCheckBundleSettings_WarnsOfMismatchedBuildPath()
        {
            var group = Settings.CreateGroup("PackedTest", false, false, false, null, typeof(BundledAssetGroupSchema));
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BuildPath.Id = "BadPath";
                
            var errorStr = BuildScriptPackedMode.ErrorCheckBundleSettings(schema, group, Settings);
            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(errorStr.Contains("is set to the dynamic-lookup version of StreamingAssets, but BuildPath is not."));
        }

        [Test]
        public void ErrorCheckBundleSettings_WarnsOfMismatchedLoadPath()
        {
            var group = Settings.CreateGroup("PackedTest", false, false, false, null, typeof(BundledAssetGroupSchema));
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.LoadPath.Id = "BadPath";
                
            var errorStr = BuildScriptPackedMode.ErrorCheckBundleSettings(schema, group, Settings);
            LogAssert.NoUnexpectedReceived();
            Assert.IsTrue(errorStr.Contains("is set to the dynamic-lookup version of StreamingAssets, but LoadPath is not."));
        }
    }
}