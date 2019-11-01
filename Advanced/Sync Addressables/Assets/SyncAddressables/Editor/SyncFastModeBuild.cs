using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using Debug = UnityEngine.Debug;

[CreateAssetMenu(fileName = "SyncFastModeBuild.asset", menuName = "Addressable Assets/Data Builders/Sync Fast")]
public class SyncFastModeBuild : BuildScriptFastMode
{
    public override string Name
    {
        get
        {
            return "Sync Fast";
        }
    }
    protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput context)
        {
            TResult result = default(TResult);
            
            var timer = new Stopwatch();
            timer.Start();
            var aaSettings = context.AddressableSettings;
            var pathFormat = "{0}Library/com.unity.addressables/{1}_BuildScriptFastMode.json";

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
            aaContext.runtimeData.ProfileEvents = context.ProfilerEventsEnabled;
            aaContext.runtimeData.CatalogLocations.Add(new ResourceLocationData(new[] { ResourceManagerRuntimeData.kCatalogAddress },
                string.Format(pathFormat, "file://{UnityEngine.Application.dataPath}/../", "catalog"),
                typeof(ContentCatalogProvider),
                typeof(ContentCatalogData)));

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

                var settingsPath = string.Format(pathFormat, "", "settings");
                WriteFile(settingsPath, JsonUtility.ToJson(aaContext.runtimeData), context.Registry);

                //save catalog
                var catalogData = new ContentCatalogData(aaContext.locations);
                if (m_legacy)
                    catalogData.ResourceProviderData.Add(ObjectInitializationData.CreateSerializedInitializationData(typeof(LegacyResourcesProvider)));
                catalogData.ResourceProviderData.Add(ObjectInitializationData.CreateSerializedInitializationData<SyncAssetDatabaseProvider>());
                catalogData.InstanceProviderData = ObjectInitializationData.CreateSerializedInitializationData(instanceProviderType.Value);
                catalogData.SceneProviderData = ObjectInitializationData.CreateSerializedInitializationData(sceneProviderType.Value);
                WriteFile(string.Format(pathFormat, "", "catalog"), JsonUtility.ToJson(catalogData), context.Registry);


                //inform runtime of the init data path
                var runtimeSettingsPath = string.Format(pathFormat, "file://{UnityEngine.Application.dataPath}/../", "settings");
                PlayerPrefs.SetString(Addressables.kAddressablesRuntimeDataPath, runtimeSettingsPath);
                result = AddressableAssetBuildResult.CreateResult<TResult>(settingsPath, aaContext.locations.Count);
            }
            
            if(result != null)
                result.Duration = timer.Elapsed.TotalSeconds;

            return result;
        }
    
    bool m_legacy = false;
    protected override string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
    {
        var errorString = string.Empty;
        
        PlayerDataGroupSchema playerSchema = assetGroup.GetSchema<PlayerDataGroupSchema>();
        if (playerSchema != null)
        {
            m_legacy = CreateLocationsForPlayerData(playerSchema, assetGroup, aaContext.locations, aaContext.providerTypes);
            return errorString;
        }

        var allEntries = new List<AddressableAssetEntry>();
        foreach (var a in assetGroup.entries)
            a.GatherAllAssets(allEntries, true, true, true);

        var typeName = typeof(SyncAssetDatabaseProvider).FullName;
        foreach (var a in allEntries)
            a.CreateCatalogEntries(aaContext.locations, false, typeName, null, null, aaContext.providerTypes);

        return errorString;
    }
}
