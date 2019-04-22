using System;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// Asset container for CacheInitializationData.
    /// </summary>
    [CreateAssetMenu(fileName = "CacheInitializationSettings.asset", menuName = "Addressable Assets/Initialization/Cache Initialization Settings")]
    public class CacheInitializationSettings : ScriptableObject, IObjectInitializationDataProvider
    {
        [FormerlySerializedAs("m_data")]
        [SerializeField]
        CacheInitializationData m_Data = new CacheInitializationData();
        /// <summary>
        /// Display name used in GUI for this object.
        /// </summary>
        public string Name { get { return "Asset Bundle Cache Settings"; } }
        /// <summary>
        /// The cache initialization data that will be serialized and applied during Addressables initialization.
        /// </summary>
        public CacheInitializationData Data
        {
            get
            {
                return m_Data;
            }
            set
            {
                m_Data = value;
            }
        }

        /// <summary>
        /// Create initialization data to be serialized into the Addressables runtime data.
        /// </summary>
        /// <returns>The serialized data for the initialization class and the data.</returns>
        public ObjectInitializationData CreateObjectInitializationData()
        {
            return ObjectInitializationData.CreateSerializedInitializationData<CacheInitialization>(typeof(CacheInitialization).Name, m_Data);
        }
    }
}