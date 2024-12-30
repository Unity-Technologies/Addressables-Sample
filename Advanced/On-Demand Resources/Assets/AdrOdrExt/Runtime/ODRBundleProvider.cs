using System;
using System.ComponentModel;
#if UNITY_IOS
using UnityEngine.iOS;
#endif
using UnityEngine.ResourceManagement.ResourceLocations;

#if UNITY_IOS

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    public class ODRAssetBundleResource : IAssetBundleResource, IUpdateReceiver
    {
        AssetBundle m_AssetBundle;
        OnDemandResourcesRequest m_Request;
        ProvideHandle m_Handle;

        public void Start(ProvideHandle handle)
        {
            m_Handle = handle;
            m_Request = OnDemandResources.PreloadAsync(new string[] { m_Handle.Location.PrimaryKey });
            m_Request.completed += Completed;
        }

        private void Completed(AsyncOperation obj)
        {
            m_AssetBundle = AssetBundle.LoadFromFile("res://" + m_Handle.Location.PrimaryKey);
            m_Handle.Complete<ODRAssetBundleResource>(this, string.IsNullOrEmpty(m_Request.error), null);
        }

        public AssetBundle GetAssetBundle()
        {
            if (m_AssetBundle == null)
            {
                if (m_Request.isDone)
                    m_AssetBundle = AssetBundle.LoadFromFile("res://" + m_Handle.Location.PrimaryKey);
            }

            return m_AssetBundle;
        }

        public void Unload()
        {
            if (m_AssetBundle != null)
            {
                m_AssetBundle.Unload(true);
                m_AssetBundle = null;
            }
        }

        public void Update(float unscaledDeltaTime)
        {
            // Check for errors
            if (m_Request.error != null)
                throw new Exception("ODR request failed: " + m_Request.error);
        }
    }

    [DisplayName("ODR AssetBundle Provider")]
    public class ODRBundleProvider : ResourceProviderBase
    {
        /// <inheritdoc/>
        public override void Provide(ProvideHandle providerInterface)
        {
            new ODRAssetBundleResource().Start(providerInterface);
        }

        /// <inheritdoc/>
        public override Type GetDefaultType(IResourceLocation location)
        {
            return typeof(IAssetBundleResource);
        }

        /// <summary>
        /// Releases the asset bundle via AssetBundle.Unload(true).
        /// </summary>
        /// <param name="location">The location of the asset to release</param>
        /// <param name="asset">The asset in question</param>
        public override void Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            if (asset == null)
            {
                Debug.LogWarningFormat("Releasing null asset bundle from location {0}.  This is an indication that the bundle failed to load.", location);
                return;
            }
            var bundle = asset as ODRAssetBundleResource;
            if (bundle != null)
            {
                bundle.Unload();
                return;
            }
        }
    }
}

#else

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    [DisplayName("ODR AssetBundle Provider")]
    public class ODRBundleProvider : AssetBundleProvider
    {}
}

#endif

