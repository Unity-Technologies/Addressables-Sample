using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Base class for build script assets
    /// </summary>
    public class BuildScriptBase : ScriptableObject, IDataBuilder
    {
        /// <summary>
        /// The type of instance provider to create for the Addressables system.
        /// </summary>
        [FormerlySerializedAs("m_InstanceProviderType")]
        [SerializedTypeRestrictionAttribute(type = typeof(IInstanceProvider))]
        public SerializedType instanceProviderType = new SerializedType() { Value = typeof(InstanceProvider) };
        /// <summary>
        /// The type of scene provider to create for the addressables system.
        /// </summary>
        [FormerlySerializedAs("m_SceneProviderType")]
        [SerializedTypeRestrictionAttribute(type = typeof(ISceneProvider))]
        public SerializedType sceneProviderType = new SerializedType() { Value = typeof(SceneProvider) };

        /// <summary>
        /// The descriptive name used in the UI.
        /// </summary>
        public virtual string Name
        {
            get
            {
                return "Undefined";
            }
        }

        /// <summary>
        /// Build the specified data with the provided builderInput.  This is the public entry point.
        ///  Child class overrides should use <see cref="BuildDataImplementation{TResult}"/>
        /// </summary>
        /// <typeparam name="TResult">The type of data to build.</typeparam>
        /// <param name="builderInput">The builderInput object used in the build.</param>
        /// <returns>The build data result.</returns>
        public TResult BuildData<TResult>(AddressablesDataBuilderInput builderInput) where TResult : IDataBuilderResult
        {
            if (!CanBuildData<TResult>())
            {
                var message = "Data builder " + Name + " cannot build requested type: " + typeof(TResult);
                Debug.LogError(message);
                return AddressableAssetBuildResult.CreateResult<TResult>(null, 0, message);
            }

            return BuildDataImplementation<TResult>(builderInput);
        }
        
        /// <summary>
        /// The implementation of <see cref="BuildData{TResult}"/>.  That is the public entry point,
        ///  this is the home for child class overrides.
        /// </summary>
        /// <param name="builderInput">The builderInput object used in the build</param>
        /// <typeparam name="TResult">The type of data to build</typeparam>
        /// <returns>The build data result</returns>
        protected virtual TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput) where TResult : IDataBuilderResult
        {
            return default(TResult);
        }

        /// <summary>
        /// Loops over each group, after doing some data checking.
        /// </summary>
        /// <param name="aaContext">The Addressables builderInput object to base the group processing on</param>
        /// <returns>An error string if there were any problems processing the groups</returns>
        protected virtual string ProcessAllGroups(AddressableAssetsBuildContext aaContext) 
        {
            if (aaContext == null ||
                aaContext.settings == null ||
                aaContext.settings.groups == null)
            {
                return "No groups found to process in build script " + Name;
            }
            //intentionally for not foreach so groups can be added mid-loop.
            for(int index = 0; index < aaContext.settings.groups.Count; index++)  
            {
                AddressableAssetGroup assetGroup = aaContext.settings.groups[index];
                var errorString = ProcessGroup(assetGroup, aaContext);
                if(!string.IsNullOrEmpty(errorString))
                    return errorString;
            }

            return string.Empty;
        }
        
        /// <summary>
        /// Build processing of an individual group.  
        /// </summary>
        /// <param name="assetGroup">The group to process</param>
        /// <param name="aaContext">The Addressables builderInput object to base the group processing on</param>
        /// <returns>An error string if there were any problems processing the groups</returns>
        protected virtual string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            return string.Empty;
        }

        /// <summary>
        /// Used to determine if this builder is capable of building a specific type of data.
        /// </summary>
        /// <typeparam name="T">The type of data needed to be built.</typeparam>
        /// <returns>True if this builder can build this data.</returns>
        public virtual bool CanBuildData<T>() where T : IDataBuilderResult
        {
            return false;
        }

        /// <summary>
        /// Utility method for creating locations from player data.
        /// </summary>
        /// <param name="assetGroup">The group to extract the locations from.</param>
        /// <param name="locations">The list of created locations to fill in.</param>
        /// <returns>True if any legacy locations were created.  This is used by the build scripts to determine if a legacy provider is needed.</returns>
        protected bool CreateLocationsForPlayerData(PlayerDataGroupSchema playerDataSchema, AddressableAssetGroup assetGroup, List<ContentCatalogDataEntry> locations)
        {
            bool needsLegacyProvider = false;
            if (playerDataSchema != null && (playerDataSchema.IncludeBuildSettingsScenes || playerDataSchema.IncludeResourcesFolders))
            {
                var entries = new List<AddressableAssetEntry>();
                assetGroup.GatherAllAssets(entries, true, true);
                foreach (var a in entries)
                {
                    if (!playerDataSchema.IncludeBuildSettingsScenes && a.IsInSceneList)
                        continue;
                    if (!playerDataSchema.IncludeResourcesFolders && a.IsInResources)
                        continue;
                    string providerId = a.IsScene ? "" : typeof(LegacyResourcesProvider).FullName;
                    locations.Add(new ContentCatalogDataEntry(a.GetAssetLoadPath(false), providerId, a.CreateKeyList()));
                    if (!a.IsScene)
                        needsLegacyProvider = true;
                }
            }
            return needsLegacyProvider;
        }

        /// <summary>
        /// Utility method for deleting files.
        /// </summary>
        /// <param name="path">The file path to delete.</param>
        protected static void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Utility method to write a file.  The directory will be created if it does not exist.
        /// </summary>
        /// <param name="path">The path of the file to write.</param>
        /// <param name="content">The content of the file.</param>
        /// <returns>True if the file was written.</returns>
        protected static bool WriteFile(string path, string content)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, content);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }

        }

        /// <summary>
        /// Used to clean up any cached data created by this builder.
        /// </summary>
        public virtual void ClearCachedData()
        {

        }
    }
}