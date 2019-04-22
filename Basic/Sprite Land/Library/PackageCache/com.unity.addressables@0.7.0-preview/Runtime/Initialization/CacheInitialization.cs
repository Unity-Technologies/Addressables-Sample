using System;
using System.IO;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

namespace UnityEngine.AddressableAssets.Initialization
{
    /// <summary>
    /// IInitializableObject that sets up the Caching system.
    /// </summary>
    [Serializable]
    public class CacheInitialization : IInitializableObject
    {
        /// <summary>
        /// Sets properties of the Caching system.
        /// </summary>
        /// <param name="id">The id of thei object.</param>
        /// <param name="dataStr">The JSON serialized CacheInitializationData object.</param>
        /// <returns>True if the initialization succeeded.</returns>
        public bool Initialize(string id, string dataStr)
        {
#if !UNITY_SWITCH && !UNITY_PS4
            var data = JsonUtility.FromJson<CacheInitializationData>(dataStr);
            if (data != null)
            {
                Caching.compressionEnabled = data.CompressionEnabled;
                var activeCache = Caching.currentCacheForWriting;
                if (!string.IsNullOrEmpty(data.CacheDirectoryOverride))
                {
                    var dir = Addressables.ResolveInternalId(data.CacheDirectoryOverride);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    activeCache = Caching.GetCacheByPath(dir);
                    if(!activeCache.valid)
                        activeCache = Caching.AddCache(dir);

                    Caching.currentCacheForWriting = activeCache;
                }
                if (data.LimitCacheSize)
                    activeCache.maximumAvailableStorageSpace = data.MaximumCacheSize;
                else
                    activeCache.maximumAvailableStorageSpace = long.MaxValue;

                activeCache.expirationDelay = data.ExpirationDelay;
            }
#endif //!UNITY_SWITCH && !UNITY_PS4
            return true;
        }

#if !UNITY_SWITCH && !UNITY_PS4
        /// <summary>
        /// The root path of the cache.
        /// </summary>
        public static string RootPath { get { return Path.GetDirectoryName(Caching.defaultCache.path); } }
#endif //!UNITY_SWITCH && !UNITY_PS4
    }

    /// <summary>
    /// Contains settings for the Caching system.
    /// </summary>
    [Serializable]
    public class CacheInitializationData
    {
        [FormerlySerializedAs("m_compressionEnabled")]
        [SerializeField]
        bool m_CompressionEnabled = true;
        /// <summary>
        /// Enable recompression of asset bundles into LZ4 format as they are saved to the cache.  This sets the Caching.compressionEnabled value.
        /// </summary>
        public bool CompressionEnabled { get { return m_CompressionEnabled; } set { m_CompressionEnabled = value; } }

        [FormerlySerializedAs("m_cacheDirectoryOverride")]
        [SerializeField]
        string m_CacheDirectoryOverride = "";
        /// <summary>
        /// If not null or empty a new cache is created using Caching.AddCache and it is set active by assigning it to Caching.currentCacheForWriting.
        /// </summary>
        public string CacheDirectoryOverride { get { return m_CacheDirectoryOverride; } set { m_CacheDirectoryOverride = value; } }

        [FormerlySerializedAs("m_expirationDelay")]
        [SerializeField]
        int m_ExpirationDelay = 12960000;  //this value taken from the docs and is 150 days
        /// <summary>
        /// Controls how long bundles are kept in the cache. This value is applied to Caching.currentCacheForWriting.expirationDelay.  The value is in seconds and has a limit of 12960000 (150 days).
        /// </summary>
        public int ExpirationDelay { get { return m_ExpirationDelay; } set { m_ExpirationDelay = value; } }

        [FormerlySerializedAs("m_limitCacheSize")]
        [SerializeField]
        bool m_LimitCacheSize;
        /// <summary>
        /// If true, the maximum cache size will be set to MaximumCacheSize. 
        /// </summary>
        public bool LimitCacheSize { get { return m_LimitCacheSize; } set { m_LimitCacheSize = value; } }

        [FormerlySerializedAs("m_maximumCacheSize")]
        [SerializeField]
        long m_MaximumCacheSize = long.MaxValue;
        /// <summary>
        /// The maximum size of the cache in bytes.  This value is applied to Caching.currentCacheForWriting.maximumAvailableStorageSpace.  This will only be set if LimitCacheSize is true.
        /// </summary>
        public long MaximumCacheSize { get { return m_MaximumCacheSize; } set { m_MaximumCacheSize = value; } }
    }
}