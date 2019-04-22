using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

#if UNITY_EDITOR
namespace UnityEngine.ResourceManagement.Tests
{
    public class ResourceManagerFastModeTests : ResourceManagerBaseTests
    {
        protected override IResourceLocation[] SetupLocations(KeyValuePair<string, string>[] assets)
        {
            IResourceLocation[] locs = new IResourceLocation[assets.Length];
            for (int i = 0; i < locs.Length; i++)
                locs[i] = CreateLocationForAsset(assets[i].Key, assets[i].Value);
            m_ResourceManager.ResourceProviders.Add(new AssetDatabaseProvider());
            return locs;
        }
        IResourceLocation CreateLocationForAsset(string name, string path)
        {
            return new ResourceLocationBase(name, path, typeof(AssetDatabaseProvider).FullName);
        }
    }
}
#endif
