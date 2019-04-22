using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using Debug = UnityEngine.Debug;

namespace UnityEditor.AddressableAssets.Build.AnalyzeRules
{
    class CheckDupeDependencies : AnalyzeRule
    {
        [NonSerialized]
        HashSet<GUID> m_ImplicitAssets;
        internal override string ruleName
        { get { return "Check Duplicate Bundle Dependencies"; } }

        internal override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
        {
            return DoFakeBuild(settings);
        }

        List<AnalyzeResult> DoFakeBuild(AddressableAssetSettings settings)
        {
            m_ImplicitAssets = new HashSet<GUID>();
            List<AnalyzeResult> emptyResult = new List<AnalyzeResult>();
            emptyResult.Add(new AnalyzeResult(ruleName + " - No issues found"));
            var context = new AddressablesDataBuilderInput(settings);
            var timer = new Stopwatch();
            timer.Start();
            var aaSettings = context.AddressableSettings;

            //gather entries
            var locations = new List<ContentCatalogDataEntry>();
            var allBundleInputDefs = new List<AssetBundleBuild>();
            var bundleToAssetGroup = new Dictionary<string, string>();
            var runtimeData = new ResourceManagerRuntimeData();
            runtimeData.LogResourceManagerExceptions = aaSettings.buildSettings.LogResourceManagerExceptions;

            foreach (var assetGroup in aaSettings.groups)
            {
                var schema = assetGroup.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                    continue;

                var bundleInputDefs = new List<AssetBundleBuild>();
                BuildScriptPackedMode.PrepGroupBundlePacking(assetGroup, bundleInputDefs, locations, schema.BundleMode);
                for (int i = 0; i < bundleInputDefs.Count; i++)
                {
                    if (bundleToAssetGroup.ContainsKey(bundleInputDefs[i].assetBundleName))
                    {
                        var bid = bundleInputDefs[i];
                        int count = 1;
                        var newName = bid.assetBundleName;
                        while (bundleToAssetGroup.ContainsKey(newName) && count < 1000)
                            newName = bid.assetBundleName.Replace(".bundle", string.Format("{0}.bundle", count++));
                        bundleInputDefs[i] = new AssetBundleBuild { assetBundleName = newName, addressableNames = bid.addressableNames, assetBundleVariant = bid.assetBundleVariant, assetNames = bid.assetNames };
                    }

                    bundleToAssetGroup.Add(bundleInputDefs[i].assetBundleName, assetGroup.Guid);
                }
                allBundleInputDefs.AddRange(bundleInputDefs);
            }
            ExtractDataTask extractData = new ExtractDataTask();

            if (allBundleInputDefs.Count > 0)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.LogError("Cannot run Analyze with unsaved scenes");
                    return emptyResult;
                }

                var buildTarget = context.Target;
                var buildTargetGroup = context.TargetGroup;
                var buildParams = new AddressableAssetsBundleBuildParameters(aaSettings, bundleToAssetGroup, buildTarget, buildTargetGroup, aaSettings.buildSettings.bundleBuildPath);
                var builtinShaderBundleName = aaSettings.DefaultGroup.Name.ToLower().Replace(" ", "").Replace('\\', '/').Replace("//", "/") + "_unitybuiltinshaders.bundle";
                var buildTasks = RuntimeDataBuildTasks(builtinShaderBundleName);
                buildTasks.Add(extractData);

                var aaContext = new AddressableAssetsBuildContext
                {
                    settings = aaSettings,
                    runtimeData = runtimeData,
                    bundleToAssetGroup = bundleToAssetGroup,
                    locations = locations
                };

                IBundleBuildResults buildResults;
                var exitCode = ContentPipeline.BuildAssetBundles(buildParams, new BundleBuildContent(allBundleInputDefs), out buildResults, buildTasks, aaContext);

                if (exitCode < ReturnCode.Success)
                {
                    Debug.LogError("Analyze build failed. " + exitCode);
                    return emptyResult;
                }
                
                HashSet<GUID> explicitGuids = new HashSet<GUID>();
                foreach (var atf in extractData.WriteData.AssetToFiles)
                {
                    explicitGuids.Add(atf.Key);
                }

                Dictionary<GUID, List<string>> implicitGuids = new Dictionary<GUID, List<string>>();
                foreach (var fto in extractData.WriteData.FileToObjects)
                {
                    foreach (ObjectIdentifier g in fto.Value)
                    {
                        if (!explicitGuids.Contains(g.guid))
                        {
                            if (!implicitGuids.ContainsKey(g.guid))
                            {
                                implicitGuids.Add(g.guid, new List<string>());
                            }
                            implicitGuids[g.guid].Add(fto.Key);
                        }
                    }
                }

                //dictionary<group, dictionary<bundle, implicit assets >>
                Dictionary<string, Dictionary<string, List<string>>> allIssues = new Dictionary<string, Dictionary<string, List<string>>>();
                foreach (var g in implicitGuids)
                {
                    if (g.Value.Count > 1) //it's duplicated...
                    {
                        var path = AssetDatabase.GUIDToAssetPath(g.Key.ToString());
                        if(!AddressableAssetUtility.IsPathValidForEntry(path) || 
                            path.ToLower().Contains("/resources/") || 
                            path.ToLower().StartsWith("resources/"))
                            continue;

                        foreach (var file in g.Value)
                        {
                            var bun = extractData.WriteData.FileToBundle[file];
                            string groupGuid;
                            if (aaContext.bundleToAssetGroup.TryGetValue(bun, out groupGuid))
                            {
                                var group = aaSettings.FindGroup(grp => grp.Guid == groupGuid);
                                if (group != null)
                                {
                                    Dictionary<string, List<string>> groupData;
                                    if (!allIssues.TryGetValue(group.Name, out groupData))
                                    {
                                        groupData = new Dictionary<string, List<string>>();
                                        allIssues.Add(group.Name, groupData);
                                    }

                                    List<string> assets;
                                    if (!groupData.TryGetValue(bun, out assets))
                                    {
                                        assets = new List<string>();
                                        groupData.Add(bun, assets);
                                    }
                                    assets.Add(path);

                                    m_ImplicitAssets.Add(g.Key);
                                }
                            }
                        }
                    }
                }

                List<AnalyzeResult> result = new List<AnalyzeResult>();
                foreach (var group in allIssues)
                {
                    foreach (var bundle in group.Value)
                    {
                        foreach (var item in bundle.Value)
                        {
                            var issueName = ruleName + kDelimiter + group.Key + kDelimiter + bundle.Key + kDelimiter + item;
                            result.Add(new AnalyzeResult(issueName, MessageType.Warning));
                        }
                    }
                }

                if (result.Count > 0)
                    return result;
            }
            return emptyResult;
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

            return buildTasks;
        }

        internal override void FixIssues(AddressableAssetSettings settings)
        {
            if (m_ImplicitAssets == null)
                DoFakeBuild(settings);

            if (m_ImplicitAssets.Count == 0)
                return;

            var group = settings.CreateGroup("Duplicate Asset Isolation", false, false, false, null, typeof(BundledAssetGroupSchema));
            foreach (var asset in m_ImplicitAssets)
                settings.CreateOrMoveEntry(asset.ToString(), group, false, false);
        }

        internal override void ClearAnalysis()
        {
            m_ImplicitAssets = null;
        }

    }

}