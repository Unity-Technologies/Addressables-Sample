using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Provides assets stored in an asset bundle.
    /// </summary>
    public class BundledAssetProvider : ResourceProviderBase
    {
        internal class InternalOp
        {
            AssetBundleRequest m_RequestOperation;
            ProvideHandle m_ProvideHandle;
            
            internal static AssetBundle LoadBundleFromDependecies(IList<object> results)
            {
                if (results == null || results.Count == 0)
                    return null;

                AssetBundle bundle = null;
                bool firstBundleWrapper = true;
                for (int i = 0; i < results.Count; i++)
                {
                    var abWrapper = results[i] as IAssetBundleResource;
                    if (abWrapper != null)
                    {
                        //only use the first asset bundle, even if it is invalid
                        var b = abWrapper.GetAssetBundle();
                        if (firstBundleWrapper)
                            bundle = b;
                        firstBundleWrapper = false;
                    }
                }
                return bundle;
            }
            
            public void Start(ProvideHandle provideHandle)
            {
                m_ProvideHandle = provideHandle;
                Type t = m_ProvideHandle.Type;
                
                
                m_RequestOperation = null;

                List<object> deps = new List<object>(); // TODO: garbage. need to pass actual count and reuse the list
                m_ProvideHandle.GetDependencies(deps);
                AssetBundle bundle = LoadBundleFromDependecies(deps);
                if (bundle == null)
                {
                    m_ProvideHandle.Complete<AssetBundle>(null, false, new Exception("Unable to load dependent bundle from location " + m_ProvideHandle.Location));
                }
                else
                {
                    if (t.IsArray)
                        m_RequestOperation = bundle.LoadAssetWithSubAssetsAsync(m_ProvideHandle.Location.InternalId, t.GetElementType());
                    else if (t.IsGenericType && typeof(IList<>) == t.GetGenericTypeDefinition())
                        m_RequestOperation = bundle.LoadAssetWithSubAssetsAsync(m_ProvideHandle.Location.InternalId, t.GetGenericArguments()[0]);
                    else
                        m_RequestOperation = bundle.LoadAssetAsync(m_ProvideHandle.Location.InternalId, t);

                    m_RequestOperation.completed += ActionComplete;
                    provideHandle.SetProgressCallback(ProgressCallback);
                }
            }

            private void ActionComplete(AsyncOperation obj)
            {
                object result = null;
                Type t = m_ProvideHandle.Type;
                if (m_RequestOperation != null)
                {
                    if (t.IsArray)
                        result = ResourceManagerConfig.CreateArrayResult(t, m_RequestOperation.allAssets);
                    if (t.IsGenericType && typeof(IList<>) == t.GetGenericTypeDefinition())
                        result = ResourceManagerConfig.CreateListResult(t, m_RequestOperation.allAssets);
                    else
                        result = (m_RequestOperation.asset != null && t.IsAssignableFrom(m_RequestOperation.asset.GetType())) ? m_RequestOperation.asset : null;
                }
                m_ProvideHandle.Complete(result, result != null, null);
            }

            public float ProgressCallback() { return m_RequestOperation != null ? m_RequestOperation.progress : 0.0f; }
        }

        /// <inheritdoc/>
        public override void Provide(ProvideHandle provideHandle)
        {
            new InternalOp().Start(provideHandle);
        }
    }
}
