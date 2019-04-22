using System;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Uses data built by BuildScriptPacked class.  This script just sets up the correct variables and runs.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildScriptPackedPlayMode.asset", menuName = "Addressable Assets/Data Builders/Packed Play Mode")]
    public class BuildScriptPackedPlayMode : BuildScriptBase
    {
        /// <inheritdoc />
        public override string Name
        {
            get
            {
                return "Packed Play Mode";
            }
        }

        /// <inheritdoc />
        public override bool CanBuildData<T>()
        {
            return typeof(T).IsAssignableFrom(typeof(AddressablesPlayModeBuildResult));
        }

        /// <inheritdoc />
        protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var settingsPath = Addressables.BuildPath + "/settings.json";
            if (!File.Exists(settingsPath))
            {
                IDataBuilderResult resE = new AddressablesPlayModeBuildResult() { Error = "Player content must be built before entering play mode with packed data.  This can be done from the Addressable Assets window in the Build->Build Player Content menu command." };
                return (TResult)resE;
            }
            var rtd = JsonUtility.FromJson<ResourceManagerRuntimeData>(File.ReadAllText(settingsPath));
            if (rtd == null)
            {
                IDataBuilderResult resE = new AddressablesPlayModeBuildResult() { Error = string.Format("Unable to load initialization data from path {0}.  This can be done from the Addressable Assets window in the Build->Build Player Content menu command.", settingsPath) };
                return (TResult)resE;
            }

            BuildTarget dataBuildTarget = BuildTarget.NoTarget;
            if (!Enum.TryParse(rtd.BuildTarget, out dataBuildTarget))
                Debug.LogWarningFormat("Unable to parse build target from initialization data: '{0}'.", rtd.BuildTarget);

            if (BuildPipeline.GetBuildTargetGroup(dataBuildTarget) != BuildTargetGroup.Standalone)
                Debug.LogWarningFormat("Asset bundles built with build target {0} may not be compatible with running in the Editor.", dataBuildTarget);
           
            //TODO: detect if the data that does exist is out of date..
            var runtimeSettingsPath = "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/settings.json";
            PlayerPrefs.SetString(Addressables.kAddressablesRuntimeDataPath, runtimeSettingsPath);
            IDataBuilderResult res = new AddressablesPlayModeBuildResult() { OutputPath = settingsPath, Duration = timer.Elapsed.TotalSeconds };
            return (TResult)res;
        }
    }
}