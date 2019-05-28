using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

namespace UnityEngine.AddressableAssets.Initialization
{
    /// <summary>
    /// Runtime data that is used to initialize the Addressables system.
    /// </summary>
    [Serializable]
    public class ResourceManagerRuntimeData
    {
        /// <summary>
        /// Address of the contained catalogs.
        /// </summary>
        public const string kCatalogAddress = "AddressablesMainContentCatalog";

        [SerializeField]
        string m_buildTarget;
        /// <summary>
        /// The name of the build target that this data was prepared for.
        /// </summary>
        public string BuildTarget { get { return m_buildTarget; } set { m_buildTarget = value; } }

        [FormerlySerializedAs("m_settingsHash")]
        [SerializeField]
        string m_SettingsHash;
        /// <summary>
        /// The hash of the settings that generated this runtime data.
        /// </summary>
        public string SettingsHash { get { return m_SettingsHash; } set { m_SettingsHash = value; } }
        [FormerlySerializedAs("m_catalogLocations")]
        [SerializeField]
        List<ResourceLocationData> m_CatalogLocations = new List<ResourceLocationData>();
        /// <summary>
        /// List of catalog locations to download in order (try remote first, then local)
        /// </summary>
        public List<ResourceLocationData> CatalogLocations { get { return m_CatalogLocations; } }
        [FormerlySerializedAs("m_profileEvents")]
        [SerializeField]
        bool m_ProfileEvents;
        /// <summary>
        /// Flag to control whether the ResourceManager sends profiler events.
        /// </summary>
        public bool ProfileEvents { get { return m_ProfileEvents; } set { m_ProfileEvents = value; } }

        [FormerlySerializedAs("m_logResourceManagerExceptions")]
        [SerializeField]
        bool m_LogResourceManagerExceptions = true;
        /// <summary>
        /// When enabled, the Addressables.ResourceManager.ExceptionHandler is set to (op, ex) => Debug.LogException(ex);
        /// </summary>
        public bool LogResourceManagerExceptions { get { return m_LogResourceManagerExceptions; } set { m_LogResourceManagerExceptions = value; } }

        [FormerlySerializedAs("m_extraInitializationData")]
        [SerializeField]
        List<ObjectInitializationData> m_ExtraInitializationData = new List<ObjectInitializationData>();
        /// <summary>
        /// The list of initialization data.  These objects will get deserialized and initialized during the Addressables initialization process.  This happens after resource providers have been set up but before any catalogs are loaded.
        /// </summary>
        public List<ObjectInitializationData> InitializationObjects { get { return m_ExtraInitializationData; } }

        [SerializeField]
        SerializedType m_CertificateHandlerType;

        /// <summary>
        /// The type of CertificateHandler to use for this provider.
        /// </summary>
        public Type CertificateHandlerType
        {
            get
            {
                return m_CertificateHandlerType.Value;
            }
            set
            {
                m_CertificateHandlerType.Value = value;
            }
        }

    }
}
