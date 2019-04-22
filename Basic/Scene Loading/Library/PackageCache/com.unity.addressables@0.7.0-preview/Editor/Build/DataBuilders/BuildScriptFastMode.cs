using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Build script used for fast iteration in the editor.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildScriptFast.asset", menuName = "Addressable Assets/Data Builders/Fast Mode")]
    public class BuildScriptFastMode : BuildScriptBase
    {
        /// <inheritdoc />
        public override string Name
        {
            get
            {
                return "Fast Mode";
            }
        }

        /// <inheritdoc />
        public override bool CanBuildData<T>()
        {
            return typeof(T).IsAssignableFrom(typeof(AddressablesPlayModeBuildResult));
        }

        /// <inheritdoc />
        public override void ClearCachedData()
        {
            DeleteFile(string.Format(m_PathFormat, "", "catalog"));
            DeleteFile(string.Format(m_PathFormat, "", "settings"));
        }

        string m_PathFormat;
        bool m_NeedsLegacyProvider = false;
        
        /// <inheritdoc />
        protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput context)
        {
            TResult result = default(TResult);
            
            var timer = new Stopwatch();
            timer.Start();
            var aaSettings = context.AddressableSettings;
            m_PathFormat = context.PathFormat;
            if(string.IsNullOrEmpty(m_PathFormat))
                m_PathFormat = "{0}Library/com.unity.addressables/{1}_BuildScriptFastMode.json";


            //create runtime data
            var aaContext = new AddressableAssetsBuildContext
            {
                settings = aaSettings,
                runtimeData = new ResourceManagerRuntimeData(),
                bundleToAssetGroup = null,
                locations = new List<ContentCatalogDataEntry>()
            };
            aaContext.runtimeData.BuildTarget = context.Target.ToString();
            aaContext.runtimeData.LogResourceManagerExceptions = aaSettings.buildSettings.LogResourceManagerExceptions;
            aaContext.runtimeData.ProfileEvents = ProjectConfigData.postProfilerEvents;
            aaContext.runtimeData.CatalogLocations.Add(new ResourceLocationData(new[] { ResourceManagerRuntimeData.kCatalogAddress }, string.Format(m_PathFormat, "file://{UnityEngine.Application.dataPath}/../", "catalog"), typeof(ContentCatalogProvider)));

            var errorString = ProcessAllGroups(aaContext);
            if(!string.IsNullOrEmpty(errorString))
                result = AddressableAssetBuildResult.CreateResult<TResult>(null, 0, errorString);

            if (result == null)
            {
                foreach (var io in aaSettings.InitializationObjects)
                {
                    if (io is IObjectInitializationDataProvider)
                        aaContext.runtimeData.InitializationObjects.Add((io as IObjectInitializationDataProvider).CreateObjectInitializationData());
                }

                var settingsPath = string.Format(m_PathFormat, "", "settings");
                WriteFile(settingsPath, JsonUtility.ToJson(aaContext.runtimeData));

                //save catalog
                var catalogData = new ContentCatalogData(aaContext.locations);
                if (m_NeedsLegacyProvider)
                    catalogData.ResourceProviderData.Add(ObjectInitializationData.CreateSerializedInitializationData(typeof(LegacyResourcesProvider)));
                catalogData.ResourceProviderData.Add(ObjectInitializationData.CreateSerializedInitializationData<AssetDatabaseProvider>());
                catalogData.InstanceProviderData = ObjectInitializationData.CreateSerializedInitializationData(instanceProviderType.Value);
                catalogData.SceneProviderData = ObjectInitializationData.CreateSerializedInitializationData(sceneProviderType.Value);
                WriteFile(string.Format(m_PathFormat, "", "catalog"), JsonUtility.ToJson(catalogData));


                //inform runtime of the init data path
                var runtimeSettingsPath = string.Format(m_PathFormat, "file://{UnityEngine.Application.dataPath}/../", "settings");
                PlayerPrefs.SetString(Addressables.kAddressablesRuntimeDataPath, runtimeSettingsPath);
                result = AddressableAssetBuildResult.CreateResult<TResult>(settingsPath, aaContext.locations.Count);
            }
            
            if(result != null)
                result.Duration = timer.Elapsed.TotalSeconds;

            return result;
        }

        /// <inheritdoc />
        protected override string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            var errorString = string.Empty;
            PlayerDataGroupSchema playerSchema = assetGroup.GetSchema<PlayerDataGroupSchema>();
            if (playerSchema != null)
            {
                m_NeedsLegacyProvider = CreateLocationsForPlayerData(playerSchema, assetGroup, aaContext.locations);
                return errorString;
            }

            var allEntries = new List<AddressableAssetEntry>();
            foreach (var a in assetGroup.entries)
                a.GatherAllAssets(allEntries, true, true);

            foreach (var a in allEntries)
                aaContext.locations.Add(new ContentCatalogDataEntry(a.GetAssetLoadPath(true), typeof(AssetDatabaseProvider).FullName, a.CreateKeyList()));

            return errorString;
        }
    }
}