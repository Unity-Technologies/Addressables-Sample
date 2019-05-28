#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.ResourceManagement.ResourceProviders.Simulation
{
    /// <summary>
    /// Custom version of AssetBundleRequestOptions used to compute needed download size while bypassing the cache.  In the future a virtual cache may be implemented.
    /// </summary>
    [Serializable]
    public class VirtualAssetBundleRequestOptions : AssetBundleRequestOptions
    {
        /// <summary>
        /// Computes the amount of data needed to be downloaded for this bundle.
        /// </summary>
        /// <param name="loc">The location of the bundle.</param>
        /// <returns>The size in bytes of the bundle that is needed to be downloaded.  If the local cache contains the bundle or it is a local bundle, 0 will be returned.</returns>
        public override long ComputeSize(IResourceLocation loc)
        {
            if (!loc.InternalId.Contains("://"))
            {
//                Debug.LogFormat("Location {0} is local, ignoring size", loc);
                return 0;
            }
            var locHash = Hash128.Parse(Hash);
            if (!locHash.isValid)
            {
 //               Debug.LogFormat("Location {0} has invalid hash, using size of {1}", loc, BundleSize);
                return BundleSize;
            }
#if !UNITY_SWITCH && !UNITY_PS4
            var bundleName = Path.GetFileNameWithoutExtension(loc.InternalId);
            if (locHash.isValid) //If we have a hash, ensure that our desired version is cached.
            {
                if (Caching.IsVersionCached(bundleName, locHash))
                    return 0;
                return BundleSize;
            }
            else //If we don't have a hash, any cached version will do.
            {
                List<Hash128> versions = new List<Hash128>();
                Caching.GetCachedVersions(bundleName, versions);
                if (versions.Count > 0)
                    return 0;
            }
#endif //!UNITY_SWITCH && !UNITY_PS4
            return BundleSize;
        }
    }

    /// <summary>
    /// Provides assets from virtual asset bundles.  Actual loads are done through the AssetDatabase API.
    /// </summary>
    public class VirtualBundledAssetProvider : ResourceProviderBase
    {
        /// <summary>
        /// Default copnstructor.
        /// </summary>
        public VirtualBundledAssetProvider()
        {
            m_ProviderId = typeof(BundledAssetProvider).FullName; 
        }

        class InternalOp
        {
            VBAsyncOperation<object> m_RequestOperation;
            ProvideHandle m_PI;

            public void Start(ProvideHandle provideHandle, VirtualAssetBundle bundle)
            {
                m_PI = provideHandle;
                m_RequestOperation = bundle.LoadAssetAsync(m_PI.Type, m_PI.Location);
                m_RequestOperation.Completed += RequestOperation_Completed;
            }

            private void RequestOperation_Completed(VBAsyncOperation<object> obj)
            {
                bool success = (obj.Result != null && m_PI.Type.IsAssignableFrom(obj.Result.GetType())) && obj.OperationException == null;
                m_PI.Complete(obj.Result, success, obj.OperationException);
            }

            public float GetPercentComplete() { return m_RequestOperation != null ? m_RequestOperation.PercentComplete : 0.0f; }
        }

        public override void Provide(ProvideHandle provideHandle)
        {
            List<object> deps = new List<object>(); // TODO: garbage. need to pass actual count and reuse the list
            provideHandle.GetDependencies(deps);
            VirtualAssetBundle bundle = deps[0] as VirtualAssetBundle;
            if (bundle == null)
            {
                provideHandle.Complete<object>(null, false, null);
            }
            else
            {
                new InternalOp().Start(provideHandle, bundle);
            }
        }
    }
}
#endif
