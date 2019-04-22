using System;
using UnityEditor.AddressableAssets.HostingServices;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings.GroupSchemas
{
    /// <summary>
    /// Schema used for bundled asset groups.
    /// </summary>
    [CreateAssetMenu(fileName = "BundledAssetGroupSchema.asset", menuName = "Addressable Assets/Group Schemas/Bundled Assets")]
    public class BundledAssetGroupSchema : AddressableAssetGroupSchema, IHostingServiceConfigurationProvider, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Defines how bundles are created.
        /// </summary>
        public enum BundlePackingMode
        {
            /// <summary>
            /// Pack all entries into as few bundles as possible (Scenes are put into separate bundles).
            /// </summary>
            PackTogether,
            /// <summary>
            /// Create a bundle per entry.  This is useful if each entry is a folder as all sub entries will go to the same bundle.
            /// </summary>
            PackSeparately,
            /// <summary>
            /// Creates a bundle per unique set of labels
            /// </summary>
            PackTogetherByLabel
        }

        /// <summary>
        /// Compression mode for bundles in this group.
        /// </summary>
        public enum BundleCompressionMode
        {
            Uncompressed,
            LZ4,
            LZMA
        }

        /// <summary>
        /// Build compression.
        /// </summary>
        public BundleCompressionMode Compression
        {
            get { return m_Compression; }
            set { m_Compression = value; }
        }

        [SerializeField]
        BundleCompressionMode m_Compression = BundleCompressionMode.LZ4;

        /// <summary>
        /// Gets the build compression settings for bundles in this group.
        /// </summary>
        /// <returns>The build compression.</returns>
        public virtual BuildCompression GetBuildCompressionForBundle(string bundleId)
        {
            //Unfortunately the BuildCompression struct is not serializable (nor is it settable), therefore this enum needs to be used to return the static members....
            switch (m_Compression)
            {
                case BundleCompressionMode.Uncompressed: return BuildCompression.Uncompressed;
                case BundleCompressionMode.LZ4: return BuildCompression.LZ4;
                case BundleCompressionMode.LZMA: return BuildCompression.LZMA;
            }
            return default(BuildCompression);
        }

        [FormerlySerializedAs("m_includeInBuild")]
        [SerializeField]
        [Tooltip("If true, the assets in this group will be included in the build of bundles.")]
        bool m_IncludeInBuild = true;
        /// <summary>
        /// If true, the assets in this group will be included in the build of bundles.
        /// </summary>
        public bool IncludeInBuild
        {
            get { return m_IncludeInBuild; }
            set
            {
                m_IncludeInBuild = value;
                SetDirty(true);
            }
        }
        [SerializeField]
        [SerializedTypeRestriction(type = typeof(IResourceProvider))]
        [Tooltip("The provider type to use for loading assets from bundles.")]
        SerializedType m_BundledAssetProviderType;
        /// <summary>
        /// The provider type to use for loading assets from bundles.
        /// </summary>
        public SerializedType BundledAssetProviderType { get { return m_BundledAssetProviderType; } }

        [SerializeField]
        [Tooltip("If true, the bundle and asset provider for assets in this group will get unique provider ids and will only provide for assets in this group.")]
        bool m_ForceUniqueProvider = false;
        /// <summary>
        /// If true, the bundle and asset provider for assets in this group will get unique provider ids and will only provide for assets in this group.
        /// </summary>
        public bool ForceUniqueProvider
        {
            get { return m_ForceUniqueProvider; }
            set
            {
                m_ForceUniqueProvider = value;
                SetDirty(true);
            }
        }

        [FormerlySerializedAs("m_useAssetBundleCache")]
        [SerializeField]
        [Tooltip("If true, the Hash value of the asset bundle is used to determine if a bundle can be loaded from the local cache instead of downloaded. (Only applies to remote asset bundles)")]
        bool m_UseAssetBundleCache = true;
        /// <summary>
        /// If true, the CRC and Hash values of the asset bundle are used to determine if a bundle can be loaded from the local cache instead of downloaded.
        /// </summary>
        public bool UseAssetBundleCache
        {
            get { return m_UseAssetBundleCache; }
            set
            {
                m_UseAssetBundleCache = value;
                SetDirty(true);
            }
        }

        [SerializeField]
        [Tooltip("If true, the CRC (Cyclic Redundancy Check) of the asset bundle is used to check the integrity.  This can be used for both local and remote bundles.")]
        bool m_UseAssetBundleCrc = true;
        /// <summary>
        /// If true, the CRC and Hash values of the asset bundle are used to determine if a bundle can be loaded from the local cache instead of downloaded.
        /// </summary>
        public bool UseAssetBundleCrc
        {
            get { return m_UseAssetBundleCrc; }
            set
            {
                m_UseAssetBundleCrc = value;
                SetDirty(true);
            }
        }

        [FormerlySerializedAs("m_timeout")]
        [SerializeField]
        [Tooltip("Sets UnityWebRequest to attempt to abort after the number of seconds in timeout have passed. (Only applies to remote asset bundles)")]
        int m_Timeout;
        /// <summary>
        /// Sets UnityWebRequest to attempt to abort after the number of seconds in timeout have passed.
        /// </summary>
        public int Timeout { get { return m_Timeout; } set { m_Timeout = value; } }
        [FormerlySerializedAs("m_chunkedTransfer")]
        [SerializeField]
        [Tooltip("Indicates whether the UnityWebRequest system should employ the HTTP/1.1 chunked-transfer encoding method. (Only applies to remote asset bundles)")]
        bool m_ChunkedTransfer;
        /// <summary>
        /// Indicates whether the UnityWebRequest system should employ the HTTP/1.1 chunked-transfer encoding method.
        /// </summary>
        public bool ChunkedTransfer { get { return m_ChunkedTransfer; } set { m_ChunkedTransfer = value; } }
        [FormerlySerializedAs("m_redirectLimit")]
        [SerializeField]
        [Tooltip("Indicates the number of redirects which this UnityWebRequest will follow before halting with a “Redirect Limit Exceeded” system error. (Only applies to remote asset bundles)")]
        int m_RedirectLimit = -1;
        /// <summary>
        /// Indicates the number of redirects which this UnityWebRequest will follow before halting with a “Redirect Limit Exceeded” system error.
        /// </summary>
        public int RedirectLimit { get { return m_RedirectLimit; } set { m_RedirectLimit = value; } }
        [FormerlySerializedAs("m_retryCount")]
        [SerializeField]
        [Tooltip("Indicates the number of times the request will be retried.")]
        int m_RetryCount;
        /// <summary>
        /// Indicates the number of times the request will be retried.  
        /// </summary>
        public int RetryCount { get { return m_RetryCount; } set { m_RetryCount = value; } }

        [FormerlySerializedAs("m_buildPath")]
        [SerializeField]
        [Tooltip("The path to copy asset bundles to.")]
        ProfileValueReference m_BuildPath = new ProfileValueReference();
        /// <summary>
        /// The path to copy asset bundles to.
        /// </summary>
        public ProfileValueReference BuildPath
        {
            get { return m_BuildPath; }
        }

        [FormerlySerializedAs("m_loadPath")]
        [SerializeField]
        [Tooltip("The path to load bundles from.")]
        ProfileValueReference m_LoadPath = new ProfileValueReference();
        /// <summary>
        /// The path to load bundles from.
        /// </summary>
        public ProfileValueReference LoadPath
        {
            get { return m_LoadPath; }
        }

        [FormerlySerializedAs("m_bundleMode")]
        [SerializeField]
        [Tooltip("Controls how bundles are packed.  If set to PackTogether, a single asset bundle will be created for the entire group, with the exception of scenes, which are packed separately.  If set to PackSeparately, an asset bundle will be created for each top level entry in the group.")]
        BundlePackingMode m_BundleMode = BundlePackingMode.PackTogether;
        /// <summary>
        /// Controls how bundles are packed.  If set to PackTogether, a single asset bundle will be created for the entire group, with the exception of scenes, which are packed separately.  If set to PackSeparately, an asset bundle will be created for each top level entry in the group.
        /// </summary>
        public BundlePackingMode BundleMode
        {
            get { return m_BundleMode; }
            set
            {
                m_BundleMode = value;
                SetDirty(true);
            }
        }

        /// <inheritdoc/>
        public string HostingServicesContentRoot
        {
            get
            {
                return BuildPath.GetValue(Group.Settings);
            }
        }

        [FormerlySerializedAs("m_assetBundleProviderType")]
        [SerializeField]
        [SerializedTypeRestriction(type = typeof(IResourceProvider))]
        [Tooltip("The provider type to use for loading asset bundles.")]
        SerializedType m_AssetBundleProviderType;
        /// <summary>
        /// The provider type to use for loading asset bundles.
        /// </summary>
        public SerializedType AssetBundleProviderType { get { return m_AssetBundleProviderType; } }

        /// <summary>
        /// Set default values taken from the assigned group.
        /// </summary>
        /// <param name="group">The group this schema has been added to.</param>
        protected override void OnSetGroup(AddressableAssetGroup group)
        {
            //this can happen during the load of the addressables asset
            if (group.Settings != null)
            {
                if (BuildPath == null || string.IsNullOrEmpty(BuildPath.GetValue(group.Settings)))
                {
                    m_BuildPath = new ProfileValueReference();
                    if (BuildPath != null)
                        BuildPath.SetVariableByName(group.Settings, AddressableAssetSettings.kLocalBuildPath);
                }

                if (LoadPath == null || string.IsNullOrEmpty(LoadPath.GetValue(group.Settings)))
                {
                    m_LoadPath = new ProfileValueReference();
                    if (LoadPath != null)
                        LoadPath.SetVariableByName(group.Settings, AddressableAssetSettings.kLocalLoadPath);
                }
            }

            if (m_AssetBundleProviderType.Value == null)
                m_AssetBundleProviderType.Value = typeof(AssetBundleProvider);
            if (m_BundledAssetProviderType.Value == null)
                m_BundledAssetProviderType.Value = typeof(BundledAssetProvider);
        }

        /// <summary>
        /// Impementation of ISerializationCallbackReceiver, does nothing.
        /// </summary>
        public void OnBeforeSerialize()
        {
            
        }

        /// <summary>
        /// Impementation of ISerializationCallbackReceiver, used to set callbacks for ProfileValueReference changes.
        /// </summary>
        public void OnAfterDeserialize()
        {
            BuildPath.OnValueChanged += s=> SetDirty(true);
            LoadPath.OnValueChanged += s => SetDirty(true);
            if (m_AssetBundleProviderType.Value == null)
                m_AssetBundleProviderType.Value = typeof(AssetBundleProvider);
            if (m_BundledAssetProviderType.Value == null)
                m_BundledAssetProviderType.Value = typeof(BundledAssetProvider);

        }

        /// <summary>
        /// Returns the id of the asset provider needed to load from this group.
        /// </summary>
        /// <returns>The id of the cached provider needed for this group.</returns>
        public string GetAssetCachedProviderId()
        {
            return ForceUniqueProvider ? string.Format("{0}_{1}", BundledAssetProviderType.Value.FullName, Group.Guid) : BundledAssetProviderType.Value.FullName;
        }

        /// <summary>
        /// Returns the id of the bundle provider needed to load from this group.
        /// </summary>
        /// <returns>The id of the cached provider needed for this group.</returns>
        public string GetBundleCachedProviderId()
        {
            return ForceUniqueProvider ? string.Format("{0}_{1}", AssetBundleProviderType.Value.FullName, Group.Guid) : AssetBundleProviderType.Value.FullName;
        }

    }
}