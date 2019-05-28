using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// Data builder context object for Addressables.
    /// </summary>
    public class AddressablesDataBuilderInput
    {

        /// <summary>
        /// The main addressables settings object.
        /// </summary>
        public AddressableAssetSettings AddressableSettings { get; private set; }

        /// <summary>
        /// Build target group.
        /// </summary>
        public BuildTargetGroup TargetGroup { get; private set; }

        /// <summary>
        /// Build target.
        /// </summary>
        public BuildTarget Target { get; private set; }
        
        /// <summary>
        /// Player build version.
        /// </summary>
        public string PlayerVersion { get; private set; }
    
        /// <summary>
        /// Bool to signify if profiler events should be broadcast.
        /// </summary>
        public bool ProfilerEventsEnabled { get; private set; }

        /// <summary>
        /// Registry of files created during the build
        /// </summary>
        public FileRegistry Registry { get; private set; }
        
        //used only by tests to inject custom info into build...
        internal string PathFormat = string.Empty;
        internal string RuntimeSettingsFilename = "settings.json";
        internal string RuntimeCatalogFilename = "catalog.json";
        

        /// <summary>
        /// Creates a default context object with values taken from the AddressableAssetSettings parameter.
        /// </summary>
        /// <param name="settings">The settings object to pull values from.</param>
        public AddressablesDataBuilderInput(AddressableAssetSettings settings)
        {
            string version = string.Empty;
            if (settings == null)
            {
                Debug.LogError("Attempting to set up AddressablesDataBuilderInput with null settings.");
            }
            else
                version = settings.PlayerBuildVersion;
            
            SetAllValues(settings,
                BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget),
                EditorUserBuildSettings.activeBuildTarget,
                version);
        }

        /// <summary>
        /// Creates a default context object with values taken from the AddressableAssetSettings parameter.
        /// </summary>
        /// <param name="settings">The settings object to pull values from.</param>
        /// <param name="playerBuildVersion">The player build version.</param>
        public AddressablesDataBuilderInput(AddressableAssetSettings settings, string playerBuildVersion)
        {
            if (settings == null)
            {
                Debug.LogError("Attempting to set up AddressablesDataBuilderInput with null settings.");
            }
            SetAllValues(settings, 
                BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget), 
                EditorUserBuildSettings.activeBuildTarget, 
                playerBuildVersion);
        }
        
        void SetAllValues(AddressableAssetSettings settings, BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, string playerBuildVersion)
        {
            AddressableSettings = settings;

            TargetGroup = buildTargetGroup;
            Target = buildTarget;
            PlayerVersion = playerBuildVersion;
            ProfilerEventsEnabled = ProjectConfigData.postProfilerEvents;
            Registry = new FileRegistry();
        }
    }
}