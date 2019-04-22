using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using System.IO;


#if UNITY_EDITOR
namespace UnityEngine.ResourceManagement.Tests
{
    public class ResourceManagerTestsLegacy : ResourceManagerBaseTests
    {
        protected override string AssetPathPrefix { get { return "Resources/"; } }
        protected IResourceLocation CreateLocationForAsset(string name, string path)
        {
            return new ResourceLocationBase(name, Path.GetFileNameWithoutExtension(path), typeof(LegacyResourcesProvider).FullName);
        }

        protected override IResourceLocation[] SetupLocations(KeyValuePair<string, string>[] assets)
        {
            IResourceLocation[] locs = new IResourceLocation[assets.Length];
            for (int i = 0; i < locs.Length; i++)
                locs[i] = CreateLocationForAsset(assets[i].Key, assets[i].Value);
            m_ResourceManager.ResourceProviders.Add(new LegacyResourcesProvider());
            return locs;
        }
    }
}
#endif
