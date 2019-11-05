using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
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
    }
    
    public override void Provide(ProvideHandle providerInterface)
    {
        new SyncAssetBundleResource().Start(providerInterface);
    }
}
