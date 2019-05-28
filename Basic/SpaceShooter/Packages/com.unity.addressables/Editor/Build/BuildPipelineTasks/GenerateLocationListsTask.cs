using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;

namespace UnityEditor.AddressableAssets.Build.BuildPipelineTasks
{
    class GenerateLocationListsTask : IBuildTask
    {
        const int k_Version = 1;
        public int Version { get { return k_Version; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IAddressableAssetsBuildContext m_AaBuildContext;

        [InjectContext]
        IBundleWriteData m_WriteData;
#pragma warning restore 649

        public ReturnCode Run()
        {
            return Run(m_AaBuildContext, m_WriteData);
        }

        public static ReturnCode Run(IAddressableAssetsBuildContext aaBuildContext, IBundleWriteData writeData)
        {
            var aaContext = aaBuildContext as AddressableAssetsBuildContext;
            if (aaContext == null)
                return ReturnCode.Error;
            var aaSettings = aaContext.settings;
            var locations = aaContext.locations;
            var bundleToAssetGroup = aaContext.bundleToAssetGroup;
            var bundleToAssets = new Dictionary<string, List<GUID>>();
            var assetsToBundles = new Dictionary<GUID, List<string>>();
            foreach (var k in writeData.AssetToFiles)
            {
                List<string> bundleList = new List<string>();
                assetsToBundles.Add(k.Key, bundleList);
                List<GUID> assetList;
                var bundle = writeData.FileToBundle[k.Value[0]];
                if (!bundleToAssets.TryGetValue(bundle, out assetList))
                    bundleToAssets.Add(bundle, assetList = new List<GUID>());
                if (!bundleList.Contains(bundle))
                    bundleList.Add(bundle);
                foreach (var file in k.Value)
                {
                    var fileBundle = writeData.FileToBundle[file];
                    if (!bundleList.Contains(fileBundle))
                        bundleList.Add(fileBundle);
                    if (!bundleToAssets.ContainsKey(fileBundle))
                        bundleToAssets.Add(fileBundle, new List<GUID>());
                }

                assetList.Add(k.Key);
            }
            var assetGroupToBundle = (aaContext.assetGroupToBundles = new Dictionary<AddressableAssetGroup, List<string>>());
            foreach (var kvp in bundleToAssets)
            {
                AddressableAssetGroup assetGroup = aaSettings.DefaultGroup;
                string groupGuid;
                if (bundleToAssetGroup.TryGetValue(kvp.Key, out groupGuid))
                    assetGroup = aaSettings.FindGroup(g => g.Guid == groupGuid);

                List<string> bundles;
                if (!assetGroupToBundle.TryGetValue(assetGroup, out bundles))
                    assetGroupToBundle.Add(assetGroup, bundles = new List<string>());
                bundles.Add(kvp.Key);
                CreateResourceLocationData(assetGroup, kvp.Key, GetLoadPath(assetGroup, kvp.Key), GetBundleProviderName(assetGroup), GetAssetProviderName(assetGroup), kvp.Value, assetsToBundles, locations);
            }

            return ReturnCode.Success;
        }

        static string GetBundleProviderName(AddressableAssetGroup group)
        {
            return group.GetSchema<BundledAssetGroupSchema>().GetBundleCachedProviderId();
        }

        static string GetAssetProviderName(AddressableAssetGroup group)
        {
            return group.GetSchema<BundledAssetGroupSchema>().GetAssetCachedProviderId();
        }

        static string GetLoadPath(AddressableAssetGroup group, string name)
        {
            var bagSchema = group.GetSchema<BundledAssetGroupSchema>();
            var loadPath = bagSchema.LoadPath.GetValue(group.Settings) + "/" + name;
            if(!string.IsNullOrEmpty(bagSchema.UrlSuffix))
                loadPath += bagSchema.UrlSuffix;
            return loadPath;
        }

        internal static void CreateResourceLocationData(
        AddressableAssetGroup assetGroup,
        string bundleName,
        string bundleInternalId,
        string bundleProvider,
        string assetProvider,
        List<GUID> assetsInBundle,
        Dictionary<GUID, List<string>> assetsToBundles,
        List<ContentCatalogDataEntry> locations)
        {
            locations.Add(new ContentCatalogDataEntry(bundleInternalId, bundleProvider, new object[] { bundleName }));

            var assets = new List<AddressableAssetEntry>();
            assetGroup.GatherAllAssets(assets, true, true);
            var guidToEntry = new Dictionary<string, AddressableAssetEntry>();
            foreach (var a in assets)
                guidToEntry.Add(a.guid, a);

            foreach (var a in assetsInBundle)
            {
                AddressableAssetEntry entry;
                if (!guidToEntry.TryGetValue(a.ToString(), out entry))
                    continue;
                var assetPath = entry.GetAssetLoadPath(true);
                locations.Add(new ContentCatalogDataEntry(assetPath, assetProvider, entry.CreateKeyList(), assetsToBundles[a].ToArray()));
            }
        }
    }

}
