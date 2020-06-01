using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

[DisplayName("Sync Bundle Provider")]
public class SyncBundleProvider : AssetBundleProvider
{

    class SyncAssetBundleResource : IAssetBundleResource
    {
        AssetBundle m_AssetBundle;
        public AssetBundle GetAssetBundle()
        {
            return m_AssetBundle;
        }

        internal void Start(ProvideHandle provideHandle)
        {
            m_AssetBundle = AssetBundle.LoadFromFile(provideHandle.Location.InternalId);
            if(m_AssetBundle == null)
                Debug.LogError("the bundle failed " + provideHandle.Location.InternalId);
            provideHandle.Complete(this, m_AssetBundle != null, null);
        }

        internal void Unload()
        {
            if (m_AssetBundle != null)
            {
                m_AssetBundle.Unload(true);
                m_AssetBundle = null;
            }
        }
    }
    
    public override void Provide(ProvideHandle providerInterface)
    {
        new SyncAssetBundleResource().Start(providerInterface);
    }

    public override void Release(IResourceLocation location, object asset)
    {
        if (location == null)
            throw new ArgumentNullException("location");
        if (asset == null)
        {
            Debug.LogWarningFormat("Releasing null asset bundle from location {0}.  This is an indication that the bundle failed to load.", location);
            return;
        }
        if (asset is SyncAssetBundleResource syncResource)
            syncResource.Unload();
    }
}
