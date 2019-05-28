using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Wrapper for asset bundles. 
    /// </summary>
    public interface IAssetBundleResource
    {
        AssetBundle GetAssetBundle();
        
    }

    /// <summary>
    /// Contains cache information to be used by the AssetBundleProvider
    /// </summary>
    [Serializable]
    public class AssetBundleRequestOptions : ILocationSizeData
    {
        [FormerlySerializedAs("m_hash")]
        [SerializeField]
        string m_Hash = "";
        /// <summary>
        /// Hash value of the asset bundle.
        /// </summary>
        public string Hash { get { return m_Hash; } set { m_Hash = value; } }
        [FormerlySerializedAs("m_crc")]
        [SerializeField]
        uint m_Crc;
        /// <summary>
        /// CRC value of the bundle.
        /// </summary>
        public uint Crc { get { return m_Crc; } set { m_Crc = value; } }
        [FormerlySerializedAs("m_timeout")]
        [SerializeField]
        int m_Timeout;
        /// <summary>
        /// Sets UnityWebRequest to attempt to abort after the number of seconds in timeout have passed.
        /// </summary>
        public int Timeout { get { return m_Timeout; } set { m_Timeout = value; } }
        [FormerlySerializedAs("m_chunkedTransfer")]
        [SerializeField]
        bool m_ChunkedTransfer;
        /// <summary>
        /// Indicates whether the UnityWebRequest system should employ the HTTP/1.1 chunked-transfer encoding method.
        /// </summary>
        public bool ChunkedTransfer { get { return m_ChunkedTransfer; } set { m_ChunkedTransfer = value; } }
        [FormerlySerializedAs("m_redirectLimit")]
        [SerializeField]
        int m_RedirectLimit = -1;
        /// <summary>
        /// Indicates the number of redirects which this UnityWebRequest will follow before halting with a “Redirect Limit Exceeded” system error.
        /// </summary>
        public int RedirectLimit { get { return m_RedirectLimit; } set { m_RedirectLimit = value; } }
        [FormerlySerializedAs("m_retryCount")]
        [SerializeField]
        int m_RetryCount;
        /// <summary>
        /// Indicates the number of times the request will be retried.  
        /// </summary>
        public int RetryCount { get { return m_RetryCount; } set { m_RetryCount = value; } }

        [SerializeField]
        string m_BundleName = null;
        /// <summary>
        /// The name of the original bundle.  This does not contain the appended hash.
        /// </summary>
        public string BundleName { get { return m_BundleName; } set { m_BundleName = value; } }

        [SerializeField]
        long m_BundleSize;
        /// <summary>
        /// The size of the bundle, in bytes.
        /// </summary>
        public long BundleSize { get { return m_BundleSize; } set { m_BundleSize = value; } }
        /// <summary>
        /// Computes the amount of data needed to be downloaded for this bundle.
        /// </summary>
        /// <param name="loc">The location of the bundle.</param>
        /// <returns>The size in bytes of the bundle that is needed to be downloaded.  If the local cache contains the bundle or it is a local bundle, 0 will be returned.</returns>
        public virtual long ComputeSize(IResourceLocation loc)
        {
            if (!loc.InternalId.Contains("://"))
                return 0;
            var locHash = Hash128.Parse(Hash);
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

    class AssetBundleResource : IAssetBundleResource
    {
        AssetBundle m_AssetBundle;
        DownloadHandlerAssetBundle m_downloadHandler;
        AsyncOperation m_RequestOperation;
        ProvideHandle m_ProvideHandle;
        AssetBundleRequestOptions m_Options;
        int m_Retries;

        UnityWebRequest CreateWebRequest(IResourceLocation loc)
        {
            if (m_Options == null)
                return UnityWebRequestAssetBundle.GetAssetBundle(loc.InternalId);

            var webRequest = !string.IsNullOrEmpty(m_Options.Hash) ?
                UnityWebRequestAssetBundle.GetAssetBundle(loc.InternalId, Hash128.Parse(m_Options.Hash), m_Options.Crc) :
                UnityWebRequestAssetBundle.GetAssetBundle(loc.InternalId, m_Options.Crc);

            if (m_Options.Timeout > 0)
                webRequest.timeout = m_Options.Timeout;
            if (m_Options.RedirectLimit > 0)
                webRequest.redirectLimit = m_Options.RedirectLimit;
            webRequest.chunkedTransfer = m_Options.ChunkedTransfer;
            if (m_ProvideHandle.ResourceManager.CertificateHandlerInstance != null)
            {
                webRequest.certificateHandler = m_ProvideHandle.ResourceManager.CertificateHandlerInstance;
                webRequest.disposeCertificateHandlerOnDispose = false;
            }
            return webRequest;
        }

        float PercentComplete() { return m_RequestOperation != null ? m_RequestOperation.progress : 0.0f; }

        /// <summary>
        /// Get the asset bundle object managed by this resource.  This call may force the bundle to load if not already loaded.
        /// </summary>
        /// <returns>The asset bundle.</returns>
        public AssetBundle GetAssetBundle()
        {
            if (m_AssetBundle == null && m_downloadHandler != null)
            {
                m_AssetBundle = m_downloadHandler.assetBundle;
                m_downloadHandler.Dispose();
                m_downloadHandler = null;
            }
            return m_AssetBundle;
        }

        internal void Start(ProvideHandle provideHandle)
        {
            m_Retries = 0;
            m_AssetBundle = null;
            m_downloadHandler = null;
            m_ProvideHandle = provideHandle;
            m_Options = m_ProvideHandle.Location.Data as AssetBundleRequestOptions;
            m_RequestOperation = null;
            BeginOperation();
            provideHandle.SetProgressCallback(PercentComplete);
        }

        private void BeginOperation()
        {
            string path = m_ProvideHandle.Location.InternalId;
            if (File.Exists(path))
            {
                m_RequestOperation = AssetBundle.LoadFromFileAsync(path, m_Options == null ? 0 : m_Options.Crc);
                m_RequestOperation.completed += LocalRequestOperationCompleted;
            }
            else if (path.Contains("://"))
            {
                var req = CreateWebRequest(m_ProvideHandle.Location);
                req.disposeDownloadHandlerOnDispose = false;
                m_RequestOperation = req.SendWebRequest();
                m_RequestOperation.completed += WebRequestOperationCompleted;
            }
            else
            {
                m_RequestOperation = null;
                m_ProvideHandle.Complete<AssetBundleResource>(null, false, new Exception(string.Format("Invalid path in AssetBundleProvider: '{0}'.", path)));
            }
        }

        private void LocalRequestOperationCompleted(AsyncOperation op)
        {
            m_AssetBundle = (op as AssetBundleCreateRequest).assetBundle;
            m_ProvideHandle.Complete(this, m_AssetBundle != null, null);
        }

        private void WebRequestOperationCompleted(AsyncOperation op)
        {
            UnityWebRequestAsyncOperation remoteReq = op as UnityWebRequestAsyncOperation;
            var webReq = remoteReq.webRequest;
            if (string.IsNullOrEmpty(webReq.error))
            {
                m_downloadHandler = webReq.downloadHandler as DownloadHandlerAssetBundle;
                m_ProvideHandle.Complete(this, true, null);
            }
            else
            {
                m_downloadHandler = webReq.downloadHandler as DownloadHandlerAssetBundle;
                m_downloadHandler.Dispose();
                m_downloadHandler = null;
                if (m_Retries++ < m_Options.RetryCount)
                {
                    Debug.LogFormat("Web request {0} failed with error '{1}', retrying ({2}/{3})...", webReq.url, webReq.error, m_Retries, m_Options.RetryCount);
                    BeginOperation();
                }
                else
                {
                    var exception = new Exception(string.Format("RemoteAssetBundleProvider unable to load from url {0}, result='{1}'.", webReq.url, webReq.error));
                    m_ProvideHandle.Complete<AssetBundleResource>(null, false, exception);
                }
            }
            webReq.Dispose();
        }
        /// <summary>
        /// Unloads all resources associated with this asset bundle.
        /// </summary>
        public void Unload()
        {
            if (m_AssetBundle != null)
            {
                m_AssetBundle.Unload(true);
                m_AssetBundle = null;
            }
            if (m_downloadHandler != null)
            {
                m_downloadHandler.Dispose();
                m_downloadHandler = null;
            }
            m_RequestOperation = null;
        }
    }
    /// <summary>
    /// IResourceProvider for asset bundles.  Loads bundles via UnityWebRequestAssetBundle API if the internalId contains "://".  If not, it will load the bundle via AssetBundle.LoadFromFileAsync.
    /// </summary>
    public class AssetBundleProvider : ResourceProviderBase
    {
        /// <inheritdoc/>
        public override void Provide(ProvideHandle providerInterface)
        {
            new AssetBundleResource().Start(providerInterface);
        }
        /// <inheritdoc/>
        public override Type GetDefaultType(IResourceLocation location)
        {
            return typeof(IAssetBundleResource);
        }

        /// <summary>
        /// Releases the asset bundle via AssetBundle.Unload(true).
        /// </summary>
        /// <param name="location"></param>
        /// <param name="asset"></param>
        /// <returns></returns>
        public override void Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            if (asset == null)
            {
                Debug.LogWarningFormat("Releasing null asset bundle from location {0}.  This is an indication that the bundle failed to load.", location);
                return;
            }
            var bundle = asset as AssetBundleResource;
            if (bundle != null)
            {
                bundle.Unload();
                return;
            }
            return;
        }
    }
}
