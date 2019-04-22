using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

#if UNITY_EDITOR
using UnityEngine.ResourceManagement.ResourceProviders.Simulation;
#endif

#if UNITY_EDITOR
namespace UnityEngine.ResourceManagement.Tests
{
    public class ResourceManagerVirtualModeTests : ResourceManagerBaseTests
    {
        VirtualAssetBundleRuntimeData virtualBundleData = null;
        List<IResourceLocation> sharedBundleLocations = null;
        Dictionary<string, VirtualAssetBundle> bundleMap = null;
        const int kBundleCount = 10;

        protected override IResourceLocation[] SetupLocations(KeyValuePair<string, string>[] assets)
        {
            Random.InitState(0);
            virtualBundleData = new VirtualAssetBundleRuntimeData();
            sharedBundleLocations = new List<IResourceLocation>();
            bundleMap = new Dictionary<string, VirtualAssetBundle>();
            for (int i = 0; i < kBundleCount; i++)
            {
                var bundleName = "shared" + i;
                var b = new VirtualAssetBundle("shared" + i, i % 2 == 0, 0, "");
                virtualBundleData.AssetBundles.Add(b);
                bundleMap.Add(b.Name, b);
                sharedBundleLocations.Add(new ResourceLocationBase(bundleName, bundleName, typeof(AssetBundleProvider).FullName));
            }

            IResourceLocation []locs = new IResourceLocation[assets.Length];
            for(int i = 0; i < locs.Length;i++)
                locs[i] = CreateLocationForAsset(assets[i].Key, assets[i].Value);

            foreach (var b in virtualBundleData.AssetBundles)
            {
                b.SetSize(2048, 1024);
                b.OnAfterDeserialize();
            }
            m_ResourceManager.ResourceProviders.Insert(0, new VirtualAssetBundleProvider(virtualBundleData));
            m_ResourceManager.ResourceProviders.Insert(0, new VirtualBundledAssetProvider());
            return locs;
        }


        protected IResourceLocation CreateLocationForAsset(string name, string path)
        {
            int sharedBundleIndex = 0;
            Random.Range(0, sharedBundleLocations.Count-3);
            IResourceLocation bundle = sharedBundleLocations[sharedBundleIndex];
            VirtualAssetBundle vBundle = bundleMap[bundle.InternalId];
            vBundle.Assets.Add(new VirtualAssetBundleEntry(path, Random.Range(1024, 1024 * 1024)));
            IResourceLocation dep1Location = sharedBundleLocations[sharedBundleIndex+1];
            IResourceLocation dep2Location = sharedBundleLocations[sharedBundleIndex+2];
            return new ResourceLocationBase(name, path, typeof(BundledAssetProvider).FullName, bundle, dep1Location, dep2Location);
        }
    }
}
#endif

