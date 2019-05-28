using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.AddressableAssets.Tests
{
    public class BuildScriptTests : AddressableAssetTestBase
    {
        [Test]
        public void ClearCachedData_CleansStreamingAssetFolder()
        {
            var context = new AddressablesDataBuilderInput(Settings);
            
            int builderCount = 0;
            for (int i = 0; i < Settings.DataBuilders.Count; i++)
            {

                var builder = Settings.DataBuilders[i] as IDataBuilder;
                if (builder.CanBuildData<AddressablesPlayerBuildResult>())
                {
                    builderCount++;
                    var existingFiles = new HashSet<string>();
                    if (System.IO.Directory.Exists("Assets/StreamingAssets"))
                    {
                        foreach (var f in System.IO.Directory.GetFiles("Assets/StreamingAssets"))
                            existingFiles.Add(f);
                    }
                    builder.BuildData<AddressablesPlayerBuildResult>(context);
                    builder.ClearCachedData();
                    if (System.IO.Directory.Exists("Assets/StreamingAssets"))
                    {
                        foreach (var f in System.IO.Directory.GetFiles("Assets/StreamingAssets"))
                            Assert.IsTrue(existingFiles.Contains(f), string.Format("Data Builder {0} did not clean up file {1}", builder.Name, f));
                    }
                }
            }
            Assert.IsTrue(builderCount > 0);

        }

        [Test]
        public void BuildCompleteCallbackGetsCalled()
        {
            LogAssert.ignoreFailingMessages = true;
            AddressableAssetSettings oldSettings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetSettingsDefaultObject.Settings = Settings;

            bool callbackCalled = false;
            BuildScript.buildCompleted += (result) =>
            {
                callbackCalled = true;
            };
            AddressableAssetSettings.BuildPlayerContent();
            Assert.IsTrue(callbackCalled);

            if (oldSettings != null)
                AddressableAssetSettingsDefaultObject.Settings = oldSettings;
            AddressableAssetSettings.BuildPlayerContent();
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void BuildScriptBase_FailsCanBuildData()
        {
            var buildScript = ScriptableObject.CreateInstance<BuildScriptBase>();
            Assert.IsFalse(buildScript.CanBuildData<IDataBuilderResult>());
            Assert.IsFalse(buildScript.CanBuildData<AddressableAssetBuildResult>());
            Assert.IsFalse(buildScript.CanBuildData<AddressablesPlayModeBuildResult>());
            Assert.IsFalse(buildScript.CanBuildData<AddressablesPlayerBuildResult>());
        }

        class BuildScriptTestClass : BuildScriptBase
        {
            public override string Name
            {
                get
                {
                    return "Test Script";
                }
            }

            public override bool CanBuildData<T>()
            {
                return typeof(T).IsAssignableFrom(typeof(AddressablesPlayModeBuildResult));
            }

            protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
            {
                Debug.LogError("Inside BuildDataInternal for test script!");
                return base.BuildDataImplementation<TResult>(builderInput);
            }
        }
        
        [Test]
        public void BuildScript_DoesNotBuildWrongDataType()
        {
            var context = new AddressablesDataBuilderInput(Settings);
            
            var baseScript = ScriptableObject.CreateInstance<BuildScriptTestClass>();
            baseScript.BuildData<AddressablesPlayerBuildResult>(context);
            LogAssert.Expect(LogType.Error, new Regex("Data builder Test Script cannot build requested type.*"));
            
            baseScript.BuildData<AddressablesPlayModeBuildResult>(context);
            LogAssert.Expect(LogType.Error, "Inside BuildDataInternal for test script!");
            
        }
        
    }
}