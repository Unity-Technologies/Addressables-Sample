#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Exceptions;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.ResourceProviders.Simulation
{
    /// <summary>
    /// Simulates the loading behavior of an asset bundle.  Internally it uses the AssetDatabase API.  This provider will only work in the editor.
    /// </summary>
    public class VirtualAssetBundleProvider : ResourceProviderBase, IUpdateReceiver
    {
        VirtualAssetBundleRuntimeData m_BundleData;

        private VirtualAssetBundleProvider()
        {
            m_ProviderId = typeof(AssetBundleProvider).FullName;
        }

        /// <inheritdoc/>
        public override Type GetDefaultType(IResourceLocation location)
        {
            return typeof(IAssetBundleResource);
        }

        /// <summary>
        /// Construct a new VirtualAssetBundleProvider object;
        /// <param name="data">Contains information on virtual bundle layout</param>
        /// </summary>
        public VirtualAssetBundleProvider(VirtualAssetBundleRuntimeData data)
        {
            InitializeInternal(typeof(AssetBundleProvider).FullName, data);
        }

        private bool InitializeInternal(string providerId, VirtualAssetBundleRuntimeData data)
        {
            m_ProviderId = providerId;
            m_BundleData = data;
            foreach (var b in m_BundleData.AssetBundles)
                m_AllBundles.Add(b.Name, b);
            return !string.IsNullOrEmpty(m_ProviderId);
        }

        /// <summary>
        /// Initilization data is passed when when constructed from serialized data
        /// </summary>
        /// <param name="id">The provider id</param>
        /// <param name="data">The data string, this is assumed to be the virtual bundle data path</param>
        /// <returns>true if the data is as expected</returns>
        public override bool Initialize(string id, string data)
        {
            VirtualAssetBundleRuntimeData bundleData = JsonUtility.FromJson<VirtualAssetBundleRuntimeData>(data);
            return InitializeInternal(id, bundleData);
        }

        class InternalOp
        {
            VBAsyncOperation<VirtualAssetBundle> m_RequestOperation;
            VirtualAssetBundleProvider m_Provider;
            ProvideHandle m_PI;

            public float GetPercentComplete()
            {
                return m_RequestOperation != null ? m_RequestOperation.PercentComplete : 0.0f;
            }

            public void Start(ProvideHandle provideHandle, VirtualAssetBundleProvider provider)
            {
                provideHandle.SetProgressCallback(GetPercentComplete);
                m_Provider = provider;
                m_PI = provideHandle;

                m_RequestOperation = m_Provider.LoadAsync(m_PI.Location);
                m_RequestOperation.Completed += bundleOp =>
                {
                    object result = (bundleOp.Result != null && m_PI.Type.IsAssignableFrom(bundleOp.Result.GetType())) ? bundleOp.Result : null;
                    m_PI.Complete(result, (result != null && bundleOp.OperationException == null), bundleOp.OperationException);
                };
            }
        }

        public override void Provide(ProvideHandle provideHandle)
        {
            new InternalOp().Start(provideHandle, this);
        }

        /// <inheritdoc/>
        public override void Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            if (asset == null)
            {
                Debug.LogWarningFormat("Releasing null asset bundle from location {0}.  This is an indication that the bundle failed to load.", location);
                return;
            }
            Unload(location);
        }

        bool m_UpdatingActiveBundles;
        Dictionary<string, VirtualAssetBundle> m_PendingOperations = new Dictionary<string, VirtualAssetBundle>();

        Dictionary<string, VirtualAssetBundle> m_AllBundles = new Dictionary<string, VirtualAssetBundle>();
        Dictionary<string, VirtualAssetBundle> m_ActiveBundles = new Dictionary<string, VirtualAssetBundle>();

        internal bool Unload(IResourceLocation location)
        {
            if (location == null)
                throw new ArgumentException("IResourceLocation location cannot be null.");

            VirtualAssetBundle bundle;
            if (!m_AllBundles.TryGetValue(location.InternalId, out bundle))
            {
                Debug.LogWarningFormat("Unable to unload virtual bundle {0}.", location);
                return false;
            }
            if (m_UpdatingActiveBundles)
                m_PendingOperations.Add(location.InternalId, null);
            else
                m_ActiveBundles.Remove(location.InternalId);
            return bundle.Unload();
        }

        internal VBAsyncOperation<VirtualAssetBundle> LoadAsync(IResourceLocation location)
        {
            if (location == null)
                throw new ArgumentException("IResourceLocation location cannot be null.");
            VirtualAssetBundle bundle;
            if (!m_AllBundles.TryGetValue(location.InternalId, out bundle))
                return new VBAsyncOperation<VirtualAssetBundle>().StartCompleted(location, location, default(VirtualAssetBundle), new ResourceManagerException(string.Format("Unable to unload virtual bundle {0}.", location)));

            try
            {
                if (m_UpdatingActiveBundles)
                    m_PendingOperations.Add(location.InternalId, bundle);
                else
                    m_ActiveBundles.Add(location.InternalId, bundle);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            return bundle.StartLoad(location);
        }

        internal void Update(float unscaledDeltaTime)
        {
            long localCount = 0;
            long remoteCount = 0;
            foreach (VirtualAssetBundle bundle in m_ActiveBundles.Values)
                bundle.CountBandwidthUsage(ref localCount, ref remoteCount);
            
            long localBw = localCount > 1 ? (m_BundleData.LocalLoadSpeed / localCount) : m_BundleData.LocalLoadSpeed;
            long remoteBw = remoteCount > 1 ? (m_BundleData.RemoteLoadSpeed / remoteCount) : m_BundleData.RemoteLoadSpeed;
            m_UpdatingActiveBundles = true;
            foreach (VirtualAssetBundle bundle in m_ActiveBundles.Values)
                bundle.UpdateAsyncOperations(localBw, remoteBw, unscaledDeltaTime);
            m_UpdatingActiveBundles = false;
            if (m_PendingOperations.Count > 0)
            {
                foreach (var o in m_PendingOperations)
                {
                    if (o.Value == null)
                        m_ActiveBundles.Remove(o.Key);
                    else
                        m_ActiveBundles.Add(o.Key, o.Value);
                }
                m_PendingOperations.Clear();
            }
        }

        void IUpdateReceiver.Update(float unscaledDeltaTime)
        {
            Update(unscaledDeltaTime);
        }

    }
}
#endif
