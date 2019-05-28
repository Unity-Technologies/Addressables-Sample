using System;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.AddressableAssets.Tests
{
    public class DataBuilderInputTests : AddressableAssetTestBase
    {
        [Test]
        public void BuildInput_FailsWithNullSettings()
        {
            var input = new AddressablesDataBuilderInput(null);
            LogAssert.Expect(LogType.Error, "Attempting to set up AddressablesDataBuilderInput with null settings.");
            Assert.AreEqual(string.Empty, input.PlayerVersion);
            input = new AddressablesDataBuilderInput(null, "123");
            LogAssert.Expect(LogType.Error, "Attempting to set up AddressablesDataBuilderInput with null settings.");
            Assert.AreEqual("123", input.PlayerVersion);
        }
        
        [Test]
        public void BuildInput_CreatesProperBuildData()
        {
            var input = new AddressablesDataBuilderInput(Settings);
            Assert.AreEqual(EditorUserBuildSettings.activeBuildTarget, input.Target);
            Assert.AreEqual(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), input.TargetGroup);
            Assert.AreEqual(Settings.PlayerBuildVersion, input.PlayerVersion);
            
            
            input = new AddressablesDataBuilderInput(Settings, "1234");
            Assert.AreEqual(EditorUserBuildSettings.activeBuildTarget, input.Target);
            Assert.AreEqual(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), input.TargetGroup);
            Assert.AreEqual("1234", input.PlayerVersion);
        }

        [Test]
        public void BuildInput_ReadsProfilerEventState()
        {
            var oldState = ProjectConfigData.postProfilerEvents;
            ProjectConfigData.postProfilerEvents = true;
            var input = new AddressablesDataBuilderInput(Settings);
            Assert.AreEqual(true, input.ProfilerEventsEnabled);
            
            ProjectConfigData.postProfilerEvents = false;
            input = new AddressablesDataBuilderInput(Settings);
            Assert.AreEqual(false, input.ProfilerEventsEnabled);
            
            
            ProjectConfigData.postProfilerEvents = oldState;
        }
    }
}