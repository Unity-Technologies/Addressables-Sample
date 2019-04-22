using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using Debug = UnityEngine.Debug;

namespace UnityEditor.AddressableAssets.Build.DataBuilders
{
    /// <summary>
    /// Build scripts used for player builds and running with bundles in the editor.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildScriptPacked.asset", menuName = "Addressable Assets/Data Builders/Packed Mode")]
    public class BuildScriptPackedMode : BuildScriptBase
    {
        /// <inheritdoc />
        public override string Name
        {
            get
            {
                return "Packed Mode";
            }
        }

        List<ObjectInitializationData> m_ResourceProviderData; 
        List<AssetBundleBuild> m_AllBundleInputDefs;
        HashSet<string> m_CreatedProviderIds;
        LinkXmlGenerator m_Linker;
        
        /// <inheritdoc />
        public override bool CanBuildData<T>()
        {
            return typeof(T).IsAssignableFrom(typeof(AddressablesPlayerBuildResult));
        }

        /// <inheritdoc />
        protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
        {
            TResult result = default(TResult);
            
            var timer = new Stopwatch();
            timer.Start();
            var aaSettings = builderInput.AddressableSettings;

            var locations = new List<ContentCatalogDataEntry>();
            m_AllBundleInputDefs = new List<AssetBundleBuild>();
            var bundleToAssetGroup = new Dictionary<string, string>();
            var runtimeData = new ResourceManagerRuntimeData();
            runtimeData.BuildTarget = builderInput.Target.ToString();
            runtimeData.ProfileEvents = builderInput.ProfilerEventsEnabled;
            runtimeData.LogResourceManagerExceptions = aaSettings.buildSettings.LogResourceManagerExceptions;

            m_Linker = new LinkXmlGenerator();
            m_ResourceProviderData = new List<ObjectInitializationData>();
            
            var aaContext = new AddressableAssetsBuildContext
            {
                settings = aaSettings,
                runtimeData = runtimeData,
                bundleToAssetGroup = bundleToAssetGroup,
                locations = locations
            };

            m_CreatedProviderIds = new HashSet<string>();
            var errorString = ProcessAllGroups(aaContext);
            if(!string.IsNullOrEmpty(errorString))
                result = AddressableAssetBuildResult.CreateResult<TResult>(null, 0, errorString);

            if (result == null)
            {
                result = DoBuild<TResult>(builderInput, aaContext);   
            }
            
            if(result != null)
                result.Duration = timer.Elapsed.TotalSeconds;
            return result;
        }

        /// <summary>
        /// The method that does the actual building after all the groups have been processed. 
        /// </summary>
        /// <param name="builderInput">The generic builderInput of the</param>
        /// <param name="aaContext"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        protected virtual TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext) where TResult : IDataBuilderResult
        {
            ExtractDataTask extractData = new ExtractDataTask();

            var playerBuildVersion = builderInput.PlayerVersion;
            if (m_AllBundleInputDefs.Count > 0)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return AddressableAssetBuildResult.CreateResult<TResult>(null, 0, "Unsaved scenes");

                var buildTarget = builderInput.Target;
                var buildTargetGroup = builderInput.TargetGroup;

                var buildParams = new AddressableAssetsBundleBuildParameters(
                    aaContext.settings, 
                    aaContext.bundleToAssetGroup, 
                    buildTarget, 
                    buildTargetGroup, 
                    aaContext.settings.buildSettings.bundleBuildPath);

                var builtinShaderBundleName = aaContext.settings.DefaultGroup.Name.ToLower().Replace(" ", "").Replace('\\', '/').Replace("//", "/") + "_unitybuiltinshaders.bundle";
                var buildTasks = RuntimeDataBuildTasks(builtinShaderBundleName);
                buildTasks.Add(extractData);

                string aaPath = aaContext.settings.AssetPath;
                IBundleBuildResults results;
                var exitCode = ContentPipeline.BuildAssetBundles(buildParams, new BundleBuildContent(m_AllBundleInputDefs), out results, buildTasks, aaContext);

                if (exitCode < ReturnCode.Success)
                    return AddressableAssetBuildResult.CreateResult<TResult>(null, 0, "SBP Error" + exitCode);
                if (aaContext.settings == null && !string.IsNullOrEmpty(aaPath))
                    aaContext.settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(aaPath);

                foreach (var assetGroup in aaContext.settings.groups)
                {
                    List<string> bundles;
                    if (aaContext.assetGroupToBundles.TryGetValue(assetGroup, out bundles))
                        PostProcessBundles(assetGroup, bundles, results, extractData.WriteData, aaContext.runtimeData, aaContext.locations);
                }
                foreach (var r in results.WriteResults)
                    m_Linker.AddTypes(r.Value.includedTypes);
            }

            //save catalog
            var contentCatalog = new ContentCatalogData(aaContext.locations);
            contentCatalog.ResourceProviderData.AddRange(m_ResourceProviderData);
            contentCatalog.InstanceProviderData = ObjectInitializationData.CreateSerializedInitializationData(instanceProviderType.Value);
            contentCatalog.SceneProviderData = ObjectInitializationData.CreateSerializedInitializationData(sceneProviderType.Value);
            CreateCatalog(aaContext.settings, contentCatalog, aaContext.runtimeData.CatalogLocations, playerBuildVersion, builderInput.RuntimeCatalogFilename);
            m_Linker.AddTypes(instanceProviderType.Value);

            foreach (var io in aaContext.settings.InitializationObjects)
            {
                var provider = io as IObjectInitializationDataProvider;
                if (provider != null)
                {
                    var id = provider.CreateObjectInitializationData();
                    aaContext.runtimeData.InitializationObjects.Add(id);
                    m_Linker.AddTypes(id.ObjectType.Value);
                    m_Linker.AddTypes(id.GetRuntimeTypes());
                }
            }
            m_Linker.AddTypes(typeof(Addressables));
            m_Linker.Save(Addressables.BuildPath + "/link.xml");
            var settingsPath = Addressables.BuildPath + "/" + builderInput.RuntimeSettingsFilename;
            WriteFile(settingsPath, JsonUtility.ToJson(aaContext.runtimeData));

            var opResult = AddressableAssetBuildResult.CreateResult<TResult>(settingsPath, aaContext.locations.Count);
            //save content update data if building for the player
            var allEntries = new List<AddressableAssetEntry>();
            aaContext.settings.GetAllAssets(allEntries, g => g.HasSchema<ContentUpdateGroupSchema>() && g.GetSchema<ContentUpdateGroupSchema>().StaticContent);
            var tempPath = Path.GetDirectoryName(Application.dataPath) + "/Library/com.unity.addressables/StreamingAssetsCopy/" + PlatformMappingService.GetPlatform() + "/addressables_content_state.bin";

            var remoteCatalogLoadPath = aaContext.settings.BuildRemoteCatalog ? aaContext.settings.RemoteCatalogLoadPath.GetValue(aaContext.settings) : string.Empty;
            if (extractData.BuildCache != null && ContentUpdateScript.SaveContentState(tempPath, allEntries, extractData.DependencyData, playerBuildVersion, remoteCatalogLoadPath))
            {
                try
                {
                    File.Copy(tempPath, ContentUpdateScript.GetContentStateDataPath(false), true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            return opResult;
        }
        
        /// <inheritdoc />
        protected override string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            foreach (var schema in assetGroup.Schemas)
            {
                var errorString = ProcessGroupSchema(schema, assetGroup, aaContext);
                if(!string.IsNullOrEmpty(errorString))
                    return errorString;
            }

            return string.Empty;
        }

        /// <summary>
        /// Called per group per schema to evaluate that schema.  This can be an easy entry point for implementing the
        ///  build aspects surrounding a custom schema.  Note, you should not rely on schemas getting called in a specific
        ///  order.
        /// </summary>
        /// <param name="schema">The schema to process</param>
        /// <param name="assetGroup">The group this schema was pulled from</param>
        /// <param name="aaContext">The general Addressables build builderInput</param>
        /// <returns></returns>
        protected virtual string ProcessGroupSchema(AddressableAssetGroupSchema schema, AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            var playerDataSchema = schema as PlayerDataGroupSchema;
            if (playerDataSchema != null)
                return ProcessPlayerDataSchema(playerDataSchema, assetGroup, aaContext);
            var bundledAssetSchema = schema as BundledAssetGroupSchema;
            if (bundledAssetSchema != null)
                return ProcessBundledAssetSchema(bundledAssetSchema, assetGroup, aaContext);
            return string.Empty;
        }

        string ProcessPlayerDataSchema(
            PlayerDataGroupSchema schema, 
            AddressableAssetGroup assetGroup,
            AddressableAssetsBuildContext aaContext)
        {            
            if (CreateLocationsForPlayerData(schema, assetGroup, aaContext.locations))
            {
                if (!m_CreatedProviderIds.Contains(typeof(LegacyResourcesProvider).Name))
                {
                    m_CreatedProviderIds.Add(typeof(LegacyResourcesProvider).Name);
                    m_ResourceProviderData.Add(ObjectInitializationData.CreateSerializedInitializationData(typeof(LegacyResourcesProvider)));
                    m_Linker.AddTypes(typeof(LegacyResourcesProvider));
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// The processing of the bundled asset schema.  This is where the bundle(s) for a given group are actually setup.
        /// </summary>
        /// <param name="schema">The BundledAssetGroupSchema to process</param>
        /// <param name="assetGroup">The group this schema was pulled from</param>
        /// <param name="aaContext">The general Addressables build builderInput</param>
        /// <returns></returns>
        protected virtual string ProcessBundledAssetSchema(
            BundledAssetGroupSchema schema, 
            AddressableAssetGroup assetGroup,
            AddressableAssetsBuildContext aaContext)
        {
            if (schema == null || !schema.IncludeInBuild)
                return string.Empty;

            var bundledProviderId = schema.GetBundleCachedProviderId();
            var assetProviderId = schema.GetAssetCachedProviderId();
            if (!m_CreatedProviderIds.Contains(bundledProviderId))
            {
                m_CreatedProviderIds.Add(bundledProviderId);

                var bundleProviderType = schema.AssetBundleProviderType.Value;
                m_Linker.AddTypes(bundleProviderType);

                var bundleProviderData = ObjectInitializationData.CreateSerializedInitializationData(bundleProviderType, bundledProviderId);
                m_Linker.AddTypes(bundleProviderData.GetRuntimeTypes());
                m_ResourceProviderData.Add(bundleProviderData);

            }

            if (!m_CreatedProviderIds.Contains(assetProviderId))
            {
                m_CreatedProviderIds.Add(assetProviderId);
                var assetProviderType = schema.BundledAssetProviderType.Value;
                m_Linker.AddTypes(assetProviderType);

                var assetProviderData = ObjectInitializationData.CreateSerializedInitializationData(assetProviderType, assetProviderId);
                m_ResourceProviderData.Add(assetProviderData);
                m_Linker.AddTypes(assetProviderData.GetRuntimeTypes());

            }

            var bundleInputDefs = new List<AssetBundleBuild>();
            PrepGroupBundlePacking(assetGroup, bundleInputDefs, aaContext.locations, schema.BundleMode);
            for (int i = 0; i < bundleInputDefs.Count; i++)
            {
                if (aaContext.bundleToAssetGroup.ContainsKey(bundleInputDefs[i].assetBundleName))
                {
                    var bid = bundleInputDefs[i];
                    int count = 1;
                    var newName = bid.assetBundleName;
                    while (aaContext.bundleToAssetGroup.ContainsKey(newName) && count < 1000)
                        newName = bid.assetBundleName.Replace(".bundle", string.Format("{0}.bundle", count++));
                    bundleInputDefs[i] = new AssetBundleBuild { assetBundleName = newName, addressableNames = bid.addressableNames, assetBundleVariant = bid.assetBundleVariant, assetNames = bid.assetNames };
                }

                aaContext.bundleToAssetGroup.Add(bundleInputDefs[i].assetBundleName, assetGroup.Guid);
            }
            m_AllBundleInputDefs.AddRange(bundleInputDefs);
            return string.Empty;
        }

        internal static void PrepGroupBundlePacking(AddressableAssetGroup assetGroup, List<AssetBundleBuild> bundleInputDefs, List<ContentCatalogDataEntry> locationData, BundledAssetGroupSchema.BundlePackingMode packingMode)
        {
            if (packingMode == BundledAssetGroupSchema.BundlePackingMode.PackTogether)
            {
                var allEntries = new List<AddressableAssetEntry>();
                foreach (var a in assetGroup.entries)
                    a.GatherAllAssets(allEntries, true, true);
                GenerateBuildInputDefinitions(allEntries, bundleInputDefs, assetGroup.Name, "all");
            }
            else
            {
                if (packingMode == BundledAssetGroupSchema.BundlePackingMode.PackSeparately)
                {
                    foreach (var a in assetGroup.entries)
                    {
                        var allEntries = new List<AddressableAssetEntry>();
                        a.GatherAllAssets(allEntries, true, true);
                        GenerateBuildInputDefinitions(allEntries, bundleInputDefs, assetGroup.Name, a.address);
                    }
                }
                else
                {
                    var labelTable = new Dictionary<string, List<AddressableAssetEntry>>();
                    foreach (var a in assetGroup.entries)
                    {
                        var sb = new StringBuilder();
                        foreach (var l in a.labels)
                            sb.Append(l);
                        var key = sb.ToString();
                        List<AddressableAssetEntry> entries;
                        if (!labelTable.TryGetValue(key, out entries))
                            labelTable.Add(key, entries = new List<AddressableAssetEntry>());
                        entries.Add(a);
                    }

                    foreach (var entryGroup in labelTable)
                    {
                        foreach (var a in entryGroup.Value)
                        {
                            var allEntries = new List<AddressableAssetEntry>();
                            a.GatherAllAssets(allEntries, true, true);
                            GenerateBuildInputDefinitions(allEntries, bundleInputDefs, assetGroup.Name, entryGroup.Key);
                        }
                    }
                }
            }
        }

        static void GenerateBuildInputDefinitions(List<AddressableAssetEntry> allEntries, List<AssetBundleBuild> buildInputDefs, string groupName, string address)
        {
            var scenes = new List<AddressableAssetEntry>();
            var assets = new List<AddressableAssetEntry>();
            foreach (var e in allEntries)
            {
                if (e.AssetPath.EndsWith(".unity"))
                    scenes.Add(e);
                else
                    assets.Add(e);
            }
            if (assets.Count > 0)
                buildInputDefs.Add(GenerateBuildInputDefinition(assets, groupName + "_assets_" + address + ".bundle"));
            if (scenes.Count > 0)
                buildInputDefs.Add(GenerateBuildInputDefinition(scenes, groupName + "_scenes_" + address + ".bundle"));
        }

        static AssetBundleBuild GenerateBuildInputDefinition(List<AddressableAssetEntry> assets, string name)
        {
            var assetsInputDef = new AssetBundleBuild();
            assetsInputDef.assetBundleName = name.ToLower().Replace(" ", "").Replace('\\', '/').Replace("//", "/");
            var assetIds = new List<string>(assets.Count);
            foreach (var a in assets)
            {
                assetIds.Add(a.AssetPath);
            }
            assetsInputDef.assetNames = assetIds.ToArray();
            assetsInputDef.addressableNames = new string[0];
            return assetsInputDef;
        }

        static void CreateCatalog(AddressableAssetSettings aaSettings, ContentCatalogData contentCatalog, List<ResourceLocationData> locations, string playerVersion, string localCatalogFilename)
        {
            
            var localBuildPath = Addressables.BuildPath + "/" + localCatalogFilename;
            var localLoadPath = "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/" + localCatalogFilename;

            var jsonText = JsonUtility.ToJson(contentCatalog);
            WriteFile(localBuildPath, jsonText);

            string[] dependencyHashes = null;
            if (aaSettings.BuildRemoteCatalog)
            {
                var contentHash = HashingMethods.Calculate(jsonText).ToString();

                var versionedFileName = aaSettings.profileSettings.EvaluateString(aaSettings.activeProfileId, "/catalog_" + playerVersion);
                var remoteBuildFolder = aaSettings.RemoteCatalogBuildPath.GetValue(aaSettings);
                var remoteLoadFolder = aaSettings.RemoteCatalogLoadPath.GetValue(aaSettings);

                if (string.IsNullOrEmpty(remoteBuildFolder) ||
                    string.IsNullOrEmpty(remoteLoadFolder) ||
                    remoteBuildFolder == AddressableAssetProfileSettings.undefinedEntryValue ||
                    remoteLoadFolder == AddressableAssetProfileSettings.undefinedEntryValue)
                {
                    Addressables.LogWarning("Remote Build and/or Load paths are not set on the main AddressableAssetSettings asset, but 'Build Remote Catalog' is true.  Cannot create remote catalog.  In the inspector for any group, double click the 'Addressable Asset Settings' object to begin inspecting it. '" + remoteBuildFolder + "', '" + remoteLoadFolder + "'");
                }
                else
                {
                    var remoteJsonBuildPath = remoteBuildFolder + versionedFileName + ".json";
                    var remoteHashBuildPath = remoteBuildFolder + versionedFileName + ".hash";

                    WriteFile(remoteJsonBuildPath, jsonText);
                    WriteFile(remoteHashBuildPath, contentHash);

                    dependencyHashes = new string[((int)ContentCatalogProvider.DependencyHashIndex.Count)];
                    dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Remote] = ResourceManagerRuntimeData.kCatalogAddress + "RemoteHash";
                    dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Cache] = ResourceManagerRuntimeData.kCatalogAddress + "CacheHash";

                    var remoteHashLoadPath = remoteLoadFolder + versionedFileName + ".hash";
                    locations.Add(new ResourceLocationData(
                        new[] { dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Remote] },
                        remoteHashLoadPath,
                        typeof(TextDataProvider)));

                    var cacheLoadPath = "{UnityEngine.Application.persistentDataPath}/com.unity.addressables" + versionedFileName + ".hash";
                    locations.Add(new ResourceLocationData(
                        new[] { dependencyHashes[(int)ContentCatalogProvider.DependencyHashIndex.Cache] },
                        cacheLoadPath,
                        typeof(TextDataProvider)));
                }
            }

            locations.Add(new ResourceLocationData(
                new []{ ResourceManagerRuntimeData.kCatalogAddress },
                localLoadPath,
                typeof(ContentCatalogProvider),
                dependencyHashes));
        }


        static IList<IBuildTask> RuntimeDataBuildTasks(string builtinShaderBundleName)
        {
            var buildTasks = new List<IBuildTask>();

            // Setup
            buildTasks.Add(new SwitchToBuildPlatform());
            buildTasks.Add(new RebuildSpriteAtlasCache());

            // Player Scripts
            buildTasks.Add(new BuildPlayerScripts());

            // Dependency
            buildTasks.Add(new CalculateSceneDependencyData());
            buildTasks.Add(new CalculateAssetDependencyData());
            buildTasks.Add(new StripUnusedSpriteSources());
            buildTasks.Add(new CreateBuiltInShadersBundle(builtinShaderBundleName));

            // Packing
            buildTasks.Add(new GenerateBundlePacking());
            buildTasks.Add(new UpdateBundleObjectLayout());
            buildTasks.Add(new GenerateLocationListsTask());

            buildTasks.Add(new GenerateBundleCommands());
            buildTasks.Add(new GenerateSubAssetPathMaps());
            buildTasks.Add(new GenerateBundleMaps());

            // Writing
            buildTasks.Add(new WriteSerializedFiles());
            buildTasks.Add(new ArchiveAndCompressBundles());
            //   buildTasks.Add(new PostProcessBundlesTask());

            return buildTasks;
        }

        static bool IsInternalIdLocal(string path)
        {
            return path.StartsWith("{UnityEngine.AddressableAssets.Addressables.RuntimePath}");
        }

        static void PostProcessBundles(AddressableAssetGroup assetGroup, List<string> bundles, IBundleBuildResults buildResult, IWriteData writeData, ResourceManagerRuntimeData runtimeData, List<ContentCatalogDataEntry> locations)
        {
            var schema = assetGroup.GetSchema<BundledAssetGroupSchema>();
            if (schema == null)
                return;

            var path = schema.BuildPath.GetValue(assetGroup.Settings);
            if (string.IsNullOrEmpty(path))
                return;

            foreach (var originalBundleName in bundles)
            {
                var newBundleName = originalBundleName;
                var info = buildResult.BundleInfos[newBundleName];
                ContentCatalogDataEntry dataEntry = locations.First(s => newBundleName == (string)s.Keys[0]);
                if (dataEntry != null)
                {
                    var requestOptions = new AssetBundleRequestOptions
                    {
                        Crc =  schema.UseAssetBundleCrc ? info.Crc : 0,
                        Hash = schema.UseAssetBundleCache ? info.Hash.ToString() : "",
                        ChunkedTransfer = schema.ChunkedTransfer,
                        RedirectLimit = schema.RedirectLimit,
                        RetryCount = schema.RetryCount,
                        Timeout = schema.Timeout,
                        BundleName = Path.GetFileName(info.FileName),
                        BundleSize = GetFileSize(info.FileName)
                    };
                    dataEntry.Data = requestOptions;
                    dataEntry.InternalId = dataEntry.InternalId.Replace(".bundle", "_" + info.Hash + ".bundle");
                    newBundleName = newBundleName.Replace(".bundle", "_" + info.Hash + ".bundle");
                }
                else
                {
                    Debug.LogWarningFormat("Unable to find ContentCatalogDataEntry for bundle {0}.", newBundleName);
                }

                var targetPath = Path.Combine(path, newBundleName);
                if (!Directory.Exists(Path.GetDirectoryName(targetPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.Copy(Path.Combine(assetGroup.Settings.buildSettings.bundleBuildPath, originalBundleName), targetPath, true);
            }
        }

        private static long GetFileSize(string fileName)
        {
            try
            {
                return new FileInfo(fileName).Length;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return 0;
            }

        }

        /// <inheritdoc />
        public override void ClearCachedData()
        {
            if (Directory.Exists(Addressables.BuildPath))
            {
                try
                {
                    var catalogPath = Addressables.BuildPath + "/catalog.json";
                    var settingsPath = Addressables.BuildPath + "/settings.json";
                    DeleteFile(catalogPath);
                    DeleteFile(settingsPath);
                    Directory.Delete(Addressables.BuildPath, true);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

        }
    }
}